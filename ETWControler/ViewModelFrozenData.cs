using ETWController.UI;
using System;
using System.Collections.Generic;
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
        public void VerifySuccesfulStop()
        {
            if( TraceStopNotExpanded.Contains(TraceControlViewModel.TraceFileNameVariable) )
            {
                if( !File.Exists(TraceFileName) )
                {
                    RootModel.MessageBoxDisplay.ShowMessage($"Error: Output file {RootModel.StopData.TraceFileName} was not found!", "Error");
                }
            }
            else if( TraceStopNotExpanded.Contains(TraceControlViewModel.TraceFileDirVariable) )
            {
                string outputDir = TraceControlViewModel.GetDirectoryNameFromFileName(TraceFileName);

                if (!Directory.Exists(outputDir))
                {
                    RootModel.MessageBoxDisplay.ShowMessage($"Error: Output directory {outputDir} does not exist", "Error");
                }
                else
                {
                    int newEtlFiles = Directory.GetFiles(outputDir, "*.etl").Select(x => new FileInfo(x)).Where(x => x.CreationTime > TraceStartTime).Count();

                    if (newEtlFiles == 0)
                    {
                        RootModel.MessageBoxDisplay.ShowMessage($"Error: No new etl files were created in directory {outputDir} since trace start!", "Error");
                    }
                }
            }
        }
    }
}
