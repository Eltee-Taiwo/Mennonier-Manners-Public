using Microsoft.Data.SqlClient;

namespace TaiwoTech.Eltee.Libraries.Utilities
{
    public class LockSettings
    {
        public int TimeOutSeconds { get; set; } = 15;
        public int DelayIntervalSeconds { get; set; } = 2;
        public string ConnectionString { get; set; } = null!;
        public SqlConnection DatabaseConnection => new (ConnectionString);
    }
}
