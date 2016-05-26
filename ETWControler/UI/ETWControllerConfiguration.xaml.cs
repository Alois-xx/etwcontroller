using System.Threading.Tasks;
using System.Windows;

namespace ETWControler.UI
{
    /// <summary>
    /// Interaction logic for ETWControlerConfiguration.xaml
    /// </summary>
    public partial class ETWControlerConfiguration : Window
    {
        ViewModel Model;
        string BackupHost;
        int BackupPortNumber;
        int BackupWCFPort;
        int BackupForcedScreenshotIntervalinMs;
        string BackupTraceOpenCmdLine;

        public ETWControlerConfiguration(ViewModel model)
        {
            BackupHost = model.Host;
            BackupPortNumber = model.PortNumber;
            BackupWCFPort = model.WCFPort;
            BackupForcedScreenshotIntervalinMs = model.ForcedScreenshotIntervalinMs;
            BackupTraceOpenCmdLine = model.TraceOpenCmdLine;
            this.DataContext = model;
            Model = model;
            InitializeComponent();
        }

        private void Ok(object sender, RoutedEventArgs e)
        {
            // Reset sender and receiver to connect to new host and or port
            Model.NetworkReceiveState.Restart();
            Model.NetworkSendState.RestartIfStarted();
            Model.WCFHost.Restart();
            Model.OpenFirewallPorts();
            
            Task.Factory.StartNew(() => Model.SaveSettings());
            this.Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            Model.Host = BackupHost;
            Model.PortNumber = BackupPortNumber;
            Model.WCFPort = BackupWCFPort;
            Model.ForcedScreenshotIntervalinMs = BackupForcedScreenshotIntervalinMs;
            Model.TraceOpenCmdLine = BackupTraceOpenCmdLine;
            this.Close();
        }
    }
}
