namespace TaiwoTech.MennoniteManners.App.Services.API
{
    public class MennoniteMannersOverview
    {
        public string MinimumAcceptableApiVersion { get; set; }
        public double TimeToForceNextRoll { get; set; }
        public string BuyMeACoffeeLink { get; set; }
        public IEnumerable<GameTypeDto> GameTypes { get; set; }
    }
}
