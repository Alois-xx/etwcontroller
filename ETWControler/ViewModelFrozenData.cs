using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWControler
{
    /// <summary>
    /// Captured ViewModel data at the point when the stop command was performed.
    /// </summary>
    public class ViewModelFrozenData
    {
        public string TraceFileName { get; internal set; }
        public string TraceStopFullCommandLine { get; internal set; }
    }
}
