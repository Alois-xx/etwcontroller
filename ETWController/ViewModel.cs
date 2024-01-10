using ETWController.Commands;
using ETWController.ETW;
using ETWController.Screenshots;
using ETWController.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;


namespace ETWController
{
    /// <summary>
    /// Main ViewModel of ETWController 
    /// </summary>
    public class ViewModel : NotifyBase, IDisposable
    {
        // Firewall rule names which open the configured ports during startup of ETWController and close
        // them when it exits.
        const string FirewallWCFRule = "ETWController WCFPort";
        const string FirewallSocketRule = "ETWController SocketPort";

        public const string CustomCommandPrefix = "::";

        public TaskScheduler UIScheduler
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
            set { SetProperty<int>(ref _PortNumber, value); }
        }

        string _Host;
        /// <summary>
        /// Host name or IP of remote host to connect to
        /// </summary>
        public string Host
        {
            get { return _Host; }
            set
            {
                SetProperty<string>(ref _Host, value);
                TraceServiceUrl = null; // force updated of dependent values
                LocalTraceServiceUrl = null;
             }
        }

        public int _WCFPort;
        /// <summary>
        /// Port on which the web service to allow remote trace collection does listen to.
        /// </summary>
        public int WCFPort
        {
            get { return _WCFPort; }
            set
            {
                SetProperty<int>(ref _WCFPort, value);
                TraceServiceUrl = null; // force updated of dependent values
                LocalTraceServiceUrl = null;
            }
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
                SetProperty<bool>(ref _CaptureKeyboard, value);
                Hooker.Hooker.IsKeyboardHooked = CaptureKeyboard;
            }
        }

        int _JpgCompessionLevel;
        public int JpgCompressionLevel
        {
            get { return _JpgCompessionLevel;  }
            set
            {
                SetProperty<int>(ref _JpgCompessionLevel, value);
            }
        }


        int _KeepNNewestScreenShots;
        public int KeepNNewestScreenShots
        {
            get { return _KeepNNewestScreenShots; }
            set
            {
                SetProperty<int>(ref _KeepNNewestScreenShots, value);
            }
        }

        int _ForcedScreenshotIntervalinMs;

        public int ForcedScreenshotIntervalinMs
        {
            get { return _ForcedScreenshotIntervalinMs; }
            set
            {
                SetProperty<int>(ref _ForcedScreenshotIntervalinMs, value);
            }
        }

        bool _IsSkipPDB;
        public bool IsSkipPDB
        {
            get => _IsSkipPDB;
            set
            {
                SetProperty(ref _IsSkipPDB, value);
            }
        }

        bool _IsSkipEnabled;
        public bool IsSkipPdbEnabled
        {
            get => _IsSkipEnabled;
            set
            {
                SetProperty(ref _IsSkipEnabled, value);
            }
        }

        private bool _Compress;
        public bool Compress
        {
            get
            {
                return _Compress;
            }
            set
            {
                SetProperty(ref _Compress, value, "Compress");
            }
        }

        public void RestartScreenCapture()
        {
            if (Hooker != null && CaptureScreenShots)
            {
                Hooker.EnableRecorder();
            }
        }

        public string UnexpandedTraceFileName
        {
            get { return String.IsNullOrEmpty(Configuration.Default.TraceFileName) ? "C:\\temp\\ETW_%TS%_%COMPUTERNAME%.etl" : Configuration.Default.TraceFileName;  }
            set { Configuration.Default.TraceFileName = value; }
        }

        int _TraceFileCounter = 1;
        public int TraceFileCounter
        {
            get { return _TraceFileCounter; }
            set
            {
                SetProperty<int>(ref _TraceFileCounter, value);
            }
        }

        /// <summary>
        /// Trace output file name with optional Index appended but still not yet expanded environment variables
        /// </summary>
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

                if (Compress)
                {
                    lret = lret.Replace(".etl", ".7z");
                }
                return lret;
            }
        }

        /// <summary>
        /// Captured during trace stop of current viewmodel state to keep a copy 
        /// of relevant viewmodel properties around which can change later. E.g. trace output file name
        /// This is needed by the Open trace file button and other things.
        /// </summary>
        public ViewModelFrozenData StopData
        {
            get;
            set;
        }

        /// <summary>
        /// Fully expanded counted output file name
        /// </summary>
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
                SetProperty<bool>(ref _CaptureMouseButtonDown, value);
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
            set
            {
                RefreshProperty();
            }
        } 


        /// <summary>
        /// Local Web service hosting URL
        /// </summary>
        public string LocalTraceServiceUrl
        {
            get
            {
                return String.Format("http://localhost:{0}/TraceControlerService", WCFPort);
            }
            set
            {
                RefreshProperty();
            }
        }

        public const string CaptureScreenShotsProperty = "CaptureScreenShots";
        private const string StopButtonLabelDefault = "Sto_p Recording (F6)";
        private const string CancelButtonLabelDefault = "Cancel Recording";

        public bool _CaptureScreenShots;
        public bool CaptureScreenShots
        {
            get { return _CaptureScreenShots; }
            set { SetProperty<bool>(ref _CaptureScreenShots, value, CaptureScreenShotsProperty); }
        }
        

        public bool _CaptureMouseWheel;
        /// <summary>
        /// Check state of Checkbox
        /// </summary>
        public bool CaptureMouseWheel
        {
            get { return _CaptureMouseWheel; }
            set { SetProperty<bool>(ref _CaptureMouseWheel, value); }
        }


        public bool _CaptureMouseMove;
        /// <summary>
        /// Check state of Checkbox
        /// </summary>
        public bool CaptureMouseMove
        {
            get { return _CaptureMouseMove; }
            set { SetProperty<bool>(ref _CaptureMouseMove, value); }
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
        /// When true tracing is started/stopped/cancelled on local machine when one of the trace start/stop/cancel buttons is pressed.
        /// </summary>
        public bool LocalTraceEnabled
        {
            get { return _LocalTraceEnabled; }
            set { SetProperty<bool>(ref _LocalTraceEnabled, value); }
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
            set { SetProperty<bool>(ref _ServerTraceEnabled, value); }
        }


        bool _NetworkSendEnabled;
        /// <summary>
        /// Check state of Checkbox
        /// </summary>
        public bool NetworkSendEnabled
        {
            get { return _NetworkSendEnabled; }
            set { SetProperty<bool>(ref _NetworkSendEnabled, value); }
        }

        bool _StartButtonEnabled = true;
        public bool StartButtonEnabled
        {
            get { return _StartButtonEnabled; }
            set { SetProperty<bool>(ref _StartButtonEnabled, value); }
        }

        bool _StopButtonEnabled;
        public bool StopButtonEnabled
        {
            get { return _StopButtonEnabled; }
            set { SetProperty<bool>(ref _StopButtonEnabled, value); }
        }

        bool _CancelButtonEnabled;
        public bool CancelButtonEnabled
        {
            get { return _CancelButtonEnabled; }
            set { SetProperty<bool>(ref _CancelButtonEnabled, value); }
        }

        ObservableCollection<string> _ReceivedMessages;
        /// <summary>
        /// List of messages received from remote machines
        /// </summary>
        public ObservableCollection<string> ReceivedMessages
        {
            get { return _ReceivedMessages; }
            set { SetProperty<ObservableCollection<string>>(ref _ReceivedMessages, value); }
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
                SetProperty<string>(ref _StatusText, value); 
            }
        }

        string _StopButtonLabel = StopButtonLabelDefault;
        public string StopButtonLabel
        {
            get => _StopButtonLabel;
            set => SetProperty<string>(ref _StopButtonLabel, value);
        }

        string _CancelButtonLabel = CancelButtonLabelDefault;
        public string CancelButtonLabel
        {
            get => _CancelButtonLabel;
            set => SetProperty<string>(ref _CancelButtonLabel, value);
        }

        Brush _StatusColor;
        /// <summary>
        /// Current color of status text
        /// </summary>
        public Brush StatusColor
        {
            get { return _StatusColor; }
            set { SetProperty<Brush>(ref _StatusColor, value); }
        }

        string _SlowEventMessage;

        /// <summary>
        /// Message is logged when Slow event hot key was pressed to find out 
        /// </summary>
        public string SlowEventMessage
        {
            get { return _SlowEventMessage; }
            set { SetProperty<string>(ref _SlowEventMessage, value); }
        }


        string _FastEventMessage;

        /// <summary>
        /// Message is logged when Fast event hot key was pressed to find out 
        /// </summary>
        public string FastEventMessage
        {
            get { return _FastEventMessage; }
            set { SetProperty<string>(ref _FastEventMessage, value); }
        }

        string _SlowEventHotkey;

        /// <summary>
        /// Hotkey which is the stringified value of System.Windows.Input.Key or ETWController.MouseButton
        /// </summary>
        public string SlowEventHotkey
        {
            get { return _SlowEventHotkey;  }
            set { SetProperty<string>( ref _SlowEventHotkey, value); }
        }

        string _FastEventHotkey;
        public string FastEventHotkey
        {
            get { return _FastEventHotkey; }
            set { SetProperty<string>(ref _FastEventHotkey, value); }
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

        string _TraceOpenCmdLine;
        public string TraceOpenCmdLine
        {
            get { return _TraceOpenCmdLine; }
            set
            {
                SetProperty<string>(ref _TraceOpenCmdLine, value);
            }
        }

        protected bool _AlwaysShowCommandEditBoxes;
        public bool AlwaysShowCommandEditBoxes
        {
            get { return _AlwaysShowCommandEditBoxes; }
            set
            {
                SetProperty<bool>(ref _AlwaysShowCommandEditBoxes, value);
            }
        }

        string[] _TraceSessions;
        /// <summary>
        /// Local trace session names
        /// </summary>
        public string[] TraceSessions
        {
            get { return _TraceSessions; }
            set { SetProperty<string[]>(ref _TraceSessions, value); }
        }

        string[] _ServerTraceSessions;
        /// <summary>
        /// Remote trace session names
        /// </summary>
        public string[] ServerTraceSessions
        {
            get { return _ServerTraceSessions; }
            set { SetProperty<string[]>(ref _ServerTraceSessions, value); }
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

        /// <summary>
        /// Time when tracing was started. Needed to determine if new files were added when trace stop command was exected.
        /// </summary>
        DateTime _TraceStartTime;


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
                {"LogSlow", CreateCommand( _ =>  Hooker.LogSlowEvent())},
                {"LogFast", CreateCommand( _ => Hooker.LogFastEvent())},
                {"Config", CreateCommand( _ => ShowConfigDialog())},
                {"TraceRefresh", CreateCommand( _ =>
                {
                    TraceSessions = LocalTraceControler.GetTraceSessions();
                    WCFHost.GetTraceSessions.Execute();
                })},
                {"StartTracing", CreateCommand( _ => StartTracing()) },
                {"StopTracing", CreateCommand( _ => StopTracing(doChecks: true))},
                {"StopTracingUnconditionally", CreateCommand( _ => StopTracing(doChecks: false))},
                {"CancelTracing", CreateCommand( _ => CancelTracing())},
                {"RegisterETWProvider", CreateCommand( _ =>
                    {
                        string output = HookEvents.RegisterItself();
                        SetStatusMessage("Registering ETW provider: " + output);
                    })},
                {"ConfigReset", CreateCommand( _ => {
                    Configuration.Default.Reset();
                    Configuration.Default.Save();
                    LoadSettings();
                })},
                {"EnableButtons", CreateCommand( _ => {
                    StartButtonEnabled = true;
                    StopButtonEnabled = true;
                    CancelButtonEnabled = true;
                })},
                {"ShowMessages", CreateCommand( _ => ShowMessages() )},
                {"NetworkSendToggle", CreateCommand( _ => NetworkSendState.NetworkSendChangeState() )},
                {"EnableLocalTraceToggle", CreateCommand( _ => EnableLocalTraceToggle() )},
                {"EnableRemoteTraceToggle", CreateCommand( _ => EnableRemoteTraceToggle() )},
                {"NetworkReceiveToggle", CreateCommand( _ => NetworkReceiveState.NetworkReceiveChangeState() )},
                {"ClearStatusMessages", CreateCommand( _ => StatusMessages.Clear() )},
                {"ShowCommandLineOptions", CreateCommand( _ => ShowCommandLineOptions()) },
                {"About", CreateCommand( _ => AboutBox()) },

            };


            return commands;
        }

        internal static readonly string WelcomeText = "Hello, and welcome to ETW-Controller!" + Environment.NewLine + Environment.NewLine +
                                                      "In this version, the UI was massively changed to make it better usable, especially for new users." + Environment.NewLine +
                                                      "" + Environment.NewLine +
                                                      "By default, only the options for local ETW recording are shown now." + Environment.NewLine +
                                                      "If you want to use remote recording, you must enable it with 'Options->Enable remote machine ETW tracing'" + Environment.NewLine +
                                                      "or by pressing F8. With F7 you can toggle local tracing" + Environment.NewLine +
                                                      "" + Environment.NewLine +
                                                      "When you choose a given preset for local or remote recording, the individual" + Environment.NewLine +
                                                      "commands for starting and stopping are no longer shown in the UI to reduce clutter." + Environment.NewLine +
                                                      "To see or edit them, choose the '<Manual Editing>' entry in the preset dropdown." + Environment.NewLine +
                                                      "" + Environment.NewLine +
                                                      "The hotkeys for logging 'Slow' and 'Fast' messages are active, but not shown" + Environment.NewLine +
                                                      "in the UI to save screen real estate. To modify them, check the checkbox" + Environment.NewLine +
                                                      "\"Redefine 'Fast'/'Slow' hotkeys and messages\" on the main tab." + Environment.NewLine +
                                                      "" + Environment.NewLine +
                                                      "Good luck with all your ETW investigations!" + Environment.NewLine +
                                                      "" + Environment.NewLine;

        static readonly string CommandLineOptions = "ETWController [-Hide] [-CaptureKeyboard] [-CaptureMouseClick] " + Environment.NewLine +
                                                    "\t[-CaptureMouseMove] [-SendToServer Server [Port1 Port2]] [-ClearKeyboardEvents] [-RegisterEtwProvider]" + Environment.NewLine +
                                                    Environment.NewLine +
                                                    "\t-Hide                              Hide main window." + Environment.NewLine +
                                                    "\t-CaptureKeyboard                   Capture keyboard events." + Environment.NewLine +
                                                    "\t-ClearKeyboardEvents               By default the keys are all logged as SomeKey. If this is enabled the actual keyboard code is also logged." + Environment.NewLine +
                                                    "\t                                   Be careful that you do not enter your password while clear keyboard logging is enabled!" + Environment.NewLine +
                                                    "\t-CaptureMouseClick                 Capture muse click events." + Environment.NewLine +
                                                    "\t-CaptureMouseWheel                 Capture mouse wheel events. " + Environment.NewLine +
                                                    "\t-CaptureMouseMove                  Capture mouse mouse events. " + Environment.NewLine +
                                                   $"\t-DisableCaptureScreenshots         Disables the save a screenshot feature where for each mouse click to the directory {Configuration.Default.ScreenshotDirectory} or specify an explicit locaton with -ScreenshotDir xxx" + Environment.NewLine +
                                                    "\t-ScreenshotDir xxxx                Directory to where the screenshots are saved if -CaptureScreenshots is set" + Environment.NewLine  +
                                                    "\t-SendToServer Server [Port1 Port2] Enable sending events to remote server. If Port1/2 are omitted the configured ports are used. " + Environment.NewLine +
                                                    "\t-RegisterEtwProvider               Register the HookEvents ETW provider and then exit. This needs to be done only once e.g. during installation." + Environment.NewLine +
                                                    "\t-UnRegisterEtwProvider             Unregister the HookEvents ETW provider and then exit." + Environment.NewLine +
                                                    "\t" + Environment.NewLine +
                                                    "Example:" + Environment.NewLine +
                                                    "\tETWController.exe -capturemouseclick -capturekeyboard -sendtoserver localhost" + Environment.NewLine +
                                                    "This will eanble mouse click, encrypted keyboard tracing which will send to to your local machine again. If you want to hide the window you can add -hide." + Environment.NewLine + 
                                                    "These commands are useful if you only want to use ETWController as keyboard/mouse event logger but the ETW recording is performed by your own script/wpr profile." + Environment.NewLine;

        static readonly string About = Environment.NewLine +
                             $"ETWController (c) by Alois Kraus 2015-2022 v{Assembly.GetExecutingAssembly().GetName().Version}" + Environment.NewLine + Environment.NewLine +
                              "    With massive UI improvements made by Achim Bursian" + Environment.NewLine;
                                        
                                           

        private bool _useCommandNameSubstitutions = true;

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

        public IMessageBoxDisplay MessageBoxDisplay
        {
            get;
        }

        /// <summary>
        /// Initialize the default values for the ViewModel and load the settings from disc.
        /// </summary>
        public ViewModel(IMessageBoxDisplay messageBoxDisplay)
        {
            MessageBoxDisplay = messageBoxDisplay;
            var addonData = ReadAddonData();
            LocalTraceSettings = new TraceControlViewModel(this, false, addonData);
            ServerTraceSettings = new TraceControlViewModel(this, true, addonData);
            _ReceivedMessages = new ObservableCollection<string>();
            _ServerTraceSessions = new string[]{ "Not yet read."};
            _TraceSessions = new string[] {"Not yet read."};
            StatusMessages = new ObservableCollection<StatusMessage>();
            LocalTraceControler = new TraceControlerService();
            Commands = CreateUICommands();
            LoadSettings();
            StatusMessages.CollectionChanged += StatusMessages_CollectionChanged;
            if (TraceFileName.Contains("%TS%") || TraceFileName.Contains("%TIME%"))
            {
                AppendIndexToOutputFileName = false;
            }
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
        public void InitUIDependantVariables(App args, TaskScheduler scheduler)
        {
            UIScheduler = scheduler;
            NetworkSendState = new NetworkSendState(this, UIScheduler);
            NetworkReceiveState = new NetworkReceiveState(this, UIScheduler);
            WCFHost = new WCFHostServiceState(this, UIScheduler);
            Hooker = new NetworkedHooker(this);
            if( !HookEvents.IsAlreadyRegistered() )
            {
                Commands["RegisterETWProvider"].Execute(null);
            }

            // use settings from command line if present
            CaptureKeyboard = args.CaptureKeyboard;
            CaptureMouseButtonDown = args.CaptureMouseButtonDown;
            CaptureMouseWheel = args.CaptureMouseWheel;
            CaptureMouseMove = args.CaptureMouseMove;
            ScreenshotDirectoryUnexpanded = (args.ScreenshotDirectory ?? Configuration.Default.ScreenshotDirectory);

            CaptureScreenShots = args.CaptureScreenshots; // This line will also trigger a property change event which instantiates and starts the screenshotrecorder

            IsKeyBoardEncrypted = !args.IsKeyBoardNotEncrypted;

            if (args.SendToServer != null)
            {
                this.Host = args.SendToServer;
            }

            if (args.SendToServerPort != null)
            {
                if (int.TryParse(args.SendToServerPort, out int portNumber))
                {
                    this.PortNumber = portNumber;
                }
            }

            if (args.SendtoServerSecondaryPort != null)
            {
                if (int.TryParse(args.SendtoServerSecondaryPort, out int secondaryPort))
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
        /// Start Tracing
        /// </summary>
        private void StartTracing()
        {
            if (!StartButtonEnabled)
            {
                // prevent activation with hotkey, if button is disabled
                return;
            }

            if (!this.LocalTraceEnabled && !this.ServerTraceEnabled)
            {
                MessageBoxDisplay.ShowMessage("Please enable tracing at the remote host and/or on your local machine.", "Error");
                return;
            }

            if (this.ServerTraceEnabled && (Configuration.Default.Host == "localhost" || Configuration.Default.Host == "127.0.0.1"))
            {
                MessageBoxDisplay.ShowMessage($"Remote tracing needs a real remote address, not \"{Configuration.Default.Host}\"", "Error");
                return;
            }

            this.Hooker.ResetId();
            _TraceStartTime = DateTime.Now;
            Environment.SetEnvironmentVariable("DATENOW", _TraceStartTime.ToString("yyyy-MM-dd"));
            Environment.SetEnvironmentVariable("TIMENOW", _TraceStartTime.ToString("HHmmss"));
            Environment.SetEnvironmentVariable("TS", _TraceStartTime.ToString("yyyy-MM-dd_HHmmss"));

            CancelButtonLabel = !string.IsNullOrEmpty(LocalTraceSettings.SelectedPreset.TraceCancelLabel) ? LocalTraceSettings.SelectedPreset.TraceCancelLabel : CancelButtonLabelDefault;
            StopButtonLabel = !string.IsNullOrEmpty(LocalTraceSettings.SelectedPreset.TraceStopLabel) ? LocalTraceSettings.SelectedPreset.TraceStopLabel : StopButtonLabelDefault;

            if (this.LocalTraceEnabled) // start async to allow the web service to start tracing simultanously on the target host
            {
                LocalTraceSettings.TraceStates = TraceStates.Starting;

                if (File.Exists(TraceFileName))
                {
                    try
                    {
                        File.Delete(TraceFileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBoxDisplay.ShowMessage($"Could not delete old trace file {TraceFileName}. Is the file still open in WPA? Full error: {ex}", "Error");
                        LocalTraceSettings.TraceStates = TraceStates.Stopped;
                        return;
                    }
                }

                var wpaArgs = ApplyCommandSubstitutions(LocalTraceSettings.TraceStartFullCommandLine);
                LocalTraceSettings.AddLogEntry(wpaArgs);
                Task.Factory.StartNew<Tuple<int, string>>(() => { return LocalTraceControler.ExecuteWPRCommand(wpaArgs); })
                            .ContinueWith(t => LocalTraceSettings.ProcessStartCommand(t.Result), UIScheduler)
                            .ContinueWith((t) => UpdateMainButtons(), UIScheduler);


                // for safety, if this is a command that never returns from Start:
                // we fake the "Running" state after a certain amount of time
                var delaySec = 30;  // max time to stay in "Starting"
                if (wpaArgs.ToLowerInvariant().Contains("cmd.exe /c start"))
                {
                    // we know that this command will not return, so we enable the buttons after a short delay
                    delaySec = 5;
                }
                Task.Delay(TimeSpan.FromSeconds(delaySec))
                    .ContinueWith(t =>
                    {
                        if (LocalTraceSettings.TraceStates == TraceStates.Starting)
                        {
                            LocalTraceSettings.TraceStates = TraceStates.Running;
                            UpdateMainButtons();
                        }
                    }, UIScheduler);
            }

            CancelButtonEnabled = true;
            StartButtonEnabled = false;
            if (this.ServerTraceEnabled)
            {
                ServerTraceSettings.TraceStates = TraceStates.Starting;
                var finalCommandLine = ApplyCommandSubstitutions(ServerTraceSettings.TraceStartFullCommandLine);
                var command = WCFHost.CreateExecuteWPRCommand(finalCommandLine);
                command.Completed = (output) =>
                {
                    ServerTraceSettings.ProcessStartCommand(output);
                    UpdateMainButtons();
                };
                command.NotifyError = (s, exception) =>
                {
                    ServerTraceSettings.TraceStates = TraceStates.Stopped;
                    SetStatusMessageWarning(s, exception);
                    ServerTraceSettings.ProcessStartCommand(new Tuple<int, string>(1, "Error: " + s));
                    UpdateMainButtons();
                };
                ServerTraceSettings.AddLogEntry(finalCommandLine);
                command.Execute();
            }
        }

        private string ApplyCommandSubstitutions(string rawCommandLine)
        {
            var fullCommandLine = rawCommandLine;
            if (_useCommandNameSubstitutions)  // feature not finished yet
            {
                foreach (var nameSubstitution in Configuration.Default.CommandNameSubstitutions)
                {
                    var parts = nameSubstitution.Split(new[] { '|' }, 2);
                    if (parts.Length == 2 && fullCommandLine.StartsWith(parts[0] + " "))
                    {
                        fullCommandLine = parts[1] + fullCommandLine.Substring(parts[0].Length);
                        break;
                    }
                }
            }

            // Escape ! character to be able to call xxwpr script correctly
            fullCommandLine = fullCommandLine.Replace("!", "^!");

            return fullCommandLine;
        }


        /// <summary>
        /// stop tracing command 
        /// </summary>
        /// <param name="b"></param>
        internal void StopTracing(bool doChecks)
        {
            if (doChecks && !StopButtonEnabled)
            {
                // prevent activation with hotkey, if button is disabled
                return;
            }

            if (this.CaptureScreenShots) // create html report also if no tracing was active perhaps someone finds this functionality in itself useful
            {
                var htmlReportGenerator = new HtmlReportGenerator(this.ScreenshotDirectory);
                htmlReportGenerator.GenerateReport();
            }

            StopData = new ViewModelFrozenData(this)
            {
                TraceStopFullCommandLine = LocalTraceSettings.TraceStopFullCommandLine,
                TraceStopNotExpanded = LocalTraceSettings.TraceStop,
                TraceFileName = TraceFileName,
                TraceStartTime = this._TraceStartTime,
            };

            StopButtonEnabled = CancelButtonEnabled = false;
            StopButtonLabel = StopButtonLabelDefault;
            CancelButtonLabel = CancelButtonLabelDefault;

            var finalCommandLine = ApplyCommandSubstitutions(StopData.TraceStopFullCommandLine);
            if (this.LocalTraceEnabled) 
            {
                LocalTraceSettings.TraceStates = TraceStates.Stopping;
                // stop tracing asynchronously so we do not need to wait until local trace collection has stopped (while blocking the UI)
                LocalTraceSettings.AddLogEntry(finalCommandLine);
                Task.Factory.StartNew<Tuple<int, string>>(() => LocalTraceControler.ExecuteWPRCommand(finalCommandLine))
                            .ContinueWith((t) => LocalTraceSettings.ProcessStopCommand(t.Result), UIScheduler)
                            .ContinueWith((t) => UpdateMainButtons(), UIScheduler);
            }
            if (this.ServerTraceEnabled)
            {
                ServerTraceSettings.TraceStates = TraceStates.Stopping;
                var command = WCFHost.CreateExecuteWPRCommand(finalCommandLine);
                command.Completed = (output) =>
                {
                    ServerTraceSettings.ProcessStopCommand(output);
                    UpdateMainButtons();
                };
                command.NotifyError = (s, exception) =>
                {
                    ServerTraceSettings.TraceStates = TraceStates.Running;
                    SetStatusMessageWarning(s, exception);
                    ServerTraceSettings.ProcessStartCommand(new Tuple<int, string>(1, "Error: " + s));
                    UpdateMainButtons();
                };
                ServerTraceSettings.AddLogEntry(finalCommandLine);
                command.Execute();
            }
             
            this.TraceFileCounter++;
        }

        private void UpdateMainButtons()
        {
            StartButtonEnabled = LocalTraceSettings.TraceStates == TraceStates.Stopped && ServerTraceSettings.TraceStates == TraceStates.Stopped;
            StopButtonEnabled = (LocalTraceSettings.TraceStates == TraceStates.Running || ServerTraceSettings.TraceStates == TraceStates.Running); ;
            CancelButtonEnabled = (LocalTraceSettings.TraceStates != TraceStates.Stopped || ServerTraceSettings.TraceStates != TraceStates.Stopped);
        }

        /// <summary>
        /// Cancel Tracing command
        /// </summary>
        private void CancelTracing()
        {
            StopButtonEnabled = CancelButtonEnabled = false;
            StopButtonLabel = StopButtonLabelDefault;
            CancelButtonLabel = CancelButtonLabelDefault;
            if (this.LocalTraceEnabled)
            {
                var finalCommandLine = ApplyCommandSubstitutions(LocalTraceSettings.TraceCancel);
                LocalTraceSettings.AddLogEntry(finalCommandLine);
                var output = LocalTraceControler.ExecuteWPRCommand(finalCommandLine);
                LocalTraceSettings.ProcessCancelCommand(output);
            }

            if (this.ServerTraceEnabled)
            {
                var finalCommandLine = ApplyCommandSubstitutions(ServerTraceSettings.TraceCancel);
                var command = WCFHost.CreateExecuteWPRCommand(finalCommandLine);
                command.Completed = (output) => ServerTraceSettings.ProcessCancelCommand(output);
                ServerTraceSettings.AddLogEntry(finalCommandLine);
                command.Execute();
            }
            StartButtonEnabled = true;
        }

        private void WriteAddonData()
        {
            var fn = @"C:\temp\addondata.xml";
            var data = new AddonData();
            data.Presets = new[]
            {
                new Preset {Name = "Test name 1", TraceStartCommand = "StartCmd", TraceStopCommand = "Stop Command",
                    TraceCancelCommand = "Cancel Command", TraceStopLabel = "Stop Label", TraceCancelLabel = "Cancel Label"},
                new Preset {Name = "Test2", TraceStartCommand = "StartCmd2"},
            };
            TextWriter writer = new StreamWriter(fn);
            new XmlSerializer(typeof(AddonData)).Serialize(writer, data);
            writer.Close();
        }

        private AddonData ReadAddonData()
        {
            //WriteAddonData();  // only used to create initial file
            var fn = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\ETWController\AddonData.xml";
            if (File.Exists(fn))
            {
                var stream = new FileStream(fn, FileMode.Open);
                using (stream)
                {
                    var data = new XmlSerializer(typeof(AddonData)).Deserialize(stream) as AddonData;
                    return data;
                }   
            }
            return null;
        }

        private void ShowConfigDialog()
        {
            var dlg = new ETWControllerConfiguration(this);
            dlg.ShowDialog();
            // if needed, show/hide command textboxes:
            LocalTraceSettings.UpdateSelectedPreset();
            ServerTraceSettings.UpdateSelectedPreset();
        }

        /// <summary>
        /// Load current settings from app or user.config
        /// </summary>
        void LoadSettings()
        {
            if (Configuration.Default.ConfigMigrationNeeded)
            {
                Configuration.Default.Upgrade();
                Configuration.Default.ConfigMigrationNeeded = false;
                Configuration.Default.Save();
            }
            this.TraceOpenCmdLine = Configuration.Default.TraceOpenCmdLine;
            this.PortNumber = Configuration.Default.PortNumber;
            this.WCFPort = Configuration.Default.WCFPort;
            this.Host = Configuration.Default.Host;
            this.KeepNNewestScreenShots = Configuration.Default.KeepNNewestScreenShots;
            this.ForcedScreenshotIntervalinMs = Configuration.Default.ForcedScreenshotIntervalinMs;
            this.JpgCompressionLevel = Configuration.Default.JpgCompressionLevel;
            this.SlowEventHotkey = Configuration.Default.SlowEventHotkey;
            this.SlowEventMessage = Configuration.Default.SlowEventMessage;
            this.FastEventMessage = Configuration.Default.FastEventMessage;
            this.FastEventHotkey = Configuration.Default.FastEventHotkey;
            this.LocalTraceSettings.TraceStart = Configuration.Default.LocalTraceStart;
            this.LocalTraceSettings.TraceStop = Configuration.Default.LocalTraceStop;
            this.LocalTraceSettings.TraceCancel = Configuration.Default.LocalTraceCancel;
            this.LocalTraceSettings.UpdateSelectedPreset();
            this.ServerTraceSettings.TraceStart = Configuration.Default.ServerTraceStart;
            this.ServerTraceSettings.TraceStop = Configuration.Default.ServerTraceStop;
            this.ServerTraceSettings.TraceCancel = Configuration.Default.ServerTraceCancel;
            this.ServerTraceSettings.UpdateSelectedPreset();
            this.ServerTraceEnabled = Configuration.Default.ServerTraceEnabled;
            this.LocalTraceEnabled = Configuration.Default.LocalTraceEnabled;
            this.AlwaysShowCommandEditBoxes = Configuration.Default.AlwaysShowCommandEditBoxes;
        }

        /// <summary>
        /// Save changed settings to user.config
        /// </summary>
        internal void SaveSettings()
        {
            Configuration.Default.PortNumber = this.PortNumber;
            Configuration.Default.WCFPort = this.WCFPort;
            Configuration.Default.Host = this.Host;
            Configuration.Default.ScreenshotDirectory = ScreenshotDirectoryUnexpanded;
            Configuration.Default.KeepNNewestScreenShots = KeepNNewestScreenShots;
            Configuration.Default.TraceOpenCmdLine = TraceOpenCmdLine;
            Configuration.Default.ForcedScreenshotIntervalinMs = this.ForcedScreenshotIntervalinMs;
            Configuration.Default.JpgCompressionLevel = this.JpgCompressionLevel;
            Configuration.Default.SlowEventHotkey = this.SlowEventHotkey;
            Configuration.Default.SlowEventMessage = this.SlowEventMessage;
            Configuration.Default.FastEventMessage = this.FastEventMessage;
            Configuration.Default.FastEventHotkey = this.FastEventHotkey;
            Configuration.Default.CaptureKeyboard = this.CaptureKeyboard;
            Configuration.Default.CaptureMouseButtonDown = this.CaptureMouseButtonDown;
            Configuration.Default.CaptureMouseWheel = this.CaptureMouseWheel;
            Configuration.Default.CaptureMouseMove = this.CaptureMouseMove;
            Configuration.Default.CaptureScreenShots = this.CaptureScreenShots;
            Configuration.Default.LocalTraceStart = this.LocalTraceSettings.TraceStart;
            Configuration.Default.LocalTraceStop = this.LocalTraceSettings.TraceStop;
            Configuration.Default.ServerTraceStart = this.ServerTraceSettings.TraceStart;
            Configuration.Default.ServerTraceStop = this.ServerTraceSettings.TraceStop;
            Configuration.Default.LocalTraceEnabled = this.LocalTraceEnabled;
            Configuration.Default.ServerTraceEnabled = this.ServerTraceEnabled;
            Configuration.Default.LocalTraceCancel = LocalTraceSettings.TraceCancel;
            Configuration.Default.ServerTraceCancel = ServerTraceSettings.TraceCancel;
            Configuration.Default.TraceFileName = this.UnexpandedTraceFileName;
            Configuration.Default.AlwaysShowCommandEditBoxes = this.AlwaysShowCommandEditBoxes;
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

        private void EnableLocalTraceToggle()
        {
            LocalTraceEnabled = !LocalTraceEnabled;
        }

        private void EnableRemoteTraceToggle()
        {
            ServerTraceEnabled = !ServerTraceEnabled;
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
                this.SetStatusMessage(String.Format("Opened firewall for port {0}. {1}", this.WCFPort, ret.Result.Item2.Trim() == "Ok." ? "" : "Netsh output: " + ret.Result.Item2.Trim())), this.UIScheduler);

            Task.Factory.StartNew( () =>
             {
                DeleteFirewallRule(FirewallSocketRule);
                string openSocketPort =
                    String.Format("advfirewall firewall add Rule Name = \"{0}\" Dir = in action = allow protocol = TCP localport = {1}", FirewallSocketRule, this.PortNumber);
                var proc = new RedirectedProcess("netsh.exe", openSocketPort);
                return proc.Start();
             }).ContinueWith(( ret =>
                 this.SetStatusMessage(String.Format("Opened firewall for port {0}. {1}", this.PortNumber, ret.Result.Item2.Trim() == "Ok." ? "" : "Netsh output: " + ret.Result.Item2.Trim()))), this.UIScheduler);
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

    public class DesignTimeCustomerViewModel : ViewModel
    {

        public DesignTimeCustomerViewModel()
            : base(null)
        {

            LocalTraceEnabled = true;
            ServerTraceEnabled = true;
            _AlwaysShowCommandEditBoxes = true;
            _CaptureMouseButtonDown = true;
            _CaptureKeyboard = true;
        }
    }
}
