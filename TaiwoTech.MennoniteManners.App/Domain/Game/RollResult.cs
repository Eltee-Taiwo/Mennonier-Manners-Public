namespace TaiwoTech.MennoniteManners.App.Domain.Game
{
    public record RollResult(int Value) : DomainTypeInt(Value)
    {
        /// <summary>
        /// This just checks if you rolled a six and can roll now
        /// </summary>
        public bool IsSuccess => Value.Equals(6);
    }
}
