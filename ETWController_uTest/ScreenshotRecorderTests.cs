using ETWController.Screenshots;
using ETWController_uTest.TestHelper;
using Xunit;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETWController_uTest
{
    public class ScreenshotRecorderTests
    {

        /// <summary>
        /// There was a spurious InvalidOperationException happening while using a brush
        /// from several threads. Things are working out nicely now and this test is here
        /// to keep it that way
        /// </summary>
        [Fact]
        public void RenderToSeveralThreads()
        {
            using (var tmp = TempDir.Create())
            {
                using (ScreenshotRecorder rec = new ScreenshotRecorder(tmp.Name, 500,100, 100))
                {
                    Barrier b = new Barrier(4);
                    Action acc = () =>
                    {
                        b.SignalAndWait();
                        using (Bitmap bmp = new Bitmap(500, 100))
                        {
                            rec.TimeStampBitmap(DateTime.Now, bmp);
                        }

                    };
                    Parallel.Invoke(acc, acc, acc, acc);
                }
            }
        }


        /// <summary>
        /// A configured interval below the time one frame needs must not disable the recorder: the timer runs
        /// at the configured rate, captures as fast as the pipeline can and drops the frames that arrive while
        /// the writer is still busy. Nothing is queued, so memory stays at the two frame buffers.
        /// </summary>
        [Fact]
        public void Fast_Forced_Timer_Still_Captures_At_The_Sustainable_Rate()
        {
            using (var tmp = TempDir.Create())
            {
                using (ScreenshotRecorder rec = new ScreenshotRecorder(tmp.Name, 10, 85, 100))
                {
                    Thread.Sleep(1500);

                    int files = Directory.GetFiles(tmp.Name, "*.jpg").Length;
                    Assert.True(files >= 5, $"expected screenshots at the sustainable capture rate, got {files} (dropped {rec.DroppedFrames})");
                    Assert.True(rec.DroppedFrames > 0, "10 ms is faster than one frame can be captured, so the extra requests must be dropped");
                }
            }
        }

        /// <summary>
        /// 0 means no forced timer, which is also what the UI uses to turn screenshot recording off.
        /// </summary>
        [Fact]
        public void Zero_Forced_Interval_Disables_The_Timer()
        {
            using (var tmp = TempDir.Create())
            {
                using (ScreenshotRecorder rec = new ScreenshotRecorder(tmp.Name, 0, 85, 100))
                {
                    Thread.Sleep(500);
                    Assert.Equal(0, Directory.GetFiles(tmp.Name, "*.jpg").Length);
                }
            }
        }

        /// <summary>
        /// Stress the startup path: the forced screenshot timer fires immediately when the recorder is
        /// created, so its callback can run while the constructor is still executing.
        /// its timers only after the frame slot, the screen reader and the encoder exist, and TakeScreenshot
        /// refuses to draw before they do. This test records unhandled exceptions across many constructions;
        /// the race itself is not reproducible on demand, so a green run here is not proof that the ordering
        /// is correct - the ordering in the constructor is.
        /// </summary>
        [Fact]
        public void Start_Recorder_Many_Times_With_Immediate_Timer_Fire_Does_Not_Throw()
        {
            using (var tmp = TempDir.Create())
            {
                List<Exception> unhandled = new List<Exception>();
                UnhandledExceptionEventHandler handler = (s, e) =>
                {
                    lock (unhandled)
                    {
                        unhandled.Add((Exception)e.ExceptionObject);
                    }
                };
                AppDomain.CurrentDomain.UnhandledException += handler;

                try
                {
                    for (int i = 0; i < 200; i++)
                    {
                        using (ScreenshotRecorder rec = new ScreenshotRecorder(tmp.Name, 100, 85, 100))
                        {
                            // Give a callback that was queued by the timer a chance to run against a
                            // recorder that is still being constructed.
                            Thread.Sleep(1);
                        }
                    }
                }
                finally
                {
                    AppDomain.CurrentDomain.UnhandledException -= handler;
                }

                lock (unhandled)
                {
                    Assert.Empty(unhandled);
                }
            }
        }

        /// <summary>
        /// There are race conditions when the timer callbacks are still executing and we leave the dispose method 
        /// already. Ensure that this does not happen.
        /// </summary>
        [Fact]
        public void Create_And_Dispose_Fast_Many_ScreenshotRecoder_Instances()
        {
            using (var tmp = TempDir.Create())
            {
                for (int i = 0; i < 20; i++)
                {
                    using (ScreenshotRecorder rec = new ScreenshotRecorder(tmp.Name, 100, 100, 100))
                    {
                       
                    }
                    using (ScreenshotRecorder rec = new ScreenshotRecorder(tmp.Name, 100, 100, 100))
                    {
                        Thread.Sleep(50);
                    }
                }
            }
        }
       
        [Fact]
        public void EnsureOldestFilesAreCleared()
        {
            using (var tmp = TempDir.Create())
            {
                for (int i = 0; i < 20; i++)
                {
                    FileInfo fi = new FileInfo(Path.Combine(tmp.Name, $"{i}.jpg"));
                    using (fi.Create())
                    {
                    }
                    fi.CreationTime = DateTime.Now + TimeSpan.FromMinutes(i);
                }

                Assert.Equal(20, Directory.GetFiles(tmp.Name, "*.jpg").Length);

                ScreenshotRecorder.ClearFiles(tmp.Name, 10);

                HashSet<string> expectedFiles = new HashSet<string>(Enumerable.Range(10, 10).Select(x => $"{x}.jpg"));

                var afterDeletion = Directory.GetFiles(tmp.Name, "*.jpg");
                Assert.Equal(10, afterDeletion.Length);

                foreach (var file in afterDeletion)
                {
                    string jpg = Path.GetFileName(file);
                    Assert.True(expectedFiles.Contains(jpg), $"File {file} was not exepcted to exist after deletion of 10 oldest files.");
                }
            }
        }
    }
}
