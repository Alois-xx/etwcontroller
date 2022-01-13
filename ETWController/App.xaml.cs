using ETWController.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace ETWController
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public ViewModel Model;

        public App()
        {
            DispatcherUnhandledException += Dispatcher_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            CaptureScreenshots = true;
        }

        Queue<string> Args;

        protected override void OnStartup(StartupEventArgs e)
        {
            Args = new Queue<string>(e.Args);

            if( Args.Count == 0) // if no args are supplied use the default or user configured values
            {
                CaptureKeyboard = Configuration.Default.CaptureKeyboard;
                CaptureMouseButtonDown = Configuration.Default.CaptureMouseButtonDown;
                CaptureMouseWheel = Configuration.Default.CaptureMouseWheel;
                CaptureMouseMove = Configuration.Default.CaptureMouseMove;
                CaptureScreenshots = Configuration.Default.CaptureScreenShots;
            }

            string currentArg = null;
            while( ( currentArg = GetNextArg()  ) !=  null )
            {
                switch(currentArg)
                {
                    case "-hide":
                        HideWindow = true;
                        break;

                    case "-disablecapturescreenshots":
                        CaptureScreenshots = false;
                        break;
                    case "-screenshotdir":
                        ScreenshotDirectory = GetNextArgArgument();
                        break;

                    case "-capturekeyboard":
                        CaptureKeyboard = true;
                        break;

                    case "-capturemouseclick":
                        CaptureMouseButtonDown = true;
                        break;
                    case "-capturemousewheel":
                        CaptureMouseWheel = true;
                        break;
                    case "-capturemousemove":
                        CaptureMouseMove = true;
                        break;

                    case "-sendtoserver":
                        SendToServer = GetNextArgArgument();
                        SendToServerPort = GetNextArgArgument();
                        SendtoServerSecondaryPort = GetNextArgArgument();
                        break;

                    case "-clearkeyboardevents":
                        IsKeyBoardNotEncrypted = true;
                        break;

                    case "-registeretwprovider":
                        try
                        {
                            ETW.HookEvents.RegisterItself();
                        }
                        catch(Exception ex)
                        {
                            ShowError(ex);
                        }
                        Application.Current.Shutdown(100);
                        break;
                    case "-unregisteretwprovider":
                        try
                        {
                            ETW.HookEvents.UnregisterItself();
                        }
                        catch (Exception ex)
                        {
                            ShowError(ex);
                        }
                        Application.Current.Shutdown(100);

                        break;

                    default:
                        throw new InvalidOperationException(String.Format("Command line argument {0} was not expected", currentArg));
                }

            }

            Model = new ViewModel(new MessageBoxDisplay());
            base.OnStartup(e);
        }

        string GetNextArg()
        {
            if( Args.Count > 0 )
            {
                if( Args.Peek().StartsWith("-") )
                {
                    return Args.Dequeue().ToLower();
                }
            }

            return null;
        }

        string GetNextArgArgument()
        {
            if (Args.Count > 0)
            {
                if (!Args.Peek().StartsWith("-"))
                {
                    return Args.Dequeue();
                }
            }

            return null;
        }

        public bool HideWindow
        {
            get; set;
        }
        public bool CaptureKeyboard { get; private set; }
        public bool CaptureMouseButtonDown { get; private set; } 
        public bool CaptureMouseWheel { get; private set; }
        public bool CaptureMouseMove { get; private set; }
        public string SendToServer { get; private set; }
        public string SendToServerPort { get; private set; }
        public bool IsKeyBoardNotEncrypted { get; private set; }
        public bool RegisterETWProviderAndThenExit { get; private set; }
        public string SendtoServerSecondaryPort { get; private set; }
        public bool CaptureScreenshots { get; internal set; }
        public string ScreenshotDirectory { get; private set; }

        void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            if (Model != null && Model.MainWindowInitialized)
            {
                Model.StatusMessages.Add(new UI.StatusMessage { MessageText = "Unobserved Task Exception", Data = e.Exception });
            }
            else
            {
                ShowError(e.Exception);
                Environment.Exit(500);
            }
        }

        void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (Model != null && Model.MainWindowInitialized)
            {
                Model.StatusMessages.Add(new UI.StatusMessage { MessageText = "Exception in Dispatcher", Data = e.Exception });
                e.Handled = true;
            }
            else
            {
                ShowError(e.Exception);
                Environment.Exit(500);
            }
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (Model != null && Model.MainWindowInitialized)
            {
                Model.StatusMessages.Add(new UI.StatusMessage { MessageText = "Unhandled", Data = e.ExceptionObject });
            }
            ShowError((Exception) e.ExceptionObject);
        }

        void ShowError(Exception e)
        {
            MessageBox.Show(String.Format("Unhandled exception occured in {0}", e), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
