using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ETWControler.UI
{
    /// <summary>
    /// A wpa trace session can have the following states
    /// </summary>
    public enum TraceStates
    {
        Stopped,
        Starting,
        Running,
        Stopping
    }
}
