namespace TaiwoTech.MennoniteManners.App.Services.API
{
    public class GameTypeDto
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public IEnumerable<int> GameLengths { get; set; }
        public List<string> TargetStringValues { get; set; }
    }
}
