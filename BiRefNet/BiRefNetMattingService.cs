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
    string? AlphaPath,
    string? Model,
    string? Device,
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

    public string SummaryText => Succeeded
        ? $"BiRefNet: ok | infer {InferSeconds?.ToString("0.###") ?? "?"}s | {Device ?? "device ?"}"
        : $"BiRefNet: failed | {Error ?? Status}";
}

internal static class BiRefNetMattingService
{
    public static async Task<BiRefNetMattingRunResult> RunAsync(
        BiRefNetMattingRunRequest request,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(request.OutputDirectory);

        if (!File.Exists(request.HelperPath))
        {
            return CreateMissingFileResult(request, "Helper not found: " + request.HelperPath);
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

        return ReadResult(request, process.ExitCode, stdout, stderr);
    }

    private static BiRefNetMattingRunResult CreateMissingFileResult(BiRefNetMattingRunRequest request, string error)
    {
        return new BiRefNetMattingRunResult(
            -1,
            request.OutputDirectory,
            "error",
            null,
            request.Model,
            request.Device,
            null,
            null,
            error,
            string.Empty,
            string.Empty);
    }

    private static BiRefNetMattingRunResult ReadResult(
        BiRefNetMattingRunRequest request,
        int exitCode,
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
                null,
                request.Model,
                request.Device,
                null,
                null,
                "birefnet_result.json not found.",
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

        string? alphaPath = root.TryGetProperty("alpha_path", out JsonElement alphaPathElement)
            ? alphaPathElement.GetString()
            : null;

        string? model = root.TryGetProperty("model", out JsonElement modelElement)
            ? modelElement.GetString()
            : request.Model;

        string? device = root.TryGetProperty("device", out JsonElement deviceElement)
            ? deviceElement.GetString()
            : request.Device;

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
            alphaPath,
            model,
            device,
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
