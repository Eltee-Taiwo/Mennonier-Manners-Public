using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AppCenter.Analytics;
using TaiwoTech.MennoniteManners.App.Constants;
using TaiwoTech.MennoniteManners.App.Domain.Game;
using TaiwoTech.MennoniteManners.App.Domain.RealTimeServer;
using TaiwoTech.MennoniteManners.App.Domain.User;
using TaiwoTech.MennoniteManners.App.Extensions;
using TaiwoTech.MennoniteManners.App.Pages.Popups;
using TaiwoTech.MennoniteManners.App.Services.API;
using TaiwoTech.MennoniteManners.App.Services.Dialog;
using TaiwoTech.MennoniteManners.App.Services.Game;
using TaiwoTech.MennoniteManners.App.Services.RealTime;
using TaiwoTech.MennoniteManners.App.Services.Settings;
using Timer = System.Timers.Timer;

namespace TaiwoTech.MennoniteManners.App.Pages.Game
{
    public partial class GamePageViewModel : BaseViewModel
    {
        [ObservableProperty]
        private bool _isHost;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsWaiting))]
        private bool _canDraw;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsWaiting))]
        private bool _canRoll;

        [ObservableProperty]
        private bool _nextButtonVisible;

        [ObservableProperty]
        private bool _finishButtonVisible;

        [ObservableProperty]
        private bool _isGameOver;

        [ObservableProperty]
        private bool _failedValidation;

        [ObservableProperty]
        private int _penSeconds;

        [ObservableProperty]
        private GameId _gameId;

        [ObservableProperty]
        private int _gameTypeId;

        [ObservableProperty]
        private GameLevel _maxGameLevel;

        [ObservableProperty]
        private GameLevel _currentGameLevel;

        [ObservableProperty]
        private string _currentHint;

        [ObservableProperty]
        private RollResult _lastRollResult;

        [ObservableProperty]
        private UserName _userName;

        [ObservableProperty]
        private UserName _currentRoller;

        [ObservableProperty]
        private UserName _currentWriter;

        [ObservableProperty]
        private UserName _lastRoller;

        [ObservableProperty]
        private GameTypeDto _gameType;

        public bool IsWaiting => !CanDraw && !CanRoll && !IsGameOver;
        public string HintTextFontFamily => GameTypeId == 2 ? "GLORY" : "OPTI";

        private bool IsSoloGame { get; set; }
        private bool IsReversedGame { get; set; }
        private bool HasFinished { get; set; }
        private Action ClearDrawingView { get; set; }
        private Func<Task<byte[]>> GetImageFromDrawingView { get; set; }
        private List<byte[]> CompletedImages { get; }
        private PenSeconds TotalPenSeconds { get; set; }

        private DialogService DialogService { get; }
        private GameService GameService { get; }
        private PopupFactory PopupFactory { get; }
        private SettingsService SettingsService { get; }
        private PenHolderKeeper PenHolderKeeper { get; }
        private Timer ForceNextRollTimer { get; }

        public GamePageViewModel(
            DialogService dialogService,
            GameService gameService,
            PopupFactory popupFactory,
            RealTimeServerService realTimeServerService,
            SettingsService settingsService
        )
        {
            PopupFactory = popupFactory;
            SettingsService = settingsService;
            DialogService = dialogService;
            GameService = gameService;
            CompletedImages = new List<byte[]>();
            CanDraw = false;
            CanRoll = false;
            PenHolderKeeper = new PenHolderKeeper();
            ForceNextRollTimer = new Timer(SettingsService.GetTimeToForceRoll() * 1_000);

            ForceNextRollTimer.Elapsed += ForceNextRollTimer_Elapsed;

            var diceRolledMethodName = new MethodName("DiceRolled");
            realTimeServerService.RegisterAnswerHandler<string, int, string>(diceRolledMethodName, HandleDiceRolled);

            var processingDiceMethodName = new MethodName("ProcessingDice");
            realTimeServerService.RegisterAnswerHandler(processingDiceMethodName, HandleProcessDice);

            var validatingResultMethodName = new MethodName("ValidatingResult");
            realTimeServerService.RegisterAnswerHandler<string>(validatingResultMethodName, HandleResultsBeingValidated);

            var successfulResultMethodName = new MethodName("SuccessfulResult");
            realTimeServerService.RegisterAnswerHandler<string, int>(successfulResultMethodName, HandleSuccessfulFinishGame);

            var failedResultMethodName = new MethodName("FailedResult");
            realTimeServerService.RegisterAnswerHandler<string, int>(failedResultMethodName, HandleFailedFinishGame);
        }

        private async void ForceNextRollTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            (sender as Timer)!.Stop();
            await GameService.ForceNextDice(GameId, CurrentRoller);
            (sender as Timer)!.Start();
        }

        public void InitialisePropertiesAsync(
            IDictionary<string, object> queryAttributes,
            Func<Task<byte[]>> getImageFromDrawingView,
            Action clearDrawingView
        )
        {
            GameId = (queryAttributes[nameof(GameId)] as GameId)!;
            GameTypeId = ((int)queryAttributes[nameof(GameTypeId)])!;
            IsHost = (bool)queryAttributes[nameof(IsHost)];
            IsSoloGame = (bool)queryAttributes[nameof(IsSoloGame)];
            UserName = (queryAttributes[nameof(UserName)] as UserName)!.Undecorated();
            MaxGameLevel = (queryAttributes[nameof(MaxGameLevel)] as GameLevel)!;
            CurrentRoller = (queryAttributes[nameof(CurrentRoller)] as UserName)!;
            IsReversedGame = queryAttributes[nameof(IsReversedGame)] as bool? ?? false;
            CanRoll = CurrentRoller.Equals(UserName);
            GameType = SettingsService.GetGameTypes().Single(x => x.Id == GameTypeId);
            if(IsReversedGame) GameType.TargetStringValues.Reverse();
            CurrentGameLevel = new GameLevel(1);
            CurrentHint = GameType.TargetStringValues[0];
            HasFinished = false;

            PenHolderKeeper.Reset();
            TotalPenSeconds = new PenSeconds(0);
            GetImageFromDrawingView = getImageFromDrawingView;
            ClearDrawingView = clearDrawingView;
            ClearDrawingCommand.Execute(null);
            Analytics.TrackEvent(TrackedEvents.NewGameStarted, queryAttributes.ToAnalyticsProperties());

            _gameOverPopUp = null;
        }

        [RelayCommand]
        public void StartedDrawing()
        {
            Application.Current!.Dispatcher.Dispatch(() =>
            {

                NextButtonVisible = CurrentGameLevel.Value < MaxGameLevel.Value;
                FinishButtonVisible = CurrentGameLevel.Value >= MaxGameLevel.Value;
            });
        }

        [RelayCommand]
        public void ClearDrawing()
        {
            Application.Current!.Dispatcher.Dispatch(() =>
            {
                NextButtonVisible = false;
                ClearDrawingView();
            });
        }

        [RelayCommand]
        public async Task MoveToNextSlide()
        {
            await Application.Current!.Dispatcher.DispatchAsync(async () =>
            {
                NextButtonVisible = false;
                if (await AddImage())
                {
                    CurrentGameLevel = new GameLevel(CurrentGameLevel.Value + 1);
                    CurrentHint = GameType.TargetStringValues[CurrentGameLevel.Value - 1];
                }
            });
        }

        [RelayCommand]
        public async Task RollDice()
        {
            await Application.Current!.Dispatcher.DispatchAsync(async () =>
            {
                CanRoll = false;
                await GameService.RollDice(GameId, UserName);
            });
        }

        [RelayCommand]
        public async Task ValidateResults()
        {
            HasFinished = true;
            await Application.Current!.Dispatcher.DispatchAsync(async () =>
            {
                PenHolderKeeper.Stop();
                NextButtonVisible = false;
                FinishButtonVisible = false;
                CanDraw = false;
                CanRoll = false;

                await AddImage();

                await GameService.ValidateResults(GameId, UserName, CompletedImages, TotalPenSeconds);
            });
        }

        [RelayCommand]
        public async Task LeaveGame()
        {
            await Application.Current!.Dispatcher.DispatchAsync(async () =>
            {
                Analytics.TrackEvent(TrackedEvents.LobbyLeft);
                await GameService.LeaveGameAsync(GameId, UserName);
                await NavigationExtensions.GoToHomePage();
            });
        }

        private void HandleResultsBeingValidated(string usernameOfFinished)
        {
            if (IsHost)
            {
                ForceNextRollTimer.Stop();
            }

            if (CurrentWriter.Equals(new UserName(usernameOfFinished)))
            {
                PenHolderKeeper.Stop();
            }
        }

        private GameOverPopUp _gameOverPopUp;
        private async void HandleSuccessfulFinishGame(string usernameOfFinished, int passMark)
        {
            HasFinished = true;
            await Application.Current!.Dispatcher.DispatchAsync(async () =>
            {
                if (IsHost)
                {
                    ForceNextRollTimer.Stop();
                }
                PenHolderKeeper.Stop();

                var winnerUserName = new UserName(usernameOfFinished);
                if (!winnerUserName.Equals(UserName))
                {
                    // Don't wait for this. The Real time server will send us a notification when it's done.
#pragma warning disable CS4014
                    GameService.ValidateResults(GameId, UserName, CompletedImages, TotalPenSeconds).ConfigureAwait(false);
#pragma warning restore CS4014
                }

                _gameOverPopUp ??= PopupFactory.CreatePopUp<GameOverPopUp>();
                _gameOverPopUp.SetWinner(winnerUserName, winnerUserName.Value.Equals(UserName.Value), passMark);
                if (!_gameOverPopUp.HasBeenOpened)
                {
                    await DialogService.DisplayPopUpAndWait(_gameOverPopUp);
                }

                _gameOverPopUp.Closed += async (_, _) =>
                {
                    var navigationParameters = new Dictionary<string, object>
                    {
                        { "GameId", GameId },
                        { "UserName", UserName },
                        { "IsHost", IsHost }
                    };
                    await NavigationExtensions.GoToPreGamePage(navigationParameters);
                };
            });
        }

        public void PostGameCleanUp()
        {
            ForceNextRollTimer.Elapsed -= ForceNextRollTimer_Elapsed;
            _gameOverPopUp?.Close();
        }

        private async void HandleFailedFinishGame(string usernameOfFinished, int validityScore)
        {
            await Application.Current!.Dispatcher.DispatchAsync(() =>
            {
                var username = new UserName(usernameOfFinished);
                if (username != UserName) return Task.CompletedTask;

                Analytics.TrackEvent(TrackedEvents.ValidationFailed);
                IsGameOver = true;
                FailedValidation = true;
                CanDraw = false;
                CanRoll = false;

                _gameOverPopUp = PopupFactory.CreatePopUp<GameOverPopUp>();
                _gameOverPopUp.SetFailedValidation(validityScore);
                DialogService.DisplayPopUp(_gameOverPopUp);
                _gameOverPopUp.HasBeenOpened = true;
                return Task.CompletedTask;
            });
        }

        private void HandleDiceRolled(string lastRollerName, int rollResult, string nextRollerName)
        {
            Application.Current!.Dispatcher.DispatchAsync(() =>
            {
                if (IsHost && !IsSoloGame)
                {
                    ForceNextRollTimer.Stop();
                    ForceNextRollTimer.Start();
                }

                LastRollResult = new RollResult(rollResult);
                LastRoller = new UserName(lastRollerName);
                CurrentRoller = new UserName(nextRollerName);

                if (LastRollResult.IsSuccess)
                {
                    CurrentWriter = LastRoller;
                    PenSeconds = 0;
                    PenHolderKeeper.Start(UpdateTimer);
                }

                if (IsGameOver) return;

                //if I can draw, I can keep drawing if no success. If I cannot draw, I can draw if i rolled success
                CanDraw = (CanDraw && !LastRollResult.IsSuccess) || (LastRollResult.IsSuccess && LastRoller.Equals(UserName));
                //If I can draw I cannot roll. If I am not drawing and it is my turn
                CanRoll = !HasFinished && (!CanDraw) && CurrentRoller.Equals(UserName);
            });
        }

        private void HandleProcessDice()
        {
            if (!IsHost) return;
            ForceNextRollTimer.Stop();
        }

        private async Task<bool> AddImage()
        {
            var imageBytes = await GetImageFromDrawingView();
            if (imageBytes == null || imageBytes.Length == 0) return false;

            CompletedImages.Add(imageBytes);
            return true;
        }

        private void UpdateTimer(object sender, EventArgs eventArgs)
        {
            PenSeconds += 1;
            if (CurrentWriter.Equals(UserName))
            {
                TotalPenSeconds += 1;
            }
        }
    }

    internal class PenHolderKeeper : BindableObject
    {
        private bool _hasStarted;
        private IDispatcherTimer _timer;

        public void Start(EventHandler updateTimer)
        {
            if (_hasStarted) return;
            _hasStarted = true;
            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += updateTimer;
            _timer.Start();
        }

        public void Reset()
        {
            _hasStarted = false;
        }

        public void Stop()
        {
            _timer.Stop();
            _hasStarted = false;
        }
    }
}
