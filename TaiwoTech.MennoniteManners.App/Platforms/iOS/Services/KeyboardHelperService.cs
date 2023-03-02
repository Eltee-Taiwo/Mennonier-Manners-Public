using UIKit;

// ReSharper disable once CheckNamespace
namespace TaiwoTech.MennoniteManners.App.Services.Keyboard
{
    public partial class KeyboardHelperService 
    {
        public KeyboardHelperService()
        {
            UIKeyboard.Notifications.ObserveWillShow((_, uiKeyboardEventArgs) =>
            {
                var newKeyboardHeight = (float)uiKeyboardEventArgs.FrameEnd.Height;
                KeyboardOpened?.Invoke(this, newKeyboardHeight);
            });

            UIKeyboard.Notifications.ObserveWillHide((_, _) =>
            {
                KeyboardClosed?.Invoke(this, EventArgs.Empty);
            });
        }
    }
}
