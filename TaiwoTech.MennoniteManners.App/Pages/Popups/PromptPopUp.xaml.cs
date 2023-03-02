using CommunityToolkit.Maui.Views;
using TaiwoTech.MennoniteManners.App.Services.DisplaySize;
using TaiwoTech.MennoniteManners.App.Services.Keyboard;
using TaiwoTech.MennoniteManners.App.Services.Settings;

namespace TaiwoTech.MennoniteManners.App.Pages.Popups;

public partial class PromptPopUp : Popup
{
    public DisplaySizeService DisplaySizeService { get; }
    public SettingsService PreferencesService { get; }

    private List<View> ViewsToHideOniOSEntry { get; }

    public PromptPopUp(DisplaySizeService displaySizeService, SettingsService preferencesService)
    {
        InitializeComponent();

        DisplaySizeService = displaySizeService;
        PreferencesService = preferencesService;
        Size = DisplaySizeService.GetSizeFromPercentages(50, 50);
        BindingContext = this;


        if (DeviceInfo.Current.Platform != DevicePlatform.iOS) return;

        var keyboardHelperService = new KeyboardHelperService();
        keyboardHelperService.KeyboardOpened += MakeMainEntryVisibleForIOS;
        keyboardHelperService.KeyboardClosed += ResetMainEntryToOriginalState;
        ViewsToHideOniOSEntry = new List<View> { Prompt, HorizontalBoxView, VerticalBoxView, CancelButton, SaveButton };
    }

    public void SetFields(string title, string subTitle, string placeholder, string cancelButton = "Cancel",
        string okButton = "Save")
    {
        Header.Text = title;
        Prompt.Text = subTitle;
        MainEntry.Placeholder = placeholder;
        CancelButton.Text = cancelButton;
        SaveButton.Text = okButton;
    }

    private void CancelButtonClicked(object sender, EventArgs e)
    {
        Close();
    }

    private void SaveButtonClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(MainEntry.Text))
        {
            Prompt.TextColor = Color.FromArgb("FF0000");
            return;
        }

        Close(MainEntry.Text);
    }

    private void RemoveChatFocus(object _, EventArgs __)
    {
        MainEntry.Unfocus();
        MainEntry.IsEnabled = false;
        MainEntry.IsEnabled = true;
    }

    private void MakeMainEntryVisibleForIOS(object _, float __)
    {

        foreach (var view in ViewsToHideOniOSEntry)
        {
            view.IsVisible = false;
        }

        Grid.SetRow(MainEntryBorder, 1);
        Grid.SetRowSpan(MainEntryBorder, 3);
        MainEntryBorder.VerticalOptions = LayoutOptions.Start;
    }


    private void ResetMainEntryToOriginalState(object _, EventArgs __)
    {
        foreach (var view in ViewsToHideOniOSEntry)
        {
            view.IsVisible = true;
        }

        Grid.SetRow(MainEntryBorder, 2);
        Grid.SetRowSpan(MainEntryBorder, 1);
        MainEntryBorder.VerticalOptions = LayoutOptions.Center;

    }

}