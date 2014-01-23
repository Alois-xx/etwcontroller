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

namespace ETWControler.UI
{
    /// <summary>
    /// Interaction logic for StatusMessages.xaml
    /// </summary>
    public partial class StatusMessages : Window
    {
        ViewModel Model;
        public StatusMessages(ViewModel model)
        {
            Model = model;
            DataContext = Model;
            InitializeComponent();
            CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, SelectAll));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, (a,b) => CopyCommand(a,b)));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, (a,b) => Model.Commands["ClearStatusMessages"].Execute(null) ));
            // Focus is needed to make copy command bindings work to put the context menu into the current visual tree by 
            // setting as top level window the current window.
            // http://wpftutorial.net/Menus.html
            Focus();
        }

        private void SelectAll(object sender, ExecutedRoutedEventArgs e)
        {
            cList.SelectAll();
        }

        private void CopyCommand(object sender, ExecutedRoutedEventArgs e)
        {
            string msg = String.Join(Environment.NewLine, cList.SelectedItems.OfType<StatusMessage>().Select(x => x.Message));

            // SetText fails with an exception: http://stackoverflow.com/questions/12769264/openclipboard-failed-when-copy-pasting-data-from-wpf-datagrid
            Clipboard.SetDataObject(msg);
            
        }
    }
}
