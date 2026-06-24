using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace KRetouchStudio;

internal sealed record MediaPipeConnectionRunRequest(
    string PythonRuntime,
    string HelperPath,
    string ModelsDirectory,
    string ImagePath,
    string OutputDirectory);

internal sealed record MediaPipeConnectionRunResult(
    int ExitCode,
    string OutputDirectory,
    string Status,
    string RunMode,
    int FaceCount,
    int LandmarkCount,
    string? Error,
    string StandardOutput,
    string StandardError)
{
    public bool Succeeded => ExitCode == 0 && string.Equals(Status, "ok", StringComparison.OrdinalIgnoreCase);

    public string SummaryText => Succeeded
        ? $"MediaPipe: ok | {RunMode} | face {FaceCount} | lm {LandmarkCount}"
        : $"MediaPipe: failed | {Error ?? Status}";
}

internal sealed record MediaPipeConnectionWarmUpRequest(
    string PythonRuntime,
    string HelperPath,
    string ModelsDirectory,
    string OutputDirectory);

internal sealed record MediaPipeConnectionWarmUpResult(
    int ExitCode,
    string OutputDirectory,
    string Status,
    string RunMode,
    double? LoadSeconds,
    double? TotalSeconds,
    string? Error,
    string StandardOutput,
    string StandardError)
{
    public bool Succeeded => ExitCode == 0 && string.Equals(Status, "ok", StringComparison.OrdinalIgnoreCase);

    public string SummaryText => Succeeded
        ? $"MediaPipe: warm | {RunMode} | load {LoadSeconds?.ToString("0.###") ?? "?"}s"
        : $"MediaPipe: warmup failed | {Error ?? Status}";
}

internal static class MediaPipeConnectionService
{
    private const string WorkerRunMode = "worker";
    private const string OneShotRunMode = "oneshot";

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly SemaphoreSlim WorkerGate = new(1, 1);
    private static readonly string WorkerLogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KRetouchStudio",
        "MediaPipeWorker");

    private static Process? _workerProcess;
    private static StreamWriter? _workerInput;
    private static StreamReader? _workerOutput;
    private static string? _workerPythonRuntime;
    private static string? _workerPath;
    private static string? _workerStderrPath;
    private static int _workerRequestId;

    public static async Task<MediaPipeConnectionRunResult> RunAsync(
        MediaPipeConnectionRunRequest request,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(request.OutputDirectory);

        MediaPipeConnectionRunResult? missingInputResult = ValidateRunRequest(request);
        if (missingInputResult is not null)
        {
            return missingInputResult;
        }

        MediaPipeConnectionRunResult workerResult = await TryRunPersistentWorkerAsync(request, cancellationToken);
        if (workerResult.Succeeded)
        {
            return workerResult;
        }

        MediaPipeConnectionRunResult oneShotResult = await RunOneShotAsync(request, cancellationToken);
        return oneShotResult.Succeeded ? oneShotResult : workerResult;
    }

    public static async Task<MediaPipeConnectionWarmUpResult> WarmUpAsync(
        MediaPipeConnectionWarmUpRequest request,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(request.OutputDirectory);

        if (!File.Exists(request.HelperPath))
        {
            return CreateWarmUpFailureResult(request, "Helper not found: " + request.HelperPath);
        }

        if (!Directory.Exists(request.ModelsDirectory))
        {
            return CreateWarmUpFailureResult(request, "Models directory not found: " + request.ModelsDirectory);
        }

        string workerPath = Path.Combine(Path.GetDirectoryName(request.HelperPath) ?? string.Empty, "mediapipe_worker.py");
        if (!File.Exists(workerPath))
        {
            return CreateWarmUpFailureResult(request, "Worker not found: " + workerPath);
        }

        await WorkerGate.WaitAsync(cancellationToken);
        try
        {
            EnsureWorkerStartedNoLock(request.PythonRuntime, workerPath);
            if (_workerInput is null || _workerOutput is null)
            {
                return CreateWarmUpFailureResult(request, "Worker stream not ready.");
            }

            int requestId = Interlocked.Increment(ref _workerRequestId);
            Dictionary<string, object?> command = new(StringComparer.Ordinal)
            {
                ["request_id"] = requestId,
                ["command"] = "warmup",
                ["models"] = request.ModelsDirectory,
                ["output"] = request.OutputDirectory,
            };

            await _workerInput.WriteLineAsync(JsonSerializer.Serialize(command));
            await _workerInput.FlushAsync();

            string stdoutPath = Path.Combine(request.OutputDirectory, "mediapipe_warmup_stdout.txt");
            string stderrPath = Path.Combine(request.OutputDirectory, "mediapipe_warmup_stderr.txt");

            while (true)
            {
                string? line = await _workerOutput.ReadLineAsync(cancellationToken);
                if (line is null)
                {
                    ShutdownWorkerProcessNoLock(sendShutdownCommand: false);
                    return CreateWarmUpFailureResult(request, "Worker stopped before returning a warmup result.");
                }

                await File.AppendAllTextAsync(stdoutPath, line + Environment.NewLine, Encoding.UTF8, cancellationToken);

                JsonDocument document;
                try
                {
                    document = JsonDocument.Parse(line);
                }
                catch (JsonException)
                {
                    continue;
                }

                using (document)
                {
                    JsonElement root = document.RootElement;
                    int responseRequestId = root.TryGetProperty("request_id", out JsonElement requestIdElement) &&
                                            requestIdElement.TryGetInt32(out int parsedRequestId)
                        ? parsedRequestId
                        : -1;
                    if (responseRequestId != requestId)
                    {
                        continue;
                    }

                    string stderrPointer = string.IsNullOrWhiteSpace(_workerStderrPath)
                        ? "Persistent worker stderr log is not available."
                        : "Persistent worker stderr log: " + _workerStderrPath;
                    await File.WriteAllTextAsync(stderrPath, stderrPointer, Encoding.UTF8, cancellationToken);
                    return ReadWarmUpResultFromRoot(request, 0, root, line, stderrPointer);
                }
            }
        }
        catch (Exception ex)
        {
            ShutdownWorkerProcessNoLock(sendShutdownCommand: false);
            return CreateWarmUpFailureResult(request, ex.Message);
        }
        finally
        {
            WorkerGate.Release();
        }
    }

    public static void ShutdownWorker()
    {
        if (!WorkerGate.Wait(TimeSpan.FromSeconds(1)))
        {
            KillWorkerProcessNoLock();
            return;
        }

        try
        {
            ShutdownWorkerProcessNoLock(sendShutdownCommand: true);
        }
        finally
        {
            WorkerGate.Release();
        }
    }

    private static MediaPipeConnectionRunResult? ValidateRunRequest(MediaPipeConnectionRunRequest request)
    {
        if (!File.Exists(request.HelperPath))
        {
            return CreateFailureResult(request, OneShotRunMode, "Helper not found: " + request.HelperPath);
        }

        if (!Directory.Exists(request.ModelsDirectory))
        {
            return CreateFailureResult(request, OneShotRunMode, "Models directory not found: " + request.ModelsDirectory);
        }

        if (!File.Exists(request.ImagePath))
        {
            return CreateFailureResult(request, OneShotRunMode, "Image not found: " + request.ImagePath);
        }

        return null;
    }

    private static async Task<MediaPipeConnectionRunResult> TryRunPersistentWorkerAsync(
        MediaPipeConnectionRunRequest request,
        CancellationToken cancellationToken)
    {
        string workerPath = Path.Combine(Path.GetDirectoryName(request.HelperPath) ?? string.Empty, "mediapipe_worker.py");
        if (!File.Exists(workerPath))
        {
            return CreateFailureResult(request, WorkerRunMode, "Worker not found: " + workerPath);
        }

        await WorkerGate.WaitAsync(cancellationToken);
        try
        {
            EnsureWorkerStartedNoLock(request.PythonRuntime, workerPath);
            if (_workerInput is null || _workerOutput is null)
            {
                return CreateFailureResult(request, WorkerRunMode, "Worker stream not ready.");
            }

            int requestId = Interlocked.Increment(ref _workerRequestId);
            Dictionary<string, object?> command = new(StringComparer.Ordinal)
            {
                ["request_id"] = requestId,
                ["command"] = "run",
                ["image"] = request.ImagePath,
                ["models"] = request.ModelsDirectory,
                ["output"] = request.OutputDirectory,
            };

            await _workerInput.WriteLineAsync(JsonSerializer.Serialize(command));
            await _workerInput.FlushAsync();

            string stdoutPath = Path.Combine(request.OutputDirectory, "helper_stdout.txt");
            string stderrPath = Path.Combine(request.OutputDirectory, "helper_stderr.txt");

            while (true)
            {
                string? line = await _workerOutput.ReadLineAsync(cancellationToken);
                if (line is null)
                {
                    ShutdownWorkerProcessNoLock(sendShutdownCommand: false);
                    return CreateFailureResult(request, WorkerRunMode, "Worker stopped before returning a result.");
                }

                await File.AppendAllTextAsync(stdoutPath, line + Environment.NewLine, Encoding.UTF8, cancellationToken);

                JsonDocument document;
                try
                {
                    document = JsonDocument.Parse(line);
                }
                catch (JsonException)
                {
                    continue;
                }

                using (document)
                {
                    JsonElement root = document.RootElement;
                    int responseRequestId = root.TryGetProperty("request_id", out JsonElement requestIdElement) &&
                                            requestIdElement.TryGetInt32(out int parsedRequestId)
                        ? parsedRequestId
                        : -1;
                    if (responseRequestId != requestId)
                    {
                        continue;
                    }

                    string stderrPointer = string.IsNullOrWhiteSpace(_workerStderrPath)
                        ? "Persistent worker stderr log is not available."
                        : "Persistent worker stderr log: " + _workerStderrPath;
                    await File.WriteAllTextAsync(stderrPath, stderrPointer, Encoding.UTF8, cancellationToken);
                    return ReadResult(request, 0, WorkerRunMode, line, stderrPointer);
                }
            }
        }
        catch (Exception ex)
        {
            ShutdownWorkerProcessNoLock(sendShutdownCommand: false);
            return CreateFailureResult(request, WorkerRunMode, ex.Message);
        }
        finally
        {
            WorkerGate.Release();
        }
    }

    private static void EnsureWorkerStartedNoLock(string requestedPythonRuntime, string workerPath)
    {
        string pythonRuntime = string.IsNullOrWhiteSpace(requestedPythonRuntime) ? "python" : requestedPythonRuntime;
        if (_workerProcess is not null &&
            !_workerProcess.HasExited &&
            _workerInput is not null &&
            _workerOutput is not null &&
            string.Equals(_workerPythonRuntime, pythonRuntime, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(_workerPath, workerPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ShutdownWorkerProcessNoLock(sendShutdownCommand: false);
        Directory.CreateDirectory(WorkerLogDirectory);
        _workerStderrPath = Path.Combine(WorkerLogDirectory, "mediapipe_worker_stderr.log");

        ProcessStartInfo startInfo = new()
        {
            FileName = pythonRuntime,
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = Utf8NoBom,
            StandardOutputEncoding = Utf8NoBom,
            StandardErrorEncoding = Utf8NoBom,
            CreateNoWindow = true,
        };
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";
        startInfo.ArgumentList.Add(workerPath);

        Process process = new() { StartInfo = startInfo };
        process.Start();

        _workerProcess = process;
        _workerInput = process.StandardInput;
        _workerOutput = process.StandardOutput;
        _workerPythonRuntime = pythonRuntime;
        _workerPath = workerPath;
        _ = Task.Run(() => PumpWorkerStandardErrorAsync(process, _workerStderrPath));
    }

    private static async Task PumpWorkerStandardErrorAsync(Process process, string? logPath)
    {
        if (string.IsNullOrWhiteSpace(logPath))
        {
            return;
        }

        try
        {
            await using StreamWriter writer = new(logPath, append: true, Encoding.UTF8);
            await writer.WriteLineAsync($"[{DateTimeOffset.Now:O}] worker started pid={process.Id}");
            while (!process.HasExited)
            {
                string? line = await process.StandardError.ReadLineAsync();
                if (line is null)
                {
                    break;
                }

                await writer.WriteLineAsync($"[{DateTimeOffset.Now:O}] {line}");
                await writer.FlushAsync();
            }

            await writer.WriteLineAsync($"[{DateTimeOffset.Now:O}] worker stopped pid={process.Id}");
        }
        catch
        {
            // Worker logging must never break the MediaPipe path.
        }
    }

    private static void ShutdownWorkerProcessNoLock(bool sendShutdownCommand)
    {
        Process? process = _workerProcess;
        StreamWriter? input = _workerInput;

        _workerProcess = null;
        _workerInput = null;
        _workerOutput = null;
        _workerPythonRuntime = null;
        _workerPath = null;

        if (process is null)
        {
            return;
        }

        try
        {
            if (sendShutdownCommand && !process.HasExited && input is not null)
            {
                int requestId = Interlocked.Increment(ref _workerRequestId);
                string shutdownJson = JsonSerializer.Serialize(new Dictionary<string, object?>
                {
                    ["request_id"] = requestId,
                    ["command"] = "shutdown",
                });
                input.WriteLine(shutdownJson);
                input.Flush();
            }
        }
        catch
        {
            // Fall through to process shutdown.
        }

        try
        {
            input?.Dispose();
        }
        catch
        {
            // Ignore disposal errors during shutdown.
        }

        try
        {
            if (!process.HasExited && !process.WaitForExit(1500))
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Ignore shutdown errors.
        }
        finally
        {
            process.Dispose();
        }
    }

    private static void KillWorkerProcessNoLock()
    {
        Process? process = _workerProcess;
        _workerProcess = null;
        _workerInput = null;
        _workerOutput = null;
        _workerPythonRuntime = null;
        _workerPath = null;

        try
        {
            if (process is not null && !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Ignore forced shutdown errors.
        }
        finally
        {
            process?.Dispose();
        }
    }

    private static async Task<MediaPipeConnectionRunResult> RunOneShotAsync(
        MediaPipeConnectionRunRequest request,
        CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = string.IsNullOrWhiteSpace(request.PythonRuntime) ? "python" : request.PythonRuntime,
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Utf8NoBom,
            StandardErrorEncoding = Utf8NoBom,
            CreateNoWindow = true,
        };
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";
        startInfo.ArgumentList.Add(request.HelperPath);
        startInfo.ArgumentList.Add("--image");
        startInfo.ArgumentList.Add(request.ImagePath);
        startInfo.ArgumentList.Add("--models");
        startInfo.ArgumentList.Add(request.ModelsDirectory);
        startInfo.ArgumentList.Add("--output");
        startInfo.ArgumentList.Add(request.OutputDirectory);

        using Process process = new() { StartInfo = startInfo };
        process.Start();

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        string stdout = await stdoutTask;
        string stderr = await stderrTask;
        await File.WriteAllTextAsync(Path.Combine(request.OutputDirectory, "helper_stdout.txt"), stdout, Encoding.UTF8, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(request.OutputDirectory, "helper_stderr.txt"), stderr, Encoding.UTF8, cancellationToken);

        return ReadResult(request, process.ExitCode, OneShotRunMode, stdout, stderr);
    }

    private static MediaPipeConnectionRunResult CreateFailureResult(
        MediaPipeConnectionRunRequest request,
        string runMode,
        string error)
    {
        return new MediaPipeConnectionRunResult(
            -1,
            request.OutputDirectory,
            "error",
            runMode,
            0,
            0,
            error,
            string.Empty,
            string.Empty);
    }

    private static MediaPipeConnectionWarmUpResult CreateWarmUpFailureResult(
        MediaPipeConnectionWarmUpRequest request,
        string error)
    {
        return new MediaPipeConnectionWarmUpResult(
            -1,
            request.OutputDirectory,
            "error",
            WorkerRunMode,
            null,
            null,
            error,
            string.Empty,
            string.Empty);
    }

    private static MediaPipeConnectionRunResult ReadResult(
        MediaPipeConnectionRunRequest request,
        int exitCode,
        string runMode,
        string stdout,
        string stderr)
    {
        string resultPath = Path.Combine(request.OutputDirectory, "mediapipe_result.json");
        if (!File.Exists(resultPath))
        {
            return new MediaPipeConnectionRunResult(
                exitCode,
                request.OutputDirectory,
                "error",
                runMode,
                0,
                0,
                "mediapipe_result.json not found.",
                stdout,
                stderr);
        }

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(resultPath, Encoding.UTF8));
        return ReadResultFromRoot(request, exitCode, runMode, document.RootElement, stdout, stderr);
    }

    private static MediaPipeConnectionRunResult ReadResultFromRoot(
        MediaPipeConnectionRunRequest request,
        int exitCode,
        string runMode,
        JsonElement root,
        string stdout,
        string stderr)
    {
        string status = root.TryGetProperty("status", out JsonElement statusElement)
            ? statusElement.GetString() ?? "unknown"
            : "unknown";

        string? error = root.TryGetProperty("error", out JsonElement errorElement)
            ? errorElement.GetString()
            : null;

        int faceCount = 0;
        int landmarkCount = 0;
        if (root.TryGetProperty("steps", out JsonElement stepsElement) && stepsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement step in stepsElement.EnumerateArray())
            {
                string? name = step.TryGetProperty("name", out JsonElement nameElement)
                    ? nameElement.GetString()
                    : null;

                if (!step.TryGetProperty("result", out JsonElement resultElement))
                {
                    continue;
                }

                if (string.Equals(name, "face_detector", StringComparison.OrdinalIgnoreCase) &&
                    resultElement.TryGetProperty("face_count", out JsonElement faceCountElement))
                {
                    faceCount = faceCountElement.GetInt32();
                }

                if (string.Equals(name, "face_landmarker", StringComparison.OrdinalIgnoreCase) &&
                    resultElement.TryGetProperty("faces", out JsonElement facesElement) &&
                    facesElement.ValueKind == JsonValueKind.Array &&
                    facesElement.GetArrayLength() > 0 &&
                    facesElement[0].TryGetProperty("landmark_count", out JsonElement landmarkCountElement))
                {
                    landmarkCount = landmarkCountElement.GetInt32();
                }
            }
        }

        if (exitCode != 0 && string.IsNullOrWhiteSpace(error))
        {
            error = stderr.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        }

        return new MediaPipeConnectionRunResult(
            exitCode,
            request.OutputDirectory,
            status,
            runMode,
            faceCount,
            landmarkCount,
            error,
            stdout,
            stderr);
    }

    private static MediaPipeConnectionWarmUpResult ReadWarmUpResultFromRoot(
        MediaPipeConnectionWarmUpRequest request,
        int exitCode,
        JsonElement root,
        string stdout,
        string stderr)
    {
        string status = root.TryGetProperty("status", out JsonElement statusElement)
            ? statusElement.GetString() ?? "unknown"
            : "unknown";

        string? error = root.TryGetProperty("error", out JsonElement errorElement)
            ? errorElement.GetString()
            : null;

        double? loadSeconds = TryGetDouble(root, "load_seconds");
        double? totalSeconds = TryGetDouble(root, "total_seconds");

        if (exitCode != 0 && string.IsNullOrWhiteSpace(error))
        {
            error = stderr.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        }

        return new MediaPipeConnectionWarmUpResult(
            exitCode,
            request.OutputDirectory,
            status,
            WorkerRunMode,
            loadSeconds,
            totalSeconds,
            error,
            stdout,
            stderr);
    }

    private static double? TryGetDouble(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out JsonElement element) &&
               element.TryGetDouble(out double value)
            ? value
            : null;
    }
}
