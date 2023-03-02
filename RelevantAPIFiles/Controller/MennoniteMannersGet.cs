using System.Collections.Generic;
using System.Linq;
using TaiwoTech.Eltee.DataServices.MennoniteManners.GameType;
using TaiwoTech.Eltee.DataServices.MennoniteManners.Settings;

namespace TaiwoTech.Eltee.Controllers.Mennonite
{
    public class MennoniteMannersGet
    {
        public int PassMark { get; }
        public double TimeToRoll { get; }
        public double TimeToForceNextRoll { get; }
        public string MinimumAcceptableApiVersion { get; }
        public string BuyMeACoffeeLink { get; }
        public IEnumerable<MennoniteMannersGameTypeGet> GameTypes { get; set; }

        public MennoniteMannersGet(SettingsDto settingsDto, IEnumerable<GameTypeDto> gameTypeDtos)
        {
            PassMark = settingsDto.PassMark;
            TimeToRoll = settingsDto.TimeToRoll;
            TimeToForceNextRoll = settingsDto.TimeToForceNextRoll;
            MinimumAcceptableApiVersion = settingsDto.MinimumAcceptableApiVersion;
            BuyMeACoffeeLink = "https://www.buymeacoffee.com/eltee";
            GameTypes = gameTypeDtos.Select(x => new MennoniteMannersGameTypeGet(x));
        }
    }
}
