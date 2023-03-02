using Dapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaiwoTech.Eltee.Settings;

namespace TaiwoTech.Eltee.DataServices.MennoniteManners.GameType
{
    public class GameTypeService : DataService
    {
        public GameTypeService(
            IOptions<DatabaseSettings> dataBaseOptions,
            ILogger<GameTypeService> logger,
            IMemoryCache cache
        ) : base(dataBaseOptions, logger, cache)
        {
        }

        public Task<IEnumerable<GameTypeDto>> Get()
        {
            Logger.LogDebug("Getting the game types");

            return Cache.GetOrCreateAsync($"{nameof(GameTypeService)}", async entry =>
            {
                const string query = @"SELECT Id, DisplayName, GameLengths, TargetString FROM Mennonite.GameTypes";
                
                await using var connection = DatabaseConnection;
                var result = await connection.QueryAsync<GameTypeDto>(query);
                entry.Value = CacheEntryOptions;
                return result;
            });
        }
    }
}
