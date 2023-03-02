namespace TaiwoTech.MennoniteManners.App.Domain.Game
{
    public record PenSeconds(int Value) : DomainTypeInt(Value)
    {
        public static PenSeconds operator +(PenSeconds self, int additionalSeconds) => new PenSeconds(self.Value + additionalSeconds);
    }
}
