﻿using System;

using ETWController.Hooking;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using ETWController.UI;


namespace ETWController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Hooker HotKeyHook = new Hooker(); // Use extra hook for the definition of hotkeys
        App App;

        public ViewModel Model
        {
            get { return ((App)App.Current).Model; }
        }

        public MainWindow()
        {
            App = Application.Current as App;

            Model.InitUIDependantVariables(App, TaskScheduler.FromCurrentSynchronizationContext());
            this.DataContext = this.Model;
            InitializeComponent();

            if( App.HideWindow )
            {
                this.Hide();
                this.ShowInTaskbar = false;
            }
            Model.OpenFirewallPorts();
            if( App.SendToServer != null)
            {
                Model.NetworkSendState.NetworkSendChangeState();
            }

            Model.MainWindowInitialized = true;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            Model.CloseFirwallPorts();
        }

        private void DefineSlowHotkeyClick(object sender, RoutedEventArgs e)
        {
            HotKeyHook.OnMouseButton += (ETWController.Hooking.MouseButton button, int x, int y) =>
            {
                HotKeyHook.DisableHooks();
                Model.SlowEventHotkey = button.ToString("G");
            };
            HotKeyHook.OnKeyDown += (Key key) =>
            {
                HotKeyHook.DisableHooks();
                Model.SlowEventHotkey = key.ToString("G");
            };

            HotKeyHook.EnableHooks();
        }

        private void DefineFastHotkeyClick(object sender, RoutedEventArgs e)
        {
            HotKeyHook.OnMouseButton += (ETWController.Hooking.MouseButton button, int x, int y) =>
            {
                HotKeyHook.DisableHooks();
                Model.FastEventHotkey = button.ToString("G");
            };
            HotKeyHook.OnKeyDown += (Key key) =>
            {
                HotKeyHook.DisableHooks();
                Model.FastEventHotkey = key.ToString("G");
            };

            HotKeyHook.EnableHooks();
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            Expander exp = sender as Expander;
            exp.BringIntoView();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Model.CloseStatusMessageWindow();
            Model.SaveSettings();
            HotKeyHook.Dispose(); // Ensure that we close all hooks or we will randomly crash during application shutdown when managed code is tried to run although the clr has been already shut down.
            Model.Dispose();
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            // make sure all subwindows are also closed:
            Application.Current.Shutdown();
        }

        private void TraceRefreshSelected(object sender, RoutedEventArgs e)
        {
            if( TraceSessionsTab.IsSelected )
            {
                Model.Commands["TraceRefresh"].Execute(null);
            }
        }

        private void ClearMessages(object sender, RoutedEventArgs e)
        {
            Model.ReceivedMessages.Clear();
        }

        private void StatusBarClick(object sender, MouseButtonEventArgs e)
        {
            Model.Commands["ShowMessages"].Execute(null);
        }


        private void ShowTraceSessions_OnClick(object sender, RoutedEventArgs e)
        {
            if (SessionsWindow == null)
            {
                SessionsWindow = new TraceSessionsWindow((ViewModel)DataContext);
                SessionsWindow.Closed += (a, b) => SessionsWindow = null;
            }
            SessionsWindow.Show();
            SessionsWindow.Activate();
        }

        public TraceSessionsWindow SessionsWindow { get; set; }

        private async void MainWindow_OnInitialized(object sender, EventArgs e)
        {
            if (!Configuration.Default.WelcomeScreenShown)
            {
                await Task.Delay(200);
                Configuration.Default.WelcomeScreenShown = true;
                var win = new HelpWindow("ETW Controller - Welcome", ViewModel.WelcomeText);
                win.ShowDialog();
            }
        }
    }
}
