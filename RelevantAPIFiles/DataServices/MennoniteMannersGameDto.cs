using System;

namespace TaiwoTech.Eltee.DataServices.MennoniteManners
{
    public class MennoniteMannersGameDto
    {
        public int Id { get; set; }
        public string GameId { get; set; }
        public string ConnectionId { get; set; }
        //public GameStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string WinnerDisplayName { get; set; }
        public string LastWriter { get; set; }
    }

    //public enum GameStatus
    //{
    //    PreGame,
    //    InProgress,
    //    NotActive
    //}
}
