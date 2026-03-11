using System.Diagnostics.CodeAnalysis;

namespace Kairo.Video.FFmpeg;

internal static class ExternalToolLocator
{
    public static string ResolveRequired(string toolName)
    {
        if (TryResolve(toolName, out var resolvedPath))
        {
            return resolvedPath;
        }

        throw new InvalidOperationException(
            $"Kairo could not locate '{GetPlatformFileName(toolName)}'. Use the portable release bundle or install '{toolName}' in PATH.");
    }

    public static string? ResolveOptional(string toolName) =>
        TryResolve(toolName, out var resolvedPath)
            ? resolvedPath
            : null;

    public static bool TryResolve(string toolName, [NotNullWhen(true)] out string? resolvedPath)
    {
        foreach (var candidate in EnumerateCandidatePaths(
                     toolName,
                     AppContext.BaseDirectory,
                     Environment.GetEnvironmentVariable(GetToolPathOverrideVariable(toolName)),
                     Environment.GetEnvironmentVariable("KAIRO_TOOLS_DIR"),
                     Environment.GetEnvironmentVariable("PATH")))
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            EnsureExecutableIfNeeded(candidate);
            resolvedPath = candidate;
            return true;
        }

        resolvedPath = null;
        return false;
    }

    internal static IEnumerable<string> EnumerateCandidatePaths(
        string toolName,
        string baseDirectory,
        string? explicitOverridePath,
        string? toolsDirectoryOverride,
        string? pathEnvironment)
    {
        var fileName = GetPlatformFileName(toolName);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var direct in EnumerateDirectCandidates(baseDirectory, explicitOverridePath, toolsDirectoryOverride, fileName))
        {
            if (seen.Add(direct))
            {
                yield return direct;
            }
        }

        if (string.IsNullOrWhiteSpace(pathEnvironment))
        {
            yield break;
        }

        foreach (var searchPath in pathEnvironment.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = Path.Combine(searchPath, fileName);
            if (seen.Add(candidate))
            {
                yield return candidate;
            }
        }
    }

    internal static string GetBundledToolsDirectory(string baseDirectory, string? toolsDirectoryOverride = null) =>
        string.IsNullOrWhiteSpace(toolsDirectoryOverride)
            ? Path.Combine(baseDirectory, "tools")
            : Path.GetFullPath(toolsDirectoryOverride);

    private static IEnumerable<string> EnumerateDirectCandidates(
        string baseDirectory,
        string? explicitOverridePath,
        string? toolsDirectoryOverride,
        string fileName)
    {
        if (!string.IsNullOrWhiteSpace(explicitOverridePath))
        {
            yield return Path.GetFullPath(explicitOverridePath);
        }

        yield return Path.Combine(GetBundledToolsDirectory(baseDirectory, toolsDirectoryOverride), fileName);
        yield return Path.Combine(baseDirectory, fileName);
    }

    private static string GetPlatformFileName(string toolName) =>
        OperatingSystem.IsWindows()
            ? $"{toolName}.exe"
            : toolName;

    private static string GetToolPathOverrideVariable(string toolName) =>
        $"KAIRO_{toolName.Replace("-", "_", StringComparison.Ordinal).ToUpperInvariant()}_PATH";

    private static void EnsureExecutableIfNeeded(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            var fileMode = File.GetUnixFileMode(path);
            var executeBits = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
            if ((fileMode & executeBits) != executeBits)
            {
                File.SetUnixFileMode(path, fileMode | executeBits);
            }
        }
        catch
        {
        }
    }
}
