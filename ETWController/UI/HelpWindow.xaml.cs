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
    /// Interaction logic for HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : Window
    {
        public HelpWindow(string caption, string helpText, bool bUseFixedFont=false)
        {
            InitializeComponent();
            if( bUseFixedFont)
            {
                cText.FontFamily = new FontFamily("Lucida Console");
                Width = 1200;
            }
            this.Title = caption;
            cText.AppendText(helpText);
        }
    }
}
