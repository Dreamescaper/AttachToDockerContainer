using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace AttachToDockerContainer
{
    public partial class AttachToDockerContainerDialog : DialogWindow
    {
        private const string VsDbgDefaultPath = "/vsdbg/vsdbg";

        private readonly IServiceProvider _serviceProvider;

        public AttachToDockerContainerDialog(IServiceProvider serviceProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _serviceProvider = serviceProvider;
            InitializeComponent();

            AttachButton.IsEnabled = false;
            PidComboBox.IsEnabled = false;

            var containerNames = GetContainerNames();
            var (previousContainer, previousVsDbgPath) = GetSettings();

            ContainerComboBox.ItemsSource = GetContainerNames();
            ContainerComboBox.Text = containerNames.Contains(previousContainer)
                ? previousContainer
                : containerNames.FirstOrDefault();

            VsDbgPathTextBox.Text = previousVsDbgPath ?? VsDbgDefaultPath;

            UpdateDotNetPIDs();
        }

        private void AttachButton_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var containerName = ContainerComboBox.Text;
            var vsDbgPath = VsDbgPathTextBox.Text;
            var pid = (int)PidComboBox.SelectedItem;

            SetSettings(containerName, vsDbgPath);

            DebugAdapterHostLauncher.Instance.Launch(containerName, vsDbgPath, pid);
            Close();
        }

        private void ContainerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDotNetPIDs();
        }

        private void UpdateDotNetPIDs()
        {
            AttachButton.IsEnabled = false;
            PidComboBox.IsEnabled = false;

            var containerName = ContainerComboBox.Text;
            var dotnetPids = DockerCli.Execute($"exec -it {containerName} pidof dotnet")
                .Split(' ')
                .Select(pid => int.Parse(pid))
                .ToArray();

            PidComboBox.ItemsSource = dotnetPids;

            if (dotnetPids.Length > 1)
                PidComboBox.IsEnabled = true;

            if (dotnetPids.Length > 0)
                AttachButton.IsEnabled = true;
        }

        private string[] GetContainerNames()
        {
            var output = DockerCli.Execute("ps --format \"{{.Names}}\"");

            return output
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .OrderBy(n => n)
                .ToArray();
        }

        private (string container, string vsDbg) GetSettings()
        {
            const string collectionPath = nameof(AttachToDockerContainerDialog);

            ThreadHelper.ThrowIfNotOnUIThread();

            SettingsStore.CollectionExists(collectionPath, out int exists);
            if (exists != 1)
            {
                SettingsStore.CreateCollection(collectionPath);
            }

            SettingsStore.GetString(collectionPath, "container", out string container);
            SettingsStore.GetString(collectionPath, "vsdbg", out string vsdbg);

            return (container, vsdbg);
        }

        private void SetSettings(string container, string vsdbg)
        {
            const string collectionPath = nameof(AttachToDockerContainerDialog);

            ThreadHelper.ThrowIfNotOnUIThread();

            SettingsStore.CollectionExists(collectionPath, out int exists);
            if (exists != 1)
            {
                SettingsStore.CreateCollection(collectionPath);
            }

            SettingsStore.SetString(collectionPath, "container", container);
            SettingsStore.SetString(collectionPath, "vsdbg", vsdbg);
        }

        private IVsWritableSettingsStore _settingsStore = null;
        private IVsWritableSettingsStore SettingsStore
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (_settingsStore == null)
                {
                    var settingsManager = (IVsSettingsManager)_serviceProvider.GetService(typeof(SVsSettingsManager));

                    // Write the user settings to _settingsStore.
                    settingsManager.GetWritableSettingsStore(
                        (uint)__VsSettingsScope.SettingsScope_UserSettings,
                        out _settingsStore);
                }
                return _settingsStore;
            }
        }
    }
}
