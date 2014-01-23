using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace ETWControler
{

    /// <summary>
    /// Redirect from the started process stderr and stdout
    /// </summary>
    public class RedirectedProcess
    {
        int StreamExited = 0;
        string Exe;
        string Args;
        StringBuilder sb = new StringBuilder();
        object Lock = new object();

        public RedirectedProcess(string exe, string args)
        {
            if( String.IsNullOrEmpty(exe))
            {
                throw new ArgumentException("exe");
            }

            Exe = exe;
            Args = args;
        }

        /// <summary>
        /// Start process with hidden window and stdout and stderr redirected.
        /// </summary>
        /// <returns>Tuple with return code and stdout and stderr output as one string in order of appearance</returns>
        public Tuple<int,string> Start()
        {
            return Start(null);
        }

        /// <summary>
        /// Start process with hidden window and stdout and stderr redirected.
        /// </summary>
        /// <param name="cwd">working directory. Can be null.</param>
        /// <returns>return code and written stdout and stderr lines in order of appearance</returns>
        public Tuple<int,string> Start(string cwd)
        {
            string output = null;
            int returnCode = 0;

            ProcessStartInfo info = new ProcessStartInfo(Exe, Args)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Minimized,
                WorkingDirectory = cwd,
                CreateNoWindow = true,
            };

            using(var p = Process.Start(info))
            {
                p.ErrorDataReceived += DataReceived;
                p.OutputDataReceived += DataReceived;
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                p.WaitForExit();
                returnCode = p.ExitCode;
                output = sb.ToString();
            }

            return new Tuple<int, string>(returnCode, output);
        }

        void DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                Interlocked.Increment(ref StreamExited);
            }
            lock(Lock)
            {
                sb.AppendLine(e.Data);
            }
        }
    }
}
