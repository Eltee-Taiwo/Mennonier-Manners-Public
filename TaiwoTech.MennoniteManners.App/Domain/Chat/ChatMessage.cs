namespace TaiwoTech.MennoniteManners.App.Domain.Chat
{
    public record ChatMessage(string Value) : DomainTypeString(Value);
}
