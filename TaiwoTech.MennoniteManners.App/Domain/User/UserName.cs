namespace TaiwoTech.MennoniteManners.App.Domain.User
{
    public record UserName(string Value) : DomainTypeString(Value)
    {
        public static UserName NoName = new ("< Insert Name >");

        public UserName Undecorated() => new(Value.Replace("<", "").Replace(">", "").Trim());
    }
}
