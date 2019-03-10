using System.Diagnostics;

namespace AttachToDockerContainer
{
    public static class DockerCmd
    {
        public static string Execute(string arguments)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = arguments,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var process = Process.Start(processStartInfo);
            process.WaitForExit();
            return process.StandardOutput.ReadToEnd().Trim();
        }
    }
}
