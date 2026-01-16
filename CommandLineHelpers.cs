
using System.Diagnostics;

namespace Myshkin;

public static class CommandLineHelpers
{
    /// <summary>
    /// Generates a file tree structure of the given directory, excluding common build/cache directories.
    /// </summary>
    public static IEnumerable<string> GenerateFileTree(string rootPath, int maxDepth = 10)
    {
        var excludeDirs = new HashSet<string> { "bin", "obj", ".git", ".vs", "node_modules", ".idea", "dist", "build" };

        if (!Directory.Exists(rootPath))
        {
            yield return $"Directory not found: {rootPath}";
            yield break;
        }

        yield return rootPath + "/";

        foreach (var line in GetTreeLines(rootPath, "", excludeDirs, 0, maxDepth))
        {
            Console.WriteLine(line);
            yield return line;
        }
    }

    private static IEnumerable<string> GetTreeLines(
        string currentPath,
        string prefix,
        HashSet<string> excludeDirs,
        int currentDepth,
        int maxDepth)
    {
        if (currentDepth >= maxDepth)
            yield break;

        var dirs = Directory.GetDirectories(currentPath)
            .Where(d => !excludeDirs.Contains(Path.GetFileName(d)))
            .OrderBy(d => d)
            .ToList();

        var files = Directory.GetFiles(currentPath)
            .OrderBy(f => f)
            .ToList();

        var entries = dirs.Concat(files).ToList();

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var isLast = i == entries.Count - 1;
            var connector = isLast ? "└── " : "├── ";
            var name = Path.GetFileName(entry);

            yield return prefix + connector + name;

            if (Directory.Exists(entry))
            {
                var newPrefix = prefix + (isLast ? "    " : "│   ");
                foreach (var subLine in GetTreeLines(entry, newPrefix, excludeDirs, currentDepth + 1, maxDepth))
                {
                    yield return subLine;
                }
            }
        }
    }

    public static void FormatProject(string projectFilePath)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"format {projectFilePath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = processStartInfo;
        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to format project. Error: {process.StandardError.ReadToEnd()}");
        }
    }



}
