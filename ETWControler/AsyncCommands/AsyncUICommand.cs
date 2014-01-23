using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ETWControler.Commands
{
    public class AsyncUICommand <T> : AsyncCommand<T> where T : class 
    {
        public AsyncUICommand(Func<T> start, ViewModel model):base(start)
        {
            base.NotifyInfo = model.SetStatusMessage;
            base.NotifyError = (message, ex) => model.SetStatusMessageWarning(message, ex);
        }
    }

    public class AsyncUICommand : AsyncCommand<object>
    {
        public AsyncUICommand(Action start, ViewModel model)
            : base(start)
        {
            base.NotifyInfo = model.SetStatusMessage;
            base.NotifyError = (message, ex) => model.SetStatusMessageWarning(message, ex);
        }
    }
}
