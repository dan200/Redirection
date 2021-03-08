using System;

namespace Dan200.Game.Level
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TileBehaviourAttribute : Attribute
    {
        public readonly string Name;

        public TileBehaviourAttribute(string name)
        {
            Name = name;
        }
    }
}
