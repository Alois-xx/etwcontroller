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
            // get top left coordinates of virtual desktop boundary in screen coordinates
            var topLeftVirtualDesktopZeroPoint = allScreens.Select(screen => screen.Bounds).Aggregate(Point.Empty,
                (point, screen) => new Point(
                                          Math.Min(point.X, screen.X),
                                          Math.Min(point.Y, screen.Y)));

            // calculate shift vector
            int shiftVectorX = -topLeftVirtualDesktopZeroPoint.X;
            int shiftVectorY = -topLeftVirtualDesktopZeroPoint.Y;
            return new Point(shiftVectorX, shiftVectorY);
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
