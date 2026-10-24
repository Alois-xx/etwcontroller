using ETWController.ETW;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;

namespace ETWController.Screenshots
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

        /// <summary>
        /// Deletes oldest files every 5 minutes to prevent filling up the disk
        /// </summary>
        System.Threading.Timer FileCleanupTimer;

        /// <summary>
        /// Do not a forced screenshot if since this time a screenshot was already captured
        /// </summary>
        const int MinimumScreenshotTimeInMs = 99;

        /// <summary>
        /// Keep only the last 100 screenshots and delete older ones
        /// </summary>
        int KeepNNewestScreenShots = 100;

        /// <summary>
        /// Throttle the rate at which we capture secondary screenshots to observe the reaction of the UI to the click event
        /// </summary>
        DateTime LastOtherScreenshot = DateTime.MinValue;

        /// <summary>
        /// Time after which another screenshot is taken when the user has clicked around in the UI to see how the UI did react
        /// </summary>
        TimeSpan SecondScreenshotTimerAfterClick = TimeSpan.FromMilliseconds(500);


        /// <summary>
        /// Lock object to serialize screenshots
        /// </summary>
        object Lock = new object();

        /// <summary>
        /// Define JPG compression level
        /// </summary>
        EncoderParameters EncoderParameters;

        ImageCodecInfo JpgEncoder;

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        private bool IsDisposed;

        internal static void ClearFiles(string screenshotDirectory, int keepNewestNFiles=0)
        {
            if (Directory.Exists(screenshotDirectory))
            {
                // Delete old files from previous runs
                foreach (var jpg in Directory.GetFiles(screenshotDirectory, "*.jpg")
                    .Select(x => new FileInfo(x))
                    .OrderByDescending(x => x.CreationTime)
                    .Skip(keepNewestNFiles))
                {
                    try
                    {
                        File.Delete(jpg.FullName);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError($"Could not delete screenshot file {jpg} because of {ex}");
                    }
                }

                // Ensure that all files in screenshot directory are deleted so no old remanents 
                // are kept in newere traces if screenshot capture is turned off
                string report = Path.Combine(screenshotDirectory, HtmlReportGenerator.HtmlReportFileName);
                if (File.Exists(report))
                {
                    try
                    {
                        File.Delete(report);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError($"Could not delete report file {report} because of {ex}");
                    }
                }
            }
        }

        /// <summary>
        /// Create a screenshot recroder which stores its screenshots to that directory.
        /// </summary>
        /// <param name="screenshotDirectory">Directory will be created if it does not yet exist.</param>
        /// <param name="forcedScreenShotAfterMs">If > 99ms it will create after forcedScreenShotAfterMs a screenshot if no other screenshot was taken during that period of time.</param>
        /// <param name="jpgCompressionLevel">Can be a value from 0-100. 100 is lossless</param>
        /// <param name="keepNNewestScreenShots">Delete oldest files every 5 minutes if total screenshot file count is over keepNNewestScreenShots.</param>
        public ScreenshotRecorder(string screenshotDirectory, int forcedScreenShotAfterMs, int jpgCompressionLevel, int keepNNewestScreenShots)
        {
            ScreenshotDirectory = screenshotDirectory;
            Directory.CreateDirectory(ScreenshotDirectory); // Ensure that dir exists

            KeepNNewestScreenShots = keepNNewestScreenShots;

            ClearFiles(ScreenshotDirectory);

            if (forcedScreenShotAfterMs > MinimumScreenshotTimeInMs)  // one screenshot needs about 100ms. If one configures less than that we assume a configuration error
            {
                Debug.Print($"Forced Screenshot timer: {forcedScreenShotAfterMs}");
                ForcedScreenshotTimer = new System.Threading.Timer(OnScreenshotTimerExpired, null, 0, forcedScreenShotAfterMs);
            }

            FileCleanupTimer = new System.Threading.Timer(OnCleanupOldestFiles, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));


            // Setup objects to save JPGs with a configured compression level
            System.Drawing.Imaging.Encoder encoder = System.Drawing.Imaging.Encoder.Quality;
            EncoderParameter encoderParameter = new EncoderParameter(encoder, (long) jpgCompressionLevel);
            EncoderParameters = new EncoderParameters(1);
            EncoderParameters.Param[0] = encoderParameter;

            JpgEncoder = GetEncoder(ImageFormat.Jpeg);
        }

        void OnCleanupOldestFiles(object o)
        {
            try
            {
                Interlocked.Increment(ref ExecutingTimerCallbacks);
                ClearFiles(ScreenshotDirectory, KeepNNewestScreenShots);
            }
            finally
            {
                Interlocked.Decrement(ref ExecutingTimerCallbacks);
            }
        }

        volatile int ExecutingTimerCallbacks = 0;

        void OnScreenshotTimerExpired(object o)
        {
            try
            {
                var concCurrentCount = Interlocked.Increment(ref ExecutingTimerCallbacks);
                var now = DateTime.Now;
                Debug.Print($"Timer expired {now.ToString("mm:ss")}, diff: {(now - LastOtherScreenshot).TotalMilliseconds}ms");
                if (concCurrentCount == 1 && (now - LastOtherScreenshot).TotalMilliseconds > MinimumScreenshotTimeInMs)
                {
                    TakeAnotherScreenshot($"Forced_{now.ToString("HH.mm.ss.fff")}", false);
                }
            }
            finally
            {
                Interlocked.Decrement(ref ExecutingTimerCallbacks);
            }
        }

        public void Dispose()
        {
            IsDisposed = true;
            ForcedScreenshotTimer.Change(0, Timeout.Infinite);
            ForcedScreenshotTimer.Dispose();
            ForcedScreenshotTimer = null;

            FileCleanupTimer.Change(0, Timeout.Infinite);
            FileCleanupTimer.Dispose();
            FileCleanupTimer = null;

            // wait until all callback have exited to ensure that we have no races while creating the next ScreenshotRecorder instance
            while (ExecutingTimerCallbacks > 0)
            {
                Thread.Sleep(1);
            }
        }


        /// <summary>
        /// Take a screenshot from all monitors and save it to disk. Add the current timestamp and the hit position to the picture to make
        /// it easy to correlate the traces with the visual interactions.
        /// </summary>
        /// <param name="clickX">Screen x-coordinate of click event or -1 if not available.</param>
        /// <param name="clickY">Screen y-cooridnate of click event or -1 if not available.</param>
        /// <param name="suffix">This suffix is appended to screenshot file name</param>
        /// <param name="suffixOfSecondScreenshot">If non null a second screenshot is taken with this suffix after 500ms.</param>
        /// <returns>Saved screenshot file name in the string tuple or the exception.</returns>
        public KeyValuePair<string,Exception> TakeScreenshot(int clickX, int clickY, string suffix, string suffixOfSecondScreenshot)
        {
            try
            {
                lock (Lock)
                {
                    if (IsDisposed)
                    {
                        return new KeyValuePair<string, Exception>(null, null);
                    }

                    // Re-read the monitor layout per capture. Screen.AllScreens invalidates its cached screen
                    // set when display settings change (internal SystemEvents.DisplaySettingsChanging handler),
                    // so this picks up monitors added, removed or moved during a long recording. The bitmap
                    // origin is derived from the same union bounds the coordinate mapper uses, so bitmap
                    // coordinates and marker coordinates can never disagree.
                    IScreen[] screens = Screen.AllScreens.Select(x => new ScreenFacade(x)).ToArray();
                    Rectangle virtualBounds = ScreenCoordinateMapper.VirtualScreenBounds(screens);
                    if (virtualBounds.Width <= 0 || virtualBounds.Height <= 0)
                    {
                        return new KeyValuePair<string, Exception>(null, null);
                    }

                    var now = DateTime.Now;

                    // Create a bitmap of the appropriate size to receive the screenshot.
                    using (Bitmap bmp = new Bitmap(virtualBounds.Width, virtualBounds.Height))
                    {
                        // Draw the screenshot into our bitmap.
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.CopyFromScreen(virtualBounds.Left, virtualBounds.Top, 0, 0, bmp.Size);

                            TimeStampBitmap(now, bmp);

                            bool bClicked = clickX != -1 && clickY != -1;

                            // Mark the actual event position when we have it. Falling back to the live
                            // cursor position only when no click position was passed produces a marker
                            // that reflects where the click happened instead of where the cursor has
                            // already moved to by the time the asynchronous capture runs.
                            Point markerSource = bClicked ? new Point(clickX, clickY) : Cursor.Position;
                            Point markerPos = ScreenCoordinateMapper.ScreenToDesktopCoordinates(markerSource.X, markerSource.Y, screens);

                            MarkMouseClick(markerPos.X, markerPos.Y, bmp, g, bClicked);
                        }

                        string savePath = Path.Combine(ScreenshotDirectory, ScreenshotFileNameBase + suffix + ".jpg");
                        bmp.Save(savePath, JpgEncoder, EncoderParameters);
                        HookEvents.ETWProvider.Screenshot(suffix, savePath);

                        if (!IsDisposed && !String.IsNullOrEmpty(suffixOfSecondScreenshot))
                        {
                            Task.Factory.StartNew(() => TakeAnotherScreenshot(suffixOfSecondScreenshot), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
                        }

                        try
                        {
                            // Update creation time to actual screenshot snapshot time so 
                            // we can use the file creation date as snapshot time to sort them during report generation
                            var fileInfo = new FileInfo(savePath)
                            {
                                CreationTime = now
                            };
                        }
                        catch(Exception) // may fail if in the meantime the file has been moved.
                        {
                        }

                        return new KeyValuePair<string, Exception>(savePath, null);
                    }
                }
            }
            catch(Win32Exception ex)
            {
                Trace.TraceWarning($"Could not take screenshot due to {ex}.");
                return new KeyValuePair<string, Exception>(null, ex);
            }
        }


        void TakeAnotherScreenshot(string suffix, bool bSleep=true)
        {
            if (bSleep)
            {
                Thread.Sleep(500);
            }

            // throttle creation of new screenshots after a click to one screenshot every 500ms
            if (DateTime.Now - LastOtherScreenshot > SecondScreenshotTimerAfterClick || !bSleep)
            {
                TakeScreenshot(-1, -1, suffix, null);
                LastOtherScreenshot = DateTime.Now;
            }
        }

        /// <summary>
        /// Draw a red rectangle where the click event did happen so it is easy to find in the screenshots.
        /// Coordinates must be bitmap coordinates relative to the top left of the union bounds of all
        /// monitors, i.e. the result of ScreenCoordinateMapper.ScreenToDesktopCoordinates.
        /// Coordinates outside the bitmap are skipped instead of mirrored via Math.Abs to the wrong quadrant.
        /// </summary>
        /// <param name="x">Bitmap x-coordinate of the marker.</param>
        /// <param name="y">Bitmap y-coordinate of the marker.</param>
        /// <param name="bmp">Screenshot bitmap.</param>
        /// <param name="g">Graphics object created on the bitmap.</param>
        /// <param name="wasClicked">True draws a red marker (click), false a green one (no click).</param>
        public static void MarkMouseClick(int x, int y, Bitmap bmp, Graphics g, bool wasClicked)
        {
            if (x < 0 || y < 0 || x >= bmp.Width || y >= bmp.Height)
            {
                Trace.TraceWarning($"Mouse marker at ({x},{y}) is outside the screenshot bitmap and was skipped.");
                return;
            }

            // Draw the marker in one call instead of GetPixel/SetPixel for 196 pixels.
            // Translucent colors keep the original look (red = click, green = no click).
            Color markerColor = wasClicked
                ? Color.FromArgb(160, 255, 0, 0)
                : Color.FromArgb(160, 0, 255, 0);

            int width = Math.Min(14, bmp.Width - x);
            int height = Math.Min(14, bmp.Height - y);

            using (SolidBrush brush = new SolidBrush(markerColor))
            {
                g.FillRectangle(brush, x, y, width, height);
            }
        }

        /// <summary>
        /// Add a translucent time stamp to the picture to make it later easy to find in the traces the corresponding time
        /// </summary>
        /// <param name="time"></param>
        /// <param name="bmp"></param>
        internal void TimeStampBitmap(DateTime time, Bitmap bmp)
        {
            Point pos = new Point((int)(bmp.Width * 0.9), 20);
            Size size = new Size(100, 25);

            using (var g = Graphics.FromImage(bmp))
            {
                // Brush objects are not thread safe or and will lead to InvalidOperatonExceptions 
                // with System.InvalidOperationException: The object is currently in use elsewhere. 
                // See http://stackoverflow.com/questions/1060280/invalidoperationexception-object-is-currently-in-use-elsewhere-red-cross
                // The font on the other hand seems to be ok
                lock (TransparentRed)  
                {
                    g.FillRectangle(TransparentRed, new Rectangle(pos, size));
                }
                g.DrawString(time.ToString("HH:mm:ss.fff"), StampFont, Brushes.Yellow, pos.X, pos.Y);
            }
        }
    }
}
