namespace sernick.Utility;

using System.Diagnostics;

public static class SystemExtensions
{
    public static (string stderr, string stdout) RunProcess(this string cmd, string arguments = "")
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = arguments,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            }
        };

        process.Start();

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadLine() ?? "";
        process.WaitForExit();

        return (stderr, stdout);
    }
}
