using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ETWController.Screenshots
{
    /// <summary>
    /// Copies the desktop pixels into an existing bitmap with one BitBlt call.
    ///
    /// Measured on a 3840x2160 desktop: a screen read costs about 17 ms of fixed overhead plus the
    /// pixel copy, and the fixed part is paid per call no matter how small the captured area is.
    /// GDI+ Graphics.CopyFromScreen creates a new display DC for every call, which costs another
    /// ~15 ms per frame on top of the blit itself. Therefore the display DC is created once and
    /// reused, and the recorder must do exactly one screen read per frame.
    ///
    /// Not thread safe: use it from one thread at a time (the recorder serializes its captures).
    /// </summary>
    sealed class ScreenReader : IDisposable
    {
        const uint SRCCOPY = 0x00CC0020;

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

        [DllImport("gdi32.dll")]
        static extern bool BitBlt(IntPtr hdc, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, uint rop);

        [DllImport("gdi32.dll")]
        static extern bool DeleteDC(IntPtr hdc);

        IntPtr displayDc;

        /// <summary>
        /// Creates the display DC once. If it cannot be created Capture falls back to
        /// Graphics.CopyFromScreen.
        /// </summary>
        public ScreenReader()
        {
            displayDc = CreateDC("DISPLAY", null, null, IntPtr.Zero);
        }

        /// <summary>
        /// Call after the monitor layout changed, because the cached display DC keeps the desktop
        /// extent it was created with.
        /// </summary>
        public void Invalidate()
        {
            if (displayDc == IntPtr.Zero)
                return;

            DeleteDC(displayDc);
            displayDc = CreateDC("DISPLAY", null, null, IntPtr.Zero);
        }

        /// <summary>
        /// Blits the given desktop area into the bitmap the Graphics object was created on.
        /// Coordinates are desktop coordinates; the destination is the top left of the bitmap.
        /// </summary>
        public void Capture(Graphics target, Rectangle bounds)
        {
            if (displayDc == IntPtr.Zero)
            {
                target.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, new Size(bounds.Width, bounds.Height));
                return;
            }

            target.Flush(); // GDI+ must push its pending drawing commands before the HDC is used

            IntPtr targetHdc = target.GetHdc();
            try
            {
                BitBlt(targetHdc, 0, 0, bounds.Width, bounds.Height, displayDc, bounds.Left, bounds.Top, SRCCOPY);
            }
            finally
            {
                // The HDC has to be released before the Graphics object can be used again.
                target.ReleaseHdc(targetHdc);
            }
        }

        public void Dispose()
        {
            if (displayDc != IntPtr.Zero)
            {
                DeleteDC(displayDc);
                displayDc = IntPtr.Zero;
            }
        }
    }
}
