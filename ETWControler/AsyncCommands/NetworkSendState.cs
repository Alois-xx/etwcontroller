using ETWControler.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ETWControler.Commands
{
    public class NetworkSendState
    {
        public MessageSender Sender
        {
            get;
            private set;
        }

        ViewModel Model;

        AsyncUICommand StartCommand
        {
            get;
            set;
        }

        AsyncUICommand StopCommand
        {
            get;
            set;
        }

        public NetworkSendState(ViewModel model)
        {
            Model = model;
        }

        public void RestartIfStarted()
        {
            if (SendEnabled)
            { 
                // Stop if sender is still running
                if (Sender != null || (StartCommand != null && StartCommand.ExecutionState == CommandState.Starting))
                {
                    CreateAndExecuteStopCommand();
                    StopCommand.MethodResult.ContinueWith((old) => CreateAndExecuteStartCommand(), TaskScheduler.FromCurrentSynchronizationContext());
                }
                else // Not running then start. This state can happen when we did error out because the host was wrong or not reachable. 
                     // Where only the stop command did complete but not the start part.
                {
                    CreateAndExecuteStartCommand();
                }
            }
        }

        bool SendEnabled = false;

        public void NetworkSendChangeState()
        {
            SendEnabled = !SendEnabled;

            if (SendEnabled )
            {
                if(StartCommand == null ||  (StartCommand != null && StartCommand.ExecutionState != CommandState.Starting) )
                {
                    CreateAndExecuteStartCommand();
                }
            }
            else
            {
                CreateAndExecuteStopCommand();
            }
        }

        void CreateAndExecuteStartCommand()
        {
            StartCommand = new AsyncUICommand(() =>
            {
                Sender = new MessageSender(Model.Host, Model.PortNumber, NetworkProtocolType.TCP);
            },
            Model)
            {
                Started =  String.Format("Connected to {0}", Model.Host),
                Starting = String.Format("Connecting to server {0}:{1} ...", Model.Host, Model.PortNumber),
                StartingError = String.Format("Failed to connect to server {0}:{1} ...", Model.Host, Model.PortNumber),
            };
            StartCommand.Execute();
        }

        void CreateAndExecuteStopCommand()
        {
            StopCommand = new AsyncUICommand(() =>
            {
                if( Sender != null)
                {
                    Sender.Dispose();
                    Sender = null;
                }
            },
            Model)
            {
                Started = String.Format("Closed connection to {0}", Model.Host),
                Starting = String.Format("Closing connection to {0}:{1}", Model.Host, Model.PortNumber),
                StartingError = String.Format("Error while closing connection to server {0}:{1} ...", Model.Host, Model.PortNumber),
            };
            StopCommand.Execute();
        }


    }
}
