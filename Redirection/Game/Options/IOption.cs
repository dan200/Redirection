using Dan200.Core.Assets;

namespace Dan200.Game.Options
{
    public interface IOption
    {
        string ToString(Language language);
        void Click();
    }
}
