using System.Diagnostics;

namespace Myshkin;

public static class CommandLineHelpers
{
    /// <summary>
    /// Applies unified diff content via stdin to the 'patch' command.
    /// </summary>
    public static async Task<(int ExitCode, string StdOut, string StdErr)> ApplyPatchTextAsync(
        string workingDirectory,
        string patchText,
        int strip = 0,
        bool dryRun = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "patch", // assumes 'patch' is in PATH
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (dryRun)
        {
            psi.ArgumentList.Add("--dry-run");
        }

        psi.ArgumentList.Add($"-p{strip}");

        using var proc = new Process();
        proc.StartInfo = psi;
        proc.Start();

        // Write the diff to stdin
        await proc.StandardInput.WriteAsync(patchText);
        proc.StandardInput.Close();

        string stdout = await proc.StandardOutput.ReadToEndAsync();
        string stderr = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
        Console.WriteLine(proc.ExitCode);
        Console.WriteLine(stdout);
        Console.WriteLine(stderr);

        return (proc.ExitCode, stdout, stderr);
    }

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

        var entries = dirs.Cast<string>().Concat(files).ToList();

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
}

