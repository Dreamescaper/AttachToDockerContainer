using System;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace AttachToDockerContainer
{
    public class DebugAdapterHostLauncher
    {
        private readonly DTE _dte;

        private DebugAdapterHostLauncher(DTE dte)
        {
            _dte = dte;
        }

        public void Launch(string containerName, string vsDbgPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dotnetPid = DockerCmd.Execute($"exec -it {containerName} pidof dotnet");

            // Need to create json file to pass to DebugAdapterHost.Launch
            var launchJsonPath = CreateLaunchJson(containerName, vsDbgPath, dotnetPid);

            try
            {
                _dte.ExecuteCommand("DebugAdapterHost.Launch", $"/LaunchJson:\"{launchJsonPath}\"");
            }
            finally
            {
                File.Delete(launchJsonPath);
            }
        }

        private string CreateLaunchJson(string containerName, string vsDbgPath, string dotnetPid)
        {
            var jsonText = $@"
            {{
              ""version"": ""0.2.0"",
              ""adapter"": ""docker.exe"",
              ""adapterArgs"": ""exec -i {containerName} {vsDbgPath} --interpreter=vscode"",
              ""languageMappings"": {{
                ""C#"": {{
                  ""languageId"": ""3F5162F8-07C6-11D3-9053-00C04FA302A1"",
                  ""extensions"": [ ""*"" ]
                }}
              }},
              ""exceptionCategoryMappings"": {{
                ""CLR"": ""449EC4CC-30D2-4032-9256-EE18EB41B62B"",
                ""MDA"": ""6ECE07A9-0EDE-45C4-8296-818D8FC401D4""
              }},
              ""configurations"": [
                {{
                  ""name"": "".NET Core Docker Attach"",
                  ""type"": ""coreclr"",
                  ""request"": ""attach"",
                  ""processId"": {dotnetPid}
                }}
              ]
            }}";

            var fileName = $"LaunchJson.{Guid.NewGuid()}.json";
            var path = Path.Combine(Path.GetTempPath(), fileName);

            using (var file = File.CreateText(path))
            {
                file.Write(jsonText);
            }

            return path;
        }

        public static DebugAdapterHostLauncher Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in AttachToDockerContainerDialogCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var dte = await package.GetServiceAsync(typeof(DTE)) as DTE;
            Instance = new DebugAdapterHostLauncher(dte);
        }
    }
}
