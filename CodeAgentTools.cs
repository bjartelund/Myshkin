using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Myshkin;

public class CodeAgentTools
{
    [KernelFunction]
    [Description("List all files in the current directory")]
    public IEnumerable<string> ListAllFiles()
    {
        Console.WriteLine($"Listing all files in {Directory.GetCurrentDirectory()}");
        return Directory.EnumerateFiles(".");
    }
    
    [KernelFunction]
    [Description("Read the content of a file with linenumbers added at the start of each line")]
    public IEnumerable<string> ReadFile(string filePath)
    {
        Console.WriteLine($"Reading file {filePath}");
        if (!File.Exists(filePath))
        { 
            yield return $"File not found: {filePath}";
            yield break;
        }

        var index = 1;
        foreach (var line in File.ReadLines(filePath))
        {
            yield return index++ + " " + line;
        }
    }

    [KernelFunction]
    [Description("Write content to a file by patching it with unified diff format. Rember counts. Remember to remove line numbers from the file content before creating the diff.")]
    public async Task<string> WriteFile(string diffContent)
    {

        Console.WriteLine("Applying patch to file...");
        Console.WriteLine($"Applying patch to {diffContent}");
        var callResult = await CommandLineHelpers.ApplyPatchTextAsync(".", diffContent, strip: 0, dryRun: false);
        if (callResult.ExitCode != 0)
        {
            Console.WriteLine("Failed to apply patch:");
            Console.WriteLine(callResult.StdErr);
            return callResult.StdErr;
        }
        Console.WriteLine("Patch applied successfully.");
        return "Patch applied successfully.";
    }

}