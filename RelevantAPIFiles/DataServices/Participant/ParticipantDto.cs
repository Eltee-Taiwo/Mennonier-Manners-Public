using System;

namespace TaiwoTech.Eltee.DataServices.MennoniteManners.Participant
{
    public class ParticipantDto
    {
        public int Id { get; set; }
        public string GameId { get; set; }
        public string UserName { get; set; }
        public string UniqueUserName { get; set; }
        public string ConnectionId { get; set; }
        public bool IsHost { get; set; }
        public bool HasFinished { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
    }
}
