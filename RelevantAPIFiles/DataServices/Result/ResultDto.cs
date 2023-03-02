using System;

namespace TaiwoTech.Eltee.DataServices.MennoniteManners.Result
{
    public class ResultDto
    {
        public int Id { get; set; }
        public int ParticipantId { get; set; }
        public string UniqueUserName { get; set; }
        public int SessionId { get; set; }
        public int ValidityScore { get; set; }
        public DateTime TimeStamp { get; set; }
        public int TotalPenSeconds { get; set; }
    }
}
