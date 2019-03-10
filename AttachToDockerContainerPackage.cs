using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace AttachToDockerContainer
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(AttachToDockerContainerPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class AttachToDockerContainerPackage : AsyncPackage
    {
        public const string PackageGuidString = "6cc17e98-631b-4af5-a461-ef1258c805a4";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await AttachToDockerContainerDialogCommand.InitializeAsync(this);
            await DebugAdapterHostLauncher.InitializeAsync(this);
        }
    }
}
