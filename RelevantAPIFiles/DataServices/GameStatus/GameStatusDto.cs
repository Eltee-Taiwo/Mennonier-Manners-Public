namespace TaiwoTech.Eltee.DataServices.MennoniteManners.GameStatus
{
    public class GameStatusDto
    {
        public char Code { get; set; }
        public string Description { get; set; }


        public static GameStatusDto Active = new GameStatusDto
        {
            Code = 'A',
            Description = "Active"
        };
        public static GameStatusDto PreGame = new GameStatusDto
        {
            Code = 'P',
            Description = "PreGame | Lobby"
        };
        public static GameStatusDto Closed = new GameStatusDto
        {
            Code = 'C',
            Description = "Closed"
        };
        public static GameStatusDto Validating = new GameStatusDto
        {
            Code = 'V',
            Description = "Validating"
        };
        public static GameStatusDto DisplayingStats = new GameStatusDto
        {
            Code = 'D',
            Description = "Display Stats"
        };
    }
}
