using ETWControler;
using ETWControler.UI;
using ETWControler_uTest.TestHelper;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETWControler_uTest
{
    [TestFixture]
    public class ViewModelTests
    {
        ConcurrentQueue<string> TraceMessages = new ConcurrentQueue<string>();
        List<TraceStates> LocalTraceStateChanges = new List<TraceStates>();
        App CurrentApp = null;


        [Test]
        public void Start_Stop_Verify_MessageBox_Is_Displayed_IfOutputFile_Was_Not_Created()
        {
            using (var tmp = TempDir.Create())
            {
                using (var model = Create(tmp.Name))
                {

                    model.Commands["StartTracing"].Execute(null);

                    WaitUntilLocalTargetState(model, TraceStates.Running);

                    model.Commands["StopTracing"].Execute(null);

                    WaitUntilLocalTargetState(model, TraceStates.Stopped);

                    ContainsMessage("hi trace start");
                    ContainsMessage($"trace stop performed {model.StopData.TraceFileName}");
                    MessageBoxShown($"Output file {model.StopData.TraceFileName} was not found");
                }
            }
        }

        [Test]
        public void Display_MessageBox_When_OutputFile_During_Trace_Start_Cannot_Be_Deleted()
        {
            using (var tmp = TempDir.Create())
            {
                using (var model = Create(tmp.Name))
                {
                    string etlFile = model.TraceFileName;
                    using (var lockedOutputFile = File.Create(etlFile))
                    {
                        ClearStates();
                        model.Commands["StartTracing"].Execute(null);
                        Assert.AreEqual(2, LocalTraceStateChanges.Count);
                        Assert.AreEqual(TraceStates.Starting, LocalTraceStateChanges[0]);
                    }

                    MessageBoxShown($"Could not delete old trace file {etlFile}");
                }
            }
        }

        void MessageBoxShown(string message)
        {
            ContainsMessage(message, "MessageBox");
        }

        private void ContainsMessage(string substring1, string substring2=null, int count=1)
        {
           int containsCount = 0;
           foreach(var message in TraceMessages)
           {
                if( message.IndexOf(substring1) != -1 && ( substring2 == null || message.IndexOf(substring2) != -1) )
                {
                    containsCount++;
                }
           }

            Assert.AreEqual(count, containsCount, $"substrings \"{substring1}\" \"{substring2 ?? "none"}\" were not found the expected number of times.");
        }

        ViewModel Create(string tempDir)
        {
            TraceMessages = new ConcurrentQueue<string>();
            if (CurrentApp == null)
            {
                CurrentApp = new App();
            }

            MessageDisplayMock display = new MessageDisplayMock();
            display.OnMessage += (str1, str2) =>
            {
                TraceMessages.Enqueue($"MessageBox: {str1} Caption: {str2}");
            };
            ViewModel model = new ViewModel(display);

            model.LocalTraceSettings.PropertyChanged += (object sender, System.ComponentModel.PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == nameof(model.LocalTraceSettings.TraceStates))
                {
                    lock (LocalTraceStateChanges)
                    {
                        LocalTraceStateChanges.Add(model.LocalTraceSettings.TraceStates);
                    }
                }
            };

            model.InitUIDependantVariables(CurrentApp, TaskScheduler.Default);
            model.CaptureKeyboard = false;
            model.CaptureMouseButtonDown = false;
            model.CaptureScreenShots = false;
            model.UnexpandedTraceFileName = Path.Combine(tempDir, "test.etl");
            model.LocalTraceSettings.TraceStart = ":: echo hi trace start";
            model.LocalTraceSettings.TraceStop = ":: echo trace stop performed " + TraceControlViewModel.TraceFileNameVariable;
            model.LocalTraceSettings.CommandOutputs.CollectionChanged += (sender, e) =>
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    TraceMessages.Enqueue(e.NewItems[0].ToString());
                }
            };

            return model;
        }

        void WaitUntilLocalTargetState(ViewModel model, ETWControler.UI.TraceStates targetState, int waitMs = 2000)
        {
            while (model.LocalTraceSettings.TraceStates != targetState && waitMs-- > 0)
            {
                Thread.Sleep(1);
            }

            Assert.AreEqual(targetState, model.LocalTraceSettings.TraceStates);
        }

        [SetUp]
        public void ClearStates()
        {
            TraceMessages = new ConcurrentQueue<string>();
            LocalTraceStateChanges.Clear();
        }

        [TearDown]
        public void PrintMesages()
        {
            foreach (var m in TraceMessages)
            {
                Console.WriteLine(m);
            }
        }
    }
}
