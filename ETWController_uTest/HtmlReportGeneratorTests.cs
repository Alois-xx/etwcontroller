using ETWController.Screenshots;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWController_uTest
{
    public class HtmlReportGeneratorTests
    {
        [Fact(Skip = "Explicit test, run manually")]
        public void GenerateHtmlReport()
        {
            string path = @"C:\temp\ScreensTest";
            Directory.CreateDirectory(path);
            var gen = new HtmlReportGenerator(path);
            gen.GenerateReport();
        }

        [Fact]
        public void ThrowsOnNotFoundDirectory()
        {
            Assert.Throws<DirectoryNotFoundException>(() =>
                 new HtmlReportGenerator("asdfas;ldfkjasd;lkfj;lsadk"));
        }
    }
}
