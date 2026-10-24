/* -------------------------------------------------------------------------------------------------
   Restricted - Copyright (C) Siemens Healthcare GmbH/Siemens Medical Solutions USA, Inc., 2016. All rights reserved
   ------------------------------------------------------------------------------------------------- */

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ETWController.Screenshots
{
    /// <summary>
    /// Maps virtual desktop coordiantes to and from virtual screen coordinates.
    /// </summary>
    class ScreenCoordinateMapper
    {
        public static Point DesktopToScreenCoordinates(int x, int y, IScreen[] allScreens)
        {
            var lret = Point.Empty;

            Point shift = GetShiftVector(allScreens);

            // shift point by shift vector into screen coordinates relative to the primary monitor.
            lret = new Point(x - shift.X,
                             y - shift.Y);

            return lret;
        }

        public static Point ScreenToDesktopCoordinates(int x, int y, IScreen[] allScreens)
        {
            var lret = Point.Empty;

            Point shift = GetShiftVector(allScreens);

            // shift point by shift vector into screen coordinates relative to the primary monitor.
            lret = new Point(x + shift.X,
                             y + shift.Y);

            return lret;
        }

        static Point GetShiftVector(IScreen[] allScreens)
        {
            if (allScreens == null || allScreens.Length == 0)
            {
                return Point.Empty;
            }

            Rectangle bounds = VirtualScreenBounds(allScreens);

            // calculate shift vector from the top left of the virtual desktop boundary
            return new Point(-bounds.X, -bounds.Y);
        }

        /// <summary>
        /// Returns the minimum bounding rectangle of all monitors in screen coordinates.
        /// This is the single source of truth for both the screenshot bitmap origin and the
        /// marker coordinates, so bitmap origin and coordinate mapping can never disagree.
        /// </summary>
        public static Rectangle VirtualScreenBounds(IScreen[] allScreens)
        {
            int left = int.MaxValue, top = int.MaxValue;
            int right = int.MinValue, bottom = int.MinValue;

            foreach (var screen in allScreens)
            {
                left = Math.Min(left, screen.Bounds.Left);
                top = Math.Min(top, screen.Bounds.Top);
                right = Math.Max(right, screen.Bounds.Right);
                bottom = Math.Max(bottom, screen.Bounds.Bottom);
            }

            return new Rectangle(left, top, right - left, bottom - top);
        }
    }

    /// <summary>
    /// Testability interface
    /// </summary>
    interface IScreen
    {
        Rectangle Bounds { get; }

        bool IsPrimary { get; }
    }

    /// <summary>
    /// Testability to wrapp Screen instances
    /// </summary>
    class ScreenFacade : IScreen
    {
        Rectangle _Bounds;
        bool _IsPrimary;
        public ScreenFacade(Screen screen)
        {
            _Bounds = screen.Bounds;
            _IsPrimary = screen.Primary;
        }

        internal ScreenFacade(Rectangle rect, bool isPrimary = false)
        {
            _Bounds = rect;
            _IsPrimary = isPrimary;
        }

        public Rectangle Bounds
        {
            get
            {
                return _Bounds;
            }
        }

        /// <summary>
        /// The Screen coordinate system starts the the top left corner with 0,0 in screen coordinates of the primary monitor.
        /// </summary>
        public bool IsPrimary
        {
            get { return _IsPrimary; }
        }
    }
}
