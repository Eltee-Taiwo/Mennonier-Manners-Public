using TaiwoTech.MennoniteManners.App.Domain.Chat;
using TaiwoTech.MennoniteManners.App.Domain.Game;
using TaiwoTech.MennoniteManners.App.Domain.RealTimeServer;
using TaiwoTech.MennoniteManners.App.Domain.User;
using TaiwoTech.MennoniteManners.App.Models;
using TaiwoTech.MennoniteManners.App.Services.Image;
using TaiwoTech.MennoniteManners.App.Services.RealTime;
using TaiwoTech.MennoniteManners.App.Services.Settings;

namespace TaiwoTech.MennoniteManners.App.Services.Game
{
    public class GameService : ISingletonService
    {
        private const string UserNameParameterKey = "UserName";
        private const string GameIdParameterKey = "GameId";
        private const string GameTypeIdParameterKey = "GameTypeId";
        private const string ChatMessageParameterKey = "ChatMessage";
        private const string IsSystemChatMessageParameterKey = "IsSystemChatMessage";
        private const string CapturedImagesParameterKey = "CapturedImages";
        private const string MaxGameLevelParameterKey = "MaxGameLevel";
        private const string ForcedRollParameterKey = "ForcedRoll";
        private const string TotalPenSecondsParameterKey = "TotalPenSeconds";
        private const string ReverseGameParameterKey = "ReverseGame";

        private RealTimeServerService RealTimeServerService { get; }
        private SettingsService SettingsService { get; }

        public GameService(RealTimeServerService realTimeServerService, SettingsService settingsService)
        {
            RealTimeServerService = realTimeServerService;
            SettingsService = settingsService;
        }

        /// <summary>
        /// Request a new game id from the real time server
        /// </summary>
        /// <returns>A GameId</returns>
        public async Task<GameId> RequestGameIdAsync()
        {
            var realTimeServerMethodName = new MethodName("RequestGameId");

            var userName = SettingsService.GetUserName();
            var realTimeServerParameters = new Dictionary<string, object> { { UserNameParameterKey, userName.Value } };
            var gameId = await RealTimeServerService.InvokeAsync<string>(realTimeServerMethodName, realTimeServerParameters);
            return gameId == null ? null : new GameId(gameId);
        }

        /// <summary>
        /// Leave the current game
        /// </summary>
        /// <param name="gameId">The Id of the game that we want to leave</param>
        /// <returns></returns>
        public async Task LeaveGameAsync(GameId gameId, UserName uniqueDisplayName)
        {
            var realTimeServerMethodName = new MethodName("LeaveGame");

            var realTimeServerParameters = new Dictionary<string, object>
            {
                { UserNameParameterKey, uniqueDisplayName.Value },
                { GameIdParameterKey, gameId.Value }
            };

            await RealTimeServerService.InvokeAsync(realTimeServerMethodName, realTimeServerParameters);
        }

        /// <summary>
        /// Join an existing game and return the Unique Display Name to use for the lobby
        /// </summary>
        /// <param name="gameId">The Id of the game that we want to leave</param>
        /// <returns>The unique username to display for the lobby</returns>
        public async Task<UserName> JoinGameAsync(GameId gameId)
        {
            var realTimeServerMethodName = new MethodName("JoinGame");

            var userName = SettingsService.GetUserName();
            var realTimeServerParameters = new Dictionary<string, object> { { UserNameParameterKey, userName.Value }, { GameIdParameterKey, gameId.Value } };

            var uniqueUserName = await RealTimeServerService.InvokeAsync<string>(realTimeServerMethodName, realTimeServerParameters);
            return string.IsNullOrWhiteSpace(uniqueUserName) ? null : new UserName(uniqueUserName);
        }

        /// <summary>
        /// Get a list of all the users that are currently in a game lobby
        /// </summary>
        /// <param name="gameId">The Id of the game that we want to leave</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<IEnumerable<UserName>> GetParticipantsAsync(GameId gameId)
        {
            var realTimeServerMethodName = new MethodName("GetParticipants");

            var realTimeServerParameters = new Dictionary<string, object> { { GameIdParameterKey, gameId.Value } };

            var participantNames = await RealTimeServerService.InvokeAsync<IEnumerable<string>>(realTimeServerMethodName, realTimeServerParameters);
            if (participantNames == null) return null;

            var participantUserNames = participantNames.Select(x => new UserName(x)).ToList();
            return participantUserNames;
        }

        /// <summary>
        /// Send a message to all the users in the current game
        /// </summary>
        /// <param name="gameDisplayName"></param>
        /// <param name="chatMessage"></param>
        /// <param name="gameId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task SendChatMessage(GameId gameId, UserName gameDisplayName, ChatMessage chatMessage, bool isSystemMessage = false)
        {
            var sendChatMessageMethodName = new MethodName("SendMessage");

            var realTimeServerParameters = new Dictionary<string, object>
            {
                { GameIdParameterKey, gameId.Value },
                { UserNameParameterKey, gameDisplayName.Value },
                { ChatMessageParameterKey, chatMessage.Value },
                { IsSystemChatMessageParameterKey, isSystemMessage }
            };
            await RealTimeServerService.InvokeAsync(sendChatMessageMethodName, realTimeServerParameters);
        }

        /// <summary>
        /// Send a request to the Real Time Server and other players to begin countdown and start the game
        /// </summary>
        /// <param name="gameId">The Id of the game that we want to leave</param>
        /// <param name="userName"></param>
        /// <param name="gameSettingsDto"></param>
        /// <returns></returns>
        public async Task StartGame(GameId gameId, UserName userName, GameSettingsDto gameSettingsDto)
        {
            var startGameMethodName = new MethodName("StartCountDown");

            var realTimeServerParameters = new Dictionary<string, object>
            {
                { GameIdParameterKey, gameId.Value },
                { UserNameParameterKey, userName.Value },
                { MaxGameLevelParameterKey, gameSettingsDto.MaxLevel},
                { GameTypeIdParameterKey, gameSettingsDto.GameTypeId},
                { ReverseGameParameterKey, gameSettingsDto.IsReversed}
            };
            await RealTimeServerService.InvokeAsync(startGameMethodName, realTimeServerParameters);
        }

        /// <summary>
        /// Send a request to the Real Time Server to validate the results and let everyone know if we have a winner or not.
        /// </summary>
        /// <param name="gameId">The Id of the game that we want to leave</param>
        /// <param name="gameDisplayName"></param>
        /// <param name="completedImages"></param>
        /// <returns></returns>
        public async Task ValidateResults(GameId gameId, UserName gameDisplayName, List<byte[]> completedImages, PenSeconds totalPenSeconds)
        {

            var validateResultMethodName = new MethodName("ValidateResults");
            var stitchedImages = ImageService.CombineImages(completedImages);

            var requestParameters = new Dictionary<string, object>
            {
                { GameIdParameterKey, gameId.Value },
                { UserNameParameterKey, gameDisplayName.Value },
                { CapturedImagesParameterKey, Convert.ToBase64String(stitchedImages) },
                { TotalPenSecondsParameterKey, totalPenSeconds.Value}
            };

            await RealTimeServerService.InvokeAsync(validateResultMethodName, requestParameters);
        }

        /// <summary>
        /// Send a request to the Real Time Server to roll the dice for me
        /// </summary>
        /// <param name="userName">The user who is rolling the dice</param>
        /// <param name="gameId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task RollDice(GameId gameId, UserName userName)
        {
            var rollDiceMethodName = new MethodName("RollDice");
            var requestParameters = new Dictionary<string, object>
            {
                { GameIdParameterKey, gameId.Value },
                { UserNameParameterKey, userName.Value }
            };

            await RealTimeServerService.InvokeAsync(rollDiceMethodName, requestParameters);
        }

        /// <summary>
        /// Send a request to the Real Time Server to roll the dice for me
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="currentRoller"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task ForceNextDice(GameId gameId, UserName currentRoller)
        {
            var rollDiceMethodName = new MethodName("RollDice");
            var requestParameters = new Dictionary<string, object>
            {
                { GameIdParameterKey, gameId.Value },
                { ForcedRollParameterKey, true },
                { UserNameParameterKey, currentRoller.Value }
            };

            await RealTimeServerService.InvokeAsync(rollDiceMethodName, requestParameters);
        }
    }
}
