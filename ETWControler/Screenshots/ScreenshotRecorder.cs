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

    /// <summary>
    /// Generate screenshots from all monitors based on various timers to record a screenshot for every mouse click and after
    /// a fixed amount of time to see the UI reaction (500ms currently).
    /// Besides that a different timer will collect a screenshot if no other screenshot has been captured since the last forcedScreenShotAfterMs timespan
    /// which is configured as parameter to the ctor.
    /// </summary>
    sealed class ScreenshotRecorder : IDisposable
    {
        private string ScreenshotDirectory;

        const string ScreenshotFileNameBase = "Screenshot_";

        SolidBrush TransparentRed = new SolidBrush(Color.FromArgb(60, 255, 0, 0));
        Font StampFont = new Font("Lucidia Console", 12);

        /// <summary>
        /// Timer which gets a screenshot if no other screenshot was recorded during the <see cref="ForcedRecordTimeSpan"/>
        /// </summary>
        System.Threading.Timer ForcedScreenshotTimer;

        const int MaxScreenshotsToSaveBeforeDeletingOldest = 100;

        // Remember the last MaxScreenshotsToSaveBeforeDeletingOldest screenshots and delete the oldest ones to prevent filling up the hard disk
        Queue<string> TakenScreenshots = new Queue<string>();

        /// <summary>
        /// Throttle the rate at which we capture secondary screenshots to observe the reaction of the UI to the click event
        /// </summary>
        DateTime LastOtherScreenshot = DateTime.MinValue;

        /// <summary>
        /// Time after which another screenshot is taken when the user has clicked around in the UI to see how the UI did react
        /// </summary>
        TimeSpan SecondScreenshotTimerAfterClick = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Time after which a screenshot is taken regardless if the user did click around or not
        /// </summary>
        TimeSpan ForcedRecordTimeSpan;

        /// <summary>
        /// Create a screenshot recroder which stores its screenshots to that directory.
        /// </summary>
        /// <param name="screenshotDirectory">Directory will be created if it does not yet exist.</param>
        /// <param name="forcedScreenShotAfterMs">If > 99ms it will create after forcedScreenShotAfterMs a screenshot if no other screenshot was taken during that period of time.</param>
        public ScreenshotRecorder(string screenshotDirectory, int forcedScreenShotAfterMs)
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

            if (forcedScreenShotAfterMs > 99)  // one screenshot needs about 100ms. If one configures less than that we assume a configuration error
            {
                ForcedRecordTimeSpan = TimeSpan.FromMilliseconds(forcedScreenShotAfterMs);
                ForcedScreenshotTimer = new System.Threading.Timer(OnScreenshotTimerExpired, null, 0, forcedScreenShotAfterMs);
            }
        }


        void OnScreenshotTimerExpired(object o)
        {
            var now = DateTime.Now;
            if(now - LastOtherScreenshot > ForcedRecordTimeSpan)
            {
                TakeAnotherScreenshot($"_Forced_{now.ToString("HH.mm.ss.fff")}", false);
            }
        }

        public void Dispose()
        {
            ForcedScreenshotTimer.Change(0, Timeout.Infinite);
            ForcedScreenshotTimer.Dispose();
            ForcedScreenshotTimer = null;
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
                var now = DateTime.Now;
                // Draw the screenshot into our bitmap.
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(screenLeft, screenTop, 0, 0, bmp.Size);
                }

                TimeStampBitmap(now, bmp);

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

                // Update creation time to actual screenshot snapshot time so 
                // we can use the file creation date as snapshot time to sort them during report generation
                var fileInfo = new FileInfo(savePath);
                fileInfo.CreationTime = now;

                return savePath;
            }
        }


        void TakeAnotherScreenshot(string suffix, bool bSleep=true)
        {
            if (bSleep)
            {
                Thread.Sleep(500);
            }

            // throttle creation of new screenshots after a click to one screenshot every 500ms
            if (DateTime.Now - LastOtherScreenshot > SecondScreenshotTimerAfterClick)
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
