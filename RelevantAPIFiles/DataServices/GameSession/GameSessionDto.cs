using System;

namespace TaiwoTech.Eltee.DataServices.MennoniteManners.GameSession
{
    public class GameSessionDto
    {
        public int Id { get; set; }
        public string GameId { get; set; }
        public int GameTypeId { get; set; }
        public int MaxLevel { get; set; }
        public bool IsReversed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? EndedAt { get; set; }
    }
}
