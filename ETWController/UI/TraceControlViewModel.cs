using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace ETWController.UI
{
    public class LogEntry
    {
        public DateTimeOffset Timestamp { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public string Command { get; set; }
        public string Output { get; set; }
        public bool HasError { get; set; }
        public bool HasFinished => Output != null;
        public string EntryKind => HasFinished ? (HasError ? "Error" : "Done") : "Start";

        public override string ToString()
        {
            return string.Format("[{0:HH:mm:ss.fff}] {1}{2}{3}", Timestamp,
                Environment.ExpandEnvironmentVariables(Command.StartsWith("-") ? "wpr " + Command : Command),
                Output == String.Empty ? String.Empty : Environment.NewLine,
                Output);
        }
    }

    /// <summary>
    /// ViewModel for TraceControl which contains the start/stop command line args, the current trace state 
    /// and the command outputs.
    /// This view model is used for both the TraceControl control and the TraceStatusDisplay class!
    /// </summary>
    public class TraceControlViewModel : NotifyBase
    {
        const int Wpr_Code_NoTraceRunning = -984076288;

        OutputWindow OutputWindow;

        ETWController.ViewModel RootModel;

        internal const string TraceFileNameVariable = "$FileName";
        internal const string TraceFileDirVariable = "$FileDirectory";
        internal const string ScreenShotVariable = "$ScreenshotDir";

        string _TraceStart;
        /// <summary>
        /// WPR trace start command line arguments
        /// </summary>
        public string TraceStart
        {
            get { return _TraceStart; }
            set { SetProperty<string>(ref _TraceStart, value); }
        }

        private List<Preset> _Presets = new List<Preset>();
        public Preset[] Presets
        {
            get { return _Presets.ToArray(); }
        }

        public class ETWEventFilter
        {
            public string Name { get; set; }
            public bool IsSelected { get; set; }

            public override string ToString()
            {
                return $"{Name} {IsSelected}";
            }
        }

        /// <summary>
        /// Until xperf/wpr support handle type filtering we need to enable it on our own.
        /// </summary>
        internal static ETWHandleTypeFilter _HandleETWFilterController = new ETWHandleTypeFilter();

        List<ETWEventFilter> _EventFilters = new List<ETWEventFilter>(_HandleETWFilterController.GetFilterNames().Select(x=> new ETWEventFilter {  Name = x}));

        /// <summary>
        /// Used by UI to display list in ComboBox with checkboxes
        /// </summary>
        public ETWEventFilter[] EventFilters
        {
            get => _EventFilters.ToArray();
        }

        /// <summary>
        /// We do not support handle type filters on remote node so we disable it by default and enable it only for local recordings.
        /// </summary>
        public Visibility HandleTypeFilterEnabled
        {
            get; set;
        } = Visibility.Collapsed;

        Preset _SelectedPreset = null;
        public Preset SelectedPreset
        {
            get { return _SelectedPreset; }
            set
            {
                SetProperty<Preset>(ref _SelectedPreset, value);
                if( value != null)
                {
                    if (value.ContainsData)
                    {
                        TraceStart = value.TraceStartCommand;
                        TraceStop = value.TraceStopCommand;
                        TraceCancel = value.TraceCancelCommand;
                        IsCustomSetting = value.NeedsManualEdit || Configuration.Default.AlwaysShowCommandEditBoxes;
                    }
                    else
                    {
                        IsCustomSetting = true;
                    }
                }
            }
        }

        string _TraceStop;
        /// <summary>
        /// WPR trace stop command line arguments
        /// </summary>
        public string TraceStop
        {
            get { return _TraceStop; }
            set 
            { 
                if( value != null)
                {
                    if (value.IndexOf("wpr", StringComparison.OrdinalIgnoreCase) == -1)
                    {
                        RootModel.IsSkipPdbEnabled = false;
                    }
                    else
                    {
                        RootModel.IsSkipPdbEnabled = true;
                    }
                }
                SetProperty<string>(ref _TraceStop, value); 
            }
        }

        public bool _IsCustomSetting;
        public bool IsCustomSetting
        {
            get { return _IsCustomSetting; }
            set
            {
                SetProperty<bool>(ref _IsCustomSetting, value);
            }
        }

        public bool _AutoOpenAfterStopped = false;
        public bool AutoOpenAfterStopped
        {
            get { return _AutoOpenAfterStopped; }
            set
            {
                SetProperty<bool>(ref _AutoOpenAfterStopped, value);
            }
        }
        public string StatusPrefix => IsRemoteState ? "Remote Recording:" : "Local Recording:";

        public void UpdateSelectedPreset()
        {
            bool found = false;
            foreach (var preset in Presets)
            {
                if (preset.ContainsData
                    && preset.TraceStartCommand.Trim() == TraceStart.Trim()
                    && preset.TraceStopCommand.Trim() == TraceStop.Trim()
                    && preset.TraceCancelCommand.Trim() == TraceCancel.Trim())
                {
                    SelectedPreset = preset;
                    found = true;
                    break;
                }
            }

            if (found)
            {
                IsCustomSetting = SelectedPreset.NeedsManualEdit || Configuration.Default.AlwaysShowCommandEditBoxes;
            }
            else
            {
                IsCustomSetting = true;
                SelectedPreset = _Presets[0];
            }
        }

        /// <summary>
        /// wpr and xxwpr wrapper supported flag
        /// </summary>
        const string SkipPdbArg = "-skipPdbGen";


        /// <summary>
        /// Expand variables and add SkipPdb option when selected to stop command line
        /// </summary>
        public string TraceStopFullCommandLine
        {
            get
            {
                string lret = TraceStop;
                lret = lret.Replace(TraceFileNameVariable, RootModel.UnexpandedCountedTraceFileName);
                lret = lret.Replace(TraceFileDirVariable,  GetDirectoryNameFromFileName(RootModel.UnexpandedCountedTraceFileName)); 
                lret = lret.Replace(ScreenShotVariable, RootModel.ScreenshotDirectory);

                // Append skip pdb argument to wpr/xxwpr stop command
                if(RootModel.IsSkipPdbEnabled &&  RootModel.IsSkipPDB && lret.IndexOf(SkipPdbArg) == -1 )
                {
                    lret += " " + SkipPdbArg;
                }
                return lret;
            }
        }

        static internal string GetDirectoryNameFromFileName(string fileName)
        {
            return Path.GetDirectoryName(fileName);
        }


        string _TraceCancel;
        /// <summary>
        /// WPR or script command line 
        /// </summary>
        public string TraceCancel
        {
            get { return _TraceCancel; }
            set { SetProperty<string>(ref _TraceCancel, value); }
        }

        /// <summary>
        /// Add our own event provider to all start commands so we have them in the event stream by default
        /// </summary>
        public string TraceStartFullCommandLine
        {
            get
            {
                string traceFileName = RootModel.UnexpandedCountedTraceFileName;

                string commandLine = TraceStart.Replace(TraceFileNameVariable, traceFileName);

                if (commandLine.StartsWith("-")) // its still WPR, no command at start, only the args
                {
                    string ownManifest = Path.Combine("ETW", "HookEvents.wprp");

                    if (!commandLine.ToLowerInvariant().Contains("hookevents.wprp"))
                    {
                        commandLine += " -start " + ownManifest;
                    }
                }
                return commandLine;
            }
        }
        TraceStates _TraceStates;
        /// <summary>
        /// Contains the current trace UI state. If all works well and the process is not terminated in between
        /// the UI does reflect the current system tracing state. 
        /// </summary>
        public TraceStates TraceStates
        {
            get { return _TraceStates; }
            set { SetProperty<TraceStates>(ref _TraceStates, value, "TraceStates"); }
        }

        /// <summary>
        /// Command line output from each executed command line
        /// </summary>
        public ObservableCollection<LogEntry> CommandOutputs
        {
            get;
            set;
        }

        /// <summary>
        /// Show from all WPR command calls the output
        /// </summary>
        public DelegateCommand ShowCommand
        {
            get;set;
        }

        /// <summary>
        /// Open an already saved etl file with WPR
        /// </summary>
        public DelegateCommand OpenTraceCommand
        {
            get; set;
        }

        /// <summary>
        /// If true it contains the trace state of the remote machine.
        /// If false this instance is the localhost trace state
        /// </summary>
        public bool IsRemoteState
        {
            get;
            private set;
        }

        public bool IsLocalState => !IsRemoteState;



        public TraceControlViewModel(ViewModel rootModel, bool isRemoteState, AddonData addonData)
        {
            RootModel = rootModel;
            IsRemoteState = isRemoteState;
            CommandOutputs = new ObservableCollection<LogEntry>();

            ShowCommand = new DelegateCommand((o) =>
                {
                    if (OutputWindow == null )
                    {
                        OutputWindow = new OutputWindow();
                        OutputWindow.Title += IsRemoteState ? " (Server)" : " (Local)";
                        OutputWindow.DataContext = this;
                        OutputWindow.Closed += (a, b) => OutputWindow = null;
                    }
                    OutputWindow.Show();
                    OutputWindow.Activate();
                });

            OpenTraceCommand = new DelegateCommand((o) =>
            {
                string outFile = RootModel.StopData.TraceFileName;
                if (outFile != null)
                {
                    var exe = ExtractExecName(RootModel.TraceOpenCmdLine);
                    var options = RootModel.TraceOpenCmdLine.Substring(exe.Length);
                    options = Environment.ExpandEnvironmentVariables(options.Replace(TraceFileNameVariable, outFile));
                    var startInfo = new ProcessStartInfo(exe,  options)
                    {
                        WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        UseShellExecute = false
                    };
                    Process.Start(startInfo);
                    AddLogEntry(exe + options);
                }
            }, 
            () => IsLocalState && RootModel.StopData != null && File.Exists(RootModel.StopData.TraceFileName)); // dynamically update the button enabled state if the output file does exist.
            _Presets.Add(new Preset{Name = "<Manual Editing>", NeedsManualEdit = true});
            if (addonData != null)
            {
                _Presets.AddRange(addonData.Presets);
            }
            if (addonData == null || !addonData.HideStandardPresets)
            {
                _Presets.AddRange(Configuration.Default.Presets);
            }

            foreach (var preset in Presets)
            {
                // heuristic: if name or command contains "xxx" it must be edited by hand
                if (preset.ContainsData && (preset.TraceStartCommand.Contains("xxx") || preset.Name.Contains("xxx")))
                {
                    preset.NeedsManualEdit = true;
                }
            }
        }

        /// <summary>
        /// Extract from a command line string the executable name which can be quoted or not.
        /// </summary>
        /// <param name="cmdLine"></param>
        /// <returns></returns>
        internal static string ExtractExecName(string cmdLine)
        {
            Func<char, bool> charFilter = ch => !Char.IsWhiteSpace(ch);

            // treat exe names with quotation marks which can contain spaces correctly
            if(cmdLine.StartsWith("\""))
            {
                int charCount = 0;
                bool bMatch = true;
                charFilter = ch =>
                {
                    bMatch = charCount < 2;

                    if (ch == '"')
                    {
                        charCount++;
                    }

                    return bMatch;
                };
            }

            var exe = new string(cmdLine.TakeWhile(charFilter).ToArray());
            return exe;
        }
        /// <summary>
        /// Set trace state, add to log and show an error message box when tracing could not be stopped.
        /// </summary>
        /// <param name="wprCommandOutput"></param>
        internal void ProcessStopCommand(Tuple<int, string> wprCommandOutput)
        {
            AddLogEntry(RootModel.StopData.TraceStopFullCommandLine, wprCommandOutput);
            OpenTraceCommand.RaiseCanExecuteChanged();
            if ((wprCommandOutput.Item1 == 0 || wprCommandOutput.Item1 == Wpr_Code_NoTraceRunning) && !IsErrorOutput(wprCommandOutput.Item2)) 
            {
                // Trace not running
            }
            else
            {
                TraceStates = TraceStates.Stopped;
                RootModel.MessageBoxDisplay.ShowMessage($"Error occured: {wprCommandOutput.Item2}", "Error");
            }

            var isSuccessful = RootModel.StopData.VerifySuccessfulStop();
            if (isSuccessful && IsLocalState && AutoOpenAfterStopped)
            {
                OpenTraceCommand.Execute(null);
            }
            TraceStates = TraceStates.Stopped;
        }

        /// <summary>
        /// Set trace state, add to log and show an error message box when tracing could not be started
        /// </summary>
        /// <param name="wprCommandOutput"></param>
        internal void ProcessStartCommand(Tuple<int, string> wprCommandOutput)
        {
            var entry = AddLogEntry(TraceStartFullCommandLine, wprCommandOutput);

            if (!entry.HasError)
            {
                if (TraceStates == TraceStates.Starting)
                {
                    TraceStates = TraceStates.Running;
                }
            }
            else
            {
                TraceStates = TraceStates.Stopped;
                RootModel.MessageBoxDisplay.ShowMessage($"Error occured: {wprCommandOutput.Item2}", "Error");
            }
        }

        private bool IsErrorOutput(string txt)
        {
            // TODO: make error patterns configurable in config file (?) [2022-01-11]
            if (txt.Contains("Invalid command")
            || txt.Contains("Error code: ")
            || txt.Contains("Error:")
            || txt.Contains("execute the script with administrative rights")
            || txt.Contains("Invalid Scenario:")
            || txt.Contains("Invalid command line argument")
            || txt.Contains("Merge Error.")
            || txt.Contains("file access error during merge")
            || txt.Contains("xperf: error"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Set trace state and add to log
        /// </summary>
        /// <param name="wprCommandOutput"></param>
        internal void ProcessCancelCommand(Tuple<int, string> wprCommandOutput)
        {
            if (wprCommandOutput.Item1 == 0 || wprCommandOutput.Item1 == Wpr_Code_NoTraceRunning) // either it was cancelled or no session was running 
            {
                TraceStates = TraceStates.Stopped;
            }
            AddLogEntry(TraceCancel, wprCommandOutput);
        }

        public LogEntry AddLogEntry(string command, Tuple<int, string> wprCommandResult)
        {
            // remove empty lines
            string[] strippedOutput = wprCommandResult.Item2.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var resultString = string.Join(Environment.NewLine, strippedOutput.Where(x=>!string.IsNullOrEmpty(x)));
            if (string.IsNullOrWhiteSpace(resultString))
            {
                resultString = "[no output from command]";
            }
            var logEntry = new LogEntry{Command = command, Output = resultString, 
                HasError = wprCommandResult.Item1 != 0 || IsErrorOutput(wprCommandResult.Item2), 
                Timestamp = DateTimeOffset.Now
            };
            CommandOutputs.Add(logEntry);
            return logEntry;
        }

        public LogEntry AddLogEntry(string command)
        {
            var logEntry = new LogEntry{Command = command, Output = null, 
                HasError = false, Timestamp = DateTimeOffset.Now
            };
            CommandOutputs.Add(logEntry);
            return logEntry;
        }
    }
}
