using TaiwoTech.Eltee.DataServices.MennoniteManners.Result;

namespace TaiwoTech.Eltee.Hubs.Mennonite
{
    public class ResultGet
    {
        public string UserName { get; }
        public int Score { get; }
        public string PenTime { get; }

        public ResultGet(ResultDto dto)
        {
            UserName = dto.UniqueUserName;
            Score = dto.ValidityScore;
            PenTime = $"{dto.TotalPenSeconds}s";
        }
    }
}
