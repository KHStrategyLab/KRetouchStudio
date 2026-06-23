using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace KRetouchStudio;

internal sealed record BiRefNetMattingRunRequest(
    string PythonRuntime,
    string HelperPath,
    string ImagePath,
    string OutputDirectory,
    string Model,
    int InferenceSize,
    string Device);

internal sealed record BiRefNetMattingRunResult(
    int ExitCode,
    string OutputDirectory,
    string Status,
    string RunMode,
    string? AlphaPath,
    string? Model,
    string? Device,
    double? LoadSeconds,
    double? InferSeconds,
    double? TotalSeconds,
    string? Error,
    string StandardOutput,
    string StandardError)
{
    public bool Succeeded =>
        ExitCode == 0 &&
        string.Equals(Status, "ok", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(AlphaPath) &&
        File.Exists(AlphaPath);

    public string SummaryText
    {
        get
        {
            if (!Succeeded)
            {
                return $"BiRefNet: failed | {Error ?? Status}";
            }

            string loadText = LoadSeconds is > 0.05
                ? $" | load {LoadSeconds.Value:0.###}s"
                : string.Empty;
            return $"BiRefNet: ok | {RunMode}{loadText} | infer {InferSeconds?.ToString("0.###") ?? "?"}s | {Device ?? "device ?"}";
        }
    }
}

internal static class BiRefNetMattingService
{
    private const string WorkerRunMode = "worker";
    private const string OneShotRunMode = "oneshot";

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly SemaphoreSlim WorkerGate = new(1, 1);
    private static readonly string WorkerLogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KRetouchStudio",
        "BiRefNetWorker");

    private static Process? _workerProcess;
    private static StreamWriter? _workerInput;
    private static StreamReader? _workerOutput;
    private static string? _workerPythonRuntime;
    private static string? _workerPath;
    private static string? _workerStderrPath;
    private static int _workerRequestId;

    public static async Task<BiRefNetMattingRunResult> RunAsync(
        BiRefNetMattingRunRequest request,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(request.OutputDirectory);

        if (!File.Exists(request.HelperPath))
        {
            return CreateFailureResult(request, OneShotRunMode, "Helper not found: " + request.HelperPath);
        }

        if (!File.Exists(request.ImagePath))
        {
            return CreateFailureResult(request, OneShotRunMode, "Image not found: " + request.ImagePath);
        }

        BiRefNetMattingRunResult workerResult = await TryRunPersistentWorkerAsync(request, cancellationToken);
        if (workerResult.Succeeded)
        {
            return workerResult;
        }

        BiRefNetMattingRunResult oneShotResult = await RunOneShotAsync(request, cancellationToken);
        return oneShotResult.Succeeded ? oneShotResult : workerResult;
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

    private static async Task<BiRefNetMattingRunResult> TryRunPersistentWorkerAsync(
        BiRefNetMattingRunRequest request,
        CancellationToken cancellationToken)
    {
        string workerPath = Path.Combine(Path.GetDirectoryName(request.HelperPath) ?? string.Empty, "birefnet_worker.py");
        if (!File.Exists(workerPath))
        {
            return CreateFailureResult(request, WorkerRunMode, "Worker not found: " + workerPath);
        }

        await WorkerGate.WaitAsync(cancellationToken);
        try
        {
            EnsureWorkerStartedNoLock(request, workerPath);
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
                ["output"] = request.OutputDirectory,
                ["model"] = request.Model,
                ["size"] = request.InferenceSize,
                ["device"] = request.Device,
            };

            string commandJson = JsonSerializer.Serialize(command);
            await _workerInput.WriteLineAsync(commandJson);
            await _workerInput.FlushAsync();

            string stdoutPath = Path.Combine(request.OutputDirectory, "birefnet_stdout.txt");
            string stderrPath = Path.Combine(request.OutputDirectory, "birefnet_stderr.txt");

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
                return ReadResultFromRoot(request, 0, WorkerRunMode, root, line, stderrPointer);
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

    private static void EnsureWorkerStartedNoLock(BiRefNetMattingRunRequest request, string workerPath)
    {
        string pythonRuntime = string.IsNullOrWhiteSpace(request.PythonRuntime) ? "python" : request.PythonRuntime;
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
        _workerStderrPath = Path.Combine(WorkerLogDirectory, "birefnet_worker_stderr.log");

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
            // Logging must never break the matting path.
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

    private static async Task<BiRefNetMattingRunResult> RunOneShotAsync(
        BiRefNetMattingRunRequest request,
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
        startInfo.ArgumentList.Add("--output");
        startInfo.ArgumentList.Add(request.OutputDirectory);
        startInfo.ArgumentList.Add("--model");
        startInfo.ArgumentList.Add(request.Model);
        startInfo.ArgumentList.Add("--size");
        startInfo.ArgumentList.Add(request.InferenceSize.ToString());
        startInfo.ArgumentList.Add("--device");
        startInfo.ArgumentList.Add(request.Device);

        using Process process = new() { StartInfo = startInfo };
        process.Start();

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        string stdout = await stdoutTask;
        string stderr = await stderrTask;
        await File.WriteAllTextAsync(Path.Combine(request.OutputDirectory, "birefnet_stdout.txt"), stdout, Encoding.UTF8, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(request.OutputDirectory, "birefnet_stderr.txt"), stderr, Encoding.UTF8, cancellationToken);

        return ReadResult(request, process.ExitCode, OneShotRunMode, stdout, stderr);
    }

    private static BiRefNetMattingRunResult CreateFailureResult(
        BiRefNetMattingRunRequest request,
        string runMode,
        string error)
    {
        return new BiRefNetMattingRunResult(
            -1,
            request.OutputDirectory,
            "error",
            runMode,
            null,
            request.Model,
            request.Device,
            null,
            null,
            null,
            error,
            string.Empty,
            string.Empty);
    }

    private static BiRefNetMattingRunResult ReadResult(
        BiRefNetMattingRunRequest request,
        int exitCode,
        string runMode,
        string stdout,
        string stderr)
    {
        string resultPath = Path.Combine(request.OutputDirectory, "birefnet_result.json");
        if (!File.Exists(resultPath))
        {
            return new BiRefNetMattingRunResult(
                exitCode,
                request.OutputDirectory,
                "error",
                runMode,
                null,
                request.Model,
                request.Device,
                null,
                null,
                null,
                "birefnet_result.json not found.",
                stdout,
                stderr);
        }

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(resultPath, Encoding.UTF8));
        return ReadResultFromRoot(request, exitCode, runMode, document.RootElement, stdout, stderr);
    }

    private static BiRefNetMattingRunResult ReadResultFromRoot(
        BiRefNetMattingRunRequest request,
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

        string? alphaPath = root.TryGetProperty("alpha_path", out JsonElement alphaPathElement)
            ? alphaPathElement.GetString()
            : null;

        string? model = root.TryGetProperty("model", out JsonElement modelElement)
            ? modelElement.GetString()
            : request.Model;

        string? device = root.TryGetProperty("device", out JsonElement deviceElement)
            ? deviceElement.GetString()
            : request.Device;

        double? loadSeconds = TryGetDouble(root, "load_seconds");
        double? inferSeconds = TryGetDouble(root, "infer_seconds");
        double? totalSeconds = TryGetDouble(root, "total_seconds");

        if (exitCode != 0 && string.IsNullOrWhiteSpace(error))
        {
            error = stderr.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        }

        return new BiRefNetMattingRunResult(
            exitCode,
            request.OutputDirectory,
            status,
            runMode,
            alphaPath,
            model,
            device,
            loadSeconds,
            inferSeconds,
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
