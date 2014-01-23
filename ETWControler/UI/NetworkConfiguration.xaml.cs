using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ETWControler.UI
{
    /// <summary>
    /// Interaction logic for NetworkConfiguration.xaml
    /// </summary>
    public partial class NetworkConfiguration : Window
    {
        ViewModel Model;
        string BackupHost;
        int BackupPortNumber;
        int BackupWCFPort;

        public NetworkConfiguration(ViewModel model)
        {
            BackupHost = model.Host;
            BackupPortNumber = model.PortNumber;
            BackupWCFPort = model.WCFPort;
            this.DataContext = model;
            Model = model;
            InitializeComponent();
        }

        private void Ok(object sender, RoutedEventArgs e)
        {
            // Reset sender and receiver to connect to new host and or port
            Model.NetworkReceiveState.Restart();
            Model.NetworkSendState.RestartIfStarted();
            this.Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            Model.Host = BackupHost;
            Model.PortNumber = BackupPortNumber;
            Model.WCFPort = BackupWCFPort;
            this.Close();
        }
    }
}
