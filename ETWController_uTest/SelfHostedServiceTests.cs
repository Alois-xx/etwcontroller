using ETWController;
using ETWController.Network;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ETWController_uTest
{
    [TestFixture]
    public class SelfHostedServiceTests
    {
        const string WCFServerUri = "http://localhost:8090/TraceControlerService";

        [Test]
        public void HostServiceAndConnect()
        {
            SelfHostedService server = new SelfHostedService(WCFServerUri);
            using (var host = server.HostService(WCFServerUri))
            {
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < 1000; i++)
                {
                    server.UseService((service) => service.DummyMethod());
                }
                sw.Stop();
                Console.WriteLine("1000 calls did take {0:F2}s", sw.Elapsed.TotalSeconds);
            }
        }

        [Test]
        public void Server_Does_Disconnect()
        {
            SelfHostedService server = new SelfHostedService(WCFServerUri);
            var host = server.HostService(WCFServerUri);
            for (int i = 0; i < 1000; i++)
            {
                if (i == 500)
                {
                    host.Close();
                    Assert.Throws<EndpointNotFoundException>( () =>
                        {
                           server.UseService((service) => service.DummyMethod());
                        }
                    );
                    break;
                }
                server.UseService((service) => service.DummyMethod());
            }
        }
    }
}
