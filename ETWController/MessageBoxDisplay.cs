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
            var captionExtended = "ETW Controller - " + caption;
            if (caption == "Error")
            {
                MessageBox.Show(message, captionExtended, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (caption == "Warning")
            {
                MessageBox.Show(message, captionExtended, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(message, captionExtended, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
