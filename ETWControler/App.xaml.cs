using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ETWControler
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
        }


        Queue<string> Args;

        protected override void OnStartup(StartupEventArgs e)
        {
            Args = new Queue<string>(e.Args);
            string currentArg = null;
            while( ( currentArg = GetNextArg()  ) !=  null )
            {
                switch(currentArg)
                {
                    case "-hide":
                        HideWindow = true;
                    break;

                    case "-capturekeyboard":
                        CaptureKeyboard = true;
                    break;

                    case "-capturemouseclick":
                        CaptureMouseButtonDown = true;
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
                        ParseError = String.Format("command line argument {0} was not expected", currentArg);
                    break;
                }

                if( ParseError != null)
                {
                    break;
                }
            }

            Model = new ViewModel();
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


        public string ParseError
        {
            get;
            set;
        }

        public bool HideWindow
        {
            get; set;
        }
        public bool CaptureKeyboard { get; private set; }
        public bool CaptureMouseButtonDown { get; private set; }
        public bool CaptureMouseMove { get; private set; }
        public string SendToServer { get; private set; }
        public string SendToServerPort { get; private set; }
        public bool IsKeyBoardNotEncrypted { get; private set; }
        public bool RegisterETWProviderAndThenExit { get; private set; }
        public string SendtoServerSecondaryPort { get; private set; }

        void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Model.StatusMessages.Add(new UI.StatusMessage { MessageText = "Unobserved Task Exception", Data = e.Exception });
        }

        void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Model.StatusMessages.Add(new UI.StatusMessage { MessageText = "Exception in Dispatcher", Data = e.Exception });
            e.Handled = true;
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Model.StatusMessages.Add(new UI.StatusMessage { MessageText = "Unhandled", Data = e.ExceptionObject });
            ShowError((Exception) e.ExceptionObject);
        }

        void ShowError(Exception e)
        {
            MessageBox.Show(String.Format("Unhandled exception occured in {0}", e), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
