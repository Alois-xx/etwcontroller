using ETWController;
using ETWController_uTest.TestHelper;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWController_uTest
{
    public class RedirectedProcessTests
    {
        [Fact]
        public void Can_Redirect_Stderr()
        {
            RedirectedProcess proc = new RedirectedProcess("cmd", "/c echo hi from stderr 1>&2");
            var lret = proc.Start();
            Console.WriteLine("Exit code: {0}", lret.Item1);
            Console.WriteLine(lret.Item2);
            Assert.Equal("hi from stderr ", lret.Item2.TrimEnd(Environment.NewLine.ToArray()));
        }

        [Fact]
        public void Can_Redirect_Stdout()
        {
            RedirectedProcess proc = new RedirectedProcess("cmd", "/c echo hi from stdout");
            var lret = proc.Start();
            Console.WriteLine("Exit code: {0}", lret.Item1);
            Console.WriteLine(lret.Item2);
            Assert.Equal("hi from stdout", lret.Item2.TrimEnd(Environment.NewLine.ToArray()));
        }

        [Fact]
        public void Can_Redirect_Large_Data()
        {
           using(var tmp = TempDir.Create())
           {
               var txtfile = Path.Combine(tmp.Name, "test.text");
               string [] lines = Enumerable.Range(0, 10000).Select(x => String.Format("Line {0}", x)).ToArray();
               File.WriteAllLines(txtfile, lines);
               var type = new RedirectedProcess("cmd", String.Format("/C type {0}",txtfile));
               var lret = type.Start();
               Assert.Equal(String.Join(Environment.NewLine, lines), lret.Item2.TrimEnd(Environment.NewLine.ToCharArray()));
           }
        }
    }
}
