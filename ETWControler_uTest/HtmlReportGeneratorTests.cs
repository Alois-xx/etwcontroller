using ETWControler.Screenshots;
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
            var gen = new HtmlReportGenerator(@"C:\temp\ScreensTest");
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
