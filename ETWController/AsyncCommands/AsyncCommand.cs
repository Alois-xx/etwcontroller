using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETWController.Commands
{
    public enum CommandState
    {
        NotStarted,
        Starting,
        Finished,
    }

    /// <summary>
    /// Execute an Action or Func on a thread pool thread and allow notifications sent back to the 
    /// UI. Derived classes are AsyncUICommand which connect to the viewmodel to show status bar messages.
    /// </summary>
    /// <typeparam name="T">Return type when for a method with a return value</typeparam>
    public class AsyncCommand<T>  where T  : class
    {
        /// <summary>
        /// Message to print via NotifyError when the command did throw an exception
        /// </summary>
        public string StartingError { get; set; }

        /// <summary>
        /// Message to print via NotifyInfo when the command did complete
        /// </summary>
        public string Started { get; set; }

        /// <summary>
        /// Mesasage to print via NotifyInfo when the command is executing
        /// </summary>
        public string Starting { get; set; }

        /// <summary>
        /// Called when the command did thrown an Exception
        /// </summary>
        public Action<string, Exception> NotifyError { get; set; }

        /// <summary>
        /// Called when start or end of the command needs to be printed
        /// </summary>
        public Action<string> NotifyInfo { get; set; }

        /// <summary>
        /// Scheduler the task is using. Null is 
        /// </summary>
        public TaskScheduler Scheduler { get; set; }

        /// <summary>
        /// Method which is executed inside a task
        /// </summary>
        public Func<T> MethodWithReturn { get; set; }

        /// <summary>
        /// Method which is executed inside a task
        /// </summary>
        public Action Method { get; set; }

        /// <summary>
        /// Called on completion via the passed Scheduler as task
        /// </summary>
        public Action<T> Completed { get; set; }

        /// <summary>
        /// Holds the Task of MethodWithReturn when executed
        /// </summary>
        public Task<T> MethodWithReturnResult { get; private set; }

        /// <summary>
        /// Holds the task of Method when executed
        /// </summary>
        public Task MethodResult { get; private set;  }

        /// <summary>
        /// Current execution state
        /// </summary>
        public CommandState ExecutionState { get; private set; }

        /// <summary>
        /// Create an asynchronous command 
        /// </summary>
        /// <param name="start">Method which returns a value to execute</param>
        /// <param name="scheduler">Scheduler to use for async command.</param>
        public AsyncCommand(Func<T> start, TaskScheduler scheduler)
        {
            MethodWithReturn = start ?? throw new ArgumentNullException("start");
            Scheduler = scheduler;
        }

        /// <summary>
        /// Create an asynchronous command which executes a method
        /// </summary>
        /// <param name="start">Method to start</param>
        /// <param name="scheduler">Scheduler to use for async command.</param>
        public AsyncCommand(Action start, TaskScheduler scheduler)
        {
            Method = start ?? throw new ArgumentNullException("start");
            Scheduler = scheduler;
        }

        /// <summary>
        /// Execute the command. When the command has not yet completed its task and therefore its result are overwritten!
        /// </summary>
        public void Execute()
        {
            if( Method == null && MethodWithReturn == null)
            {
                throw new ArgumentNullException("MethodWithReturn and Method was null. We need at least something to execute.");
            }
            if( Method != null && MethodWithReturn != null )
            {
                throw new ArgumentException("Cannot execute Method and MethodWithreturn at once.");
            }

            ExecutionState = CommandState.Starting;

            if( !String.IsNullOrEmpty(Starting) )
            {
                SafeNotifyMessage(Starting);
            }

            if( Method != null )
            {
                MethodResult = Task.Factory.StartNew(Method).ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            SafeNotifyError(String.Format("{0}\n{1}", StartingError, task.Exception.InnerException.Message), task.Exception);
                        }
                        else
                        {
                            if (!String.IsNullOrEmpty(Started))
                            {
                                SafeNotifyMessage(Started);
                            }
                        }
                        Completed?.Invoke(null);
                        ExecutionState = CommandState.Finished;
                    }, Scheduler);
            }

            if( MethodWithReturn != null)
            {
                MethodWithReturnResult = Task.Factory.StartNew<T>(MethodWithReturn).ContinueWith<T>(task =>
                {
                    if( task.IsFaulted )
                    {
                        SafeNotifyError(String.Format("{0}\n{1}", StartingError, task.Exception.InnerException.Message), task.Exception);
                        ExecutionState = CommandState.Finished;
                        return null;
                    }
                    else
                    {
                        if( !String.IsNullOrEmpty(Started))
                        {
                            SafeNotifyMessage(Started);
                        }
                        Completed?.Invoke(task.Result);

                        ExecutionState = CommandState.Finished;
                        return task.Result;
                    }
                }, Scheduler);
            }
        }

        void SafeNotifyError(string message, Exception ex)
        {
            NotifyError?.Invoke(message, ex);
        }

        void SafeNotifyMessage(string message)
        {
            NotifyInfo?.Invoke(message);
        }
    }
}
