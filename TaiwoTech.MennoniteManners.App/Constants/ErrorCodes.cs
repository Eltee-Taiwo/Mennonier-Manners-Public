using TaiwoTech.MennoniteManners.App.Domain;

namespace TaiwoTech.MennoniteManners.App.Constants
{
    /// <summary>
    /// Common Error Codes to make looking up issues easier
    /// <br />
    /// <br />
    /// Systems:
    /// <br />
    /// 9** - Real Time Server
    /// 8** - API
    /// </summary>
    public static class ErrorCodes
    {
        public static ErrorCode ApiServerUnknown { get; } = new("ER800");
        public static ErrorCode ApiServerNoData { get; } = new("ER801");

        public static ErrorCode RealTimeServerUnknown { get; } = new("ER900");
        public static ErrorCode RealTimeServerTimeOut { get; } = new("ER901");
        public static ErrorCode RealTimeServerNoData { get; } = new("ER902");
        public static ErrorCode RealTimeServerHubError { get; } = new("ER903");
    }

    public record ErrorCode(string Value) : DomainTypeString(Value);
}
