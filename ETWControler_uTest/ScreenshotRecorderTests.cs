using ETWControler.Screenshots;
using ETWControler_uTest.TestHelper;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETWControler_uTest
{
    [TestFixture]
    public class ScreenshotRecorderTests
    {

        /// <summary>
        /// There was a spurious InvalidOperationException happening while using a brush
        /// from several threads. Things are working out nicely now and this test is here
        /// to keep it that way
        /// </summary>
        [Test]
        public void RenderToSeveralThreads()
        {
            using (var tmp = TempDir.Create())
            {
                using (ScreenshotRecorder rec = new ScreenshotRecorder(tmp.Name, 500,100))
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

        [Test]
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

                Assert.AreEqual(20, Directory.GetFiles(tmp.Name, "*.jpg").Length);

                ScreenshotRecorder.ClearFiles(tmp.Name, 10);

                HashSet<string> expectedFiles = new HashSet<string>(Enumerable.Range(10, 10).Select(x => $"{x}.jpg"));

                var afterDeletion = Directory.GetFiles(tmp.Name, "*.jpg");
                Assert.AreEqual(10, afterDeletion.Length);

                foreach (var file in afterDeletion)
                {
                    string jpg = Path.GetFileName(file);
                    Assert.IsTrue(expectedFiles.Contains(jpg), $"File {file} was not exepcted to exist after deletion of 10 oldest files.");
                }
            }
        }
    }
}
