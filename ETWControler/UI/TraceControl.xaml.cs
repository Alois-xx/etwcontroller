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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ETWControler.UI
{
    /// <summary>
    /// Interaction logic for TraceControl.xaml
    /// </summary>
    public partial class TraceControl : UserControl
    {
        TraceControlViewModel Model;

        public TraceControl()
        {
            InitializeComponent();
            this.DataContextChanged += TraceControl_DataContextChanged;
            Running =  (Storyboard)cTraceState.TryFindResource("TraceRunningAnimation");
            Starting = (Storyboard)cTraceState.TryFindResource("TraceStartingAnimation");
            Stopping = (Storyboard)cTraceState.TryFindResource("TraceStoppingAnimation");
        }

        void TraceControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var model = e.NewValue as TraceControlViewModel;
            if( model != null )
            {
                model.PropertyChanged += model_PropertyChanged;
                Model = model;
            }
        }

        void model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TraceStates")
            {

                switch(Model.TraceStates)
                {
                    case TraceStates.Running:
                        Running.Begin(cTraceState,true);
                        Starting.Stop(cTraceState);
                        Stopping.Stop(cTraceState);
                        
                        break;
                    case TraceStates.Starting:
                        Starting.Begin(cTraceState,true);
                        Stopping.Stop(cTraceState);
                        Running.Stop(cTraceState);
                        
                        break;
                    case TraceStates.Stopping:
                        Stopping.Begin(cTraceState,true);
                        Running.Stop(cTraceState);
                        Starting.Stop(cTraceState);
                        break;
                    case TraceStates.Stopped:
                    default:
                        Starting.Stop(cTraceState);
                        Stopping.Stop(cTraceState);
                        Running.Stop(cTraceState);
                        break;
                }
            }
        }

        Storyboard Running
        {
            get; set;
        }

        Storyboard Starting
        {
            get; set;
        }

        Storyboard Stopping
        {
            get; set;
        }
    }
}
