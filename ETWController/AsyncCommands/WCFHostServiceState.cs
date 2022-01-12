using ETWController.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ETWController.Commands
{
    class WCFHostServiceState
    {
        ViewModel Model;
        AsyncUICommand AsyncInitializer;
        volatile ServiceHost Host;
        public AsyncUICommand<string[]> GetTraceSessions;
        public AsyncUICommand<string> ExecuteWPRCommand;
        TaskScheduler Scheduler;

        public WCFHostServiceState(ViewModel model, TaskScheduler scheduler)
        {
            Model = model;
            Scheduler = scheduler;
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
            }, Model, Scheduler)
            {
                StartingError = "Could not start TraceControllerService service. Error: ",
                Starting = "Starting TraceControllerService at " + Model.LocalTraceServiceUrl,
                Started = "TraceControllerService started at " + Model.LocalTraceServiceUrl
            };
        }

        AsyncUICommand<string[]> CreateGetTraceSessions()
        {
            return new AsyncUICommand<string[]>(() =>
            {
                SelfHostedService service = new SelfHostedService(Model.TraceServiceUrl);
                return service.UseService<string[]>(webservice => webservice.GetTraceSessions());
            }, Model, Scheduler)
            {
                Starting = "Fetching active trace sessions from remote machine",
                Started = "Fetched Trace Sessions from remote machine",
                Completed = sessions => Model.ServerTraceSessions = sessions
            };
        }

        public AsyncUICommand<Tuple<int,string>> CreateExecuteWPRCommand(string wprArgs)
        {
            return new AsyncUICommand<Tuple<int,string>>(() =>
            {
                SelfHostedService service = new SelfHostedService(Model.TraceServiceUrl);
                return service.UseService<Tuple<int,string>>(webservice => webservice.ExecuteWPRCommand(wprArgs));
            }, Model, Scheduler)
            {
                Starting = String.Format("Executing on host '{0}': {1}", Model.Host, wprArgs),
                Started = "WPR completed on host '" + Model.Host + "'",
                StartingError = "WPR command could not be executed on host '" + Model.Host + "'",
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
