using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace KRetouchStudio;

internal sealed class DevelopmentPackageService
{
    private readonly string _repositoryRootPath;
    private readonly string _projectFilePath;
    private readonly string _repositoryUpdateManifestPath;
    private readonly string _releaseDirectoryPath;

    public DevelopmentPackageService(string repositoryRootPath)
    {
        _repositoryRootPath = repositoryRootPath;
        _projectFilePath = Path.Combine(_repositoryRootPath, "KRetouchStudio.csproj");
        _repositoryUpdateManifestPath = Path.Combine(_repositoryRootPath, "KRetouchStudio_update.json");
        _releaseDirectoryPath = Path.Combine(_repositoryRootPath, "release");
    }

    public string GetNextDevelopmentPackageVersion(Version currentAppVersion)
    {
        if (!File.Exists(_projectFilePath))
        {
            throw new FileNotFoundException("Project file was not found.", _projectFilePath);
        }

        if (!File.Exists(_repositoryUpdateManifestPath))
        {
            throw new FileNotFoundException("Update manifest was not found.", _repositoryUpdateManifestPath);
        }

        Version baselineVersion = MaxVersion(
            NormalizeVersion(ReadProjectVersion()),
            NormalizeVersion(ReadRepositoryManifestVersion()),
            NormalizeVersion(currentAppVersion));
        Version nextVersion = new(
            baselineVersion.Major,
            baselineVersion.Minor,
            Math.Max(0, baselineVersion.Build) + 1);
        return FormatVersion(nextVersion);
    }

    public void PrepareDevelopmentPackageVersion(string versionText)
    {
        WriteProjectVersion(versionText);
        WriteRepositoryManifestVersion(versionText);
    }

    public string WriteDevelopmentPackageHelperScript(int processId, string appPath)
    {
        Directory.CreateDirectory(_releaseDirectoryPath);

        string logPath = Path.Combine(_releaseDirectoryPath, "package_build.log");
        string scriptPath = Path.Combine(Path.GetTempPath(), $"KRetouchStudio_dev_package_{processId}.ps1");
        string newLine = Environment.NewLine;
        string script =
            "$ErrorActionPreference = 'Stop'" + newLine +
            $"$processId = {processId}" + newLine +
            $"$repoRoot = '{EscapePowerShellSingleQuoted(_repositoryRootPath)}'" + newLine +
            $"$logPath = '{EscapePowerShellSingleQuoted(logPath)}'" + newLine +
            $"$appPath = '{EscapePowerShellSingleQuoted(appPath)}'" + newLine +
            newLine +
            "New-Item -ItemType Directory -Path (Split-Path $logPath) -Force | Out-Null" + newLine +
            newLine +
            "while (Get-Process -Id $processId -ErrorAction SilentlyContinue) {" + newLine +
            "    Start-Sleep -Milliseconds 200" + newLine +
            "}" + newLine +
            newLine +
            "Push-Location $repoRoot" + newLine +
            "try {" + newLine +
            "    & dotnet build .\\KRetouchStudio.sln -p:Platform=x64 -p:CreateReleasePackage=true *> $logPath" + newLine +
            "    $exitCode = $LASTEXITCODE" + newLine +
            "}" + newLine +
            "finally {" + newLine +
            "    Pop-Location" + newLine +
            "}" + newLine +
            newLine +
            "if ($exitCode -eq 0) {" + newLine +
            "    Start-Process -FilePath $appPath" + newLine +
            "}" + newLine +
            "else {" + newLine +
            "    Start-Process notepad.exe $logPath" + newLine +
            "}" + newLine;

        File.WriteAllText(scriptPath, script, new UTF8Encoding(true));
        return scriptPath;
    }

    public static Version NormalizeVersion(Version version)
    {
        return new Version(version.Major, version.Minor, Math.Max(0, version.Build));
    }

    public static string FormatVersion(Version version)
    {
        return $"{version.Major}.{version.Minor}.{Math.Max(0, version.Build)}";
    }

    private Version ReadProjectVersion()
    {
        string projectText = File.ReadAllText(_projectFilePath, Encoding.UTF8);
        Match match = Regex.Match(projectText, @"<Version>\s*([^<\r\n]+)\s*</Version>");
        if (!match.Success || !Version.TryParse(match.Groups[1].Value.Trim(), out Version? version))
        {
            throw new InvalidOperationException("The project version could not be read.");
        }

        return version;
    }

    private Version ReadRepositoryManifestVersion()
    {
        string manifestText = File.ReadAllText(_repositoryUpdateManifestPath, Encoding.UTF8);
        DevelopmentPackageManifest? manifest = JsonSerializer.Deserialize<DevelopmentPackageManifest>(manifestText);
        if (manifest is null || !Version.TryParse(manifest.LatestVersion, out Version? version))
        {
            throw new InvalidOperationException("The update manifest version could not be read.");
        }

        return version;
    }

    private void WriteProjectVersion(string versionText)
    {
        string projectText = File.ReadAllText(_projectFilePath, Encoding.UTF8);
        Regex versionRegex = new(@"(<Version>\s*)([^<\r\n]+)(\s*</Version>)");
        string updatedProjectText = versionRegex.Replace(
            projectText,
            match => $"{match.Groups[1].Value}{versionText}{match.Groups[3].Value}",
            1);
        File.WriteAllText(_projectFilePath, updatedProjectText, new UTF8Encoding(true));
    }

    private void WriteRepositoryManifestVersion(string versionText)
    {
        string manifestText = File.ReadAllText(_repositoryUpdateManifestPath, Encoding.UTF8);
        DevelopmentPackageManifest manifest = JsonSerializer.Deserialize<DevelopmentPackageManifest>(manifestText) ?? new DevelopmentPackageManifest();
        manifest.LatestVersion = versionText;
        manifest.ReleaseNotes = $"Development package v{versionText} created on {DateTime.Now:yyyy-MM-dd HH:mm:ss}.";

        JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };

        string updatedManifestText = JsonSerializer.Serialize(manifest, options);
        File.WriteAllText(_repositoryUpdateManifestPath, updatedManifestText, new UTF8Encoding(true));
    }

    private static Version MaxVersion(Version first, Version second, Version third)
    {
        Version max = first;
        if (second > max)
        {
            max = second;
        }

        if (third > max)
        {
            max = third;
        }

        return max;
    }

    private static string EscapePowerShellSingleQuoted(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }

    private sealed class DevelopmentPackageManifest
    {
        [JsonPropertyName("latestVersion")]
        public string? LatestVersion { get; set; }

        [JsonPropertyName("downloadUrl")]
        public string? DownloadUrl { get; set; }

        [JsonPropertyName("releaseNotes")]
        public string? ReleaseNotes { get; set; }
    }
}
