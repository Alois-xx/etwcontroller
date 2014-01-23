using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace ETWControler.UI
{
    /// <summary>
    /// Execute a command binding which is bound to the passed in delegate
    /// </summary>
    public class DelegateCommand : ICommand
    {
        EventHandler _internalCanExecuteChanged;

        public DelegateCommand(Action<object> action): this(action,null)
        {

        }

        /// <summary>
        /// Create a command instance which WPF can call.
        /// </summary>
        /// <param name="action">Delegate which is called when the command is executed</param>
        /// <param name="canExecuteFunc">Can be null. Check method if command can be executed. Usually this will determine the button state.</param>
        public DelegateCommand(Action<object> action, Func<bool> canExecuteFunc)
        {
            if( action == null )
            {
                throw new ArgumentNullException("action");
            }

            CanExecuteFunc = canExecuteFunc;
            Command = action;
        }

        /// <summary>
        /// Execute current command.
        /// </summary>
        /// <param name="parameter"></param>
        public void Execute(object parameter)
        {
            Command(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                _internalCanExecuteChanged += value;
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                _internalCanExecuteChanged -= value;
                CommandManager.RequerySuggested -= value;
            }
        }

        /// <summary>
        /// Actual command to execute
        /// </summary>
        public Action<object> Command
        {
            get;
            set;
        }

        /// <summary>
        /// When set call this delegate to check the command execution status
        /// </summary>
        public Func<bool> CanExecuteFunc
        {
            get;
            set;
        }

        /// <summary>
        /// Check if current command can be executed or not depending on the return value 
        /// of CanExecuteFunc method or return true always. 
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public bool CanExecute(object parameter)
        {
            if (CanExecuteFunc == null)
            {
                return true;
            }
            else
            {
                return CanExecuteFunc();
            }
        }


        /// <summary>
        /// This method can be used to raise the CanExecuteChanged handler.
        /// This will force WPF to re-query the status of this command directly.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged();
        }

        /// <summary>
        /// This method is used to walk the delegate chain and well WPF that
        /// our command execution status has changed.
        /// </summary>
        protected virtual void OnCanExecuteChanged()
        {
            EventHandler eCanExecuteChanged = _internalCanExecuteChanged;
            if (eCanExecuteChanged != null)
                eCanExecuteChanged(this, EventArgs.Empty);
        }
    }
}
