using ETWController.Screenshots;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWControler_uTest
{
    [TestFixture]
    public class HtmlReportGeneratorTests
    {
        [Test]
        [Explicit]
        public void GenerateHtmlReport()
        {
            string path = @"C:\temp\ScreensTest";
            Directory.CreateDirectory(path);
            var gen = new HtmlReportGenerator(path);
            gen.GenerateReport();
        }

        [Test]
        public void ThrowsOnNotFoundDirectory()
        {
            Assert.Throws<DirectoryNotFoundException>(() =>
                 new HtmlReportGenerator("asdfas;ldfkjasd;lkfj;lsadk"));
        }
    }
}
