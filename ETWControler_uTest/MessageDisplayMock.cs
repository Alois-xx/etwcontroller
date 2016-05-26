using ETWControler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWControler_uTest
{
    class MessageDisplayMock : IMessageBoxDisplay
    {
        public void ShowMessage(string message, string caption)
        {
            OnMessage?.Invoke(message, caption);
        }

        public event Action<string, string> OnMessage;
    }
}
