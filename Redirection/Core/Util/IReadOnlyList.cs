namespace Dan200.Core.Util
{
    public interface IReadOnlyList<T> : IReadOnlyCollection<T>
    {
        T this[int index]
        {
            get;
        }
    }
}

