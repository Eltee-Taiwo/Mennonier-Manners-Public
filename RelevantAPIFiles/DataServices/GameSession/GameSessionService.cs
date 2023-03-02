using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaiwoTech.Eltee.DataServices.MennoniteManners.Participant;
using TaiwoTech.Eltee.Settings;

namespace TaiwoTech.Eltee.DataServices.MennoniteManners.GameSession
{
    public class GameSessionService : DataService
    {
        public GameSessionService(
            IOptions<DatabaseSettings> dataBaseOptions,
            ILogger<GameSessionService> logger,
            IMemoryCache cache
        ) : base(dataBaseOptions, logger, cache)
        {
        }

        public async Task<int> Create(string gameId, int maxGameLevel, int gameTypeId, bool isReversed)
        {
            Logger.LogDebug("Creating a new game session for {gameId}", gameId);
            var cacheKey = $"{nameof(GameSessionService)}-{gameId}";
            const string query = @"
                INSERT INTO Mennonite.GameSessions (GameId, CreatedAt, MaxLevel, GameTypeId, IsReversed)
                OUTPUT INSERTED.Id
                VALUES
                (@gameId, @createdAt, @maxGameLevel, @gameTypeId, @isReversed)

                IF(@@ROWCOUNT > 0)
                BEGIN
	                UPDATE Mennonite.Games
	                SET StatusCode = 'P'
	                WHERE GameId = @gameId
                END
            ";
            var dynamicParameters = new DynamicParameters();
            dynamicParameters.Add(nameof(gameId), gameId);
            dynamicParameters.Add(nameof(gameTypeId), gameTypeId);
            dynamicParameters.Add(nameof(isReversed), isReversed);
            dynamicParameters.Add("createdAt", DateTime.UtcNow);
            dynamicParameters.Add("maxGameLevel", maxGameLevel);

            var sessionId = await DatabaseConnection.ExecuteScalarAsync<int>(query, dynamicParameters);
            if (sessionId > 0)
            {
                Cache.Remove(cacheKey);
                Writers.CurrentWriters.TryAdd(gameId, string.Empty);
            }
            return sessionId;
        }

        public Task<GameSessionDto> GetActiveSession(string gameId)
        {
            Logger.LogDebug("Getting the sessions for {gameId}", gameId);
            return Cache.GetOrCreateAsync($"{nameof(GameSessionService)}-{gameId}", async entry =>
            {
                const string query = @"
                    SELECT
                        Id,
                        GameId,
                        GameTypeId,
                        MaxLevel,
                        IsReversed,
                        CreatedAt,
                        EndedAt
                    FROM Mennonite.GameSessions
                    WHERE GameId = @gameId
                        AND EndedAt IS NULL
                ";

                await using var connection = DatabaseConnection;
                var result = await connection.QuerySingleOrDefaultAsync<GameSessionDto>(query, new { gameId });
                entry.Value = CacheEntryOptions;

                return result;
            });
        }

        public async Task EndActiveSession(string gameId)
        {
            Logger.LogDebug("Ending active game session for {gameId}", gameId);
            var cacheKey = $"{nameof(GameSessionService)}-{gameId}";
            const string query = @"
                UPDATE Mennonite.GameSessions
                SET EndedAt = @endedAt
                WHERE GameId = @gameId
                    AND EndedAt IS NULL
            ";
            var dynamicParameters = new DynamicParameters();
            dynamicParameters.Add(nameof(gameId), gameId);
            dynamicParameters.Add("endedAt", DateTime.UtcNow);

            var rowCount = await DatabaseConnection.ExecuteAsync(query, dynamicParameters);
            if (rowCount > 0)
            {
                Cache.Remove(cacheKey);
                Writers.CurrentWriters.TryRemove(gameId, out _);
            }
        }

    }
}
