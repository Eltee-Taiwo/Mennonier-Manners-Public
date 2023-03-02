using CommunityToolkit.Maui.Views;
using TaiwoTech.MennoniteManners.App.Models;
using TaiwoTech.MennoniteManners.App.Services.API;
using TaiwoTech.MennoniteManners.App.Services.DisplaySize;
using TaiwoTech.MennoniteManners.App.Services.Settings;

namespace TaiwoTech.MennoniteManners.App.Pages.Popups;

public partial class GameSettingsPopup : Popup
{
    private DisplaySizeService DisplaySizeService { get; }
    private SettingsService PreferencesService { get; }
    private GameSettingsDto Dto { get; }
    public IEnumerable<GameTypeDto> GameTypes { get; }

    public GameSettingsPopup(DisplaySizeService displaySizeService, SettingsService preferencesService)
    {
        InitializeComponent();
        BindingContext = this;

        DisplaySizeService = displaySizeService;
        PreferencesService = preferencesService;
        Size = DisplaySizeService.GetSizeFromPercentages(50, 75);
        Dto = PreferencesService.GetGameSettings();
        GameTypes = PreferencesService.GetGameTypes();

        var selectedItem = GameTypes.SingleOrDefault(x => x.Id == Dto?.GameTypeId) ?? GameTypes.Single(x => x.Id == 1);
        GameTypePicker.ItemsSource = GameTypes.Select(x => x.DisplayName).ToList();
        GameLengthPicker.ItemsSource = selectedItem.GameLengths.ToList();
        GameTypePicker.SelectedIndex = GameTypePicker.ItemsSource.IndexOf(selectedItem.DisplayName);
        GameLengthPicker.SelectedIndex = GameLengthPicker.ItemsSource.Contains(Dto.MaxLevel) ? GameLengthPicker.ItemsSource.IndexOf(Dto.MaxLevel) : 0;
        ReverseCheckBox.IsChecked = Dto.IsReversed;
    }


    private void CancelButtonClicked(object sender, EventArgs e)
    {
        Close();
    }

    private void SaveButtonClicked(object sender, EventArgs e)
    {
        Dto.IsReversed = ReverseCheckBox.IsChecked;
        PreferencesService.SetGameSettings(Dto);
        Close();
    }


    private void GameTypeSelected(object sender, EventArgs e)
    {
        var picker = (Picker)sender;
        var gameTypeName = picker.SelectedItem.ToString();
        var selectedGameType = GameTypes.Single(x => x.DisplayName.Equals(gameTypeName));
        Dto.GameTypeId = selectedGameType.Id;
        Dto.GameType = selectedGameType.DisplayName;
        GameLengthPicker.ItemsSource = selectedGameType.GameLengths.ToList();
        GameLengthPicker.SelectedIndex = 0;
    }

    private void GameLengthSelected(object sender, EventArgs e)
    {
        var picker = (Picker)sender;
        var autoIndex = int.TryParse(picker.SelectedItem?.ToString(), out var newMaxLevel);
        Dto.MaxLevel = autoIndex ? newMaxLevel : 0;
    }

    private void GameTypePickerFocus(object sender, FocusEventArgs e)
    {
        ToggleOptionsForPicker(GameTypePickerLabel, GameTypePicker, false);
    }

    private void GameTypePickerUnFocus(object sender, FocusEventArgs e)
    {
        ToggleOptionsForPicker(GameTypePickerLabel, GameTypePicker, true);
    }

    private void GameLengthPickerFocus(object sender, FocusEventArgs e)
    {
        ToggleOptionsForPicker(GameLengthLabel, GameLengthPicker, false);
    }

    private void GameLengthPickerUnFocus(object sender, FocusEventArgs e)
    {
        ToggleOptionsForPicker(GameLengthLabel, GameLengthPicker, true);
    }

    private void ToggleOptionsForPicker(View label, View picker, bool showOthers)
    {
        if (DeviceInfo.Current.Platform == DevicePlatform.Android) return;

        foreach (View view in SettingsGrid)
        {
            if (view != label && view != picker)
            {
                view.IsVisible = showOthers;
            }
        }
        Title.IsVisible = showOthers;
        CancelButton.IsVisible = showOthers;
        SaveButton.IsVisible = showOthers;
    }
}