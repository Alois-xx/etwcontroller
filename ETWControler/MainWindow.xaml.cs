using ETWControler.Hooking;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ETWControler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Hooker HotKeyHook = new Hooker(); // Use extra hook for the definition of hotkeys

        public ViewModel Model
        {
            get { return ((App)App.Current).Model; }
        }

        public MainWindow()
        {
            Model.InitUIDependantVariables();
            this.DataContext = this.Model;
            Model.UISheduler = TaskScheduler.FromCurrentSynchronizationContext();
            InitializeComponent();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
        }

        private void HookOrUnhookKeyboard(object sender, RoutedEventArgs e)
        {
            Model.Hooker.Hooker.IsKeyboardHooked = Model.CaptureKeyboard;
        }

        private void HookOrUnhookMouse(object sender, RoutedEventArgs e)
        {
            Model.Hooker.Hooker.IsMouseHooked = Model.CaptureMouseButtonDown;
        }

        private void cSignalButton_Click(object sender, RoutedEventArgs e)
        {
            HotKeyHook.OnMouseButton += (ETWControler.Hooking.MouseButton button, int x, int y) =>
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

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            Expander exp = sender as Expander;
            exp.BringIntoView();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Model.CloseStatusMessageWindow();
            Model.SaveSettings();
        }

        private void TraceRefreshSelected(object sender, RoutedEventArgs e)
        {
            if( cTraceSessionsTab.IsSelected == true )
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
    }
}
