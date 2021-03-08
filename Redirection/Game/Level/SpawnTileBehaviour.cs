using Dan200.Core.Animation;
using Dan200.Core.Assets;
using Dan200.Core.Audio;
using Dan200.Core.Render;
using Dan200.Game.Robot;
using OpenTK;
using System.IO;

namespace Dan200.Game.Level
{
    [TileBehaviour(name: "spawn")]
    public class SpawnTileBehaviour : TileBehaviour
    {
        private string m_colour;
        private bool m_immobile;
        private bool m_required;
        private Vector3 m_guiColour;
        private TurnDirection m_turnPreference;

        private string m_robotModel;
        private string m_robotAnimSet;
        private string m_robotSoundSet;
        private Vector3? m_robotLightColour;
        private float? m_robotLightRadius;

        public Model RobotModel
        {
            get
            {
                return Model.Get(m_robotModel);
            }
        }

        public AnimSet RobotAnimSet
        {
            get
            {
                return m_robotAnimSet != null ? AnimSet.Get(m_robotAnimSet) : null;
            }
        }

        public Vector3? RobotLightColour
        {
            get
            {
                return m_robotLightColour;
            }
        }

        public float? RobotLightRadius
        {
            get
            {
                return m_robotLightRadius;
            }
        }

        public bool Immobile
        {
            get
            {
                return m_immobile;
            }
        }

        public bool Required
        {
            get
            {
                return m_required;
            }
        }

        public SpawnTileBehaviour(Tile tile, KeyValuePairs kvp) : base(tile, kvp)
        {
            m_colour = kvp.GetString("colour", "grey");
            m_immobile = kvp.GetBool("immobile", false);
            m_required = kvp.GetBool("required", m_colour != "grey" && !m_immobile);
            m_turnPreference = kvp.GetEnum("turn_preference", TurnDirection.Left);
            if (kvp.ContainsKey("robot_model"))
            {
                m_robotModel = kvp.GetString("robot_model");
            }
            else
            {
                throw new IOException("robot_model not specified");
            }
            m_robotLightColour = kvp.GetColour("light_colour", Vector3.Zero);
            if (m_robotLightColour.Value.Length <= 0.0f)
            {
                m_robotLightColour = null;
            }
            m_robotLightRadius = kvp.GetFloat("light_radius", 15.0f);
            if (m_robotLightRadius.Value <= 0.0f)
            {
                m_robotLightRadius = null;
            }
            m_robotAnimSet = kvp.GetString("robot_animset", "animation/entities/new_robot/new_robot.animset");
            m_robotSoundSet = kvp.GetString("robot_soundset", "sound/new_robot/new_robot.soundset");
            m_guiColour = kvp.GetColour("gui_colour", Vector3.One);
        }

        public Robot.Robot CreateRobot(ILevel level, TileCoordinates coordinates, FlatDirection direction, RobotAction action)
        {
            return new Robot.Robot(
                coordinates,
                direction,
                m_colour,
                m_immobile,
                m_required,
                action,
                m_guiColour,
                m_turnPreference,
                RobotModel,
                (m_robotAnimSet != null ? AnimSet.Get(m_robotAnimSet) : null),
                (m_robotSoundSet != null ? SoundSet.Get(m_robotSoundSet) : null),
                m_robotLightColour,
                m_robotLightRadius,
                Tile.RenderPass,
                Tile.CastShadows
            );
        }

        public override Entity CreateEntity(ILevel level, TileCoordinates coordinates)
        {
            if (level.InEditor)
            {
                return new EditorRobot(Tile, coordinates);
            }
            return null;
        }

        public override void OnLevelStart(ILevel level, TileCoordinates coordinates)
        {
            if (!level.InEditor)
            {
                var direction = level.Tiles[coordinates].GetDirection(level, coordinates);
                var initialAction = (m_immobile || level.InMenu) ? (RobotAction)RobotActions.LongWait : (RobotAction)RobotActions.PreSpawn;
                level.Entities.Add(CreateRobot(level, coordinates, direction, initialAction));
            }
        }
    }
}
