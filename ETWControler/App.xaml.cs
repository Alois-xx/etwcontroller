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
        public ViewModel Model = new ViewModel();

        public App()
        {
            DispatcherUnhandledException += Dispatcher_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

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
