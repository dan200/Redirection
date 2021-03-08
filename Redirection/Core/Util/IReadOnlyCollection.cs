using System.Collections.Generic;

namespace Dan200.Core.Util
{
    public interface IReadOnlyCollection<T> : IEnumerable<T>
    {
        int Count { get; }
    }
}

