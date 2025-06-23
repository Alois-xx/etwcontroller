using ETWController.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWController
{
    /// <summary>
    /// Captured ViewModel data at the point when the stop command was performed.
    /// </summary>
    public class ViewModelFrozenData
    {
        
        public ViewModelFrozenData(ViewModel rootModel)
        {
            RootModel = rootModel;
        }

        public DateTime TraceStartTime { get; internal set; }

        private ViewModel RootModel { get; set; }

        /// <summary>
        /// Output trace file name full path
        /// </summary>
        public string TraceFileName { get; internal set; }

        /// <summary>
        /// Get input etl file name and not compressed archive. If necessary decompres compressed file.
        /// </summary>
        public string TraceFileNameNotCompressed 
        {
            get
            {
                if (RootModel.Compress)
                {
                    string uncompressed = Path.ChangeExtension(TraceFileName, ".etl");
                    if (!File.Exists(uncompressed))
                    {
                        DecompressWith7z(TraceFileName);
                    }

                    return uncompressed;
                }
                else
                {
                    return TraceFileName;
                }
            }
        }

         

        /// <summary>
        /// Decompresses the specified .7z archive to the same directory using 7z.exe.
        /// </summary>
        /// <param name="inputFile">The full path to the .7z archive.</param>
        /// <returns>True if decompression succeeded, false otherwise.</returns>
        public bool DecompressWith7z(string inputFile)
        {
            if (string.IsNullOrWhiteSpace(inputFile) || !File.Exists(inputFile))
                return false;

            string outputDir = Path.GetDirectoryName(inputFile);
            string sevenZipExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "7z.exe");

            var psi = new ProcessStartInfo
            {
                FileName = sevenZipExe,
                Arguments = $"x \"{inputFile}\" -o\"{outputDir}\" -y",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };

            try
            {
                using (var process = Process.Start(psi))
                {
                    var stdout = process.StandardOutput.ReadToEnd(); // Read output to avoid deadlocks
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                RootModel?.MessageBoxDisplay?.ShowMessage($"Failed to decompress file: {ex.Message}", "Error");
                return false;
            }
        }

        /// <summary>
        /// Expanded trace stop command line
        /// </summary>
        public string TraceStopFullCommandLine { get; internal set; }

        /// <summary>
        /// Not expanded trace stop command line
        /// </summary>
        public string TraceStopNotExpanded { get; internal set; }

        /// <summary>
        /// Validation checks when tracing was stopped. E.g. the output file was created if the command line did contain a $FileName parameter
        /// </summary>
        public bool VerifySuccessfulStop()
        {
            var retval = true;
            if( TraceStopNotExpanded.Contains(TraceControlViewModel.TraceFileNameVariable) )
            {
                if( !File.Exists(TraceFileName) )
                {
                    retval = false;
                    RootModel.MessageBoxDisplay.ShowMessage($"Expected output file '{RootModel.StopData.TraceFileName}' was not found!", "Error");
                }
            }
            else if( TraceStopNotExpanded.Contains(TraceControlViewModel.TraceFileDirVariable) )
            {
                string outputDir = TraceControlViewModel.GetDirectoryNameFromFileName(TraceFileName);

                if (!Directory.Exists(outputDir))
                {
                    retval = false;
                    RootModel.MessageBoxDisplay.ShowMessage($"Output directory '{outputDir}' does not exist", "Error");
                }
                else
                {
                    int newEtlFiles = Directory.GetFiles(outputDir, "*.etl").Select(x => new FileInfo(x)).Where(x => x.CreationTime > TraceStartTime).Count();

                    if (newEtlFiles == 0)
                    {
                        retval = false;
                        RootModel.MessageBoxDisplay.ShowMessage($"No new *.etl files were created in directory '{outputDir}' since trace start!", "Error");
                    }
                }
            }

            return retval;
        }
    }
}
