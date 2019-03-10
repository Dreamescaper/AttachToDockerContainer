using Microsoft.VisualStudio.PlatformUI;

namespace AttachToDockerContainer
{
    public partial class AttachToDockerContainerDialog : DialogWindow
    {
        public AttachToDockerContainerDialog()
        {
            InitializeComponent();
            ContainerComboBox.ItemsSource = GetContainerNames();
        }

        private string[] GetContainerNames()
        {

        }
    }
}
