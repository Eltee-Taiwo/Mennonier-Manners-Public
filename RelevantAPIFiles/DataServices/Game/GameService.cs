using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TaiwoTech.Eltee.DataServices.MennoniteManners.GameSession;
using TaiwoTech.Eltee.DataServices.MennoniteManners.GameStatus;
using TaiwoTech.Eltee.Settings;

namespace TaiwoTech.Eltee.DataServices.MennoniteManners.Game
{
    public class GameService : DataService
    {
        private const string GameIdPattern = @"[a-zA-z]{3}\d{3}";
        private const int PartLength = 3;
        private const string Letters = "ABCDEFGHJKMNPQRSTUVWXYZ";
        private const string Numbers = "123456789";
        private GameSessionService GameSessionService { get; }


        private static readonly Random Random = new();
        private static readonly SemaphoreSlim SemaphoreSlim = new(1, 1);

        public GameService(
            IOptions<DatabaseSettings> dataBaseOptions,
            ILogger<GameService> logger,
            IMemoryCache cache,
            GameSessionService gameSessionService
        ) : base(dataBaseOptions, logger, cache)
        {
            GameSessionService = gameSessionService;
        }

        public async Task<GameDto> Create()
        {
            var gameId = GenerateGameId();
            var semaphoreReleased = false;

            if (!Regex.IsMatch(gameId, GameIdPattern)) return await Create();

            await SemaphoreSlim.WaitAsync();
            try
            {
                var existingGame = await Get(gameId);
                if (existingGame != null)
                {
                    semaphoreReleased = true;
                    SemaphoreSlim.Release();
                    return await Create();
                }

                var newGame = new GameDto
                {
                    GameId = gameId,
                    StatusCode = GameStatusDto.PreGame.Code,
                    CreatedAt = DateTime.UtcNow
                };

                var gameCreated = await Save(newGame);
                return gameCreated ? newGame : null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while creating a new game");
                return null;
            }
            finally
            {
                if (!semaphoreReleased)
                {
                    SemaphoreSlim.Release();
                }
            }
            
        }

        public Task<GameDto> Get(string gameId)
        {
            Logger.LogDebug("Getting the details for gameId: {gameId}", gameId);
            return Cache.GetOrCreateAsync<GameDto>($"{nameof(GameService)}-{gameId}", async entry =>
            {
                const string query = @"
                    SELECT
                        GameId,
                        StatusCode,
                        CreatedAt,
                        EndedAt
                    FROM Mennonite.Games
                    WHERE GameId = @gameId
                ";

                await using var connection = DatabaseConnection;
                var result = await connection.QuerySingleOrDefaultAsync<GameDto>(query, new { gameId });
                entry.Value = CacheEntryOptions;

                return result;
            });
        }

        private string GenerateGameId()
        {
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < PartLength; i++)
            {
                stringBuilder.Append(Letters[Random.Next(Letters.Length)]);
            }

            for (var i = 0; i < PartLength; i++)
            {
                stringBuilder.Append(Numbers[Random.Next(Numbers.Length)]);
            }

            return stringBuilder.ToString();
        }

        private async Task<bool> Save(GameDto dto)
        {
            await using (DatabaseConnection)
            {
                var cacheKey = $"{nameof(GameService)}-{dto.GameId}";
                const string query = @"
                    INSERT INTO Mennonite.Games (GameId, StatusCode, CreatedAt, EndedAt)
                    VALUES
                    (@gameId, @statusCode, @createdAt, null)
                ";
                var dynamicParameters = new DynamicParameters(dto);

                var rowsAffected = await DatabaseConnection.ExecuteAsync(query, dynamicParameters);
                if (rowsAffected > 0)
                {
                    Cache.Remove(cacheKey);
                }
                return rowsAffected > 0;
            }
        }

        public async Task UpdateStatus(string gameId, GameStatusDto newStatus)
        {
            await using (DatabaseConnection)
            {
                var cacheKey = $"{nameof(GameService)}-{gameId}";
                const string query = @"
                    UPDATE Mennonite.Games
                        SET StatusCode = @statusCode,
                            EndedAt = @endedAt
                    WHERE GameId = @gameId
                ";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add(nameof(gameId), gameId);
                dynamicParameters.Add("statusCode", newStatus.Code);
                dynamicParameters.Add("endedAt", newStatus.Code == GameStatusDto.Closed.Code ? DateTime.UtcNow : null);

                var rowsAffected = await DatabaseConnection.ExecuteAsync(query, dynamicParameters);
                if (rowsAffected > 0)
                {
                    Cache.Remove(cacheKey);
                    if (newStatus.Code == GameStatusDto.Closed.Code || newStatus.Code == GameStatusDto.PreGame.Code)
                    {
                        await GameSessionService.EndActiveSession(gameId);
                    }
                }
            }
        }
    }
}
