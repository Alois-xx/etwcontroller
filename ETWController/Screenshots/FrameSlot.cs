using System;
using System.Drawing;

namespace ETWController.Screenshots
{
    /// <summary>
    /// One frame that was captured into a buffer and is handed to the writer thread.
    /// </summary>
    sealed class FrameTake
    {
        public Bitmap Buffer { get; }
        public string Suffix { get; }
        public DateTime CaptureTime { get; }

        public FrameTake(Bitmap buffer, string suffix, DateTime captureTime)
        {
            Buffer = buffer;
            Suffix = suffix;
            CaptureTime = captureTime;
        }
    }

    /// <summary>
    /// Holds the two frame buffers of the recorder and the single frame that is currently with the
    /// writer thread.
    ///
    /// There is deliberately no queue: at most one frame is in flight. When the writer still owns a
    /// buffer, BeginFrame returns null and the caller has to drop that screenshot instead of
    /// buffering it. If screenshots are produced faster than the system can encode them this keeps
    /// memory flat (two buffers) instead of piling up unfinished frames.
    ///
    /// Capture path: BeginFrame() -> draw into the returned buffer -> EndFrame().
    /// Writer path:   Acquire()      -> save the buffer                 -> Release().
    /// </summary>
    sealed class FrameSlot : IDisposable
    {
        readonly object gate = new object();

        Bitmap freeBuffer;       // buffer the capture path may draw into
        Bitmap inFlightBuffer;   // buffer owned by the writer while inFlight is true
        bool inFlight;

        string pendingSuffix;
        DateTime pendingCaptureTime;

        public FrameSlot(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException("Frame buffers need a positive size.");

            freeBuffer = new Bitmap(width, height);
            inFlightBuffer = new Bitmap(width, height);
        }

        /// <summary>
        /// Buffer the capture path draws into. Only valid while no frame is in flight.
        /// </summary>
        public Bitmap CaptureBuffer
        {
            get
            {
                lock (gate)
                {
                    return freeBuffer;
                }
            }
        }

        /// <summary>
        /// Number of frame buffers this slot owns. Stays 2 no matter how many frames are captured.
        /// </summary>
        public int BufferCount
        {
            get
            {
                lock (gate)
                {
                    return (freeBuffer != null ? 1 : 0) + (inFlightBuffer != null ? 1 : 0);
                }
            }
        }

        /// <summary>
        /// Returns the buffer the capture path may draw into, or null when the writer still owns a
        /// buffer. In that case the screenshot must be dropped, never queued.
        /// Must be paired with EndFrame when a buffer was returned.
        /// </summary>
        public Bitmap BeginFrame()
        {
            lock (gate)
            {
                return inFlight ? null : freeBuffer;
            }
        }

        /// <summary>
        /// Hands the buffer that BeginFrame returned to the writer. Returns false if the writer is
        /// still busy, which means the caller must drop the frame.
        /// </summary>
        public bool EndFrame(string suffix, DateTime captureTime)
        {
            lock (gate)
            {
                if (inFlight)
                    return false;

                // The buffer the writer used last is free (inFlight was false), so swapping keeps
                // exactly two buffers alive and hands out a buffer the writer is not touching.
                Bitmap handed = freeBuffer;
                freeBuffer = inFlightBuffer;
                inFlightBuffer = handed;

                inFlight = true;
                pendingSuffix = suffix;
                pendingCaptureTime = captureTime;
                return true;
            }
        }

        /// <summary>
        /// Takes ownership of the in flight buffer. The caller must call Release after saving.
        /// </summary>
        public FrameTake Acquire()
        {
            lock (gate)
            {
                if (!inFlight)
                    return null;

                return new FrameTake(inFlightBuffer, pendingSuffix, pendingCaptureTime);
            }
        }

        /// <summary>
        /// Gives the saved buffer back to the capture path.
        /// </summary>
        public void Release()
        {
            lock (gate)
            {
                inFlight = false;
            }
        }

        /// <summary>
        /// Resizes the buffer the capture path uses after a monitor layout change. The buffer that
        /// is with the writer is left alone, its already captured content must not be lost.
        /// Returns true when the capture buffer was (re)allocated.
        /// </summary>
        public bool EnsureSize(int width, int height)
        {
            lock (gate)
            {
                if (width <= 0 || height <= 0)
                    return false;

                if (freeBuffer.Width == width && freeBuffer.Height == height)
                    return false;

                Bitmap old = freeBuffer;
                freeBuffer = new Bitmap(width, height);
                old.Dispose();
                return true;
            }
        }

        public void Dispose()
        {
            lock (gate)
            {
                if (freeBuffer != null)
                    freeBuffer.Dispose();

                if (inFlightBuffer != null)
                    inFlightBuffer.Dispose();
            }
        }
    }
}
