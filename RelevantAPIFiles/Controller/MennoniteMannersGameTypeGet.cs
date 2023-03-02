using System.Collections.Generic;
using System.Linq;
using TaiwoTech.Eltee.DataServices.MennoniteManners.GameType;

namespace TaiwoTech.Eltee.Controllers.Mennonite
{
    public class MennoniteMannersGameTypeGet
    {
        public int Id { get; }
        public string DisplayName { get; }
        public IEnumerable<int> GameLengths { get; }
        public IEnumerable<string> TargetStringValues { get; }

        public MennoniteMannersGameTypeGet(GameTypeDto dto)
        {
            Id = dto.Id;
            DisplayName = dto.DisplayName;
            GameLengths = dto.GameLengths.Any() ? dto.GameLengths.Split('|').Select(int.Parse) : Enumerable.Empty<int>();
            TargetStringValues = dto.TargetString.Split('|');
        }
    }
}
