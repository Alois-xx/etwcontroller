using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading;

namespace ETWController
{
    /// <summary>
    /// Implemenation class of WCF ITraceControlerService
    /// </summary>
    public class TraceControlerService : ITraceControlerService
    {
        string ThisExeStartDirectory;

        /// <summary>
        /// Raised on the hosting (remote) instance when a remote ETWController client starts a WPR command via the WCF service.
        /// The argument is the executed command line. Used by the UI to disable the trace buttons and to show a status
        /// message while a remote command is running.
        /// </summary>
        public static event Action<string> RemoteWPRCommandStarted;

        /// <summary>
        /// Raised on the hosting (remote) instance when a remote initiated WPR command has finished.
        /// The argument is the executed command line.
        /// </summary>
        public static event Action<string> RemoteWPRCommandFinished;

        /// <summary>
        /// Used by unit tests
        /// </summary>
        public void DummyMethod()
        {
         //   Thread.Sleep(10 * 60 * 1000); // wait 10 minutes
         //   Console.WriteLine("Hi Server was reached");
        }

        /// <summary>
        /// Execute a WPR command with specified command line args. Used e.g. to start/stop/cancel tracing. 
        /// Commands can take a long time to execute. Currently the WCF service has a time of 10 minutes.
        /// </summary>
        /// <param name="wpaArgs">WPR command line args</param>
        /// <returns>stdout and stderr of executed command when it has finished.</returns>
        public Tuple<int,string> ExecuteWPRCommand(string wpaArgs)
        {
            // OperationContext.Current is only set when this method is dispatched through WCF, i.e. when a remote
            // ETWController instance triggered the command. Local calls (started from this instance) pass it as null
            // so that we do not disable the local buttons twice.
            bool isRemoteCall = OperationContext.Current != null;
            string commandLine = wpaArgs;
            if (isRemoteCall)
            {
                RemoteWPRCommandStarted?.Invoke(commandLine);
            }

            try
            {
                RedirectedProcess proc = null;
                wpaArgs = Environment.ExpandEnvironmentVariables(wpaArgs);
                if (wpaArgs.StartsWith("-"))
                {
                    // legacy: the command line does not contain the executable, assume wpr.exe
                    proc = new RedirectedProcess("wpr.exe", wpaArgs);
                }
                else 
                {
                    if (wpaArgs.StartsWith(ViewModel.CustomCommandPrefix))
                    {
                        proc = new RedirectedProcess("cmd.exe", $"/C {wpaArgs.Substring(ViewModel.CustomCommandPrefix.Length)}");
                    }
                    else
                    {
                        proc = new RedirectedProcess("cmd.exe", $"/C {wpaArgs}");
                    }
                }
                var lret = proc.Start(ThisExeStartDirectory);
                return lret;
            }
            finally
            {
                if (isRemoteCall)
                {
                    RemoteWPRCommandFinished?.Invoke(commandLine);
                }
            }
        }

        /// <summary>
        /// Get all active ETW sessions
        /// </summary>
        /// <returns>string array of with the trace session names</returns>
        public string[] GetTraceSessions()
        {
            return TraceEventSession.GetActiveSessionNames().OrderBy(n => n).ToArray();
        }

        public TraceControlerService()
        {
            ThisExeStartDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        }
    }
}
