using Dan200.Game.Game;
using OpenTK;
using System;

namespace Dan200.Game.Level
{
    public interface ILevel
    {
        bool InEditor { get; }
        bool InMenu { get; }
        int RandomSeed { get; }
        Matrix4 Transform { get; }
        ITileMap Tiles { get; }
        IEntityCollection Entities { get; }
        ILightCollection Lights { get; }
        TelepadDirectory Telepads { get; }
        HintDirectory Hints { get; }
        TimeMachine TimeMachine { get; }
        Random Random { get; }
        GameAudio Audio { get; }
        ParticleManager Particles { get; }
    }
}
