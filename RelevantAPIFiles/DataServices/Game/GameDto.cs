using System;

namespace TaiwoTech.Eltee.DataServices.MennoniteManners.Game
{
    public class GameDto
    {
        public string GameId { get; set; }
        public char StatusCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? EndedAt { get; set; }
    }
}
