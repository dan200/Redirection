using Dan200.Core.Render;
using System.Collections.Generic;

namespace Dan200.Game.Level
{
    public interface ILightCollection
    {
        AmbientLight AmbientLight { get; }
        DirectionalLight SkyLight { get; }
        DirectionalLight SkyLight2 { get; }
        ICollection<PointLight> PointLights { get; }
    }
}

