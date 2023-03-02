using System.Diagnostics;
using Dapper;

namespace TaiwoTech.Eltee.Libraries.Utilities
{
    public static class Lock
    {
        public static async Task AsLock(this string key, Func<Task> actionToTake, LockSettings settings)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var lockAcquired = false;
            while (stopwatch.Elapsed.Seconds <= settings.TimeOutSeconds)
            {
                const string query = @"
			        IF EXISTS(Select * from dbo.Locks where [Key] = @key AND [TimeStamp] > DATEADD(minute, -5, GetUtcDate()))
				        SELECT 0;
			        ELSE
				        BEGIN
					        DELETE FROM dbo.Locks where [Key] = @key
					        INSERT INTO dbo.Locks ([Key]) VALUES (@key);
					        SELECT 1;
				        END
                    ";

                await using var connection = settings.DatabaseConnection;
                lockAcquired = await connection.QuerySingleAsync<bool>(query, new { key });
                if (lockAcquired)
                {
                    break;
                }
                await Task.Delay(settings.TimeOutSeconds * 1_000);
            }

            if (lockAcquired)
            {
                try
                {
                    await actionToTake();
                }
                finally
                {
                    const string query = @"DELETE FROM dbo.Locks where [Key] = @key";

                    await using var connection = settings.DatabaseConnection;
                    await connection.ExecuteAsync(query, new { key });
                }
            }
            else
            {
                throw new SemaphoreFullException("Unable to acquire lock");
            }

        }
    }
}