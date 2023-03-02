using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaiwoTech.Eltee.DataServices.MennoniteManners.GameSession;
using TaiwoTech.Eltee.DataServices.MennoniteManners.GameType;
using TaiwoTech.Eltee.DataServices.MennoniteManners.Participant;
using TaiwoTech.Eltee.Settings;

namespace TaiwoTech.Eltee.DataServices.MennoniteManners.Result
{
    public class ResultService : DataService
    {
        private GameSessionService GameSessionService { get; }
        private GameTypeService GameTypeService { get; }
        private ParticipantService ParticipantService { get; }

        public ResultService(
            IOptions<DatabaseSettings> dataBaseOptions,
            ILogger<ResultService> logger,
            IMemoryCache cache,
            GameSessionService gameSessionService,
            GameTypeService gameTypeService,
            ParticipantService participantService
        ) : base(dataBaseOptions, logger, cache)
        {
            GameSessionService = gameSessionService;
            ParticipantService = participantService;
            GameTypeService = gameTypeService;
        }


        /// <summary>
        /// Validate that the Image meets the threshold for an acceptable results
        /// </summary>
        /// <param name="gameId">The ID of Game this is being done for</param>
        /// <param name="imageData">A single image with all responses as a byte array</param>
        /// <param name="userDisplayName">The display name of the user that has finished</param>
        /// <param name="totalPenSeconds">The total number of seconds the user had the pen for</param>
        /// <returns></returns>
        public async Task<ResultDto> ValidateGameResults(string gameId, byte[] imageData, string userDisplayName, int totalPenSeconds)
        {
            var session = await GameSessionService.GetActiveSession(gameId);
            if (session == null) return null;

            //Confirm user belongs to game
            var participants = await ParticipantService.Get(gameId);
            var participantDto = participants.FirstOrDefault(x => x.UniqueUserName.Equals(userDisplayName, StringComparison.InvariantCultureIgnoreCase));
            if (participantDto == null) return null;

            await ParticipantService.SetHasFinished(gameId, userDisplayName);

            //Build target string
            var gameType = (await GameTypeService.Get()).Single(x => x.Id == session.GameTypeId);
            var targetStringValues = session.IsReversed ? gameType.TargetString.Split('|').Reverse() : gameType.TargetString.Split('|');
            var targetString = string.Join(null, targetStringValues.Take(session.MaxLevel));

            //Determine similarity
            var imageText = await ReadImageContents(imageData);
            var similarityCoefficient = GetDamerauLevenshteinDistance(imageText, targetString);
            Logger.LogInformation($"Validating [{imageText}] against [{targetString}] resulted in a result of {similarityCoefficient}");

            var id = await Save(session.Id, participantDto.Id, similarityCoefficient, totalPenSeconds);
            var dto = await Get(id);

            return dto;
        }

        public Task<ResultDto> Get(int resultId)
        {
            Logger.LogDebug("Getting results with Id {resultId}", resultId);
            return Cache.GetOrCreateAsync($"{nameof(ResultService)}-{resultId}", async entry =>
            {
                var result = (await Get("WHERE GR.Id = @resultId", new { resultId })).SingleOrDefault();
                entry.Value = CacheEntryOptions;
                return result;
            });
        }

        public Task<List<ResultDto>> GetBySessionId(int sessionId)
        {
            Logger.LogDebug("Getting results with session Id {sessionId}", sessionId);
            return Cache.GetOrCreateAsync($"{nameof(ResultService)}-session-{sessionId}", async entry =>
            {
                var result = (await Get("WHERE GR.SessionId = @sessionId", new { sessionId }));
                entry.Value = CacheEntryOptions;
                return result;
            });
        }

        private async Task<List<ResultDto>> Get(string whereClause, object parameters)
        {
            const string query = @"
                SELECT
                    GR.Id,
                    GR.ParticipantId,
                    P.UniqueUserName,
                    GR.SessionId,
                    GR.ValidityScore,
                    GR.TimeStamp,
                    GR.TotalPenSeconds
                FROM Mennonite.GameResults GR
                LEFT JOIN Mennonite.Participants P ON P.Id = GR.ParticipantId
            ";

            await using var connection = DatabaseConnection;
            var result = await connection.QueryAsync<ResultDto>($"{query} {whereClause}", parameters);
            return result.ToList();
        }

        private async Task<int> Save(int sessionId, int participantId, double validityScore, int totalPenSeconds)
        {
            Logger.LogDebug("Saving result for session {sessionId}", sessionId);
            const string query = @"
                INSERT INTO Mennonite.GameResults(SessionId, ParticipantId, ValidityScore, TimeStamp, TotalPenSeconds)
                OUTPUT INSERTED.Id
                VALUES
                (@sessionId, @participantId, @validityScore, @timeStamp, @totalPenSeconds)
            ";
            var dynamicParameters = new DynamicParameters();
            dynamicParameters.Add(nameof(sessionId), sessionId);
            dynamicParameters.Add(nameof(participantId), participantId);
            dynamicParameters.Add(nameof(validityScore), (int)(validityScore * 100));
            dynamicParameters.Add(nameof(totalPenSeconds), totalPenSeconds);
            dynamicParameters.Add("TimeStamp", DateTime.UtcNow);

            var dtoId = await DatabaseConnection.ExecuteScalarAsync<int>(query, dynamicParameters);
            Cache.Remove($"{nameof(ResultService)}-session-{sessionId}");
            return dtoId;
        }

        private static double GetDamerauLevenshteinDistance(string submitted, string target)
        {
            var sanitisedSubmitted = submitted.Trim();
            var sanitisedTarget = target.Trim();

            if (string.IsNullOrEmpty(sanitisedSubmitted) || string.IsNullOrEmpty(sanitisedTarget))
            {
                return 0.0;
            }

            int submittedLength = sanitisedSubmitted.Length; // length of s
            double targetLength = sanitisedTarget.Length; // length of t

            //Below is the actual algorithm. I don't understand it :/
            int[] p = new int[submittedLength + 1]; //'previous' cost array, horizontally
            int[] d = new int[submittedLength + 1]; // cost array, horizontally

            // indexes into strings s and t
            int i; // iterates through s
            int j; // iterates through t

            for (i = 0; i <= submittedLength; i++)
            {
                p[i] = i;
            }

            for (j = 1; j <= targetLength; j++)
            {
                char tJ = sanitisedTarget[j - 1]; // jth character of t
                d[0] = j;

                for (i = 1; i <= submittedLength; i++)
                {
                    int cost = sanitisedSubmitted[i - 1] == tJ ? 0 : 1; // cost
                    // minimum of cell to the left+1, to the top+1, diagonally left and up +cost                
                    d[i] = Math.Min(Math.Min(d[i - 1] + 1, p[i] + 1), p[i - 1] + cost);
                }

                // copy current distance counts to 'previous row' distance counts
                int[] dPlaceholder = p; //placeholder to assist in swapping p and d
                p = d;
                d = dPlaceholder;
            }

            // our last action in the above loop was to switch d and p, so p now 
            // actually has the most recent cost counts
            var decimalAccuracy = (targetLength - p[submittedLength]) / targetLength;
            return Math.Round(decimalAccuracy, 2, MidpointRounding.AwayFromZero);
        }

        private async Task<string> ReadImageContents(byte[] imageBytes)
        {
            const string azureSubscriptionKey = "";
            const string azureVisionEndpoint = "";
            const int numberOfCharsInOperationId = 36;

            using var stream = new MemoryStream(imageBytes);
            var visionClient = new ComputerVisionClient(new ApiKeyServiceClientCredentials(azureSubscriptionKey))
            {
                Endpoint = azureVisionEndpoint
            };

            //Tell VisionAPI to start processing the image
            var textHeaders = await visionClient.ReadInStreamAsync(stream);
            // After the request, get the operation location (operation ID)
            var operationLocation = textHeaders.OperationLocation;
            var operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

            //Keep checking until we have a response from the VISION API
            ReadOperationResult results;
            do
            {
                await Task.Delay(500);
                results = await visionClient.GetReadResultAsync(Guid.Parse(operationId));
            }
            while (results.Status == OperationStatusCodes.Running || results.Status == OperationStatusCodes.NotStarted);

            //Pull in the results into a single string
            var textUrlFileResults = results.AnalyzeResult.ReadResults;
            var textResult = new StringBuilder();
            foreach (var page in textUrlFileResults)
            {
                foreach (var line in page.Lines)
                {
                    textResult.Append(line.Text);
                }
            }

            return textResult.ToString().Replace(" ", "").Replace("-", "").Replace(".", "");
        }
    }
}
