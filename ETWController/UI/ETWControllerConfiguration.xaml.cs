﻿using System.Threading.Tasks;
using System.Windows;

namespace ETWController.UI
{
    /// <summary>
    /// Interaction logic for ETWControllerConfiguration.xaml
    /// </summary>
    public partial class ETWControllerConfiguration : Window
    {
        ETWController.ViewModel Model;
        string BackupHost;
        int BackupPortNumber;
        int BackupWCFPort;
        int BackupForcedScreenshotIntervalinMs;
        string BackupTraceOpenCmdLine;
        int BackupKeepNNewestScreenShots;
        bool BackupAlwaysShowCommandEditBoxes;

        public ETWControllerConfiguration(ETWController.ViewModel model)
        {
            BackupHost = model.Host;
            BackupPortNumber = model.PortNumber;
            BackupWCFPort = model.WCFPort;
            BackupForcedScreenshotIntervalinMs = model.ForcedScreenshotIntervalinMs;
            BackupKeepNNewestScreenShots = model.KeepNNewestScreenShots;
            BackupTraceOpenCmdLine = model.TraceOpenCmdLine;
            BackupAlwaysShowCommandEditBoxes = model.AlwaysShowCommandEditBoxes;
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
            Model.RestartScreenCapture();

            Task.Factory.StartNew(() => Model.SaveSettings());
            this.Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            Model.Host = BackupHost;
            Model.PortNumber = BackupPortNumber;
            Model.WCFPort = BackupWCFPort;
            Model.ForcedScreenshotIntervalinMs = BackupForcedScreenshotIntervalinMs;
            Model.KeepNNewestScreenShots = BackupKeepNNewestScreenShots;
            Model.TraceOpenCmdLine = BackupTraceOpenCmdLine;
            Model.AlwaysShowCommandEditBoxes = BackupAlwaysShowCommandEditBoxes;
            this.Close();
        }
    }
}
