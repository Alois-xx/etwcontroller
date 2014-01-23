using ETWControler.Commands;
using ETWControler.ETW;
using ETWControler.Network;
using ETWControler.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace ETWControler
{
    public class ViewModel : NotifyBase
    {
        public TaskScheduler UISheduler
        {
            get;
            set;
        }

        int _PortNumber;

        /// <summary>
        /// Port of remote host to connect to
        /// </summary>
        public int PortNumber
        {
            get { return _PortNumber; }
            set { SetProperty<int>(ref _PortNumber, value, "PortNumber"); }
        }

        string _Host;
        /// <summary>
        /// Host name or IP of remote host to connect to
        /// </summary>
        public string Host
        {
            get { return _Host; }
            set { SetProperty<string>(ref _Host, value, "Host"); }
        }

        public int _WCFPort;
        /// <summary>
        /// Port on which the web service to allow remote trace collection does listen to.
        /// </summary>
        public int WCFPort
        {
            get { return _WCFPort; }
            set { SetProperty<int>(ref _WCFPort, value, "WCFPort"); }
        }

        public bool _CaptureKeyboard;
        /// <summary>
        /// Check state of Checkbox to capture all keyboard presses
        /// </summary>
        public bool CaptureKeyboard
        {
            get { return _CaptureKeyboard;  }
            set { SetProperty<bool>(ref _CaptureKeyboard, value, "CaptureKeyboard"); }
        }

        public bool _CaptureMouseButtonDown;
        /// <summary>
        /// Check state of Checkbox to capture all mouse down button events
        /// </summary>
        public bool CaptureMouseButtonDown
        {
            get { return _CaptureMouseButtonDown; }
            set { SetProperty<bool>(ref _CaptureMouseButtonDown, value, "CaptureMouseButtonDown"); }
        }

        /// <summary>
        /// WCF TraceControlerService url to control trace sessions on the remote host
        /// </summary>
        public string TraceServiceUrl
        {
            get
            {
                return String.Format("http://{0}:{1}/TraceControlerService", Host, WCFPort);
            }
        }

        public bool _CaptureMouseMove;
        /// <summary>
        /// Check state of Checkbox
        /// </summary>
        public bool CaptureMouseMove
        {
            get { return _CaptureMouseMove; }
            set { SetProperty<bool>(ref _CaptureMouseMove, value, "CaptureMouseMove"); }
        }

        /// <summary>
        /// Trace settings and tracing state on current machine
        /// </summary>
        public TraceControlViewModel LocalTraceSettings
        {
            get;
            private set;
        }

        /// <summary>
        /// Trace settings and tracing state on remote machine.
        /// </summary>
        public TraceControlViewModel ServerTraceSettings
        {
            get;
            private set;
        }

        bool _LocalTraceEnabled;
        /// <summary>
        /// When true tracing is started/stopped/canelled on local machine when one of the trace start/stop/cancel buttons is pressed.
        /// </summary>
        public bool LocalTraceEnabled
        {
            get { return _LocalTraceEnabled; }
            set { SetProperty<bool>(ref _LocalTraceEnabled, value, "LocalTraceEnabled"); }
        }

        bool _ServerTraceEnabled;
        /// <summary>
        /// When true tracing is started/stopped/canelled on remote machine when one of the trace start/stop/cancel buttons is pressed.
        /// </summary>
        public bool ServerTraceEnabled
        {
            get { return _ServerTraceEnabled; }
            set { SetProperty<bool>(ref _ServerTraceEnabled, value, "ServerTraceEnabled"); }
        }


        bool _NetworkSendEnabled;
        /// <summary>
        /// Check state of Checkbox
        /// </summary>
        public bool NetworkSendEnabled
        {
            get { return _NetworkSendEnabled; }
            set { SetProperty<bool>(ref _NetworkSendEnabled, value, "NetworkSendEnabled"); }
        }

        ObservableCollection<string> _ReceivedMessages;
        /// <summary>
        /// List of messages received from remote machines
        /// </summary>
        public ObservableCollection<string> ReceivedMessages
        {
            get { return _ReceivedMessages; }
            set { SetProperty<ObservableCollection<string>>(ref _ReceivedMessages, value, "ReceivedMessages"); }
        }

        string _StatusText;
        /// <summary>
        /// Current status text which is displayed in status bar. 
        /// </summary>
        public string StatusText
        {
            get { return _StatusText; }
            set 
            {
                SetProperty<string>(ref _StatusText, value, "StatusText"); 
            }
        }

        Brush _StatusColor;
        /// <summary>
        /// Current color of status text
        /// </summary>
        public Brush StatusColor
        {
            get { return _StatusColor; }
            set { SetProperty<Brush>(ref _StatusColor, value, "StatusColor"); }
        }

        string _SlowEventMessage;

        /// <summary>
        /// Message is logged when Slow event hot key was pressed to find out 
        /// </summary>
        public string SlowEventMessage
        {
            get { return _SlowEventMessage; }
            set { SetProperty<string>(ref _SlowEventMessage, value, "SlowMessage"); }
        }

        string _SlowEventHotkey;

        /// <summary>
        /// Hotkey which is the stringified value of System.Windows.Input.Key or ETWControler.MouseButton
        /// </summary>
        public string SlowEventHotkey
        {
            get { return _SlowEventHotkey;  }
            set { SetProperty<string>( ref _SlowEventHotkey, value, "SlowEventHotkey"); }
        }

        /// <summary>
        /// Socket connection which sends captured mouse/keyboard events to remote machine
        /// </summary>
        public NetworkSendState NetworkSendState
        {
            get;
            set;
        }

        /// <summary>
        /// Socket server which receives capture mouse/keyboard events.
        /// </summary>
        public NetworkReceiveState NetworkReceiveState
        {
            get;
            set;
        }

        /// <summary>
        /// Actual keyboard hook which can send them over the network when enabled.
        /// </summary>
        public NetworkedHooker Hooker
        {
            get;
            set;
        }

        bool _ErrorHasOccured;

        /// <summary>
        /// Used as trigger to flash the status bar when an error has been logged
        /// </summary>
        public bool ErrorHasOccured
        {
            get { return _ErrorHasOccured; }
            set { SetProperty<bool>(ref _ErrorHasOccured, value); }
        }


        string[] _TraceSessions;
        /// <summary>
        /// Local trace session names
        /// </summary>
        public string[] TraceSessions
        {
            get { return _TraceSessions; }
            set { SetProperty<string[]>(ref _TraceSessions, value, "TraceSessions"); }
        }

        string[] _ServerTraceSessions;
        /// <summary>
        /// Remote trace session names
        /// </summary>
        public string[] ServerTraceSessions
        {
            get { return _ServerTraceSessions; }
            set { SetProperty<string[]>(ref _ServerTraceSessions, value, "ServerTraceSessions"); }
        }

        /// <summary>
        /// Is filled by CreateCommands method
        /// </summary>
        public Dictionary<string, ICommand> Commands
        {
            get;
            private set;
        }

        StatusMessages StatusWindow;

        public ObservableCollection<StatusMessage> StatusMessages
        {
            get;
            set;
        }

        /// <summary>
        /// WCF Trace controler server which controls tracing on remote machine
        /// </summary>
        internal WCFHostServiceState WCFHost
        {
            get;
            set;
        }

        static Brush Red = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        static Brush Black = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        TraceControlerService LocalTraceControler;


        /// <summary>
        /// All button presses are routed via comamnds here which do execute one of these commands
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, ICommand> CreateUICommands()
        {
            Dictionary<string, ICommand> commands = new Dictionary<string, ICommand>
            {
                {"LogSlow", CreateCommand( o => 
                {
                    Hooker.LogSlowEvent();
                })},
                {"Network", CreateCommand( (o)=> 
                                { 
                                    var dlg = new NetworkConfiguration(this);
                                    dlg.ShowDialog();
                                })},
                {"TraceRefresh", CreateCommand( (o) =>
                {
                    TraceSessions = LocalTraceControler.GetTraceSessions();
                    WCFHost.GetTraceSessions.Execute();
                })},
                {"StartTracing", CreateCommand(
                (o) =>
                {
                    if(!this.LocalTraceEnabled && !this.ServerTraceEnabled) 
                    {
                        MessageBox.Show("Please enable tracing at the remote host and/or on your local machine.");
                        return;
                    }

                    if (this.LocalTraceEnabled) // start async to allow the web service to start tracing simultanously on the target host
                    {
                        LocalTraceSettings.TraceStates = TraceStates.Starting;
                        Task.Factory.StartNew<Tuple<int, string>>(() => LocalTraceControler.ExecuteWPRCommand(LocalTraceSettings.TraceStartFullCommandLine))
                                    .ContinueWith(t => LocalTraceSettings.ProcessStartComand(t.Result), UISheduler);
                    }
                    if (this.ServerTraceEnabled)
                    {
                        ServerTraceSettings.TraceStates = TraceStates.Starting;
                        var command = WCFHost.CreateExecuteWPRCommand(ServerTraceSettings.TraceStartFullCommandLine);
                        command.Completed = (output) => ServerTraceSettings.ProcessStartComand(output);
                        command.Execute();
                    }
                } 
                )},
                {"StopTracing", CreateCommand( o => StopTracing())},
                {"CancelTracing", CreateCommand( o=> 
                {
                    if (this.LocalTraceEnabled)
                    {
                        var output = LocalTraceControler.ExecuteWPRCommand("-cancel");
                        LocalTraceSettings.ProcessCancelCommand(output);
                    }
                    if (this.ServerTraceEnabled)
                    {
                        var command = WCFHost.CreateExecuteWPRCommand("-cancel");
                        command.Completed = (output) => ServerTraceSettings.ProcessCancelCommand(output);
                        command.Execute();
                    }
                })},
                {"RegisterETWProvider", CreateCommand( o => HookEvents.RegisterItself()) },
                {"ShowMessages", CreateCommand( o=> ShowMessages() )},
                {"NetworkSendToggle", CreateCommand( o=> NetworkSendState.NetworkSendChangeState() )},
                {"NetworkReceiveToggle", CreateCommand( o=> NetworkReceiveState.NetworkReceiveChangeState() )},
                {"ClearStatusMessages", CreateCommand( o => StatusMessages.Clear() )},

            };


            return commands;
        }

        /// <summary>
        /// Initialize the default values for the ViewModel and load the settings from disc.
        /// </summary>
        public ViewModel()
        {
            LocalTraceSettings = new TraceControlViewModel(false);
            ServerTraceSettings = new TraceControlViewModel(true);
            _ReceivedMessages = new ObservableCollection<string>();
            _ServerTraceSessions = new string[]{ "Not yet read."};
            _TraceSessions = new string[] {"Not yet read."};
            StatusMessages = new ObservableCollection<StatusMessage>();
            LocalTraceControler = new TraceControlerService();
            Commands = CreateUICommands();
            LoadSettings();
            StatusMessages.CollectionChanged += StatusMessages_CollectionChanged;
        }

        void StatusMessages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null &&
                e.NewItems.Count > 0 )
            {
                ErrorHasOccured = false;
                foreach(StatusMessage statusMesage in e.NewItems)
                {
                    if( statusMesage.Data != null )
                    {
                        ErrorHasOccured = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Initialize things which must happen on a UI thread.
        /// </summary>
        public void InitUIDependantVariables()
        {
            NetworkSendState = new NetworkSendState(this);
            NetworkReceiveState = new NetworkReceiveState(this);
            WCFHost = new WCFHostServiceState(this);
            Hooker = new NetworkedHooker(this);
        }

        /// <summary>
        /// Set status message with black color
        /// </summary>
        /// <param name="message"></param>
        public void SetStatusMessage(string message)
        {
            StatusColor = Black;
            StatusMessages.Add(new StatusMessage { MessageText = message });
            StatusText = message;
        }

        /// <summary>
        /// Set status message in any color
        /// </summary>
        /// <param name="message"></param>
        /// <param name="color"></param>
        public void SetStatusMessage(string message, Brush color)
        {
            StatusColor = color;
            StatusMessages.Add(new StatusMessage { MessageText = message });
            StatusText = message;
        }

        /// <summary>
        /// Set status message with red font color.
        /// </summary>
        /// <param name="message"></param>
        public void SetStatusMessageWarning(string message, Exception ex = null)
        {
            StatusColor = Red;
            StatusMessages.Add(new StatusMessage { MessageText = message, Data = ex });
            StatusText = message;
        }

        /// <summary>
        /// Yeah I know no mvvm to events. In a later version I will perhaps use Caliburn.Micro 
        /// </summary>
        internal void CloseStatusMessageWindow()
        {
            if( StatusWindow != null)
            {
                StatusWindow.Close();
            }
        }

        /// <summary>
        /// Show status messages in status bar context menu
        /// </summary>
        private void ShowMessages()
        {
            if( StatusWindow == null )
            {
                StatusWindow = new StatusMessages(this);
                StatusWindow.Closed += (a, b) => StatusWindow = null;
            }

            StatusWindow.Show();
            StatusWindow.Activate();
        }

        /// <summary>
        /// stop tracing command 
        /// </summary>
        void StopTracing()
        {
            if (this.LocalTraceEnabled) 
            {
                LocalTraceSettings.TraceStates = TraceStates.Stopping;
                // stop tracing asynchronously so we do not need to wait until local trace collection has stopped (while blocking the UI)
                Task.Factory.StartNew< Tuple<int, string>>(() => LocalTraceControler.ExecuteWPRCommand(LocalTraceSettings.TraceStop))
                            .ContinueWith((t) => LocalTraceSettings.ProcessStopCommand(t.Result), UISheduler);
            }
            if (this.ServerTraceEnabled)
            {
                ServerTraceSettings.TraceStates = TraceStates.Stopping;
                var command = WCFHost.CreateExecuteWPRCommand(ServerTraceSettings.TraceStop);
                command.Completed = (output) => ServerTraceSettings.ProcessStopCommand(output);
                command.Execute();
            }
        }

        /// <summary>
        /// Load current settings from app or user.config
        /// </summary>
        void LoadSettings()
        {
            this.PortNumber = Configuration.Default.PortNumber;
            this.WCFPort = Configuration.Default.WCFPort;
            this.Host = Configuration.Default.Host;
            this.SlowEventHotkey = Configuration.Default.SlowEventHotkey;
            this.SlowEventMessage = Configuration.Default.SlowEventMessage;
            this.LocalTraceSettings.TraceStart = Configuration.Default.LocalTraceStart;
            this.LocalTraceSettings.TraceStop = Configuration.Default.LocalTraceStop;
            this.ServerTraceSettings.TraceStart = Configuration.Default.ServerTraceStart;
            this.ServerTraceSettings.TraceStop = Configuration.Default.ServerTraceStop;
            this.ServerTraceEnabled = Configuration.Default.ServerTraceEnabled;
            this.LocalTraceEnabled = Configuration.Default.LocalTraceEnabled;
        }

        /// <summary>
        /// Save changed settings to user.config
        /// </summary>
        internal void SaveSettings()
        {
             Configuration.Default.PortNumber = this.PortNumber;
             Configuration.Default.WCFPort = this.WCFPort;
             Configuration.Default.Host = this.Host;
             Configuration.Default.SlowEventHotkey = this.SlowEventHotkey;
             Configuration.Default.SlowEventMessage = this.SlowEventMessage;
             Configuration.Default.LocalTraceStart = this.LocalTraceSettings.TraceStart;
             Configuration.Default.LocalTraceStop = this.LocalTraceSettings.TraceStop;
             Configuration.Default.ServerTraceStart = this.ServerTraceSettings.TraceStart;
             Configuration.Default.ServerTraceStop = this.ServerTraceSettings.TraceStop;
             Configuration.Default.LocalTraceEnabled = this.LocalTraceEnabled;
             Configuration.Default.ServerTraceEnabled = this.ServerTraceEnabled;
             Configuration.Default.Save();
        }

        /// <summary>
        /// Create a UI command from a delegate
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        static DelegateCommand CreateCommand(Action<object> o)
        {
            return new DelegateCommand(o);
        }

    }
}
