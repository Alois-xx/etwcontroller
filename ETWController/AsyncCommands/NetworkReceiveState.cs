﻿using ETWController.ETW;
using ETWController.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETWController.Commands
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
        public TaskScheduler Scheduler { get; private set; }

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
            Model, Scheduler)
            {
                Started = String.Format("MessageReceiver service started at port {0}", Model.PortNumber),
                Starting = String.Format("Starting MessageReceiver service at port {0}", Model.PortNumber),
                StartingError = "Failed to start MessageReceiver service",
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
            }, Model, Scheduler)
            {
                Starting = String.Format("Closing MessageReceiver service port {0}", Model.PortNumber),
                Started = String.Format("Closed MessageReceiver service port {0}", Model.PortNumber),
                StartingError = String.Format("Failed to stop MessageReceiver service port {0}", Model.PortNumber),
            };
            StopCommand.Execute();
        }

        public NetworkReceiveState(ViewModel model, TaskScheduler scheduler)
        {
            Model = model;
            Scheduler = scheduler;
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
                if (ReceivedMessages.TryDequeue(out string msg))
                {
                    string[] parts = msg.Split(RemoteMessageSeparator);
                    if( parts.Length == 2 )
                    {
                        int eventId = int.Parse(parts[0], CultureInfo.InvariantCulture);
                        HookEvents.ETWProvider.FromNetworkReceived(eventId, parts[1]);
                        Model.ReceivedMessages.Add(String.Format("Received[{0}]: {1}", eventId, parts[1])); ; 
                    }
                    
                }

            }, CancellationToken.None, TaskCreationOptions.None, Model.UIScheduler);
        }

        static char[] RemoteMessageSeparator = new char[] { '~' };

    }
}
