namespace ETWController
{

    /// <summary>
    /// Abstract message box display away from ViewModel to enable unit testing
    /// </summary>
    public interface IMessageBoxDisplay
    {
        void ShowMessage(string message, string caption);
    }
}