using TaiwoTech.MennoniteManners.App.Services.Dialog;
using TaiwoTech.MennoniteManners.App.Services.Keyboard;

namespace TaiwoTech.MennoniteManners.App.Pages.PreGame;

public partial class PreGamePage : ContentPage, IPage, IQueryAttributable
{
    private DialogService DialogService { get; }
    private readonly PreGameViewModel _viewModel;

    public PreGamePage(DialogService dialogService, PreGameViewModel viewModel)
    {
        DialogService = dialogService;
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        if (DeviceInfo.Current.Platform != DevicePlatform.iOS) return;

        var keyboardHelperService = new KeyboardHelperService();

        keyboardHelperService.KeyboardOpened += MakeEntryVisibleWhenKeyboardOpens;
        keyboardHelperService.KeyboardClosed += ResetEntryToOriginalPosition;

    }

    protected override bool OnBackButtonPressed()
    {
        DialogService.DisplayAlertAsync("Leave Game?", "Are you sure you want to leave the game?", "Yes", "No")
            .ContinueWith(async answer =>
            {
                if (answer.Result)
                {
                    await (BindingContext as PreGameViewModel)!.LeaveGame();
                }
            });

        return true;
    }

    /// <summary>
    /// This is called on page load with any parameters that are passed into it.
    /// </summary>
    /// <param name="queryAttributes"></param>
    public async void ApplyQueryAttributes(IDictionary<string, object> queryAttributes)
    {
        await _viewModel.InitialisePropertiesAsync(queryAttributes, ScrollChatToBottom, RemoveChatFocus, ClearChatText);
    }

    private async void ScrollChatToBottom()
    {
        await Dispatcher.DispatchAsync(() =>
        {
            var viewModel = (PreGameViewModel)BindingContext;
            ChatCollectionView.ScrollTo(viewModel.ChatMessages.Count - 1, position: ScrollToPosition.End);
        });
    }

    private void RemoveChatFocus()
    {
        ChatEntry.Unfocus();
        ChatEntry.IsEnabled = false;
        ChatEntry.IsEnabled = true;
    }

    private void ClearChatText()
    {
        ChatEntry.Text = string.Empty;
    }

    private async void MakeEntryVisibleWhenKeyboardOpens(object _, float keyboardHeight)
    {
        await EntryContainer.TranslateTo(0, -keyboardHeight, 100);

        EntryContainer.BackgroundColor = Colors.White;
        Grid.SetColumn(EntryContainer, 0);
        Grid.SetColumnSpan(EntryContainer, 3);
    }

    private async void ResetEntryToOriginalPosition(object _, EventArgs __)
    {

        await EntryContainer.TranslateTo(0, 0, 100);

        EntryContainer.BackgroundColor = Colors.Transparent;
        Grid.SetColumn(EntryContainer, 1);
        Grid.SetColumnSpan(EntryContainer, 2);
    }
}