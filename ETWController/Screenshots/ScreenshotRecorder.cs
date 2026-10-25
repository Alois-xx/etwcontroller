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
    ///
    /// A frame is processed in two stages so a capture does not have to wait for the previous frame's encoder:
    /// capture (one BitBlt into a reused buffer) on the calling thread, encode and file write on one writer thread.
    /// Between the stages there is exactly one frame slot. When the writer still owns a frame the new screenshot
    /// is dropped instead of buffered, so a capture rate above the encode rate can never make frames pile up.
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
        /// Interval the forced screenshot timer is configured with. Frames that cannot be captured in time
        /// are dropped instead of queued, so an interval below the sustainable capture rate still produces
        /// screenshots - just fewer than requested.
        /// </summary>
        int ForcedScreenshotIntervalMs;

        /// <summary>
        /// Roughly what one frame needs on this machine (measured: ~46-60 ms for the screen read plus the
        /// stamp and marker drawing). A configured interval below this cannot be sustained and is reported
        /// once at startup instead of silently disabling the timer.
        /// </summary>
        const int SustainableScreenshotTimeInMs = 99;

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

        /// <summary>
        /// The two frame buffers plus the single frame that is with the writer. Never more, frames are
        /// dropped when the writer is busy, so nothing piles up.
        /// </summary>
        FrameSlot frameSlot;

        /// <summary>
        /// Display DC that is kept instead of being created again for every screenshot.
        /// </summary>
        ScreenReader screenReader;

        Thread writerThread;
        AutoResetEvent frameReady = new AutoResetEvent(false);

        /// <summary>
        /// Max time the writer thread waits for a frame, so Dispose does not block on an idle writer.
        /// </summary>
        const int WriterWakeUpMs = 50;

        volatile bool stopRequested;

        int droppedFrames;

        /// <summary>
        /// Screenshots that had to be dropped because the writer still owned a frame. Nothing is queued,
        /// so this only grows when captures come in faster than the system can encode them.
        /// </summary>
        public int DroppedFrames
        {
            get { return Interlocked.Add(ref droppedFrames, 0); }
        }

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
        /// <param name="forcedScreenShotAfterMs">Takes a screenshot after forcedScreenShotAfterMs if no other screenshot was taken during that period. 0 disables the timer. Values below what one frame needs (~100 ms) still run: the timer fires at the configured rate and frames that cannot be captured in time are dropped instead of queued.</param>
        /// <param name="jpgCompressionLevel">Can be a value from 0-100. 100 is lossless</param>
        /// <param name="keepNNewestScreenShots">Delete oldest files every 5 minutes if total screenshot file count is over keepNNewestScreenShots.</param>
        public ScreenshotRecorder(string screenshotDirectory, int forcedScreenShotAfterMs, int jpgCompressionLevel, int keepNNewestScreenShots)
        {
            ScreenshotDirectory = screenshotDirectory;
            Directory.CreateDirectory(ScreenshotDirectory); // Ensure that dir exists

            KeepNNewestScreenShots = keepNNewestScreenShots;
            ForcedScreenshotIntervalMs = Math.Max(0, forcedScreenShotAfterMs);

            ClearFiles(ScreenshotDirectory);

            // Setup objects to save JPGs with a configured compression level
            System.Drawing.Imaging.Encoder encoder = System.Drawing.Imaging.Encoder.Quality;
            EncoderParameter encoderParameter = new EncoderParameter(encoder, (long) jpgCompressionLevel);
            EncoderParameters = new EncoderParameters(1);
            EncoderParameters.Param[0] = encoderParameter;

            JpgEncoder = GetEncoder(ImageFormat.Jpeg);

            screenReader = new ScreenReader();

            // The frame buffers are allocated once for the current monitor layout instead of once per screenshot.
            IScreen[] initialScreens = Screen.AllScreens.Select(x => new ScreenFacade(x)).ToArray();
            Rectangle initialBounds = ScreenCoordinateMapper.VirtualScreenBounds(initialScreens);
            frameSlot = new FrameSlot(Math.Max(1, initialBounds.Width), Math.Max(1, initialBounds.Height));

            writerThread = new Thread(WriterLoop)
            {
                IsBackground = true,
                Name = "ScreenshotWriter",
            };
            writerThread.Start();

            // Arm the timers last. A timer created with dueTime 0 fires immediately on a thread pool
            // thread, and its callback uses the encoder, the frame slot and the screen reader, which must
            // exist before the first callback can run.
            if (forcedScreenShotAfterMs > 0)
            {
                if (forcedScreenShotAfterMs < SustainableScreenshotTimeInMs)
                {
                    Debug.Print($"Forced Screenshot timer: {forcedScreenShotAfterMs} ms. One frame needs about {SustainableScreenshotTimeInMs} ms to capture on this machine, so frames that arrive faster than that are dropped instead of queued.");
                }

                ForcedScreenshotTimer = new System.Threading.Timer(OnScreenshotTimerExpired, null, 0, forcedScreenShotAfterMs);
            }

            FileCleanupTimer = new System.Threading.Timer(OnCleanupOldestFiles, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
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
                if (concCurrentCount == 1 && (now - LastOtherScreenshot).TotalMilliseconds >= ForcedScreenshotIntervalMs)
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
            stopRequested = true;

            if (ForcedScreenshotTimer != null)
            {
                ForcedScreenshotTimer.Change(0, Timeout.Infinite);
                ForcedScreenshotTimer.Dispose();
                ForcedScreenshotTimer = null;
            }

            if (FileCleanupTimer != null)
            {
                FileCleanupTimer.Change(0, Timeout.Infinite);
                FileCleanupTimer.Dispose();
                FileCleanupTimer = null;
            }

            // wait until all callback have exited to ensure that we have no races while creating the next ScreenshotRecorder instance
            while (ExecutingTimerCallbacks > 0)
            {
                Thread.Sleep(1);
            }

            // Let the writer finish the frame it currently owns before the buffers are disposed.
            frameReady.Set();
            if (writerThread != null)
            {
                writerThread.Join();
                writerThread = null;
            }

            if (frameSlot != null)
            {
                frameSlot.Dispose();
                frameSlot = null;
            }

            if (screenReader != null)
            {
                screenReader.Dispose();
                screenReader = null;
            }

            TransparentRed.Dispose();
            StampFont.Dispose();
        }

        /// <summary>
        /// Single writer thread. It owns exactly one buffer at a time, so there is no queue that could
        /// grow when captures come in faster than frames can be encoded.
        /// </summary>
        void WriterLoop()
        {
            while (!stopRequested)
            {
                // Poll with a timeout so disposing an idle writer does not have to wait for a frame.
                if (!frameReady.WaitOne(WriterWakeUpMs))
                    continue;

                FrameTake frame = frameSlot.Acquire();
                if (frame == null)
                    continue;

                if (JpgEncoder == null || EncoderParameters == null)
                {
                    frameSlot.Release();
                    continue;
                }

                try
                {
                    WriteFrame(frame);
                }
                finally
                {
                    frameSlot.Release();
                }
            }
        }

        void WriteFrame(FrameTake frame)
        {
            string savePath = Path.Combine(ScreenshotDirectory, ScreenshotFileNameBase + frame.Suffix + ".jpg");

            try
            {
                frame.Buffer.Save(savePath, JpgEncoder, EncoderParameters);
                HookEvents.ETWProvider.Screenshot(frame.Suffix, savePath);

                try
                {
                    // Update creation time to actual screenshot snapshot time so 
                    // we can use the file creation date as snapshot time to sort them during report generation
                    var fileInfo = new FileInfo(savePath)
                    {
                        CreationTime = frame.CaptureTime
                    };
                }
                catch(Exception) // may fail if in the meantime the file has been moved.
                {
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"Could not save screenshot {savePath} due to {ex}.");
            }
        }


        /// <summary>
        /// Take a screenshot from all monitors and hand it to the writer thread, which encodes and saves it.
        /// Add the current timestamp and the hit position to the picture to make
        /// it easy to correlate the traces with the visual interactions.
        /// </summary>
        /// <param name="clickX">Screen x-coordinate of click event or -1 if not available.</param>
        /// <param name="clickY">Screen y-cooridnate of click event or -1 if not available.</param>
        /// <param name="suffix">This suffix is appended to screenshot file name</param>
        /// <param name="suffixOfSecondScreenshot">If non null a second screenshot is taken with this suffix after 500ms.</param>
        /// <returns>Screenshot file name in the string tuple or the exception. The key is the path the frame will
        /// be written to, or null when the frame was dropped because the writer was still busy with the previous one.</returns>
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

                    // A timer callback can start on a thread pool thread before this constructor has
                    // finished, so nothing may be dereferenced before it exists.
                    if (frameSlot == null || screenReader == null)
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

                    // No queue: when the writer still owns a buffer this screenshot is dropped.
                    Bitmap bmp = frameSlot.BeginFrame();
                    if (bmp == null)
                    {
                        int dropped = Interlocked.Increment(ref droppedFrames);
                        if (dropped == 1 || dropped % 100 == 0)
                        {
                            Trace.TraceWarning($"Screenshot {suffix} dropped because the writer is still saving the previous frame ({dropped} dropped in total).");
                        }
                        return new KeyValuePair<string, Exception>(null, null);
                    }

                    // Reallocate only when the monitor layout changed, otherwise the buffers are reused for every frame.
                    if (frameSlot.EnsureSize(virtualBounds.Width, virtualBounds.Height))
                    {
                        bmp = frameSlot.CaptureBuffer;
                        screenReader.Invalidate(); // the cached display DC keeps the extent it was created with
                    }

                    var now = DateTime.Now;

                    // The Graphics must be created on the buffer BeginFrame handed back, because the slot
                    // alternates between its two buffers. Creating it per frame costs 0.0 ms (measured),
                    // so there is no reason to keep one Graphics alive for the whole recorder.
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        // Exactly one screen read per frame. A screen read costs about 17 ms of fixed overhead per
                        // call regardless of the captured area, so splitting it into strips would multiply that cost.
                        screenReader.Capture(g, virtualBounds);

                        TimeStampBitmap(now, bmp);

                        bool bClicked = clickX != -1 && clickY != -1;

                        // Mark the actual event position when we have it. Falling back to the live
                        // cursor position only when no click position was passed produces a marker
                        // that reflects where the click happened instead of where the cursor has
                        // already moved to by the time the asynchronous capture runs.
                        Point markerSource = bClicked ? new Point(clickX, clickY) : Cursor.Position;
                        Point markerPos = ScreenCoordinateMapper.ScreenToDesktopCoordinates(markerSource.X, markerSource.Y, screens);

                        MarkMouseClick(markerPos.X, markerPos.Y, bmp, g, bClicked);

                        // GDI+ buffers drawing commands until a flush and the writer thread must not use the
                        // Graphics object, so push everything before the buffer is handed over.
                        g.Flush();
                    }

                    string savePath = Path.Combine(ScreenshotDirectory, ScreenshotFileNameBase + suffix + ".jpg");

                    if (!frameSlot.EndFrame(suffix, now))
                    {
                        // Cannot happen while captures are serialized, but frames are dropped rather than
                        // buffered, so keep the guard.
                        int dropped = Interlocked.Increment(ref droppedFrames);
                        if (dropped == 1 || dropped % 100 == 0)
                        {
                            Trace.TraceWarning($"Screenshot {suffix} dropped after capture because the writer became busy ({dropped} dropped in total).");
                        }
                        return new KeyValuePair<string, Exception>(null, null);
                    }

                    frameReady.Set();

                    if (!IsDisposed && !String.IsNullOrEmpty(suffixOfSecondScreenshot))
                    {
                        Task.Factory.StartNew(() => TakeAnotherScreenshot(suffixOfSecondScreenshot), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
                    }

                    return new KeyValuePair<string, Exception>(savePath, null);
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
