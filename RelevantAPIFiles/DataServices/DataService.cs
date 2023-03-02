using System;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaiwoTech.Eltee.Settings;

namespace TaiwoTech.Eltee.DataServices
{
    public class DataService
    {
        protected SqlConnection DatabaseConnection => new SqlConnection(ConnectionString);
        protected ILogger<DataService> Logger { get; }
        protected IMemoryCache Cache { get; }
        protected string ConnectionString { get; }

        protected static MemoryCacheEntryOptions CacheEntryOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(1)
        };


        public DataService(IOptions<DatabaseSettings> dataBaseOptions, ILogger<DataService> logger, IMemoryCache cache)
        {
            ConnectionString = dataBaseOptions.Value.ConnectionString;
            Logger = logger;
            Cache = cache;
        }
    }
}
