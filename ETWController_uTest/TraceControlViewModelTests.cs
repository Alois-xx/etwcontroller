using ETWController.UI;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWController_uTest
{
    public class TraceControlViewModelTests
    {
        [Fact]
        public void CanExtract_Exe_Name()
        {
            const string cmdLine = "wpa -i c:\\temp\\otherfile.etl";
            string exe = TraceControlViewModel.ExtractExecName(cmdLine);
            Assert.Equal("wpa", exe);
        }

        [Fact]
        public void Can_Extract_Exe_Name_With_Quotation_Marks()
        {
            const string cmdLine = "\"C:\\program files\\wpa.exe\" -i c:\\temp\\otherfile.etl";
            string exe = TraceControlViewModel.ExtractExecName(cmdLine);
            Assert.Equal("\"C:\\program files\\wpa.exe\"", exe);
            Console.WriteLine($"ExeName: {exe}");
        }

        [Fact]
        public void CanExtractDirectoryWithPlaceholders()
        {
            string ret = TraceControlViewModel.GetDirectoryNameFromFileName(@"C:\temp\etlFile%COMPUTERNAME%.ETL");
            Assert.Equal(@"C:\temp", ret);
        }
    }
}
