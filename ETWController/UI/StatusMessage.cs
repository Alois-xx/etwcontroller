using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ETWController.UI
{
    public class StatusMessage
    {
        public string MessageText
        {
            get;
            set;
        }

        public string Message
        {
            get
            {
                return CreationTime.ToString("[HH:mm:ss.fff") + "] " + MessageText + (Data != null ? Data.ToString() : "");
            }
        }

        public object Data
        {
            get;
            set;
        }

        DateTime CreationTime;

        public StatusMessage()
        {
            CreationTime = DateTime.Now;
        }

    }
}
