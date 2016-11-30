using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ETWController.UI
{
    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>
    public partial class OutputWindow : Window
    {
        public OutputWindow()
        {
            InitializeComponent();
        }

        private void ClearAllMessages(object sender, RoutedEventArgs e)
        {
            TraceControlViewModel model = (TraceControlViewModel) this.DataContext;
            if( model != null )
            {
                model.CommandOutputs.Clear();
            }
        }

        void CopyMessage(object sender, RoutedEventArgs e)
        {
            TraceControlViewModel model = (TraceControlViewModel) this.DataContext;
            if (model != null)
            {
                if( cView.SelectedValue != null)
                {
                    Clipboard.SetText(cView.SelectedValue.ToString());
                }
            }
        }
    }
}
