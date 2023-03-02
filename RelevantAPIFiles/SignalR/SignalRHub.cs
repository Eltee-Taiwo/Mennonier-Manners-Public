using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using TaiwoTech.Eltee.DataServices.MennoniteManners.Game;
using TaiwoTech.Eltee.DataServices.MennoniteManners.GameSession;
using TaiwoTech.Eltee.DataServices.MennoniteManners.GameStatus;
using TaiwoTech.Eltee.DataServices.MennoniteManners.Participant;
using TaiwoTech.Eltee.DataServices.MennoniteManners.Result;
using TaiwoTech.Eltee.DataServices.MennoniteManners.Settings;
using TaiwoTech.Eltee.Libraries.Utilities;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace TaiwoTech.Eltee.Hubs.Mennonite
{
    public class MennoniteMannersHub : Hub
    {
        private const string CapturedImagesParameterKey = "CapturedImages";
        private const string ChatMessageParameterKey = "ChatMessage";
        private const string GameIdParameterKey = "GameId";
        private const string GameTypeIdParameterKey = "GameTypeId";
        private const string IsHostParameterKey = "IsHost";
        private const string IsSystemChatMessageParameterKey = "IsSystemChatMessage";
        private const string MaxGameLevelParameterKey = "MaxGameLevel";
        private const string UserNameParameterKey = "UserName";
        private const string ForcedRollParameterKey = "ForcedRoll";
        private const string TotalPenSecondsParameterKey = "TotalPenSeconds";
        private const string ReverseGameParameterKey = "ReverseGame";

        private Random Random { get; }
        private ILogger Logger { get; }
        private GameService GameService { get; }
        private GameSessionService GameSessionService { get; }
        private ParticipantService ParticipantService { get; }
        private ResultService ResultService { get; }
        private SettingsService SettingsService { get; }

        private LockSettings LockSettings { get; }


        public MennoniteMannersHub(
            ILogger<MennoniteMannersHub> logger,
            GameService gameService,
            GameSessionService gameSessionService,
            ParticipantService participantService,
            ResultService resultService,
            SettingsService settingsService,
            LockSettings lockSettings)
        {
            Logger = logger;
            GameService = gameService;
            GameSessionService = gameSessionService;
            ParticipantService = participantService;
            ResultService = resultService;
            SettingsService = settingsService;
            LockSettings = lockSettings;
            Random = new Random();
        }

        public override Task OnConnectedAsync()
        {
            Logger.LogDebug($"New connection {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var user = await ParticipantService.GetByConnectionId(Context.ConnectionId);
            if (user != null)
            {
                Logger.LogInformation("User {username} with connection {connectionId} lost connection and will be removed from gamed",
                    user.UniqueUserName ?? user.UserName,
                    Context.ConnectionId
                    );
                await LeaveGame(new Dictionary<string, string>
                {
                    { UserNameParameterKey, user.UniqueUserName ?? user.UserName },
                    { GameIdParameterKey, user.GameId},
                });
            }
            Logger.LogDebug(exception, $"Lost connection {Context.ConnectionId}");
        }

        /// <summary>
        /// Request a new Game Id that uniquely identifies a game session
        /// </summary>
        /// <param name="parameters">A dictionary of values that requires a key matching <see cref="UserNameParameterKey"/></param>
        /// <returns></returns>
        public async Task<string> RequestGameId(Dictionary<string, string> parameters)
        {
            using (LogContext.PushProperty("RequestParameters", parameters))
            {
                Logger.LogInformation($"{nameof(RequestGameId)}: New Game ID requested.");
                var dto = await GameService.Create();
                parameters.Add(GameIdParameterKey, dto.GameId);
                parameters.Add(IsHostParameterKey, true.ToString());
                await JoinGame(parameters);
                return dto.GameId;
            }
        }

        /// <summary>
        /// Join an existing game by providing the game session and your username.
        /// </summary>
        /// <param name="parameters">A dictionary of values that requires keys matching <see cref="UserNameParameterKey"/> and <see cref="GameIdParameterKey"/></param>
        /// <returns>The unique Username for the user in the specified game</returns>
        public async Task<string> JoinGame(Dictionary<string, string> parameters)
        {
            using (LogContext.PushProperty("RequestParameters", parameters))
            {
                var userName = parameters[UserNameParameterKey];
                var gameId = parameters[GameIdParameterKey].ToUpper();
                Logger.LogInformation($"{nameof(JoinGame)}: {{userName}} is leaving game.", userName);
                var isHost = parameters.TryGetValue(IsHostParameterKey, out _); //The `isHost` key is only passed in from the RequestGameId function 

                if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(gameId))
                {
                    return null;
                }

                await LeaveGame(parameters);

                var game = await GameService.Get(gameId);
                if (game == null) return null;

                var participantDto = new ParticipantDto
                {
                    GameId = game.GameId,
                    UserName = userName,
                    ConnectionId = Context.ConnectionId,
                    IsHost = isHost,
                    JoinedAt = DateTime.UtcNow,
                    LeftAt = null
                };
                var uniqueDisplayName = await ParticipantService.Add(participantDto);
                if (string.IsNullOrWhiteSpace(uniqueDisplayName)) return null;

                await Clients.Group(gameId).SendAsync("NewParticipant", uniqueDisplayName);
                await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
                return uniqueDisplayName;
            }
        }

        /// <summary>
        /// Removes the specified user from the specified game
        /// </summary>
        /// <param name="parameters">A dictionary of values that requires keys matching <see cref="UserNameParameterKey"/> and <see cref="GameIdParameterKey"/></param>
        /// <returns></returns>
        public async Task LeaveGame(Dictionary<string, string> parameters)
        {
            using (LogContext.PushProperty("RequestParameters", parameters))
            {
                var userName = parameters[UserNameParameterKey];
                var gameId = parameters[GameIdParameterKey].ToUpper();

                Logger.LogInformation($"{nameof(LeaveGame)}: {{userName}} is leaving game.", userName);
                var game = await GameService.Get(gameId);
                if (game == null) return;

                var leaveResult = await ParticipantService.Leave(gameId, userName);
                if (leaveResult == null) return;

                if (!string.IsNullOrWhiteSpace(leaveResult.PromotedHost))
                {
                    await Clients.Group(gameId).SendAsync("NewHost", leaveResult.PromotedHost);
                }

                await Clients.Group(gameId).SendAsync("RemoveParticipant", userName);
            }
        }

        /// <summary>
        /// Get a list of all the participants that are currently in a game lobby
        /// </summary>
        /// <param name="parameters">A dictionary of values that requires a key matching <see cref="GameIdParameterKey"/></param>
        /// <returns>The list of current participants in the game lobby</returns>
        public async Task<IEnumerable<string>> GetParticipants(Dictionary<string, string> parameters)
        {
            using (LogContext.PushProperty("RequestParameters", parameters))
            {
                Logger.LogDebug($"{nameof(GetParticipants)}: Getting the list of participants.");

                var gameId = parameters[GameIdParameterKey].ToUpper();
                var game = await GameService.Get(gameId);
                if (game == null) return null;

                var participants = await ParticipantService.Get(game.GameId);
                var participantNames = participants.Select(x => x.UniqueUserName);
                return participantNames;
            }
        }

        /// <summary>
        /// Forward the message from a user to all the players in that game lobby
        /// </summary>
        /// <returns></returns>
        public Task SendMessage(Dictionary<string, object> parameters)
        {
            using (LogContext.PushProperty("RequestParameters", parameters))
            {
                Logger.LogDebug($"{nameof(SendMessage)}: Sending a message to game lobby.");

                var userName = parameters[UserNameParameterKey].ToString();
                var gameId = parameters[GameIdParameterKey].ToString()!.ToUpper();
                var message = parameters[ChatMessageParameterKey].ToString();
                var isSystemMessage = bool.TryParse(parameters[IsSystemChatMessageParameterKey].ToString(), out var result) && result;

                gameId = gameId.ToUpper();
                return isSystemMessage
                    ? Clients.Group(gameId).SendAsync("ReceiveSystemMessage", message)
                    : Clients.Group(gameId).SendAsync("ReceiveMessage", userName, message);
            }
        }

        /// <summary>
        /// Trigger a countdown then begin the game for all players
        /// </summary>
        /// <param name="parameters">A dictionary of values that requires keys matching <see cref="UserNameParameterKey"/>, <see cref="GameIdParameterKey"/>, <br />
        /// and <see cref="ChatMessageParameterKey"/></param>
        /// <returns></returns>
        public async Task StartCountDown(Dictionary<string, object> parameters)
        {
            using (LogContext.PushProperty("RequestParameters", parameters))
            {
                Logger.LogInformation($"{nameof(StartCountDown)}: A game is being started. ");

                var gameId = parameters[GameIdParameterKey].ToString()!.ToUpper();
                var gameTypeId = int.Parse(parameters[GameTypeIdParameterKey].ToString()!);
                var maxGameLevel = int.Parse(parameters[MaxGameLevelParameterKey].ToString()!);
                var isGameReversed = bool.Parse(parameters[ReverseGameParameterKey].ToString()!);

                var game = await GameService.Get(gameId);
                if (game == null) return;

                await ParticipantService.ResetHasFinished(gameId);

                for (var i = 5; i > 0; i--)
                {
                    await Clients.Group(gameId).SendAsync("ReceiveSystemMessage", $"Starting in {i}");
                    await Task.Delay(1_000);
                }

                await GameSessionService.EndActiveSession(gameId);
                await GameSessionService.Create(gameId, maxGameLevel, gameTypeId, isGameReversed);
                var participants = await ParticipantService.Get(game.GameId);
                var nextRoller = participants[Random.Next(participants.Count)];

                Logger.LogInformation("Starting a new session for game {gameId}. First roller is {@firstRoller} from the following participants {@participants}", gameId, nextRoller, participants);

                await Clients.Group(gameId).SendAsync("StartGame", nextRoller.UniqueUserName, maxGameLevel, gameTypeId, isGameReversed);
                await GameService.UpdateStatus(gameId, GameStatusDto.Active);
            }
        }

        /// <summary>
        /// Receive the result of someone's dice roll and prompt players on who the next roller is
        /// </summary>
        /// <param name="parameters">A dictionary of values that requires keys matching <see cref="UserNameParameterKey"/>, <see cref="GameIdParameterKey"/>, <br />
        /// and <see cref="CapturedImagesParameterKey"/></param>
        public async Task RollDice(Dictionary<string, object> parameters)
        {
            using (LogContext.PushProperty("RequestParameters", parameters))
            {
                Logger.LogDebug($"{nameof(RollDice)}: Process Dice Roll a game.");

                var lastRollerName = parameters[UserNameParameterKey].ToString();
                var gameId = parameters[GameIdParameterKey].ToString()!.ToUpper();
                var forcedMoveOn = parameters.TryGetValue(ForcedRollParameterKey, out _);

                var game = await GameService.Get(gameId);

                if (game == null)
                {
                    Logger.LogError("No game found for {gameId}", gameId);
                    return;
                }

                await Clients.Groups(gameId).SendAsync("ProcessingDice");

                var settings = await SettingsService.Get();
                if (!forcedMoveOn)
                {
                    await Task.Delay((int)(settings.TimeToRoll * 1_000));
                }

                var rollResult = forcedMoveOn ? 0 : Random.Next(1, 7); //Note. The upper bound is exclusive.
                var gameSession = await GameSessionService.GetActiveSession(gameId);
                if (gameSession is not { EndedAt: null })
                {
                    Logger.LogError("Tried to roll the dice for already finished game {gameId}", gameId);
                    return;
                }
                var nextRoller = await ParticipantService.GetNextRoller(game.GameId, rollResult, lastRollerName);

                await Clients.Groups(gameId).SendAsync("DiceRolled", lastRollerName, rollResult, nextRoller);
            }
        }

        /// <summary>
        /// Validate the images and see if it passes the threshold mark.
        /// </summary>
        /// <param name="parameters">A dictionary of values that requires keys matching <see cref="UserNameParameterKey"/>, <see cref="GameIdParameterKey"/>, <br />
        /// and <see cref="CapturedImagesParameterKey"/></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task ValidateResults(Dictionary<string, object> parameters)
        {
            using (LogContext.PushProperty(
                       "RequestParameters",
                       parameters.Where(x => !x.Key.Equals("CapturedImages", StringComparison.InvariantCultureIgnoreCase)))
            )
            {
                Logger.LogInformation($"{nameof(ValidateResults)}: Validating the results of a game.");

                var userDisplayName = parameters[UserNameParameterKey].ToString();
                var gameId = parameters[GameIdParameterKey].ToString()!.ToUpper();
                var imageBytes = Convert.FromBase64String(
                    parameters[CapturedImagesParameterKey].ToString() ??
                    throw new InvalidOperationException($"{nameof(CapturedImagesParameterKey)} is missing.")
                );
                var totalPenSeconds = int.Parse(parameters[TotalPenSecondsParameterKey].ToString()!);

                var gameSession = await GameSessionService.GetActiveSession(gameId);
                var existingResults = await ResultService.GetBySessionId(gameSession.Id);
                var participant = (await ParticipantService.Get(gameId, userDisplayName)).Single();

                if (existingResults.Any(x => x.ParticipantId == participant.Id))
                {
                    return;
                }

                async Task<List<ResultDto>> GetResultsIfAllUsersValidated()
                {
                    var allResults = await ResultService.GetBySessionId(gameSession.Id);
                    var participants = await ParticipantService.Get(gameId);
                    return !participants.Select(x => x.Id).Except(allResults.Select(x => x.ParticipantId)).Any() ? allResults : null;
                }
                async Task SendStatsIfEveryoneIsDone()
                {
                    await gameId.AsLock(async () =>
                    {
                        var results = await GetResultsIfAllUsersValidated();
                        if (results != null)
                        {
                            await Task.Delay(1_500); //Give bugger buffer to ensure all devices are ready to receive this message.
                            await Clients.Group(gameId).SendAsync("ShowStats", JsonConvert.SerializeObject(results.Select(result => new ResultGet(result))));
                        }

                    }, LockSettings);
                }

                if ((await GetResultsIfAllUsersValidated()) == null)
                {
                    var settings = await SettingsService.Get();
                    var thereIsAPreviousWinner = existingResults.Any(x => x.ValidityScore > settings.PassMark);
                    if (!thereIsAPreviousWinner)
                    {
                        await gameId.AsLock(async () =>
                        {
                            var game = await GameService.Get(gameId);
                            if (game.StatusCode != GameStatusDto.Active.Code)
                            {
                                Logger.LogWarning("Game [{gameId}] is not in an active state", gameId);
                                return;
                            }

                            //Let other users know we are validating the results
                            await Clients.Group(gameId).SendAsync("ValidatingResult", userDisplayName);
                            await GameService.UpdateStatus(game.GameId, GameStatusDto.Validating);

                            var resultDto = await ResultService.ValidateGameResults(gameId, imageBytes, userDisplayName,
                                totalPenSeconds);
                            if (resultDto.ValidityScore >= settings.PassMark)
                            {
                                await GameService.UpdateStatus(game.GameId, GameStatusDto.DisplayingStats);

                                Logger.LogInformation(
                                    $"{nameof(ValidateResults)} Results successful with score of {resultDto.ValidityScore}.");
                                await Clients.Group(gameId).SendAsync("SuccessfulResult", userDisplayName,
                                    resultDto.ValidityScore);
                            }
                            else
                            {
                                Logger.LogInformation(
                                    $"{nameof(ValidateResults)} Results not successful with score of {resultDto.ValidityScore}.");
                                var results = await GetResultsIfAllUsersValidated();

                                if (!results.Any())
                                {
                                    await GameService.UpdateStatus(game.GameId, GameStatusDto.Active);
                                    await Clients.Group(gameId).SendAsync("FailedResult", userDisplayName,
                                        resultDto.ValidityScore);
                                }
                                else
                                {
                                    await GameService.UpdateStatus(game.GameId, GameStatusDto.DisplayingStats);
                                    await Clients.Group(gameId).SendAsync("SuccessfulResult", "Chicken", -999);
                                }
                            }
                        }, LockSettings);
                    }
                    else
                    {
                        await ResultService.ValidateGameResults(gameId, imageBytes, userDisplayName, totalPenSeconds);
                    }
                }

                await SendStatsIfEveryoneIsDone();
            }
        }
    }
}
