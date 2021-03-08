using Dan200.Core.Lua;
using Dan200.Core.Util;
using Dan200.Game.Game;
using Dan200.Game.GUI;
using Dan200.Game.Level;

namespace Dan200.Game.Script
{
    public class GameAPI : API
    {
        private InGameState m_state;

        public GameAPI(InGameState state)
        {
            m_state = state;
        }

        [LuaMethod]
        public LuaArgs isTest(LuaArgs args)
        {
            return new LuaArgs((m_state is TestState));
        }

        [LuaMethod]
        public LuaArgs getState(LuaArgs args)
        {
            return new LuaArgs(m_state.State.ToString().ToLowerUnderscored());
        }

        [LuaMethod]
        public LuaArgs isLevelComplete(LuaArgs args)
        {
            return new LuaArgs(m_state.IsComplete());
        }

        private TileCoordinates GetCoordinates(LuaArgs args, int index)
        {
            var table = args.GetTable(index);
            return new TileCoordinates(
                table.GetInt("x"),
                table.GetInt("y"),
                table.GetInt("z")
            );
        }

        [LuaMethod]
        public LuaArgs showHint(LuaArgs args)
        {
            var type = args.GetString(0);
            switch (type)
            {
                case "play":
                    {
                        m_state.ShowPlayHint = true;
                        break;
                    }
                case "rewind":
                    {
                        m_state.ShowRewindHint = true;
                        break;
                    }
                case "fastforward":
                    {
                        m_state.ShowFastForwardHint = true;
                        break;
                    }
                case "place":
                    {
                        var coords = GetCoordinates(args, 1);
                        m_state.ShowWorldHint(coords, WorldHintType.Place);
                        break;
                    }
                case "remove":
                    {
                        var coords = GetCoordinates(args, 1);
                        m_state.ShowWorldHint(coords, WorldHintType.Remove);
                        break;
                    }
                case "tweak":
                    {
                        var coords = GetCoordinates(args, 1);
                        m_state.ShowWorldHint(coords, WorldHintType.Tweak);
                        break;
                    }
                default:
                    {
                        throw new LuaError("Unrecognised hint type: " + type);
                    }
            }
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs hideHint(LuaArgs args)
        {
            if (args.IsTable(0))
            {
                var coords = GetCoordinates(args, 0);
                m_state.HideWorldHint(coords);
            }
            else
            {
                var type = args.GetString(0);
                switch (type)
                {
                    case "play":
                        {
                            m_state.ShowPlayHint = false;
                            break;
                        }
                    case "rewind":
                        {
                            m_state.ShowRewindHint = false;
                            break;
                        }
                    case "fastforward":
                        {
                            m_state.ShowFastForwardHint = false;
                            break;
                        }
                    case "place":
                    case "remove":
                    case "tweak":
                        {
                            var coords = GetCoordinates(args, 1);
                            m_state.HideWorldHint(coords);
                            break;
                        }
                    default:
                        {
                            throw new LuaError("Unrecognised hint type: " + type);
                        }
                }
            }
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs setPlayDisabled(LuaArgs args)
        {
            m_state.PlayDisabled = args.GetBool(0);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs setRewindDisabled(LuaArgs args)
        {
            m_state.RewindDisabled = args.GetBool(0);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs setFastForwardDisabled(LuaArgs args)
        {
            m_state.FastForwardDisabled = args.GetBool(0);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs setPlaceDisabled(LuaArgs args)
        {
            m_state.PlaceDisabled = args.GetBool(0);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs setRemoveDisabled(LuaArgs args)
        {
            m_state.RemoveDisabled = args.GetBool(0);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs setTweakDisabled(LuaArgs args)
        {
            m_state.TweakDisabled = args.GetBool(0);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs getPlacementsLeft(LuaArgs args)
        {
            return new LuaArgs(m_state.Level.Info.PlacementsLeft);
        }

        [LuaMethod]
        public LuaArgs getPlacementLocations(LuaArgs args)
        {
            var result = new LuaTable();
            foreach (Entity e in m_state.Level.Entities)
            {
                if (!e.Dead && e is SpawnMarker)
                {
                    var marker = (SpawnMarker)e;
                    result[result.Count + 1] = marker.Location.ToLuaValue();
                }
            }
            return new LuaArgs(result);
        }

        [LuaMethod]
        public LuaArgs getPlacementDelay(LuaArgs args)
        {
            var table = args.GetTable(0);
            var location = new TileCoordinates(
                table.GetInt("x"),
                table.GetInt("y"),
                table.GetInt("z")
            );
            foreach (Entity e in m_state.Level.Entities)
            {
                if (!e.Dead && e is SpawnMarker)
                {
                    var marker = (SpawnMarker)e;
                    if (marker.Location == location)
                    {
                        return new LuaArgs(marker.SpawnDelay);
                    }
                }
            }
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs isPlacementSelected(LuaArgs args)
        {
            var table = args.GetTable(0);
            var location = new TileCoordinates(
                table.GetInt("x"),
                table.GetInt("y"),
                table.GetInt("z")
            );
            var marker = ((InGameState)m_state).SelectedSpawnMarker;
            if (marker != null && marker.Location == location)
            {
                return new LuaArgs(true);
            }
            return new LuaArgs(false);
        }

        [LuaMethod]
        public LuaArgs showDialogue(LuaArgs args)
        {
            var character = args.GetString(0);
            var dialogue = args.GetString(1);
            var modal = args.IsNil(2) ? true : args.GetBool(2);
            m_state.ShowDialogue(character, dialogue, modal);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs hideDialogue(LuaArgs args)
        {
            m_state.HideDialogue();
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs isDialogueVisible(LuaArgs args)
        {
            return new LuaArgs(m_state.IsDialogueVisible());
        }

        [LuaMethod]
        public LuaArgs isDialogueReadyForInput(LuaArgs args)
        {
            return new LuaArgs(m_state.IsDialogueReadyForInput());
        }

        [LuaMethod]
        public LuaArgs isDialogueReadyToContinue(LuaArgs args)
        {
            return new LuaArgs(m_state.IsDialogueReadyToContinue());
        }
    }
}

