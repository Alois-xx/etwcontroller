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

namespace ETWControler.UI
{
    /// <summary>
    /// ViewModel for TraceControl which contains the start/stop command line args, the current trace state 
    /// and the command outputs
    /// </summary>
    public class TraceControlViewModel : NotifyBase
    {
        const int Wpr_Code_NoTraceRunning = -984076288;

        OutputWindow OutputWindow;

        ViewModel RootModel;

        internal const string TraceFileNameVariable = "$FileName";
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


        public Preset[] Presets
        {
            get { return Configuration.Default.Presets; }
        }

        Preset _Preset = null;
        public Preset SelectedPreset
        {
            get { return _Preset; }
            set
            {
                SetProperty<Preset>(ref _Preset, value);
                if( value != null)
                {
                    TraceStart = value.TraceStartCommand;
                    TraceStop = value.TraceStopCommand;
                    TraceCancel = value.TraceCancelCommand;
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
            set { SetProperty<string>(ref _TraceStop, value); }
        }

        public string TraceStopFullCommandLine
        {
            get
            {
                string lret = TraceStop;
                lret = lret.Replace(TraceFileNameVariable, RootModel.UnexpandedCountedTraceFileName);
                lret = lret.Replace(ScreenShotVariable, RootModel.ScreenshotDirectory);
                return lret;
            }
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

                // some day we might specify the output file already with the start command ... 
                string lret = TraceStart.Replace(TraceFileNameVariable, traceFileName);

                if (!lret.StartsWith(ViewModel.CustomCommandPrefix)) // its still WPR
                {
                    string ownManifest = Path.Combine("ETW", "HookEvents.wprp");
                    lret += " -start " + ownManifest;
                }
                return lret;
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
        public ObservableCollection<string> CommandOutputs
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



        public TraceControlViewModel(ViewModel rootModel, bool isRemoteState)
        {
            RootModel = rootModel;
            IsRemoteState = isRemoteState;
            CommandOutputs = new ObservableCollection<string>();

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
                        WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                    };
                    Process.Start(startInfo);
                    AddLogEntry(ViewModel.CustomCommandPrefix + exe + options, new Tuple<int, string>(0, ""), CommandOutputs);
                }
            }, 
            () => !IsRemoteState && RootModel.StopData != null && File.Exists(RootModel.StopData.TraceFileName)); // dynamically update the button enabled state if the output file does exist.
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
            AddLogEntry(RootModel.StopData.TraceStopFullCommandLine, wprCommandOutput, CommandOutputs);
            OpenTraceCommand.RaiseCanExecuteChanged();
            if (wprCommandOutput.Item1 == 0 || wprCommandOutput.Item1 == Wpr_Code_NoTraceRunning) 
            {
                // Trace not running
            }
            else
            {
                RootModel.MessageBoxDisplay.ShowMessage($"Error occured: {wprCommandOutput.Item2}", "Error");
            }

            if( !File.Exists(RootModel.StopData.TraceFileName))
            {
                RootModel.MessageBoxDisplay.ShowMessage($"Error: Output file {RootModel.StopData.TraceFileName} was not found!", "Error");
            }

            TraceStates = TraceStates.Stopped;
        }

        /// <summary>
        /// Set trace state, add to log and show an error message box when tracing could not be started
        /// </summary>
        /// <param name="wprCommandOutput"></param>
        internal void ProcessStartCommand(Tuple<int, string> wprCommandOutput)
        {
            AddLogEntry(TraceStartFullCommandLine, wprCommandOutput, CommandOutputs);

            if (wprCommandOutput.Item1 == 0)
            {
                TraceStates = TraceStates.Running;
            }
            else
            {
                RootModel.MessageBoxDisplay.ShowMessage($"Error occured: {wprCommandOutput.Item2}", "Error");
            }
        }

        /// <summary>
        /// Set trace state and add to log
        /// </summary>
        /// <param name="wprCommandOutput"></param>
        internal void ProcessCancelCommand(Tuple<int, string> wprCommandOutput)
        {
            if (wprCommandOutput.Item1 == 0 || wprCommandOutput.Item1 == Wpr_Code_NoTraceRunning) // either it was cancelled or no session was running 
            {
                this.TraceStates = TraceStates.Stopped;
            }
            AddLogEntry(this.TraceCancel, wprCommandOutput, CommandOutputs);
        }

        void AddLogEntry(string command, Tuple<int, string> wprCommandResult, Collection<string> log)
        {
            // remove empty lines
            string[] strippedOutput = wprCommandResult.Item2.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            string logMessage = String.Format("{0}: {1}{2}{3}", 
                                            DateTime.Now.ToString("hh:mm:ss.fff"),
                                            Environment.ExpandEnvironmentVariables(command.StartsWith(ViewModel.CustomCommandPrefix) ? 
                                                                                   command.Substring(ViewModel.CustomCommandPrefix.Length) : "wpr " + command), 
                                            Environment.NewLine,
                                            String.Join(Environment.NewLine, strippedOutput.Where(x=>!String.IsNullOrEmpty(x))));
            log.Add(logMessage);
        }
    }
}
