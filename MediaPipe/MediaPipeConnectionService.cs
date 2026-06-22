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
    int FaceCount,
    int LandmarkCount,
    string? Error,
    string StandardOutput,
    string StandardError)
{
    public bool Succeeded => ExitCode == 0 && string.Equals(Status, "ok", StringComparison.OrdinalIgnoreCase);

    public string SummaryText => Succeeded
        ? $"MediaPipe: ok | face {FaceCount} | lm {LandmarkCount}"
        : $"MediaPipe: failed | {Error ?? Status}";
}

internal static class MediaPipeConnectionService
{
    public static async Task<MediaPipeConnectionRunResult> RunAsync(
        MediaPipeConnectionRunRequest request,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(request.OutputDirectory);

        if (!File.Exists(request.HelperPath))
        {
            return CreateMissingFileResult(request, "Helper not found: " + request.HelperPath);
        }

        if (!Directory.Exists(request.ModelsDirectory))
        {
            return CreateMissingFileResult(request, "Models directory not found: " + request.ModelsDirectory);
        }

        if (!File.Exists(request.ImagePath))
        {
            return CreateMissingFileResult(request, "Image not found: " + request.ImagePath);
        }

        ProcessStartInfo startInfo = new()
        {
            FileName = string.IsNullOrWhiteSpace(request.PythonRuntime) ? "python" : request.PythonRuntime,
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
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

        return ReadResult(request, process.ExitCode, stdout, stderr);
    }

    private static MediaPipeConnectionRunResult CreateMissingFileResult(MediaPipeConnectionRunRequest request, string error)
    {
        return new MediaPipeConnectionRunResult(
            -1,
            request.OutputDirectory,
            "error",
            0,
            0,
            error,
            string.Empty,
            string.Empty);
    }

    private static MediaPipeConnectionRunResult ReadResult(
        MediaPipeConnectionRunRequest request,
        int exitCode,
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
                0,
                0,
                "mediapipe_result.json not found.",
                stdout,
                stderr);
        }

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(resultPath, Encoding.UTF8));
        JsonElement root = document.RootElement;
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
            faceCount,
            landmarkCount,
            error,
            stdout,
            stderr);
    }
}
