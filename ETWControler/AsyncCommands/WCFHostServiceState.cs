using ETWControler.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ETWControler.Commands
{
    class WCFHostServiceState
    {
        ViewModel Model;
        AsyncUICommand AsyncInitializer;
        volatile ServiceHost Host;
        public AsyncUICommand<string[]> GetTraceSessions;
        public AsyncUICommand<string> ExecuteWPRCommand;

        public WCFHostServiceState(ViewModel model)
        {
            Model = model;
            GetTraceSessions = CreateGetTraceSessions();
            AsyncInitializer = CreateService();
            AsyncInitializer.Execute();
        }

        AsyncUICommand CreateService()
        {
            return new AsyncUICommand(() =>
            {
                var service = new SelfHostedService(Model.TraceServiceUrl);
                try
                {
                    Host = service.HostService(Model.LocalTraceServiceUrl);
                }
                catch (AddressAlreadyInUseException)
                {
                    // Server already running inside another instance 
                }
            }, Model)
            {
                StartingError = "Could not start TraceControlerService service. Error: ",
                Starting = "Starting TraceControlerService" + Model.LocalTraceServiceUrl,
                Started = "Started TraceContolerService at " + Model.LocalTraceServiceUrl
            };
        }

        AsyncUICommand<string[]> CreateGetTraceSessions()
        {
            return new AsyncUICommand<string[]>(() =>
            {
                SelfHostedService service = new SelfHostedService(Model.TraceServiceUrl);
                return service.UseService<string[]>(webservice => webservice.GetTraceSessions());
            }, Model)
            {
                Starting = "Fetching active trace sessions from server",
                Started = "Fetched Trace Sessions",
                Completed = sessions => Model.ServerTraceSessions = sessions
            };
        }

        public AsyncUICommand<Tuple<int,string>> CreateExecuteWPRCommand(string wprArgs)
        {
            return new AsyncUICommand<Tuple<int,string>>(() =>
            {
                SelfHostedService service = new SelfHostedService(Model.TraceServiceUrl);
                return service.UseService<Tuple<int,string>>(webservice => webservice.ExecuteWPRCommand(wprArgs));
            }, Model)
            {
                Starting = String.Format("Execute on host {0}: wpr.exe {1}", Model.Host, wprArgs),
                Started = "WPR completed on host" + Model.Host,
                StartingError = "WPR command could not be executed on host " + Model.Host,
            };
        }

        public void Restart()
        {
            if (Host != null)
            {
                Host.Close();
                Host = null;
            }

            AsyncInitializer = CreateService();
            AsyncInitializer.Execute();
        }
    }
}
