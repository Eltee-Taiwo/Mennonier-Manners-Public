using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaiwoTech.Eltee.DataServices.MennoniteManners.Game;
using TaiwoTech.Eltee.DataServices.MennoniteManners.GameSession;
using TaiwoTech.Eltee.DataServices.MennoniteManners.GameStatus;
using TaiwoTech.Eltee.Settings;

namespace TaiwoTech.Eltee.DataServices.MennoniteManners.Participant
{
    public class ParticipantService : DataService
    {
        private GameService GameService { get; }
        private GameSessionService GameSessionService { get; }

        public ParticipantService(
            IOptions<DatabaseSettings> dataBaseOptions,
            ILogger<ParticipantService> logger,
            IMemoryCache cache,
            GameService gameService,
            GameSessionService gameSessionService
        ) : base(dataBaseOptions, logger, cache)
        {
            GameService = gameService;
            GameSessionService = gameSessionService;
        }

        public async Task<List<ParticipantDto>> Get(string gameId, string uniqueName = null)
        {
            if (string.IsNullOrWhiteSpace(gameId))
            {
                throw new ValidationException("No identifier provided");
            }
            Logger.LogDebug("Getting the participants for {gameId}", gameId);
            
            var result = await Cache.GetOrCreateAsync($"{nameof(ParticipantService)}-{gameId}", async entry =>
            {
                const string query = @"
                    SELECT
                        Id,
                        GameId,
                        UserName,
                        UniqueUserName,
                        ConnectionId,
                        IsHost,
                        HasFinished,
                        JoinedAt,
                        LeftAt
                    FROM Mennonite.Participants
                    WHERE GameId = @gameId
                    AND LeftAt Is Null
                ";

                const string uniqueNameWhereClause = " AND UniqueUserName = @uniqueName";

                await using var connection = DatabaseConnection;
                var result = await connection.QueryAsync<ParticipantDto>(
                    query + (string.IsNullOrWhiteSpace(uniqueName) ? string.Empty : uniqueNameWhereClause),
                    new { gameId, uniqueName }
                );
                entry.Value = CacheEntryOptions;
                return result;
            });


            return string.IsNullOrWhiteSpace(uniqueName) ?
                result?.ToList() :
                result?.Where(x => x.UniqueUserName.Equals(uniqueName, StringComparison.InvariantCultureIgnoreCase)).ToList();
        }

        public Task<ParticipantDto> GetByConnectionId(string connectionId)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
            {
                throw new ValidationException("No identifier provided");
            }

            Logger.LogDebug("Getting the participant with connectionId {connectionId}", connectionId);
            return Cache.GetOrCreateAsync($"{nameof(ParticipantService)}-{connectionId}", async entry =>
            {
                const string query = @"
                    SELECT
                        Id,
                        GameId,
                        UserName,
                        UniqueUserName,
                        ConnectionId,
                        IsHost,
                        HasFinished,
                        JoinedAt,
                        LeftAt
                    FROM Mennonite.Participants
                    WHERE ConnectionId = @connectionId
                    AND LeftAt Is Null
                ";

                await using var connection = DatabaseConnection;
                var result = await connection.QuerySingleOrDefaultAsync<ParticipantDto>(query, new { connectionId });
                entry.Value = CacheEntryOptions;

                return result;
            });
        }

        public async Task<string> Add(ParticipantDto dto)
        {
            try
            {
                await using (DatabaseConnection)
                {
                    var cacheKey = $"{nameof(ParticipantService)}-{dto.GameId}";
                    const string query = @"

                    declare @OGRecord table(
	                    Id INT,
	                    UserName VarChar(15),
	                    UniqueUserName VARCHAR(15)
                    )

                    declare @DuplicateRecord table(
	                    Id INT,
	                    UserName VarChar(15),
	                    UniqueUserName VARCHAR(15)
                    )

                    insert into @DuplicateRecord
                    select TOP 1 Id, UserName, UniqueUserName FROM Mennonite.Participants
                    WHERE 
	                    GameId = @gameId
	                    AND UserName = @userName
                    ORDER BY JoinedAt DESC

                    insert into @OGRecord
                    select TOP 1 -1, @userName, null

                    declare @hyphenIndex int;
                    select TOP 1 @hyphenIndex = charindex('-', reverse(UniqueUserName)) - 1
                    FROM @DuplicateRecord

                    declare @realHyphenIndex int = IIF(@hyphenIndex > 0, @hyphenIndex, 0)

                    declare @uniqueNameCount INT;
                    select @uniqueNameCount = RIGHT(UniqueUserName, @realHyphenIndex)
                    FROM @DuplicateRecord

                    DECLARE @NewUniqueName VARCHAR(15)
                    select @NewUniqueName = CASE WHEN @uniqueNameCount IS NULL then @userName else CONCAT(D.UserName, '-', (@uniqueNameCount + 1)) end
                    From @OGRecord O
                    left join @DuplicateRecord D on O.UserName = D.UserName

                    INSERT INTO Mennonite.Participants (GameId, UserName, UniqueUserName, ConnectionId, IsHost, JoinedAt, LeftAt)
                    OUTPUT INSERTED.UniqueUserName
                    VALUES
                    (@gameId, @userName, @NewUniqueName, @connectionId, @isHost, @joinedAt, null)
                ";
                    var dynamicParameters = new DynamicParameters(dto);

                    var uniqueUserName = await DatabaseConnection.ExecuteScalarAsync<string>(query, dynamicParameters);
                    if (!string.IsNullOrWhiteSpace(uniqueUserName))
                    {
                        Cache.Remove(cacheKey);
                    }
                    return uniqueUserName;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while trying to add a new participant. {@dto}", dto);
                return string.Empty;
            }
        }

        public async Task<RemoveParticipantResult> Leave(string gameId, string participantUniqueName)
        {
            try
            {
                await using (DatabaseConnection)
                {
                    var participant = (await Get(gameId, participantUniqueName)).SingleOrDefault();
                    if (participant == null) return null;

                    var cacheKey = $"{nameof(ParticipantService)}-{gameId}";
                    const string query = @"
                    --Results here
                    declare @result table(
	                    RemovedParticipantId INT NOT NULL,
	                    WasHost bit NOT NULL,
	                    PromotedHost varchar (15) NULL
                    );

                    --Remove participant from game
                    UPDATE Mennonite.Participants
                    SET LeftAt = @leaveDate
                    OUTPUT INSERTED.Id, INSERTED.IsHost, null INTO @result
                    WHERE GameId = @gameId
                    AND UniqueUserName = @participantUniqueName


                    --If participant was host, promote a new host
                    IF EXISTS (Select * from @result WHERE WasHost = 1)
                    BEGIN
	                    --Flag the new host
	                    WITH NewHost As (
		                    SELECT TOP (1) UniqueUserName, IsHost
		                    FROM Mennonite.Participants	
                            WHERE GameId = @gameId
                                AND LeftAt IS NULL
                            ORDER BY JoinedAt Asc
                        )
	                    UPDATE NewHost
	                    SET IsHost = 1

	                    --Add new host as return variable
	                    UPDATE @result
	                    SET PromotedHost = (SELECT UniqueUserName FROM Mennonite.Participants WHERE GameId = @gameId and IsHost = 1 AND LeftAt IS NULL)
                    END

                    SELECT RemovedParticipantId, PromotedHost FROM @result
                ";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add(nameof(gameId), gameId);
                    dynamicParameters.Add(nameof(participantUniqueName), participantUniqueName);
                    dynamicParameters.Add("leaveDate", DateTime.UtcNow);

                    var gridReader = await DatabaseConnection.QueryMultipleAsync(query, dynamicParameters);
                    var rez = await gridReader.ReadAsync<RemoveParticipantResult>();

                    var result = rez.First();
                    Cache.Remove(cacheKey);

                    var participants = await Get(gameId);
                    if (!participants.Any())
                    {
                        await GameSessionService.EndActiveSession(gameId);
                        await GameService.UpdateStatus(gameId, GameStatusDto.Closed);
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while trying to remove participant {participantName} from game {gameId}.", participantUniqueName, gameId);
                return null;
            }
        }


        public async Task<string> GetNextRoller(string gameId, int rollResult, string roller)
        {
            var participants = (await Get(gameId)).Where(x => !x.HasFinished).ToList();

            //if only one person is playing, they are the only one that can roll
            if (participants.Count == 1) return participants.First().UniqueUserName;

            var participantsMinusWriter = participants
                .Select(x => x.UniqueUserName)
                .OrderBy(x => x)
                .Where(x => !x.Equals(Writers.CurrentWriters[gameId]))
                .ToList();

            var lastWriter = Writers.CurrentWriters[gameId];

            if (rollResult == 6)
            {
                Writers.CurrentWriters[gameId] = roller;

                //if only two people are playing, the next roller is whoever was just writing
                if (participantsMinusWriter.Count == 1)
                {
                    return lastWriter;
                }
            }

            // Get the next roller from the sorted participants
            var nextRoller = participantsMinusWriter
                .SkipWhile(x => !x.Equals(roller))
                .Skip(1)
                .DefaultIfEmpty(participantsMinusWriter.FirstOrDefault())
                .FirstOrDefault();

            return nextRoller ?? roller;
        }

        public async Task SetHasFinished(string gameId, string uniqueUserName)
        {
            await using (DatabaseConnection)
            {
                var cacheKey = $"{nameof(ParticipantService)}-{gameId}";
                const string query = @"
                    UPDATE Mennonite.Participants
                    SET HasFinished = 1
                    WHERE 
	                    GameId = @gameId AND
	                    UniqueUserName = @uniqueUserName
                ";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add(nameof(gameId), gameId);
                dynamicParameters.Add(nameof(uniqueUserName), uniqueUserName);

                var rowsUpdated = await DatabaseConnection.ExecuteAsync(query, dynamicParameters);

                if (rowsUpdated > 0)
                {
                    Cache.Remove(cacheKey);
                }
            }
        }

        public async Task ResetHasFinished(string gameId)
        {
            await using (DatabaseConnection)
            {
                var cacheKey = $"{nameof(ParticipantService)}-{gameId}";
                const string query = @"
                    UPDATE Mennonite.Participants
                    SET HasFinished = 0
                    WHERE 
	                    GameId = @gameId
                ";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add(nameof(gameId), gameId);

                var rowsUpdated = await DatabaseConnection.ExecuteAsync(query, dynamicParameters);

                if (rowsUpdated > 0)
                {
                    Cache.Remove(cacheKey);
                }
            }
        }
    }
}
