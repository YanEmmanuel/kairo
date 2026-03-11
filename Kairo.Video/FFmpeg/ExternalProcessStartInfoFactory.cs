using System.Diagnostics;

namespace Kairo.Video.FFmpeg;

internal static class ExternalProcessStartInfoFactory
{
    public static ProcessStartInfo Create(
        string toolName,
        string arguments,
        bool redirectStandardOutput = false,
        bool redirectStandardError = false)
    {
        var fileName = ExternalToolLocator.ResolveRequired(toolName);
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = redirectStandardOutput,
            RedirectStandardError = redirectStandardError,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        PrependToolsDirectoryToPath(startInfo, fileName);
        return startInfo;
    }

    private static void PrependToolsDirectoryToPath(ProcessStartInfo startInfo, string fileName)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var bundledToolsDirectory = ExternalToolLocator.GetBundledToolsDirectory(
            baseDirectory,
            Environment.GetEnvironmentVariable("KAIRO_TOOLS_DIR"));
        var executableDirectory = Path.GetDirectoryName(fileName);
        var currentPath = Environment.GetEnvironmentVariable("PATH");
        var pathEntries = new List<string>(3);

        if (Directory.Exists(bundledToolsDirectory))
        {
            pathEntries.Add(bundledToolsDirectory);
        }

        if (!string.IsNullOrWhiteSpace(executableDirectory) && Directory.Exists(executableDirectory))
        {
            pathEntries.Add(executableDirectory);
        }

        if (!string.IsNullOrWhiteSpace(currentPath))
        {
            pathEntries.Add(currentPath);
        }

        if (pathEntries.Count > 0)
        {
            startInfo.Environment["PATH"] = string.Join(Path.PathSeparator, pathEntries.Distinct(StringComparer.OrdinalIgnoreCase));
        }
    }
}
