using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Myshkin;

public class PlanAgentTools(string? baseDirectory = null)
{
    private readonly string _baseDirectory = baseDirectory ?? Directory.GetCurrentDirectory();

    [KernelFunction]
    [Description("Display the directory structure as a tree, showing all files and directories (excluding build artifacts)")]
    public IEnumerable<string> ShowFileTree(string? path = null)
    {
        var targetPath = ResolvePath(path);
        Console.WriteLine($"Showing file tree for {targetPath}");
        return CommandLineHelpers.GenerateFileTree(targetPath, maxDepth: 10);
    }

    //[KernelFunction]
    //[Description("List all files in a specific directory, optionally recursive")]
    public IEnumerable<string> ListAllFiles(string? path = null, bool recursive = true)
    {
        var targetPath = ResolvePath(path);
        Console.WriteLine($"Listing files in {targetPath} (recursive: {recursive})");

        if (!Directory.Exists(targetPath))
        {
            yield return $"Directory not found: {targetPath}";
            yield break;
        }

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.EnumerateFiles(targetPath, "*", searchOption).OrderBy(f => f);

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(_baseDirectory, file);
            yield return relativePath;
            Console.WriteLine($"{relativePath}");
        }
    }

    [KernelFunction]
    [Description("Read the content of a file with line numbers added at the start of each line. Supports nested paths.")]
    public IEnumerable<string> ReadFile(string filePath)
    {
        var fullPath = ResolvePath(filePath);
        Console.WriteLine($"Reading file {fullPath}");

        if (!File.Exists(fullPath))
        {
            yield return $"File not found: {fullPath}";
            yield break;
        }

        var index = 1;
        foreach (var line in File.ReadLines(fullPath))
        {
            yield return index++ + " " + line;
        }
    }

    /// <summary>
    /// Resolves a relative path to an absolute path within the base directory.
    /// </summary>
    private string ResolvePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return _baseDirectory;
        }

        // If the path is absolute, validate it's within the base directory for security
        if (Path.IsPathRooted(path))
        {
            var fullPath = Path.GetFullPath(path);
            if (!fullPath.StartsWith(_baseDirectory))
            {
                throw new UnauthorizedAccessException($"Access denied: path is outside the base directory");
            }
            return fullPath;
        }

        // Combine with base directory and resolve any .. or . references
        var resolvedPath = Path.GetFullPath(Path.Combine(_baseDirectory, path));

        // Security check: ensure the resolved path is still within the base directory
        if (!resolvedPath.StartsWith(_baseDirectory))
        {
            throw new UnauthorizedAccessException($"Access denied: path traversal outside base directory is not allowed");
        }

        return resolvedPath;
    }
}

