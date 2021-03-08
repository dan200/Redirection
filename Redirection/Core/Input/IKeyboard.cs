using Dan200.Core.Util;

namespace Dan200.Core.Input
{
    public interface IKeyboard
    {
        IReadOnlyDictionary<Key, IButton> Keys { get; }
        string Text { get; }
    }
}

