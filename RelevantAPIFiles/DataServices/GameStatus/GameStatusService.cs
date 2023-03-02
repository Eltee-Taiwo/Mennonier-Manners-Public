using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaiwoTech.Eltee.Settings;

namespace TaiwoTech.Eltee.DataServices.MennoniteManners.GameStatus
{
    public class GameStatusService : DataService
    {
        public GameStatusService(
            IOptions<DatabaseSettings> dataBaseOptions,
            ILogger<GameStatusService> logger,
            IMemoryCache cache
        ) : base(dataBaseOptions, logger, cache)
        {
        }

        public Task<List<GameStatusDto>> Get()
        {
            Logger.LogDebug("Getting the list of game statuses");
            return Cache.GetOrCreateAsync(nameof(GameStatusService), async entry =>
            {
                const string query = @"
                    SELECT
                        Code,
                        Description
                    FROM Mennonite.GameStatus
                ";

                await using var connection = DatabaseConnection;
                var result = await connection.QueryAsync<GameStatusDto>(query);
                entry.Value = CacheEntryOptions;

                return result.ToList();
            });
        }
    }
}
