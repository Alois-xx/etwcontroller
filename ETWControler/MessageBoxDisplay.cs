using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ETWController
{
    class MessageBoxDisplay : IMessageBoxDisplay
    {
        public void ShowMessage(string message, string caption)
        {
            MessageBox.Show(message, caption);
        }
    }
}
