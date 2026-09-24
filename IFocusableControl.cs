namespace MeroDokan
{
    /// <summary>
    /// Implemented by UserControls to define their ergonomic primary control that should receive 
    /// default keyboard cursor focus when displayed or switched into view.
    /// </summary>
    public interface IFocusableControl
    {
        void FocusDefaultControl();
    }
}
