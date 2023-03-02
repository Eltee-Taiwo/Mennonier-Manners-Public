namespace TaiwoTech.MennoniteManners.App.Services.Keyboard
{
    public partial class KeyboardHelperService
    {
        /// <summary>
        /// Gets a <see cref="EventHandler{T}" /> which notifies when the keyboard has been opened.
        /// <br />
        /// Ensure you unregister from this event when navigating away from the page.
        /// </summary>
        public event EventHandler<float> KeyboardOpened;

        /// <summary>
        /// Gets a <see cref="EventHandler" /> which notifies when the keyboard has been closed.
        /// <br />
        /// Ensure you unregister from this event when navigating away from the page.
        /// </summary>
        public event EventHandler KeyboardClosed;
    }
}
