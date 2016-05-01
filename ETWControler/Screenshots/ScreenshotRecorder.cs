using ETWControler.ETW;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ETWControler.Screenshots
{
    sealed class ScreenshotRecorder : IDisposable
    {
        private string ScreenshotDirectory;

        const string ScreenshotFileNameBase = "Screenshot_";

        SolidBrush TransparentRed = new SolidBrush(Color.FromArgb(60, 255, 0, 0));
        Font StampFont = new Font("Lucidia Console", 12);

        const int MaxScreenshotsToSaveBeforeDeletingOldest = 100;

        // Remember the last MaxScreenshotsToSaveBeforeDeletingOldest screenshots and delete the oldest ones to prevent filling up the hard disk
        Queue<string> TakenScreenshots = new Queue<string>();

        /// <summary>
        /// Throttle the rate at which we capture secondary screenshots to observe the reaction of the UI to the click event
        /// </summary>
        DateTime LastOtherScreenshot = DateTime.MinValue;
        TimeSpan MaxRecordTime = TimeSpan.FromMilliseconds(500);

        public ScreenshotRecorder(string screenshotDirectory)
        {
            ScreenshotDirectory = screenshotDirectory;
            Directory.CreateDirectory(ScreenshotDirectory); // Ensure that dir exists

            // Delete old files from previous runs
            foreach (var jpg in Directory.GetFiles(ScreenshotDirectory, "*.jpg"))
            {
                try
                {
                    File.Delete(jpg);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(String.Format("Could not delete screenshot file {0} because of {1}", jpg, ex));
                }
            }
        }


        public void Dispose()
        {
           
        }


        /// <summary>
        /// Take a screenshot from all monitors and save it to disk. Add the current timestamp and the hit position to the picture to make
        /// it easy to correlate the traces with the visual interactions.
        /// </summary>
        /// <param name="clickX">Screen x-coordinate of click event or -1 if not available.</param>
        /// <param name="clickY">Screen y-cooridnate of click event or -1 if not available.</param>
        /// <param name="suffix">This suffix is appended to screenshot file name</param>
        /// <param name="suffixOfSecondScreenshot">If non null a second screenshot is taken with this suffix after 500ms.</param>
        /// <returns>Saved screenshot file name</returns>
        public string TakeScreenshot(int clickX, int clickY, string suffix, string suffixOfSecondScreenshot)
        {
            // Determine the size of the "virtual screen", which includes all monitors.
            int screenLeft = SystemInformation.VirtualScreen.Left;
            int screenTop = SystemInformation.VirtualScreen.Top;
            int screenWidth = SystemInformation.VirtualScreen.Width;
            int screenHeight = SystemInformation.VirtualScreen.Height;

            // Create a bitmap of the appropriate size to receive the screenshot.
            using (Bitmap bmp = new Bitmap(screenWidth, screenHeight))
            {
                // Draw the screenshot into our bitmap.
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(screenLeft, screenTop, 0, 0, bmp.Size);
                }

                TimeStampBitmap(DateTime.Now, bmp);

                if (clickX != -1 && clickY != -1)
                {
                    MarkMouseClick(clickX, clickY, bmp);
                }

                string savePath = Path.Combine(ScreenshotDirectory, ScreenshotFileNameBase + suffix + ".jpg");
                bmp.Save(savePath, ImageFormat.Jpeg);
                HookEvents.ETWProvider.Screenshot(suffix, savePath);

                AddSavedFileAndRollOverOldesFiles(savePath);

                if( !String.IsNullOrEmpty(suffixOfSecondScreenshot) )
                {
                    Task.Factory.StartNew(() => TakeAnotherScreenshot(suffixOfSecondScreenshot), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
                }

                return savePath;
            }
        }


        void TakeAnotherScreenshot(string suffix)
        {
            Thread.Sleep(500);
            // throttle creation of new screenshots after a click to one screenshot every 500ms
            if (DateTime.Now - LastOtherScreenshot > MaxRecordTime)
            {
                string file = TakeScreenshot(-1, -1, suffix, null);
                ETW.HookEvents.ETWProvider.Screenshot(suffix, file);
            }
            LastOtherScreenshot = DateTime.Now;
        }

        private void AddSavedFileAndRollOverOldesFiles(string savePath)
        {
            lock(TakenScreenshots)
            {
                TakenScreenshots.Enqueue(savePath);
                while (TakenScreenshots.Count > MaxScreenshotsToSaveBeforeDeletingOldest)
                {
                    try
                    {
                        File.Delete(TakenScreenshots.Dequeue());
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(String.Format("Could not delete oldest screenshot file {0} because limit of 100 screenshots was reached. {1}", savePath, ex));
                    }
                }
            }
        }

        /// <summary>
        /// Draw a red rectangle where the click event did happen so it is easy to find in the screenshots
        /// </summary>
        /// <param name="clickX"></param>
        /// <param name="clickY"></param>
        /// <param name="bmp"></param>
        private static void MarkMouseClick(int clickX, int clickY, Bitmap bmp)
        {
            for (int x = Math.Abs(clickX); x < bmp.Width && x < Math.Abs(clickX) + 14; x++)
            {
                for (int y = Math.Abs(clickY); y < bmp.Height && y < Math.Abs(clickY) + 14; y++)
                {
                    Color c = bmp.GetPixel(x, y);
                    bmp.SetPixel(x, y, Color.FromArgb(255, c.G / 2, c.B / 2));
                }
            }
        }

        /// <summary>
        /// Add a translucent time stamp to the picture to make it later easy to find in the traces the corresponding time
        /// </summary>
        /// <param name="time"></param>
        /// <param name="bmp"></param>
        void TimeStampBitmap(DateTime time, Bitmap bmp)
        {
            Point pos = new Point((int)(bmp.Width * 0.9), 20);
            Size size = new Size(100, 25);

            using (var g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(TransparentRed, new Rectangle(pos, size));
                g.DrawString(time.ToString("HH:mm:ss.fff"), StampFont, Brushes.Yellow, pos.X, pos.Y);
            }
        }
    }
}
