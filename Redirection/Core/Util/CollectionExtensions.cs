using System.Collections;
using System.Collections.Generic;

namespace Dan200.Core.Util
{
    public static class CollectionExtensions
    {
        private class ReadOnlyCollection<T> : IReadOnlyCollection<T>
        {
            private ICollection<T> m_collection;

            public int Count
            {
                get
                {
                    return m_collection.Count;
                }
            }

            public ReadOnlyCollection(ICollection<T> collection)
            {
                m_collection = collection;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return m_collection.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)m_collection).GetEnumerator();
            }
        }

        public static IReadOnlyCollection<T> ToReadOnly<T>(this ICollection<T> collection)
        {
            return new ReadOnlyCollection<T>(collection);
        }

        private class ReadOnlyList<T> : IReadOnlyList<T>
        {
            private IList<T> m_collection;

            public int Count
            {
                get
                {
                    return m_collection.Count;
                }
            }

            public T this[int index]
            {
                get
                {
                    return m_collection[index];
                }
            }

            public ReadOnlyList(IList<T> collection)
            {
                m_collection = collection;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return m_collection.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)m_collection).GetEnumerator();
            }
        }

        public static IReadOnlyList<T> ToReadOnly<T>(this IList<T> collection)
        {
            return new ReadOnlyList<T>(collection);
        }

        private class ReadOnlyQueue<T> : IReadOnlyCollection<T>
        {
            private Queue<T> m_queue;

            public int Count
            {
                get
                {
                    return m_queue.Count;
                }
            }

            public ReadOnlyQueue(Queue<T> queue)
            {
                m_queue = queue;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return m_queue.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)m_queue).GetEnumerator();
            }
        }

        public static IReadOnlyCollection<T> ToReadOnly<T>(this Queue<T> queue)
        {
            return new ReadOnlyQueue<T>(queue);
        }

        private class ReadOnlyDictionary<K, V> : IReadOnlyDictionary<K, V>
        {
            private IDictionary<K, V> m_collection;
            private ReadOnlyCollection<K> m_keys;
            private ReadOnlyCollection<V> m_values;

            public int Count
            {
                get
                {
                    return m_collection.Count;
                }
            }

            public V this[K key]
            {
                get
                {
                    return m_collection[key];
                }
            }

            public IReadOnlyCollection<K> Keys
            {
                get
                {
                    return m_keys;
                }
            }

            public IReadOnlyCollection<V> Values
            {
                get
                {
                    return m_values;
                }
            }

            public ReadOnlyDictionary(IDictionary<K, V> collection)
            {
                m_collection = collection;
                m_keys = new ReadOnlyCollection<K>(collection.Keys);
                m_values = new ReadOnlyCollection<V>(collection.Values);
            }

            public bool ContainsKey(K key)
            {
                return m_collection.ContainsKey(key);
            }
        }

        public static IReadOnlyDictionary<K, V> ToReadOnly<K, V>(this IDictionary<K, V> collection)
        {
            return new ReadOnlyDictionary<K, V>(collection);
        }
    }
}

