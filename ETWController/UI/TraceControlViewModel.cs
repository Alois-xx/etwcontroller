﻿using System;
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
            set { SetProperty<string>(ref _TraceStop, value); }
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


        public string TraceStopFullCommandLine
        {
            get
            {
                string lret = TraceStop;
                lret = lret.Replace(TraceFileNameVariable, RootModel.UnexpandedCountedTraceFileName);
                lret = lret.Replace(TraceFileDirVariable,  GetDirectoryNameFromFileName(RootModel.UnexpandedCountedTraceFileName)); 
                lret = lret.Replace(ScreenShotVariable, RootModel.ScreenshotDirectory);
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

                // some day we might specify the output file already with the start command ... 
                string lret = TraceStart.Replace(TraceFileNameVariable, traceFileName);

                if (!lret.StartsWith(ETWController.ViewModel.CustomCommandPrefix)) // its still WPR
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



        public TraceControlViewModel(ETWController.ViewModel rootModel, bool isRemoteState)
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
                    AddLogEntry(ETWController.ViewModel.CustomCommandPrefix + exe + options, new Tuple<int, string>(0, ""), CommandOutputs);
                }
            }, 
            () => !IsRemoteState && RootModel.StopData != null && File.Exists(RootModel.StopData.TraceFileName)); // dynamically update the button enabled state if the output file does exist.
            _Presets.Add(new Preset{Name = "<Manual Editing>", NeedsManualEdit = true});
            _Presets.AddRange(Configuration.Default.Presets);
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
            AddLogEntry(RootModel.StopData.TraceStopFullCommandLine, wprCommandOutput, CommandOutputs);
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

            RootModel.StopData.VerifySuccessfulStop();


            TraceStates = TraceStates.Stopped;
        }

        /// <summary>
        /// Set trace state, add to log and show an error message box when tracing could not be started
        /// </summary>
        /// <param name="wprCommandOutput"></param>
        internal void ProcessStartCommand(Tuple<int, string> wprCommandOutput)
        {
            AddLogEntry(TraceStartFullCommandLine, wprCommandOutput, CommandOutputs);

            if (wprCommandOutput.Item1 == 0 && ! IsErrorOutput(wprCommandOutput.Item2))
            {
                TraceStates = TraceStates.Running;
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
                this.TraceStates = TraceStates.Stopped;
            }
            AddLogEntry(this.TraceCancel, wprCommandOutput, CommandOutputs);
        }

        void AddLogEntry(string command, Tuple<int, string> wprCommandResult, Collection<string> log)
        {
            // remove empty lines
            string[] strippedOutput = wprCommandResult.Item2.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            var resultString = string.Join(Environment.NewLine, strippedOutput.Where(x=>!string.IsNullOrEmpty(x)));
            string logMessage = string.Format("[{0}] {1}{2}{3}",
                                            DateTime.Now.ToString("HH:mm:ss.fff"),
                                            Environment.ExpandEnvironmentVariables(command.StartsWith(ViewModel.CustomCommandPrefix) ? 
                                                                                   command.Substring(ViewModel.CustomCommandPrefix.Length) : "wpr " + command),
                                            resultString == String.Empty ? String.Empty : Environment.NewLine,
                                            resultString);
            log.Add(logMessage);
        }
    }
}
