namespace TaiwoTech.MennoniteManners.App.Extensions
{
    public static class AnalyticsExtensions
    {
        private static string[] DoNotShowInAnalytics => new[] { "CurrentRoller", "GameId", "UserName" };
        public static Dictionary<string, string> ToAnalyticsProperties(this IDictionary<string, object> self)
        {

            var analyticsProperties =  self.Where(pair => !DoNotShowInAnalytics.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value.ToString());
            return analyticsProperties;
        }
    }
}
