using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AppCenter.Analytics;
using TaiwoTech.MennoniteManners.App.Constants;
using TaiwoTech.MennoniteManners.App.Domain.Chat;
using TaiwoTech.MennoniteManners.App.Domain.Game;
using TaiwoTech.MennoniteManners.App.Domain.RealTimeServer;
using TaiwoTech.MennoniteManners.App.Domain.User;
using TaiwoTech.MennoniteManners.App.Extensions;
using TaiwoTech.MennoniteManners.App.Models;
using TaiwoTech.MennoniteManners.App.Pages.Popups;
using TaiwoTech.MennoniteManners.App.Services.Dialog;
using TaiwoTech.MennoniteManners.App.Services.Game;
using TaiwoTech.MennoniteManners.App.Services.RealTime;
using TaiwoTech.MennoniteManners.App.Services.Settings;

namespace TaiwoTech.MennoniteManners.App.Pages.PreGame
{
    public partial class PreGameViewModel : BaseViewModel
    {

        [ObservableProperty]
        private GameId _gameId;

        [ObservableProperty]
        private bool _isHost;

        [ObservableProperty]
        private UserName _userName;

        [ObservableProperty]
        private ObservableCollection<UserName> _participants;

        [ObservableProperty]
        private ObservableCollection<ChatMessageDto> _chatMessages;

        private DialogService DialogService { get; }
        private GameService GameService { get; }
        private RealTimeServerService RealTimeServerService { get; }
        private SettingsService SettingsService { get; }
        private PopupFactory PopupFactory { get; }

        private Action _scrollChatToBottomAction;
        private Action _removeChatFocusFunction;
        private Action _clearChatTextFunction;

        public PreGameViewModel(
            DialogService dialogService,
            GameService gameService,
            RealTimeServerService realTimeServerService,
            SettingsService settingsService,
            PopupFactory popupFactory
        )
        {
            DialogService = dialogService;
            GameService = gameService;
            RealTimeServerService = realTimeServerService;
            SettingsService = settingsService;
            PopupFactory = popupFactory;

            Participants = new ObservableCollection<UserName>();
            ChatMessages = new ObservableCollection<ChatMessageDto>();

            #region real time handlers
            var newParticipantMethodName = new MethodName("NewParticipant");
            RealTimeServerService.RegisterAnswerHandler<string>(newParticipantMethodName, HandleNewParticipant);

            var removeParticipantMethodName = new MethodName("RemoveParticipant");
            RealTimeServerService.RegisterAnswerHandler<string>(removeParticipantMethodName, HandleRemoveParticipant);

            var receiveMessageMethodName = new MethodName("ReceiveMessage");
            RealTimeServerService.RegisterAnswerHandler<string, string>(receiveMessageMethodName, HandleNewMessage);

            var receiveSystemMessageMethodName = new MethodName("ReceiveSystemMessage");
            RealTimeServerService.RegisterAnswerHandler<string>(receiveSystemMessageMethodName, HandleSystemMessage);

            var receiveStartGameMethodName = new MethodName("StartGame");
            RealTimeServerService.RegisterAnswerHandler<string, int, int, bool>(receiveStartGameMethodName, HandleStartGame);

            var receiveNewHostMethodName = new MethodName("NewHost");
            RealTimeServerService.RegisterAnswerHandler<string>(receiveNewHostMethodName, HandleNewHost);
            #endregion
        }

        [RelayCommand]
        public async Task LeaveGame()
        {
            var shouldLeave = await DialogService.DisplayAlertAsync("Leave Game?", "Are you sure you want to leave the game?", "Yes", "No");
            if (!shouldLeave) return;

            Analytics.TrackEvent(TrackedEvents.LobbyLeft);
            await GameService.LeaveGameAsync(GameId, UserName);
            await NavigationExtensions.GoToHomePage();
        }

        [RelayCommand]
        public async Task SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            _removeChatFocusFunction();
            _clearChatTextFunction();
            await GameService.SendChatMessage(GameId, UserName, new ChatMessage(message));
            Analytics.TrackEvent(TrackedEvents.SendMessage);
        }

        [RelayCommand]
        public void RemoveChatFocus()
        {
            _removeChatFocusFunction();
        }

        [RelayCommand]
        public async Task StartGame()
        {
            var gameSettingsDto = SettingsService.GetGameSettings();
#if DEBUG
            gameSettingsDto.MaxLevel = 5;
#endif
            await GameService.StartGame(GameId, UserName, gameSettingsDto);
        }

        [RelayCommand]
        public async Task ShowOptions()
        {
            var gameSettings = SettingsService.GetGameSettings();
            var popUp = PopupFactory.CreatePopUp<GameSettingsPopup>();
            await DialogService.DisplayPopUpAndWait(popUp);
            var newGameSettings = SettingsService.GetGameSettings();
            if (gameSettings.GameTypeId != newGameSettings.GameTypeId)
            {
                await GameService.SendChatMessage(GameId, UserName, new ChatMessage($"New Game Setting: Game Type is now {newGameSettings.GameType}"), true);
            }
            if (gameSettings.MaxLevel != newGameSettings.MaxLevel)
            {
                await GameService.SendChatMessage(GameId, UserName, new ChatMessage($"New Game Setting: Game Length is now {newGameSettings.MaxLevel}"), true);
            }
            if (gameSettings.IsReversed != newGameSettings.IsReversed)
            {
                await GameService.SendChatMessage(GameId, UserName, new ChatMessage($"New Game Setting: Reverse Mode is now {newGameSettings.IsReversed}"), true);
            }
        }

        public async Task InitialisePropertiesAsync(
            IDictionary<string, object> queryAttributes,
            Action scrollChatToBottomFunction,
            Action removeChatFocusFunction,
            Action clearChatTextFunction
        )
        {
            GameId = (queryAttributes[nameof(GameId)] as GameId)!;
            IsHost = (bool)queryAttributes[nameof(IsHost)];
            UserName = (queryAttributes[nameof(UserName)] as UserName)!.Undecorated();
            _scrollChatToBottomAction = scrollChatToBottomFunction;
            _removeChatFocusFunction = removeChatFocusFunction;
            _clearChatTextFunction = clearChatTextFunction;

            Analytics.TrackEvent(IsHost ? TrackedEvents.LobbyCreated : TrackedEvents.LobbyJoined, queryAttributes.ToAnalyticsProperties());

            var players = await GameService.GetParticipantsAsync(GameId);
            if (players == null)
            {
                if (IsHost)
                {
                    Participants.Add(UserName);
                }
                return;
            }

            Participants = new ObservableCollection<UserName>(players);
            AddChat(ChatMessageTemplates.NewUser(UserName));
        }

        private void HandleNewParticipant(string participantName)
        {
            if (participantName.Equals(UserName.Value)) return;

            var participantUserName = new UserName(participantName);
            Participants.Add(participantUserName);

            AddChat(ChatMessageTemplates.NewUser(participantUserName));
        }

        private void HandleRemoveParticipant(string participantName)
        {
            var participantUserName = new UserName(participantName);
            Participants.Remove(participantUserName);


            AddChat(ChatMessageTemplates.RemoveUser(participantUserName));
        }

        private void HandleNewMessage(string userName, string message)
        {
            var chatMessageDto = new ChatMessageDto(
                new UserName(userName),
                new ChatMessage(message),
                isMine: userName.Equals(UserName.Value, StringComparison.InvariantCultureIgnoreCase)
            );
            AddChat(chatMessageDto);
        }

        private void HandleSystemMessage(string message)
        {
            var chatMessageDto = new ChatMessageDto(
                new UserName("System"),
                new ChatMessage(message),
                isSystem: true
            );
            AddChat(chatMessageDto);
        }

        private async void HandleStartGame(string firstRollerDisplayName, int maxGameLevel, int gameTypeId, bool isGameReversed)
        {
            var navigationParameters = new Dictionary<string, object>
            {
                { "GameId", GameId },
                { "GameTypeId", gameTypeId },
                { "UserName", UserName},
                { "IsHost", IsHost},
                { "IsReversedGame", isGameReversed},
                { "IsSoloGame", Participants.Count == 1},
                { "CurrentRoller", new UserName(firstRollerDisplayName)},
                { "MaxGameLevel", new GameLevel(maxGameLevel)},

            };

            Analytics.TrackEvent(TrackedEvents.NewGameStarting, navigationParameters.ToAnalyticsProperties());
            await NavigationExtensions.GoToGamePage(navigationParameters);
        }

        private void HandleNewHost(string displayName)
        {
            var newHost = new UserName(displayName);
            IsHost = UserName.Equals(newHost);
            AddChat(ChatMessageTemplates.NewHost(newHost));
        }

        private void AddChat(ChatMessageDto chatMessage)
        {
            ChatMessages.Add(chatMessage);
            _scrollChatToBottomAction();
        }
    }
}
