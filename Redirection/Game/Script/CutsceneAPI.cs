using Dan200.Core.Animation;
using Dan200.Core.Assets;
using Dan200.Core.GUI;
using Dan200.Core.Lua;
using Dan200.Core.Render;
using Dan200.Game.Game;
using Dan200.Game.Level;
using OpenTK;
using System.Collections.Generic;

namespace Dan200.Game.Script
{
    public class CutsceneAPI : API
    {
        private CutsceneState m_state;

        public CutsceneAPI(CutsceneState state)
        {
            m_state = state;
        }

        [LuaType("cutscene_entity")]
        private class LuaCutsceneEntity : LuaObject
        {
            private CutsceneEntity m_entity;

            public LuaCutsceneEntity(CutsceneEntity entity)
            {
                m_entity = entity;
            }

            public override void Dispose()
            {
            }

            [LuaMethod]
            public LuaArgs setVisible(LuaArgs args)
            {
                var visible = args.GetBool(0);
                m_entity.Visible = visible;
                return LuaArgs.Empty;
            }

            [LuaMethod]
            public LuaArgs setCastShadows(LuaArgs args)
            {
                var castShadows = args.GetBool(0);
                m_entity.CastShadows = castShadows;
                return LuaArgs.Empty;
            }

            [LuaMethod]
            public LuaArgs playAnimation(LuaArgs args)
            {
                var path = args.GetString(0);
                var animateRoot = args.IsNil(1) ? true : args.GetBool(1);
                if (LuaAnimation.Exists(path))
                {
                    var anim = LuaAnimation.Get(path);
                    m_entity.StartAnimation(anim, animateRoot);
                    return LuaArgs.Empty;
                }
                else
                {
                    throw new LuaError(string.Format("No such animation: {0}", path));
                }
            }

            [LuaMethod]
            public LuaArgs playSound(LuaArgs args)
            {
                var path = args.GetString(0);
                var loop = args.IsNil(1) ? false : args.GetBool(1);
                m_entity.PlaySound(path, loop);
                return LuaArgs.Empty;
            }

            [LuaMethod]
            public LuaArgs stopSound(LuaArgs args)
            {
                var path = args.GetString(0);
                m_entity.StopSound(path);
                return LuaArgs.Empty;
            }

            [LuaMethod]
            public LuaArgs startParticles(LuaArgs args)
            {
                var path = args.GetString(0);
                var startActive = args.IsNil(1) ? false : args.GetBool(1);
                m_entity.StartParticles(path, startActive);
                return LuaArgs.Empty;
            }

            [LuaMethod]
            public LuaArgs stopParticles(LuaArgs args)
            {
                var path = args.GetString(0);
                m_entity.StopParticles(path);
                return LuaArgs.Empty;
            }
        }

        [LuaMethod]
        public LuaArgs createEntity(LuaArgs args)
        {
            var path = args.GetString(0);
            var renderPassStr = args.IsNil(1) ? "opaque" : args.GetString(1);
            if (Assets.Exists<Model>(path))
            {
                RenderPass renderPass;
                switch (renderPassStr)
                {
                    case "opaque":
                        {
                            renderPass = RenderPass.Opaque;
                            break;
                        }
                    case "cutout":
                        {
                            renderPass = RenderPass.Cutout;
                            break;
                        }
                    case "translucent":
                        {
                            renderPass = RenderPass.Translucent;
                            break;
                        }
                    default:
                        {
                            throw new LuaError("Unsupported render pass: " + renderPassStr);
                        }
                }

                var model = Model.Get(path);
                var entity = new CutsceneEntity(model, renderPass);
                m_state.Level.Entities.Add(entity);
                return new LuaArgs(new LuaCutsceneEntity(entity));
            }
            else
            {
                throw new LuaError(string.Format("No such model: {0}", path));
            }
        }

        [LuaMethod]
        public LuaArgs playCameraAnimation(LuaArgs args)
        {
            var path = args.GetString(0);
            if (LuaAnimation.Exists(path))
            {
                var anim = LuaAnimation.Get(path);
                m_state.StartCameraAnimation(anim);
                return LuaArgs.Empty;
            }
            else
            {
                throw new LuaError(string.Format("No such animation: {0}", path));
            }
        }

        [LuaMethod]
        public LuaArgs setSkyOrigin(LuaArgs args)
        {
            var x = args.GetFloat(0);
            var y = args.GetFloat(1);
            var z = args.GetFloat(2);
            var sky = m_state.Game.Sky;
            if (sky != null)
            {
                sky.ForegroundModelTransform = Matrix4.CreateTranslation(x, y, z);
            }
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs playSound(LuaArgs args)
        {
            var path = args.GetString(0);
            var loop = args.IsNil(1) ? false : args.GetBool(1);
            m_state.PlaySound(path, loop);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs stopSound(LuaArgs args)
        {
            var path = args.GetString(0);
            m_state.StopSound(path);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs playMusic(LuaArgs args)
        {
            var path = args.IsNil(0) ? null : args.GetString(0);
            var loop = args.IsNil(1) ? true : args.GetBool(1);
            var transition = args.IsNil(2) ? 1.0f : args.GetFloat(2);
            if (transition < 0.0f)
            {
                throw new LuaError("Transition duration must be positive");
            }
            m_state.PlayMusic(path, transition, loop);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs stopMusic(LuaArgs args)
        {
            var transition = args.IsNil(0) ? 1.0f : args.GetFloat(1);
            if (transition < 0.0f)
            {
                throw new LuaError("Transition duration must be positive");
            }
            m_state.PlayMusic(null, transition, false);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs rumble(LuaArgs args)
        {
            var strength = args.GetFloat(0);
            var duration = args.GetFloat(1);
            if (duration < 0.0f || duration > 5.0f)
            {
                throw new LuaError("Duration must be within 0 and 5 seconds");
            }
            m_state.Rumble(strength, duration);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs startScrollingText(LuaArgs args)
        {
            var path = args.GetString(0);
            if (Assets.Exists<TextAsset>(path))
            {
                var text = TextAsset.Get(path);
                m_state.StartScrollingText(text);
                return LuaArgs.Empty;
            }
            else
            {
                throw new LuaError(string.Format("No such text file: {0}", path));
            }
        }

        [LuaMethod]
        public LuaArgs stopScrollingText(LuaArgs args)
        {
            m_state.StopScrollingText();
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs isScrollingTextVisible(LuaArgs args)
        {
            if (m_state.IsScrollingTextVisible)
            {
                return new LuaArgs(true, m_state.ScrollingTextTimeLeft);
            }
            else
            {
                return new LuaArgs(false);
            }
        }

        [LuaMethod]
        public LuaArgs setOverlayLine(LuaArgs args)
        {
            var line = args.GetInt(0) - 1;
            var text = args.GetString(1);
            m_state.SetTerminalLine(line, text);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs setOverlayAlignment(LuaArgs args)
        {
            var line = args.GetInt(0) - 1;
            var alignment = args.GetString(1);
            switch (alignment)
            {
                case "left":
                    {
                        m_state.SetTerminalAlignment(line, TextAlignment.Left);
                        break;
                    }
                case "right":
                    {
                        m_state.SetTerminalAlignment(line, TextAlignment.Right);
                        break;
                    }
                case "center":
                    {
                        m_state.SetTerminalAlignment(line, TextAlignment.Center);
                        break;
                    }
                default:
                    {
                        throw new LuaError("Invalid alignment: " + alignment);
                    }
            }
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs getOverlayLine(LuaArgs args)
        {
            var line = args.GetInt(0) - 1;
            return new LuaArgs(m_state.GetTerminalLine(line));
        }

        [LuaMethod]
        public LuaArgs wrapTextForOverlay(LuaArgs args)
        {
            var line = args.GetString(0);
            int pos = m_state.WrapTerminalLine(line, 0);
            if (pos < line.Length)
            {
                var results = new List<LuaValue>();
                results.Add(line.Substring(0, pos));
                pos += Font.AdvanceWhitespace(line, pos);
                while (pos < line.Length)
                {
                    var partLen = m_state.WrapTerminalLine(line, pos);
                    results.Add(line.Substring(pos, partLen));
                    pos += partLen;
                    pos += Font.AdvanceWhitespace(line, pos);
                }
                return new LuaArgs(results.ToArray());
            }
            else
            {
                return new LuaArgs(line);
            }
        }

        [LuaMethod]
        public LuaArgs getOverlayHeight(LuaArgs args)
        {
            return new LuaArgs(m_state.GetTerminalHeight());
        }

        [LuaMethod]
        public LuaArgs clearOverlay(LuaArgs args)
        {
            m_state.ClearTerminal();
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs scrollOverlay(LuaArgs args)
        {
            var i = args.GetInt(0);
            m_state.ScrollTerminal(i);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs cutTo(LuaArgs args)
        {
            var path = args.GetString(0);
            if (Assets.Exists<LevelData>(path))
            {
                m_state.CutTo(path);
                return LuaArgs.Empty;
            }
            else
            {
                throw new LuaError(string.Format("No such level: {0}", path));
            }
        }

        [LuaMethod]
        public LuaArgs wipeTo(LuaArgs args)
        {
            var path = args.GetString(0);
            if (Assets.Exists<LevelData>(path))
            {
                m_state.WipeTo(path);
                return LuaArgs.Empty;
            }
            else
            {
                throw new LuaError(string.Format("No such level: {0}", path));
            }
        }

        [LuaMethod]
        public LuaArgs complete(LuaArgs args)
        {
            m_state.Complete();
            return LuaArgs.Empty;
        }
    }
}

