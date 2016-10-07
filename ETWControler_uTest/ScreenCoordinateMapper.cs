/* -------------------------------------------------------------------------------------------------
   Restricted - Copyright (C) Siemens Healthcare GmbH/Siemens Medical Solutions USA, Inc., 2016. All rights reserved
   ------------------------------------------------------------------------------------------------- */

using NUnit.Framework;
using System.Drawing;
using ETWControler.Screenshots;
using NUnit.Framework.Compatibility;
using System;

namespace ETWControler_uTest
{
    [TestFixture]
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
        [Test]
        public void DesktopToScreenCoordinates()
        {
            var mon1 = new ScreenFacade(new Rectangle(0,0, 200, 100), isPrimary: true);
            var mon2 = new ScreenFacade(new Rectangle(200, 50, 200, 100));
            
            var monPoint = ScreenCoordinateMapper.DesktopToScreenCoordinates(200, 50, new IScreen[] { mon1, mon2 });
            Assert.AreEqual(200, monPoint.X);
            Assert.AreEqual(50, monPoint.Y);

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
        [Test]
        public void CalcBoundsOnQuadrantIII()
        {
            ScreenFacade negMon = new ScreenFacade(new Rectangle(-100, 0, 100, 200));
            IScreen[] screens = new IScreen[] { negMon };
            var xy = ScreenCoordinateMapper.ScreenToDesktopCoordinates(-10, 20, screens);
            Assert.AreEqual(90, xy.X);
            Assert.AreEqual(20, xy.Y);

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
        [Test]
        public void CalcBoundsOnQuadrantI()
        {
            ScreenFacade negMon = new ScreenFacade(new Rectangle(0,-200, 100, 200));
            ScreenFacade primary = new ScreenFacade(new Rectangle(0, 0, 150, 250));
            IScreen[] screens = new IScreen[] { negMon, primary };
            var xy = ScreenCoordinateMapper.ScreenToDesktopCoordinates(10, -20, screens);
            Assert.AreEqual(10, xy.X);
            Assert.AreEqual(180, xy.Y);
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
        [Test]
        public void CalcBoundsOnQuadrantII()
        {
            ScreenFacade negMon = new ScreenFacade(new Rectangle(-100, -200, 100, 200));
            IScreen[] screens = new IScreen[] { negMon };
            var xy = ScreenCoordinateMapper.ScreenToDesktopCoordinates(-10, -20, screens);
            Assert.AreEqual(90, xy.X);
            Assert.AreEqual(180, xy.Y);
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
        [Test]
        public void CalcBoundsOnQuadrantIV()
        {
            ScreenFacade negMon = new ScreenFacade(new Rectangle(0, 0, 100, 200));
            IScreen[] screens = new IScreen[] { negMon };
            var xy = ScreenCoordinateMapper.ScreenToDesktopCoordinates(10, 20, screens);
            Assert.AreEqual(10, xy.X);
            Assert.AreEqual(20, xy.Y);
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
        [Test]
        public void CalcOverlappingQuadrants()
        {
            ScreenFacade negMon = new ScreenFacade(new Rectangle(-50, -50, 100, 200));
            IScreen[] screens = new IScreen[] { negMon };
            var xy = ScreenCoordinateMapper.ScreenToDesktopCoordinates(-10, -20, screens);
            Assert.AreEqual(40, xy.X);
            Assert.AreEqual(30, xy.Y);

            var xy00 = ScreenCoordinateMapper.ScreenToDesktopCoordinates(0, 0, screens);
            Assert.AreEqual(50, xy00.X);
            Assert.AreEqual(50, xy00.Y);

            var xy50_150 = ScreenCoordinateMapper.ScreenToDesktopCoordinates(49, 149, screens);
            Assert.AreEqual(99, xy50_150.X);
            Assert.AreEqual(199, xy50_150.Y);
        }

        [Test]
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
    }
}
