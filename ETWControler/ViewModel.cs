using ETWControler.Commands;
using ETWControler.ETW;
using ETWControler.Network;
using ETWControler.Screenshots;
using ETWControler.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace ETWControler
{
    public class ViewModel : NotifyBase, IDisposable
    {
        // Firewall rule names which open the configured ports during startup of ETWControler and close
        // them when it exits.
        const string FirewallWCFRule = "ETWControler WCFPort";
        const string FirewallSocketRule = "ETWControler SocketPort";

        public const string CustomCommandPrefix = "::";

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
            set
            {
                SetProperty<bool>(ref _CaptureKeyboard, value, "CaptureKeyboard");
                Hooker.Hooker.IsKeyboardHooked = CaptureKeyboard;
            }
        }

        public string UnexpandedTraceFileName
        {
            get { return Configuration.Default.TraceFileName;  }
            set { Configuration.Default.TraceFileName = value; }
        }

        public int TraceFileCounter = 0;

        public string UnexpandedCountedTraceFileName
        {
            get
            {
                string lret = UnexpandedTraceFileName;
                if (AppendIndexToOutputFileName)
                {
                    string dir = Path.GetDirectoryName(lret);
                    string file = Path.GetFileNameWithoutExtension(lret);
                    lret = Path.Combine(dir, $"{file}_{TraceFileCounter}{Path.GetExtension(lret)}");
                }
                return lret;
            }
        }

        public int TraceCounter = 0;

        public string TraceFileName
        {
            get {  return Environment.ExpandEnvironmentVariables(UnexpandedCountedTraceFileName); }
        }

        public bool _CaptureMouseButtonDown;
        /// <summary>
        /// Check state of Checkbox to capture all mouse down button events
        /// </summary>
        public bool CaptureMouseButtonDown
        {
            get { return _CaptureMouseButtonDown; }
            set
            {
                SetProperty<bool>(ref _CaptureMouseButtonDown, value, "CaptureMouseButtonDown");
                Hooker.Hooker.IsMouseHooked = CaptureMouseButtonDown;
            }
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

        public string LocalTraceServiceUrl
        {
            get
            {
                return String.Format("http://localhost:{1}/TraceControlerService", Host, WCFPort);
            }
        }

        public const string CaptureScreenShotsProperty = "CaptureScreenShots";

        public bool _CaptureScreenShots;
        public bool CaptureScreenShots
        {
            get { return _CaptureScreenShots; }
            set { SetProperty<bool>(ref _CaptureScreenShots, value, CaptureScreenShotsProperty); }
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

        bool _AppendIndexToOutputFileName = true;

        /// <summary>
        /// Append to file name a unique counter starting with 1 which is incremented for every trace start.
        /// </summary>
        public bool AppendIndexToOutputFileName
        {
            get { return _AppendIndexToOutputFileName; }
            set
            {
                SetProperty<bool>(ref _AppendIndexToOutputFileName, value);
            }
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


        string _FastEventMessage;

        /// <summary>
        /// Message is logged when Fast event hot key was pressed to find out 
        /// </summary>
        public string FastEventMessage
        {
            get { return _FastEventMessage; }
            set { SetProperty<string>(ref _FastEventMessage, value, "FastMessage"); }
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

        string _FastEventHotkey;
        public string FastEventHotkey
        {
            get { return _FastEventHotkey; }
            set { SetProperty<string>(ref _FastEventHotkey, value, "FastEventHotKey"); }
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


        bool _IsKeyboardEncrypted = true;
        public bool IsKeyBoardEncrypted
        {
            get { return _IsKeyboardEncrypted; }
            set
            {
                SetProperty<bool>(ref _IsKeyboardEncrypted, value);
            }
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


        public string ScreenshotDirectory { get { return Environment.ExpandEnvironmentVariables(ScreenshotDirectoryUnexpanded); } }

        public string ScreenshotDirectoryUnexpanded { get; set; }

        /// <summary>
        /// True if MainWindow ctor did run without an exception. This flag is used 
        /// to decide if early errors are shown in an extra message box or the status bar
        /// </summary>
        public bool MainWindowInitialized { get; internal set; }

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
                {"LogFast", CreateCommand( o =>
                {
                    Hooker.LogFastEvent();
                })},
                {"Config", CreateCommand( (o)=>
                                {
                                    var dlg = new ETWControlerConfiguration(this);
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

                    this.Hooker.ResetId();

                    this.TraceFileCounter++; // Increment file counter for every trace start so we get unique files names within the current trace session of ETWControler

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
                        var output = LocalTraceControler.ExecuteWPRCommand(LocalTraceSettings.TraceCancel);
                        LocalTraceSettings.ProcessCancelCommand(output);
                    }
                    if (this.ServerTraceEnabled)
                    {
                        var command = WCFHost.CreateExecuteWPRCommand(ServerTraceSettings.TraceCancel);
                        command.Completed = (output) => ServerTraceSettings.ProcessCancelCommand(output);
                        command.Execute();
                    }
                })},
                {"RegisterETWProvider", CreateCommand( o =>
                    {
                        string output = HookEvents.RegisterItself();
                        SetStatusMessage("Registering ETW provider: " + output);
                    })},
                {"ConfigReset", CreateCommand( _ => {
                    Configuration.Default.Reset();
                    Configuration.Default.Save();
                    LoadSettings();
                })}, 
                {"ShowMessages", CreateCommand( o=> ShowMessages() )},
                {"NetworkSendToggle", CreateCommand( o=> NetworkSendState.NetworkSendChangeState() )},
                {"NetworkReceiveToggle", CreateCommand( o=> NetworkReceiveState.NetworkReceiveChangeState() )},
                {"ClearStatusMessages", CreateCommand( o => StatusMessages.Clear() )},
                {"ShowCommandLineOptions", CreateCommand( o=> ShowCommandLineOptions()) },
                {"About", CreateCommand( _ => AboutBox()) },

            };


            return commands;
        }


        static readonly string CommandLineOptions = "ETWControler [-Hide] [-CaptureKeyboard] [-CaptureMouseClick] [-CaptureMouseMove] [-SendToServer Server [Port1 Port2]] [-ClearKeyboardEvents] [-RegisterEtwProvider]" + Environment.NewLine +
                                                    "\t-Hide                              Hide main window." + Environment.NewLine +
                                                    "\t-CaptureKeyboard                   Capture keyboard events." + Environment.NewLine +
                                                    "\t-ClearKeyboardEvents               By default the keys are all logged as SomeKey. If this is enabled the actual keyboard code is also logged." + Environment.NewLine +
                                                    "\t                                   Be careful that you do not enter your password while clear keyboard logging is enabled!" + Environment.NewLine +
                                                    "\t-CaptureMouseClick                 Capture muse click events." + Environment.NewLine +
                                                    "\t-CaptureMouseMove                  Capture mouse mouse events. " + Environment.NewLine +
                                      String.Format("\t-DisableCaptureScreenshots         Disables the save a screenshot feature where for each mouse click to the directory {0} or specify an explicit locaton with -ScreenshotDir xxx", Configuration.Default.ScreenshotDirectory) + Environment.NewLine +
                                                    "\t-ScreenshotDir xxxx                Directory to where the screenshots are saved if -CaptureScreenshots is set" + Environment.NewLine  +
                                                    "\t-SendToServer Server [Port1 Port2] Enable sending events to remote server. If Port1/2 are omitted the configured ports are used. " + Environment.NewLine +
                                                    "\t-RegisterEtwProvider               Register the HookEvents ETW provider and then exit. This needs to be done only once e.g. during installation." + Environment.NewLine +
                                                    "\t-UnRegisterEtwProvider             Unregister the HookEvents ETW provider and then exit." + Environment.NewLine +
                                                    "\t" + Environment.NewLine +
                                                    "Example:" + Environment.NewLine +
                                                    "\tETWControler.exe -capturemouseclick -capturekeyboard -sendtoserver localhost" + Environment.NewLine +
                                                    "This will eanble mouse click, encrypted keyboard tracing which will send to to your local machine again. If you want to hide the window you can add -hide." + Environment.NewLine + 
                                                    "These commands are useful if you only want to use ETWControler as keyboard/mouse event logger but the ETW recording is performed by your own script/wpr profile." + Environment.NewLine;                 

        static readonly string About = String.Format("ETWControler (c) by Alois Kraus 2015 v{0}", Assembly.GetExecutingAssembly().GetName().Version);
        private void AboutBox()
        {
            var window = new HelpWindow("About", About);
            window.Show();
        }

        private void ShowCommandLineOptions()
        {
            HelpWindow window = new HelpWindow("Command Line Options", CommandLineOptions, true);
            window.Show();
        }

        /// <summary>
        /// Initialize the default values for the ViewModel and load the settings from disc.
        /// </summary>
        public ViewModel()
        {
            LocalTraceSettings = new TraceControlViewModel(this, false);
            ServerTraceSettings = new TraceControlViewModel(this, true);
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
        public void InitUIDependantVariables(App args)
        {
            NetworkSendState = new NetworkSendState(this);
            NetworkReceiveState = new NetworkReceiveState(this);
            WCFHost = new WCFHostServiceState(this);
            Hooker = new NetworkedHooker(this);
            if( !HookEvents.IsAlreadyRegistered() )
            {
                Commands["RegisterETWProvider"].Execute(null);
            }

            // use settings from command line if present
            CaptureKeyboard = args.CaptureKeyboard;
            CaptureMouseButtonDown = args.CaptureMouseButtonDown;
            CaptureMouseMove = args.CaptureMouseMove;
            ScreenshotDirectoryUnexpanded = (args.ScreenshotDirectory ?? Configuration.Default.ScreenshotDirectory);
            CaptureScreenShots = args.CaptureScreenshots;

            IsKeyBoardEncrypted = !args.IsKeyBoardNotEncrypted;

            if (args.SendToServer != null)
            {
                this.Host = args.SendToServer;
            }

            if (args.SendToServerPort != null)
            {
                int portNumber = 0;
                if (int.TryParse(args.SendToServerPort, out portNumber))
                {
                    this.PortNumber = portNumber;
                }
            }

            if (args.SendtoServerSecondaryPort != null)
            {
                int secondaryPort = 0;
                if (int.TryParse(args.SendtoServerSecondaryPort, out secondaryPort))
                {
                    this.WCFPort = secondaryPort;
                }
            }
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
            if (this.CaptureScreenShots) // create html report also if no tracing was active perhaps someone finds this functionality in itself useful
            {
                var htmlReportGenerator = new HtmlReportGenerator(this.ScreenshotDirectory);
                htmlReportGenerator.GenerateReport();
            }

            if (this.LocalTraceEnabled) 
            {
                LocalTraceSettings.TraceStates = TraceStates.Stopping;
                // stop tracing asynchronously so we do not need to wait until local trace collection has stopped (while blocking the UI)
                Task.Factory.StartNew< Tuple<int, string>>(() => LocalTraceControler.ExecuteWPRCommand(LocalTraceSettings.TraceStopFullCommandLine))
                            .ContinueWith((t) => LocalTraceSettings.ProcessStopCommand(t.Result), UISheduler);
            }
            if (this.ServerTraceEnabled)
            {
                ServerTraceSettings.TraceStates = TraceStates.Stopping;
                var command = WCFHost.CreateExecuteWPRCommand(ServerTraceSettings.TraceStopFullCommandLine);
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
            this.FastEventMessage = Configuration.Default.FastEventMessage;
            this.FastEventHotkey = Configuration.Default.FastEventHotkey;
            this.LocalTraceSettings.TraceStart = Configuration.Default.LocalTraceStart;
            this.LocalTraceSettings.TraceStop = Configuration.Default.LocalTraceStop;
            this.LocalTraceSettings.TraceCancel = Configuration.Default.LocalTraceCancel;
            this.ServerTraceSettings.TraceStart = Configuration.Default.ServerTraceStart;
            this.ServerTraceSettings.TraceStop = Configuration.Default.ServerTraceStop;
            this.ServerTraceSettings.TraceCancel = Configuration.Default.ServerTraceCancel;
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
            Configuration.Default.FastEventHotkey = this.FastEventHotkey;
            Configuration.Default.FastEventMessage = this.FastEventMessage;
            Configuration.Default.LocalTraceStart = this.LocalTraceSettings.TraceStart;
            Configuration.Default.LocalTraceStop = this.LocalTraceSettings.TraceStop;
            Configuration.Default.ServerTraceStart = this.ServerTraceSettings.TraceStart;
            Configuration.Default.ServerTraceStop = this.ServerTraceSettings.TraceStop;
            Configuration.Default.LocalTraceEnabled = this.LocalTraceEnabled;
            Configuration.Default.ServerTraceEnabled = this.ServerTraceEnabled;
            Configuration.Default.LocalTraceCancel = LocalTraceSettings.TraceCancel;
            Configuration.Default.ServerTraceCancel = ServerTraceSettings.TraceCancel;
            Configuration.Default.TraceFileName = this.UnexpandedTraceFileName;
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

        public void CloseFirwallPorts()
        {
            DeleteFirewallRule(FirewallWCFRule);
            DeleteFirewallRule(FirewallSocketRule);
        }

        Tuple<int,string> DeleteFirewallRule(string ruleName)
        {
            var proc = new RedirectedProcess("netsh.exe", String.Format("Advfirewall Firewall delete Rule Name=\"{0}\"", ruleName));
            return proc.Start();
        }

        public void OpenFirewallPorts()
        {
            Task.Factory.StartNew(() =>
            {
               DeleteFirewallRule(FirewallWCFRule);
               string openWCFPort =
                    String.Format("advfirewall firewall add Rule Name = \"{0}\" Dir = in action = allow protocol = TCP localport = {1}", FirewallWCFRule, this.WCFPort);

                var proc = new RedirectedProcess("netsh.exe", openWCFPort);
                return proc.Start();
            }).ContinueWith( ret => 
                this.SetStatusMessage(String.Format("Opened firewall for port {0}. Netsh output: {1}", this.WCFPort, ret.Result.Item2.Trim())), this.UISheduler);

            Task.Factory.StartNew( () =>
             {
                DeleteFirewallRule(FirewallSocketRule);
                string openSocketPort =
                    String.Format("advfirewall firewall add Rule Name = \"{0}\" Dir = in action = allow protocol = TCP localport = {1}", FirewallSocketRule, this.PortNumber);
                var proc = new RedirectedProcess("netsh.exe", openSocketPort);
                return proc.Start();
             }).ContinueWith(( ret =>
                 this.SetStatusMessage(String.Format("Opened firewall for port {0}. Netsh ouputput: {1}", this.PortNumber, ret.Result.Item2.Trim()))), this.UISheduler);
        }

        public void Dispose()
        {
            if( Hooker != null )
            {
                Hooker.Dispose();
                Hooker = null;
            }
        }
    }
}
