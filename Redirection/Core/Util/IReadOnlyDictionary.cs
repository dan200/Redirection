namespace Dan200.Core.Util
{
    public interface IReadOnlyDictionary<K, V>
    {
        int Count { get; }
        V this[K key] { get; }
        IReadOnlyCollection<K> Keys { get; }
        IReadOnlyCollection<V> Values { get; }
        bool ContainsKey(K key);
    }
}

