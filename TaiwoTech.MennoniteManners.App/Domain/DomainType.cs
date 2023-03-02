namespace TaiwoTech.MennoniteManners.App.Domain
{
    public abstract record DomainType<T>(T Value) where T : notnull
    {
        public virtual bool Equals(DomainType<T> other)
        {
            return other != null && Value.Equals(other.Value);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(Value);
        }

        public sealed override string ToString()
        {
            return Value?.ToString();
        }
    }
}
