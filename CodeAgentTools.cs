using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Myshkin;

public class CodeAgentTools(string? baseDirectory = null)
{
    [KernelFunction]
    [Description("Formats the project using the command line helpers.")]
    public static void FormatProject(string projectFilePath)
    {
        CommandLineHelpers.FormatProject(projectFilePath);
    }

    private readonly string _baseDirectory = baseDirectory ?? Directory.GetCurrentDirectory();


    [KernelFunction]
    [Description("Insert text at a specific line number in a file. Returns the updated file content with line numbers.")]
    public static IEnumerable<string> InsertTextAtLine(string path, string text, int lineNumber)
    {
        Console.WriteLine($"Inserting text at line {lineNumber}: {text}");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The specified file does not exist.", path);
        }
        var originalText = File.ReadAllText(path);
        var lines = originalText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None).ToList();
        if (lineNumber < 0 || lineNumber > lines.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(lineNumber), "Line number is out of range.");
        }
        lines.Insert(lineNumber, text);
        originalText = string.Join(Environment.NewLine, lines);
        File.WriteAllText(path, originalText);

        var index = 1;
        foreach (var line in lines)
        {
            yield return index++ + " " + line;
        }
    }

    [KernelFunction]
    [Description("Remove text from a specific line number range in a file. Returns the updated file content with line numbers.")]
    public static IEnumerable<string> RemoveTextAtLine(string path, int startLineNumber, int endLineNumber)
    {
        Console.WriteLine($"Removing text from line {startLineNumber} to {endLineNumber}");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The specified file does not exist.", path);
        }
        var originalText = File.ReadAllText(path);
        var lines = originalText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None).ToList();
        if (startLineNumber < 1 || startLineNumber > lines.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(startLineNumber), "Start line number is out of range. Use 1-based indexing.");
        }
        if (endLineNumber < startLineNumber || endLineNumber > lines.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(endLineNumber), "End line number is out of range. Use 1-based indexing.");
        }
        var zeroBasedStart = startLineNumber - 1;
        lines.RemoveRange(zeroBasedStart, endLineNumber - startLineNumber + 1);
        originalText = string.Join(Environment.NewLine, lines);
        File.WriteAllText(path, originalText);

        var index = 1;
        foreach (var line in lines)
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