using ETWControler.ETW;
using ETWControler.Hooking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;

namespace ETWControler
{
    /// <summary>
    /// Captures mouse/keybaord events and log them locally and send them over the network to a configures host.
    /// </summary>
    public class NetworkedHooker
    {
        /// <summary>
        /// Hook which captures all keyboard and mouse events
        /// </summary>
        internal Hooker Hooker
        {
            get;
            private set;
        }

        ViewModel Model;



        /// <summary>
        /// Each logged slowEvent gets its own number so we can later analyze slowevent 0,1,2 ... 
        /// </summary>
        int SlowEventNumber;

        int _CurrentId;
        /// <summary>
        /// Each captured keyboard or mouse event gets an id to be able to find e.g. keyboard event 500 where the slowdown did happen.
        /// </summary>
        int CurrentId
        {
            get { return Interlocked.Increment(ref _CurrentId); }
        }

        public NetworkedHooker(ViewModel model)
        {
            Model = model;
            Hooker = new Hooker();
            if( !HookEvents.IsAlreadyRegistered())
            {
                HookEvents.RegisterItself();
            }

            Hooker.OnKeyDown += Hooker_OnKeyDown;
            Hooker.OnMouseButton += Hooker_OnMouseButton;
            Hooker.OnMouseMove += Hooker_OnMouseMove;
            Hooker.OnMouseWheel += Hooker_OnMouseWheel;
        }


        void Hooker_OnMouseWheel(int wheelDelta, int x, int y)
        {
            int id = CurrentId;
            HookEvents.ETWProvider.MouseWheel(id, wheelDelta, x, y); // write to ETW
            string message = String.Format("MouseWheel Delta {0}, ({1},{2})", wheelDelta, x, y);
            SendToNetwork(id, message); // Send over network when present 
        }

        void Hooker_OnMouseMove(int x, int y)
        {
            if (Model.CaptureMouseMove)
            {
                int id = CurrentId;
                HookEvents.ETWProvider.MouseMove(id, x, y);
            }
        }

        void Hooker_OnMouseButton(ETWControler.Hooking.MouseButton button, int x, int y)
        {
            string strButton = button.ToString("G");
            int id = CurrentId;

            HookEvents.ETWProvider.MouseButton(id, strButton, x, y);
            string message = String.Format("Mouse Button {0}, ({1},{2})", button, x, y);

            SendToNetwork(id, message);
            if (strButton == Model.SlowEventHotkey)
            {
                LogSlowEvent();
            }
        }

        void Hooker_OnKeyDown(Key key)
        {
            string strKey = key.ToString("G");
            int id = CurrentId;
            HookEvents.ETWProvider.KeyDown(id, strKey);
            SendToNetwork(id, String.Format("KeyDown {0}", strKey));

            if (Model.SlowEventHotkey == strKey)
            {
                LogSlowEvent();
            }
        }

        public void LogSlowEvent()
        {
            string msg = String.Format("Slow Event[{0}]: {1}", SlowEventNumber, Model.SlowEventMessage);
            int id = CurrentId;
            HookEvents.ETWProvider.SlowMarker(id, msg);
            SendToNetwork(id, msg);

            SlowEventNumber++;
        }

        /// <summary>
        /// Send message over network can report any sending errors in the status bar. If an error does happen we 
        /// disable the sending checkbox to prevent further attempts to send data.
        /// </summary>
        /// <param name="message"></param>
        void SendToNetwork(int eventId, string message)
        {
            try
            {
                Model.ReceivedMessages.Add(String.Format("Local[{0}]: {1}", eventId, message));
                if (Model.NetworkSendState.Sender != null)
                {
                    Model.NetworkSendState.Sender.Send(String.Format("{0}~{1}",eventId, message));
                }
            }
            catch (Exception ex)
            {
                Model.SetStatusMessageWarning(String.Format("Could not send message to {0}:{1}. Error: {2}", Model.Host, Model.PortNumber, ex.Message), ex);
                Model.NetworkSendEnabled = false;
            }
        }
    }
}
