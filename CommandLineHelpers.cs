using System.Diagnostics;

namespace Myshkin;

    
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

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
}

