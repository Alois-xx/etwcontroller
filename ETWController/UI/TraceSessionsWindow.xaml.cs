using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    /// Interaction logic for TraceSessionsWindow.xaml
    /// </summary>
    public partial class TraceSessionsWindow : Window
    {
        private ViewModel myVm;
            
        public TraceSessionsWindow(ViewModel vm)
        {
            DataContext = vm;
            myVm = vm;
            InitializeComponent();
        }

        private void TraceSessionsWindow_OnInitialized(object sender, EventArgs e)
        {
            myVm.Commands["TraceRefresh"].Execute(null);
        }

        private void Refresh_OnClick(object sender, RoutedEventArgs e)
        {
            myVm.Commands["TraceRefresh"].Execute(null);
        }
    }
}
