using ETWController.UI;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWControler_uTest
{
    [TestFixture]
    public class TraceControlViewModelTests
    {
        [Test]
        public void CanExtract_Exe_Name()
        {
            const string cmdLine = "wpa -i c:\\temp\\otherfile.etl";
            string exe = TraceControlViewModel.ExtractExecName(cmdLine);
            Assert.AreEqual("wpa", exe);
        }

        [Test]
        public void Can_Extract_Exe_Name_With_Quotation_Marks()
        {
            const string cmdLine = "\"C:\\program files\\wpa.exe\" -i c:\\temp\\otherfile.etl";
            string exe = TraceControlViewModel.ExtractExecName(cmdLine);
            Assert.AreEqual("\"C:\\program files\\wpa.exe\"", exe);
            Console.WriteLine($"ExeName: {exe}");
        }

        [Test]
        public void CanExtractDirectoryWithPlaceholders()
        {
            string ret = TraceControlViewModel.GetDirectoryNameFromFileName(@"C:\temp\etlFile%COMPUTERNAME%.ETL");
            Assert.AreEqual(@"C:\temp", ret);
        }
    }
}
