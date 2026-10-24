/* -------------------------------------------------------------------------------------------------
   Restricted - Copyright (C) Siemens Healthcare GmbH/Siemens Medical Solutions USA, Inc., 2016. All rights reserved
   ------------------------------------------------------------------------------------------------- */

using Xunit;
using System.Drawing;
using ETWController.Screenshots;
using System;
using System.Diagnostics;

namespace ETWController_uTest
{
    public class ScreenCoordinateMapperTests
    {
        /// <summary>
        /// Virtual coordinates is the minimum bounding rectangle of all attached monitors where the 0,0 is the top left start point of the 
        /// top left monitor. 
        /// (0,0)     ------------------> x  (100,0)
        ///           |
        ///           |
        ///           | 
        ///           |
        ///           |
        /// (0,100) y ˅
        /// 
        /// 
        /// Screen.AllMonitors returns the monitors in monitor screen coordinates relative to Screen 1 which starts at 0,0 in monitor coordinates. 
        /// 
        /// The monitor bounding rectangles are specified in rectangles with top/left coordinates in virtual desktop coordinates. 
        /// Test the virtual to screen coordinates calculation with two overlapping heights like this
        /// |-----------------------|
        /// |                       |-----------------|
        /// |-----------------------|                 |
        ///                         |-----------------|
        /// </summary>
        [Fact]
        public void DesktopToScreenCoordinates()
        {
            var mon1 = new ScreenFacade(new Rectangle(0,0, 200, 100), isPrimary: true);
            var mon2 = new ScreenFacade(new Rectangle(200, 50, 200, 100));
            
            var monPoint = ScreenCoordinateMapper.DesktopToScreenCoordinates(200, 50, new IScreen[] { mon1, mon2 });
            Assert.Equal(200, monPoint.X);
            Assert.Equal(50, monPoint.Y);

           // var point2 = ScreenshotCoordinateMapper.
        }

        /// <summary>
        /// Place monitor on quadrant III and calculate bitmap coordinates
        /// Screen coordinate system 
        /// (0,0)
        /// --------------->x
        /// |
        /// |
        /// |
        /// ˅y             (100,100)
        /// 
        ///  II   |   I
        ///  -----------
        ///  III  |   IV
        /// </summary>
        [Fact]
        public void CalcBoundsOnQuadrantIII()
        {
            ScreenFacade negMon = new ScreenFacade(new Rectangle(-100, 0, 100, 200));
            IScreen[] screens = new IScreen[] { negMon };
            var xy = ScreenCoordinateMapper.ScreenToDesktopCoordinates(-10, 20, screens);
            Assert.Equal(90, xy.X);
            Assert.Equal(20, xy.Y);

        }

        /// <summary>
        /// Place monitor on quadrant I and calculate bitmap coordinates
        /// Screen coordinate system 
        /// (0,0)
        /// --------------->x
        /// |
        /// |
        /// |
        /// ˅y             (100,100)
        /// 
        ///  II   |   I
        ///  ----------->
        ///  III  |   IV
        ///       ˅
        /// </summary>
        [Fact]
        public void CalcBoundsOnQuadrantI()
        {
            ScreenFacade negMon = new ScreenFacade(new Rectangle(0,-200, 100, 200));
            ScreenFacade primary = new ScreenFacade(new Rectangle(0, 0, 150, 250));
            IScreen[] screens = new IScreen[] { negMon, primary };
            var xy = ScreenCoordinateMapper.ScreenToDesktopCoordinates(10, -20, screens);
            Assert.Equal(10, xy.X);
            Assert.Equal(180, xy.Y);
        }

        /// <summary>
        /// Place monitor on quadrant 2 and calculate bitmap coordinates
        /// Screen coordinate system 
        /// (0,0)
        /// --------------->x
        /// |
        /// |
        /// |
        /// ˅y             (100,100)
        /// 
        ///  II   |   I
        ///  ----------->
        ///  III  |   IV
        ///       ˅
        /// </summary>
        [Fact]
        public void CalcBoundsOnQuadrantII()
        {
            ScreenFacade negMon = new ScreenFacade(new Rectangle(-100, -200, 100, 200));
            IScreen[] screens = new IScreen[] { negMon };
            var xy = ScreenCoordinateMapper.ScreenToDesktopCoordinates(-10, -20, screens);
            Assert.Equal(90, xy.X);
            Assert.Equal(180, xy.Y);
        }


        /// <summary>
        /// Place monitor on quadrant IV and calculate bitmap coordinates
        /// Screen coordinate system 
        /// (0,0)
        /// --------------->x
        /// |
        /// |
        /// |
        /// ˅y             (100,100)
        ///  II   |   I
        ///  -----------
        ///  III  |   IV
        /// </summary>
        [Fact]
        public void CalcBoundsOnQuadrantIV()
        {
            ScreenFacade negMon = new ScreenFacade(new Rectangle(0, 0, 100, 200));
            IScreen[] screens = new IScreen[] { negMon };
            var xy = ScreenCoordinateMapper.ScreenToDesktopCoordinates(10, 20, screens);
            Assert.Equal(10, xy.X);
            Assert.Equal(20, xy.Y);
        }


        /// <summary>
        /// Place rectangle between I/IV and check some boundary conditions
        /// Screen coordinate system 
        /// (0,0)
        /// --------------->x
        /// |
        /// |
        /// |
        /// ˅y             (100,100)
        /// 
        ///  II   |   I
        ///    |--|---|
        ///  --|--|---|----------->
        ///    |--|---|
        ///  III  |   IV
        ///       |
        ///       ˅
        /// </summary>
        [Fact]
        public void CalcOverlappingQuadrants()
        {
            ScreenFacade negMon = new ScreenFacade(new Rectangle(-50, -50, 100, 200));
            IScreen[] screens = new IScreen[] { negMon };
            var xy = ScreenCoordinateMapper.ScreenToDesktopCoordinates(-10, -20, screens);
            Assert.Equal(40, xy.X);
            Assert.Equal(30, xy.Y);

            var xy00 = ScreenCoordinateMapper.ScreenToDesktopCoordinates(0, 0, screens);
            Assert.Equal(50, xy00.X);
            Assert.Equal(50, xy00.Y);

            var xy50_150 = ScreenCoordinateMapper.ScreenToDesktopCoordinates(49, 149, screens);
            Assert.Equal(99, xy50_150.X);
            Assert.Equal(199, xy50_150.Y);
        }

        [Fact]
        public void PerfMaping()
        {
            var negMon = new ScreenFacade(new Rectangle(-50, -50, 100, 200));
            IScreen[] screens = new IScreen[] { negMon };
            var sw = Stopwatch.StartNew();
            const int Runs = 1000 * 1000;
            for (int i = 0; i< Runs; i++)
            {
                var xy = ScreenCoordinateMapper.ScreenToDesktopCoordinates(-10, -20, screens);
            }
            sw.Stop();
            Console.WriteLine($"Calls/s {Runs / sw.Elapsed.TotalSeconds:N0}");
        }

        [Fact]
        public void VirtualScreenBoundsOnDiagonalLayout()
        {
            // Two monitors in different quadrants that do not form a rectangular grid:
            //   mon1: origin (-100,-50), size 100x100 -> right = 0,   bottom = 50
            //   mon2: origin (0,50),      size 100x100 -> right = 100, bottom = 150
            // Union bounding box: (-100,-50) with size 200x200.
            var mon1 = new ScreenFacade(new Rectangle(-100, -50, 100, 100));
            var mon2 = new ScreenFacade(new Rectangle(0, 50, 100, 100));

            var bounds = ScreenCoordinateMapper.VirtualScreenBounds(new IScreen[] { mon1, mon2 });

            Assert.Equal(-100, bounds.X);
            Assert.Equal(-50, bounds.Y);
            Assert.Equal(200, bounds.Width);
            Assert.Equal(200, bounds.Height);

            // The shift vector must be the negated top-left origin so that mapping a point on
            // mon1 gives the same coordinates the screenshot bitmap uses.
            var mapped = ScreenCoordinateMapper.ScreenToDesktopCoordinates(-50, 0, new IScreen[] { mon1, mon2 });
            Assert.Equal(50, mapped.X);
            Assert.Equal(50, mapped.Y);
        }

        [Fact]
        public void MarkMouseClickDrawsRedMarkerAtGivenPosition()
        {
            using (var bmp = new Bitmap(100, 100))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    ScreenshotRecorder.MarkMouseClick(10, 20, bmp, g, wasClicked: true);
                }

                Color marker = bmp.GetPixel(17, 27);
                Assert.True(marker.R > 150);
                Assert.True(marker.G < 150);

                // Outside the marker area the bitmap must be untouched.
                // Note: GetPixel returns an ARGB color which is never equal to the named
                // Color.White (different Color state), so compare ToArgb() values.
                Assert.Equal(Color.FromArgb(255, 255, 255, 255).ToArgb(), bmp.GetPixel(30, 40).ToArgb());
            }
        }

        [Fact]
        public void MarkMouseClickDrawsGreenMarkerWhenNotClicked()
        {
            using (var bmp = new Bitmap(100, 100))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    ScreenshotRecorder.MarkMouseClick(50, 50, bmp, g, wasClicked: false);
                }

                Color marker = bmp.GetPixel(57, 57);
                Assert.True(marker.G > 150);
                Assert.True(marker.R < 150);
            }
        }

        [Fact]
        public void MarkMouseClickSkipsNegativeCoordinatesInsteadOfMirroring()
        {
            using (var bmp = new Bitmap(100, 100))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);

                    // The old Math.Abs implementation mirrored (-10,-10) to (10,10) and drew the
                    // marker there. Out-of-range coordinates must now be skipped instead.
                    ScreenshotRecorder.MarkMouseClick(-10, -10, bmp, g, wasClicked: true);
                }

                // Must be skipped, not mirrored: compare ARGB values because a named color
                // like Color.White is never equal to a known-color ARGB value.
                Assert.Equal(Color.FromArgb(255, 255, 255, 255).ToArgb(), bmp.GetPixel(10, 10).ToArgb());
            }
        }
    }
}
