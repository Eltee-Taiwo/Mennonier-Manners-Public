using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaiwoTech.Eltee.Settings;

namespace TaiwoTech.Eltee.DataServices.MennoniteManners.Settings
{
    public class SettingsService : DataService
    {
        public SettingsService(
            IOptions<DatabaseSettings> dataBaseOptions,
            ILogger<SettingsService> logger,
            IMemoryCache cache
        ) : base(dataBaseOptions, logger, cache)
        {
        }

        public Task<SettingsDto> Get()
        {
            Logger.LogDebug("Getting the settings");
            return Cache.GetOrCreateAsync($"{nameof(SettingsService)}", async entry =>
            {
                const string query = @"
                    SELECT
                        PassMark,
                        TimeToRoll,
                        TimeToForceNextRoll,
                        MinimumAcceptableApiVersion,
                        EnabledAt,
                        ExpiredAt
                    FROM Mennonite.Settings
                    WHERE ExpiredAt IS NULL
                ";

                await using var connection = DatabaseConnection;
                var result = await connection.QuerySingleAsync<SettingsDto>(query);
                entry.Value = CacheEntryOptions;

                return result;
            });
        }
    }
}
