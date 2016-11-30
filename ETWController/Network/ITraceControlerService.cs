using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace ETWController
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ITraceControlerService" in both code and config file together.
    [ServiceContract]
    public interface ITraceControlerService
    {
        [OperationContract]
        Tuple<int,string> ExecuteWPRCommand(string wpaArgs);

        [OperationContract]
        string[] GetTraceSessions();

        [OperationContract]
        void DummyMethod();
    }
}
