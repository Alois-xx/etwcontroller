using ETWControler.ETW;
using ETWControler.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETWControler.Commands
{
    public class NetworkReceiveState
    {
        public MessageReceiver Receiver
        {
            get;
            private set;
        }

        ConcurrentQueue<string> ReceivedMessages = new ConcurrentQueue<string>();
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

        public void Restart()
        {
            if (StopCommand == null || (StopCommand != null && StopCommand.ExecutionState != CommandState.Starting))
            {
                CreateAndExecuteStopCommand();
                StopCommand.MethodResult.ContinueWith(old => CreateAndExecuteStartCommand(), TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                CreateAndExecuteStartCommand();
            }

        }

        void CreateAndExecuteStartCommand()
        {
            StartCommand = new AsyncUICommand(() => 
                {
                    Receiver = new MessageReceiver(Model.PortNumber, NetworkProtocolType.TCP);
                    Receiver.OnMessageReceived += Receiver_OnMessageReceived;
                },
                                                    Model)
            {
                Started = String.Format("Server started at port {0}", Model.PortNumber),
                Starting = String.Format("Starting server at port {0}", Model.PortNumber),
                StartingError = "Failed to start server ",
            };
            StartCommand.Execute();
        }

        void CreateAndExecuteStopCommand()
        {
            StopCommand = new AsyncUICommand(() =>
            {
                var tmp = Receiver;

                if (Receiver != null)
                {
                    Receiver.OnMessageReceived -= Receiver_OnMessageReceived;
                    Receiver.Dispose();
                    Receiver = null;
                }
            }, Model)
            {
                Starting = String.Format("Closing server port {0}", Model.PortNumber),
                Started = String.Format("Closed server port {0}", Model.PortNumber),
                StartingError = String.Format("Failed to stop server port {0}", Model.PortNumber),
            };
            StopCommand.Execute();
        }

        public NetworkReceiveState(ViewModel model)
        {
            Model = model;
            CreateAndExecuteStartCommand();
        }

        public void NetworkReceiveChangeState()
        {
            if (Receiver != null)
            {
                CreateAndExecuteStopCommand();
            }
            else if( Receiver == null && (StartCommand == null || StartCommand.ExecutionState == CommandState.Finished) )
            {
                CreateAndExecuteStartCommand();
            }
        }

        void Receiver_OnMessageReceived(string networkMessage)
        {
            ReceivedMessages.Enqueue(networkMessage);
            Task.Factory.StartNew( () =>
            {
                string msg;

                if (ReceivedMessages.TryDequeue(out msg))
                {
                    string[] parts = msg.Split(RemoteMessageSeparator);
                    if( parts.Length == 2 )
                    {
                        int eventId = int.Parse(parts[0], CultureInfo.InvariantCulture);
                        HookEvents.ETWProvider.FromNetworkReceived(eventId, parts[1]);
                        Model.ReceivedMessages.Add(String.Format("Received[{0}]: {1}", eventId, parts[1])); ; 
                    }
                    
                }

            }, CancellationToken.None, TaskCreationOptions.None, Model.UISheduler);
        }

        static char[] RemoteMessageSeparator = new char[] { '~' };

    }
}
