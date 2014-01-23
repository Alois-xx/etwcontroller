using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ETWControler.ETW
{

    /// <summary>
    /// ETW Provider for window and mouse hook messages.
    /// </summary>
    [EventSource(Guid = "F0A5DA64-0FBC-4D41-B6C7-BF56A0601D7D", Name = "HookEvents")]
    public sealed class HookEvents : EventSource
    {
        /// <summary>
        /// ETW provider to log keyboard and mouse events to ETW to enable coherent analysis between user actions and the system reactions.
        /// </summary>
        public static HookEvents ETWProvider = new HookEvents();

        /// <summary>
        /// ctor
        /// </summary>
        HookEvents():base(true)
        {
        }

        /// <summary>
        /// Check if ETW manifest is already installed.
        /// </summary>
        /// <returns></returns>
        public static bool IsAlreadyRegistered()
        {
            return File.Exists(CreateManifestPath());
        }

        static string CreateManifestPath()
        {
            return Path.Combine(Path.GetTempPath(), "HookEvents.man");
        }

        /// <summary>
        /// Register as regular event provider in the system to make xperf happy. The pure dynamic registration less provider approach
        /// does not work out well if you need to do a full rundown to dump the manifest into the ETW stream which currently only PerfView does.
        /// By registering it in the old fashioned way WPA does also display the event type as name and not the cryptic guid. 
        /// </summary>
        public static void RegisterItself()
        {
            string manifest = EventSource.GenerateManifest(typeof(HookEvents), Assembly.GetExecutingAssembly().Location);
            string manifestPath = CreateManifestPath();
            File.WriteAllText(manifestPath, manifest);
            ProcessStartInfo info = new ProcessStartInfo()
            {
                Arguments = String.Format("um \"{0}\"", manifestPath),
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = "wevtutil",
            };

            Process.Start(info).WaitForExit();
            info.Arguments = String.Format("im \"{0}\"", manifestPath);
            Process.Start(info).WaitForExit();
        }

        [Event(1,Level=EventLevel.Informational, Opcode=EventOpcode.Info, Task=Tasks.MouseButtonDown)]
        public void MouseButton(int EventNumber, string MouseButton, int x, int y)
        {
            WriteEvent(1, EventNumber, MouseButton, x, y);
        }

        /// <summary>
        /// High frequency event where all mouse move events are recorded
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        [Event(2, Level = EventLevel.Verbose, Opcode = EventOpcode.Info, Task=Tasks.MouseMove)]
        public void MouseMove(int EventNumber, int x, int y)
        {
            WriteEvent(2, EventNumber, x, y);
        }

        /// <summary>
        /// Mouse wheel event where the mouse wheel is rolled forward or backward.
        /// </summary>
        /// <param name="WheelDelta"></param>
        /// <param name="x">Mouse position.</param>
        /// <param name="y">Mouse position.</param>
        [Event(3, Level = EventLevel.Informational, Opcode = EventOpcode.Info, Task=Tasks.MouseWheel)]
        public void MouseWheel(int EventNumber, int WheelDelta, int x, int y)
        {
            WriteEvent(3, EventNumber, WheelDelta, x, y);
        }

        /// <summary>
        /// Key was pressed.
        /// </summary>
        /// <param name="Key">Stringified version of key name.</param>
        [Event(4, Level = EventLevel.Informational, Opcode = EventOpcode.Info, Task = Tasks.KeyDown)]
        public void KeyDown(int EventNumber, string Key)
        {
            WriteEvent(4, EventNumber, Key);
        }

        /// <summary>
        /// Write a slow event which is fired when the hotkey (mouse or keyboard) is pressed to signal a slow condition noticed by the user.
        /// </summary>
        /// <param name="SlowMessage">Message describing the observed slowness</param>
        [Event(5, Level=EventLevel.LogAlways, Opcode=EventOpcode.Info, Task=Tasks.Slow)]
        public void SlowMarker(int EventNumber, string SlowMessage)
        {
            WriteEvent(5, EventNumber, SlowMessage);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="NetworkMessage"></param>
        [Event(6, Level=EventLevel.Informational, Task = Tasks.RemoteReceived)]
        public void FromNetworkReceived(int EventNumber, string NetworkMessage)
        {
            WriteEvent(6, EventNumber, NetworkMessage);
        }

        class Tasks 
        {
            public const EventTask Slow = (EventTask)0x1;
            public const EventTask MouseButtonDown = (EventTask)0x2;
            public const EventTask MouseMove = (EventTask)0x3;
            public const EventTask MouseWheel = (EventTask)0x4;

            public const EventTask KeyDown = (EventTask)        0x5;
            public const EventTask RemoteReceived = (EventTask) 0x6;

        }
    }


}
