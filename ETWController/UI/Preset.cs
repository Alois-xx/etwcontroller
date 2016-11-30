using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWController.UI
{
    public class Preset
    {
        public string Name
        {
            get;set;
        }

        public string TraceStartCommand { get; set; }
        public string TraceStopCommand  { get; set;  }
        public string TraceCancelCommand { get; set;  }

        public override string ToString()
        {
            return Name;
        }
    }
}
