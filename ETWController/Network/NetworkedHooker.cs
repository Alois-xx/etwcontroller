using ETWController.ETW;
using ETWController.Hooking;
using ETWController.Screenshots;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ETWController
{
    /// <summary>
    /// Captures mouse/keybaord events and log them locally and send them over the network to a configures host.
    /// </summary>
    public class NetworkedHooker : IDisposable
    {
        /// <summary>
        /// Hook which captures all keyboard and mouse events
        /// </summary>
        internal Hooker Hooker
        {
            get;
            private set;
        }

        /// <summary>
        /// These keys are still sent as cleartext so we can at least see when the user did press enter, the cursor keys and the F keys.
        /// </summary>
        HashSet<string> ClearKeys = new HashSet<string>()
        {
            "Up",
            "Down",
            "Left",
            "Right",
            "Home",
            "End",
            "PageUp",
            "Next",
            "Insert",
            "Delete",
            "Escape",
            "Return",
            "Capital",
            "F1",
            "F2",
            "F3",
            "F4",
            "F5",
            "F6",
            "F7",
            "F8",
            "F9",
            "F10",
            "F11",
            "F12",
            "LeftAlt",
            "LeftCtrl",
            "LeftShift",
            "RightShift",
            "RightAlt",
            "RightCtrl",
            "LWin",
            "RWin",
            "Apps",
            "Tab",
            "Oem1",
            "Oem2",
            "Oem3",
            "Oem4",
            "Oem5",
            "Oem6",
            "Oem7",
            "Oem8",
            "Oem9",
            "OemPlus",
            "OemMinus",
            "OemPeriod",
            "OemComma",
            "Snapshot",
            "Scroll",
            "Pause",
            "NumLock",
            "Divide",
            "Multiply",
            "Subtract",
            "Add",
            "Decimal",
            "Space"
        };

        ViewModel Model;
        int ConcurrentScreenshots = 0;

        ScreenshotRecorder Recorder = null;



        /// <summary>
        /// Each logged slowEvent gets its own number so we can later analyze slowevent 0,1,2 ... 
        /// </summary>
        int SlowEventNumber;

        volatile int _CurrentId;
        /// <summary>
        /// Each captured keyboard or mouse event gets an id to be able to find e.g. keyboard event 500 where the slowdown did happen.
        /// </summary>
        int CurrentId
        {
            get { return Interlocked.Increment(ref _CurrentId); }
        }

        public void ResetId()
        {
            _CurrentId = 0;
            ScreenshotRecorder.ClearFiles(Model.ScreenshotDirectory);
        }

        public NetworkedHooker(ViewModel model)
        {
            Model = model;
            Hooker = new Hooker();
            if( !HookEvents.IsAlreadyRegistered())
            {
                HookEvents.RegisterItself();
            }

            Model.PropertyChanged += (a, b) =>
            {
                if (b.PropertyName == ViewModel.CaptureScreenShotsProperty)
                {
                    if (Model.CaptureScreenShots == true)
                    {
                        EnableRecorder();
                    }
                    else
                    {
                        Recorder?.Dispose();
                        Recorder = null;
                    }

                }
            };

            if( Model.CaptureScreenShots ) // default is to enable it
            {
                EnableRecorder();
            }

            Hooker.OnKeyDown += Hooker_OnKeyDown;
            Hooker.OnMouseButton += Hooker_OnMouseButton;
            Hooker.OnMouseMove += Hooker_OnMouseMove;
            Hooker.OnMouseWheel += Hooker_OnMouseWheel;
        }

        public void EnableRecorder()
        {
            if (Recorder != null)
            {
                Recorder.Dispose();
                Recorder = null;
            }

            Recorder = new ScreenshotRecorder(Model.ScreenshotDirectory, Model.ForcedScreenshotIntervalinMs, Model.JpgCompressionLevel, Model.KeepNNewestScreenShots);
        }

        void Hooker_OnMouseWheel(int wheelDelta, int x, int y)
        {
            if (Model.CaptureMouseWheel)
            {
                int id = CurrentId;
                HookEvents.ETWProvider.MouseWheel(id, wheelDelta, x, y); // write to ETW
                string message = String.Format("MouseWheel Delta {0}, ({1},{2})", wheelDelta, x, y);
                SendToNetworkAsync(id, message); // Send over network when present 
            }
        }

        void Hooker_OnMouseMove(int x, int y)
        {
            if (Model.CaptureMouseMove)
            {
                int id = CurrentId;
                HookEvents.ETWProvider.MouseMove(id, x, y);
            }
        }

        void Hooker_OnMouseButton(ETWController.Hooking.MouseButton button, int x, int y)
        {
            string strButton = button.ToString("G");
            int id = CurrentId;

            HookEvents.ETWProvider.MouseButton(id, strButton, x, y);
            string message = String.Format("Mouse Button {0}, ({1},{2})", button, x, y);

            if( strButton.Contains("Down") && Model.CaptureScreenShots && Recorder != null)
            {
                Task.Factory.StartNew<KeyValuePair<string,Exception>>(() =>
                {
                    int newConcurrentScreenshotCount = Interlocked.Increment(ref ConcurrentScreenshots);
                    
                    try
                    {
                        if (newConcurrentScreenshotCount == 1) // prevent too many concurrent screenshots
                        {
                            return Recorder.TakeScreenshot(x, y, id.ToString(), $"{id}After500ms");
                        }
                    }
                    finally
                    {
                        Interlocked.Decrement(ref ConcurrentScreenshots);
                    }
                    return new KeyValuePair<string, Exception>(null,null);
                }).ContinueWith(screenshotTask =>
                {
                    if (screenshotTask.Result.Key != null)
                    {
                        Model.ReceivedMessages.Add($"Saved Screenshot to {screenshotTask.Result.Key}");
                    }
                    if(screenshotTask.Result.Value != null)
                    {
                        Model.ReceivedMessages.Add($"Got Exception while taking screenshot {screenshotTask.Result.Value}");
                    }

                }, CancellationToken.None, TaskContinuationOptions.None, Model.UIScheduler);
            }

            SendToNetworkAsync(id, message);
            LogEventIfKeyOrMouseMatches(strButton);
        }



        void LogEventIfKeyOrMouseMatches(string keyboardOrMouseButton)
        {
            if (keyboardOrMouseButton == Model.SlowEventHotkey)
            {
                LogSlowEvent();
            }
            else if (keyboardOrMouseButton == Model.FastEventHotkey)
            {
                LogFastEvent();
            }
        }

        void Hooker_OnKeyDown(Key key)
        {
            string strKey = key.ToString("G");
            // by default encrypt the logged keyboard key
            if(this.Model.IsKeyBoardEncrypted && !ClearKeys.Contains(strKey))
            {
                strKey = "SomeKey";
            }
            int id = CurrentId;
            HookEvents.ETWProvider.KeyDown(id, strKey);

            
            if (Model.CaptureScreenShots && Recorder != null) // Many actions start with enter take a screenshot from Enter as well. 
            {
                switch (key)
                {
                    case Key.Enter:
                        Recorder.TakeScreenshot(-1, -1, $"{id}_Enter", $"{id}After500ms");
                        break;
                    default:
                        break;
                }
            }

            SendToNetworkAsync(id, String.Format("KeyDown {0}", strKey));

            LogEventIfKeyOrMouseMatches(key.ToString("G")); // to match we need the clear string of the keyboard event
        }

        public void LogSlowEvent()
        {
            string msg = String.Format("Slow Event[{0}]: {1}", SlowEventNumber, Model.SlowEventMessage);
            int id = CurrentId;
            HookEvents.ETWProvider.SlowMarker(id, msg);
            SendToNetworkAsync(id, msg);

            SlowEventNumber++;
        }

        internal void LogFastEvent()
        {
            string msg = String.Format("Fast Event[{0}]: {1}", SlowEventNumber, Model.FastEventMessage);
            int id = CurrentId;
            HookEvents.ETWProvider.FastMarker(id, msg);
            SendToNetworkAsync(id, msg);

            SlowEventNumber++;
        }

        /// <summary>
        /// Send message over network can report any sending errors in the status bar. If an error does happen we 
        /// disable the sending checkbox to prevent further attempts to send data.
        /// </summary>
        /// <param name="message"></param>
        async void SendToNetworkAsync(int eventId, string message)
        {
            try
            {
                Model.ReceivedMessages.Add(String.Format("Local[{0}]: {1}", eventId, message));
                if (Model.NetworkSendState.Sender != null)
                {
                    await Model.NetworkSendState.Sender.SendAsync(String.Format("{0}~{1}",eventId, message));
                }
            }
            catch (Exception ex)
            {
                Model.SetStatusMessageWarning(String.Format("Could not send message to {0}:{1}. Error: {2}", Model.Host, Model.PortNumber, ex.Message), ex);
                Model.NetworkSendEnabled = false;
            }
        }

        public void Dispose()
        {
            if( Hooker != null )
            {
                Hooker.Dispose();
            }
        }
    }
}
