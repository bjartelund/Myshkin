using System.ComponentModel;
using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
    
    [KernelFunction]
    [Description("Get a skeleton view of a C# file, hiding method bodies and showing] line numbers.")]
    public static string GetSkeleton(string path)
    {
        var code = File.ReadAllText(path);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var lines = code.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

        // Map out ranges to hide (exclusive of the opening/closing braces if you prefer)
        var hiddenRanges = root.DescendantNodes()
            .OfType<BlockSyntax>()
            .Select(b => {
                var span = tree.GetLineSpan(b.Span);
                return new { Start = span.StartLinePosition.Line, End = span.EndLinePosition.Line };
            })
            .OrderBy(r => r.Start)
            .ToList();

        var sb = new StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            var currentRange = hiddenRanges.FirstOrDefault(r => i > r.Start && i < r.End);

            if (currentRange != null)
            {
                // If we are at the first line of a hidden block, add the placeholder
                if (i == currentRange.Start + 1)
                {
                    sb.AppendLine($"      :         /* ... {(currentRange.End - currentRange.Start - 1)} lines hidden ... */");
                }
                // Skip the rest of the lines in this range
                i = currentRange.End - 1; 
            }
            else
            {
                // Print the line with its 1-based original index
                sb.AppendLine($"{(i + 1),4}: {lines[i]}");
            }
        }

        return sb.ToString();
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

