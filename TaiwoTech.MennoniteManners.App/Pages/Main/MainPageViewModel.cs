using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AppCenter.Crashes;
using TaiwoTech.MennoniteManners.App.Domain.Game;
using TaiwoTech.MennoniteManners.App.Domain.User;
using TaiwoTech.MennoniteManners.App.Extensions;
using TaiwoTech.MennoniteManners.App.Pages.Popups;
using TaiwoTech.MennoniteManners.App.Services.API;
using TaiwoTech.MennoniteManners.App.Services.Dialog;
using TaiwoTech.MennoniteManners.App.Services.Game;
using TaiwoTech.MennoniteManners.App.Services.Settings;

namespace TaiwoTech.MennoniteManners.App.Pages.Main
{
    public partial class MainPageViewModel : BaseViewModel
    {
        [ObservableProperty]
        private UserName _userName;

        private ApiService ApiService { get; }
        private DialogService DialogService { get; }
        private GameService GameService { get; }
        private PopupFactory PopupFactory { get; }
        private SettingsService SettingsService { get; }

        public MainPageViewModel(
            ApiService apiService,
            DialogService dialogService,
            GameService gameService,
            PopupFactory popupFactory,
            SettingsService settingsService
        )
        {
            ApiService = apiService;
            DialogService = dialogService;
            GameService = gameService;
            PopupFactory = popupFactory;
            SettingsService = settingsService;

            UserName = SettingsService.GetUserNameForButton();
        }

        /// <summary>
        /// Display a popup 
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task ChangeName()
        {
            var promptPopup = PopupFactory.CreatePopUp<PromptPopUp>();
            promptPopup.SetFields("Display Name", "Enter a new Display Name:", "Name...");
            var newUserName = await DialogService.DisplayPopUpAndWait<string>(promptPopup);
            if (!string.IsNullOrWhiteSpace(newUserName))
            {
                SettingsService.SetUserName(new UserName(newUserName));
            }
            UserName = SettingsService.GetUserNameForButton();
        }

        /// <summary>
        /// Attempt to retrieve a game Id and host a new game
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task HostNewGame()
        {
            IsBusy = true;
            if (!await IsGameStateValid())
            {
                IsBusy = false;
                return;
            }

            var gameId = await GameService.RequestGameIdAsync();
            if (gameId == null)
            {
                IsBusy = false; 
                return;
            }

            var navigationParameters = new Dictionary<string, object>
            {
                { "GameId", gameId },
                { "UserName", UserName},
                { "IsHost", true}
            };

            await NavigationExtensions.GoToPreGamePage(navigationParameters);
            IsBusy = false;
        }

        [RelayCommand]
        private async Task JoinGame()
        {
            IsBusy = true;
            if (!await IsGameStateValid())
            {
                IsBusy = false;
                return;
            }
            var promptPopup = PopupFactory.CreatePopUp<PromptPopUp>();
            promptPopup.SetFields("Join Game", "Enter 6 digit game code:", "AAA###...", okButton: "Join");
            var enteredGameId = await DialogService.DisplayPopUpAndWait<string>(promptPopup);

            if (string.IsNullOrWhiteSpace(enteredGameId))
            {
                IsBusy = false;
                return;
            }

            var gameId = new GameId(enteredGameId);
            var userName = await GameService.JoinGameAsync(gameId);
            if (userName == null)
            {
                IsBusy = false;
                return;
            }

            var navigationParameters = new Dictionary<string, object>
            {
                { "GameId", gameId },
                { "UserName", userName},
                { "IsHost", false}
            };

            await NavigationExtensions.GoToPreGamePage(navigationParameters);
            IsBusy = false;
        }

        [RelayCommand]
        private async Task ToggleSound()
        {
            await DialogService.DisplayAlertAsync("Sorry!", "This feature has not yet been implemented.", "OK");
        }

        [RelayCommand]
        private async Task BuyMeADrink()
        {
            try
            {
                IsBusy = true;
                var settings = await ApiService.GetSettings();

                if (settings == null)
                {
                    return;
                }

                var uri = new Uri(settings.BuyMeACoffeeLink);
                var options = new BrowserLaunchOptions
                {
                    LaunchMode = BrowserLaunchMode.External,
                    TitleMode = BrowserTitleMode.Default,
                    PreferredToolbarColor = Colors.Green,
                    PreferredControlColor = Colors.Purple
                };

                await Browser.Default.OpenAsync(uri, options);
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
                await DialogService.DisplayAlertAsync("Error", "An error occurred while opening the browser", "Close");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<bool> IsGameStateValid()
        {
            if (SettingsService.GetUserName() == null)
            {
                await DialogService.DisplayAlertAsync("No Username", "Please set your username", "Ok");
                return false;
            }

            var deviceVersion = new Version(VersionTracking.CurrentVersion);
            var settings = await ApiService.GetSettings();

            if (settings == null)
            {
                return false;
            }

            if (deviceVersion.CompareTo(new Version(settings.MinimumAcceptableApiVersion)) < 0)
            {
                await DialogService.DisplayAlertAsync("Update App", $"Please update your app to at least {settings.MinimumAcceptableApiVersion}", "Ok");
                return false;
            }

            SettingsService.SetTimeToForceRoll(settings.TimeToForceNextRoll);
            SettingsService.SetGameTypes(settings.GameTypes);
            return true;
        }
    }
}
