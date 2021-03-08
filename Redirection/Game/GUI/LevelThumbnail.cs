using Dan200.Core.Assets;
using Dan200.Core.Audio;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Game.Level;
using OpenTK;
using System;

namespace Dan200.Game.GUI
{
    public class LevelThumbnail : Box
    {
        private static readonly Vector4 BORDER_COLOUR = new Vector4(0.3f, 0.3f, 0.3f, 1.0f);
        private static readonly Vector4 HIGHLIGHT_BORDER_COLOUR = new Vector4(0.9f, 0.9f, 0.9f, 1.0f);

        private static readonly Vector4 BORDER_COLOUR_COMPLETE = new Vector4(0.36f, 0.32f, 0.2f, 1.0f);
        private static readonly Vector4 HIGHLIGHT_BORDER_COLOUR_COMPLETE = new Vector4(0.9f, 0.8f, 0.5f, 1.0f);

        private const float ICON_SIZE = 24.0f;
        private const float BORDER_SIZE = 4.0f;
        private const float DELETE_BUTTON_SIZE = 32.0f;
        private const float ICON_PADDING_SIZE = 4.0f;
        private const float MOVE_ARROW_SIZE = 24.0f;

        private const float ANIM_DURATION = 1.8f;

        private string m_levelPath;
        private string m_levelTitle;
        private bool m_highlight;
        private bool m_locked;
        private bool m_completed;
        private bool m_canDelete;
        private bool m_canMoveLeft;
        private bool m_canMoveRight;

        private Texture m_texture;
        private Geometry m_geometry;

        private Geometry m_deleteGeometry;
        private bool m_deleteHover;

        private Geometry m_moveGeometry;
        private bool m_moveLeftHover;
        private bool m_moveRightHover;

        private Geometry m_iconGeometry;

        private bool m_justUnlocked;
        private bool m_justCompleted;
        private float m_animTime;

        public string LevelPath
        {
            get
            {
                return m_levelPath;
            }
        }

        public string LevelTitle
        {
            get
            {
                return m_levelTitle;
            }
        }

        public bool MouseOverDelete
        {
            get
            {
                return m_highlight && m_deleteHover;
            }
        }

        public bool MouseOverMoveLeft
        {
            get
            {
                return m_highlight && m_moveLeftHover;
            }
        }

        public bool MouseOverMoveRight
        {
            get
            {
                return m_highlight && m_moveRightHover;
            }
        }

        public bool Highlight
        {
            get
            {
                return m_highlight;
            }
            set
            {
                if (m_highlight != value)
                {
                    m_highlight = value;
                    Colour = m_completed ?
                        (m_highlight ? HIGHLIGHT_BORDER_COLOUR_COMPLETE : BORDER_COLOUR_COMPLETE) :
                        (m_highlight ? HIGHLIGHT_BORDER_COLOUR : BORDER_COLOUR);
                }
            }
        }

        public bool Locked
        {
            get
            {
                return m_locked;
            }
            set
            {
                m_locked = value;
            }
        }

        public bool JustUnlocked
        {
            get
            {
                return m_justUnlocked;
            }
            set
            {
                m_justUnlocked = value;
            }
        }

        public bool Completed
        {
            get
            {
                return m_completed;
            }
            set
            {
                if (m_completed != value)
                {
                    m_completed = value;
                    Colour = m_completed ?
                        (m_highlight ? HIGHLIGHT_BORDER_COLOUR_COMPLETE : BORDER_COLOUR_COMPLETE) :
                        (m_highlight ? HIGHLIGHT_BORDER_COLOUR : BORDER_COLOUR);
                }
            }
        }

        public bool JustCompleted
        {
            get
            {
                return m_justCompleted;
            }
            set
            {
                m_justCompleted = value;
            }
        }

        public bool CanDelete
        {
            get
            {
                return m_canDelete;
            }
            set
            {
                m_canDelete = value;
            }
        }

        public bool CanMoveLeft
        {
            get
            {
                return m_canMoveLeft;
            }
            set
            {
                m_canMoveLeft = value;
            }
        }

        public bool CanMoveRight
        {
            get
            {
                return m_canMoveRight;
            }
            set
            {
                m_canMoveRight = value;
            }
        }

        public event EventHandler<EventArgs> OnClicked;
        public event EventHandler<EventArgs> OnDeleteClicked;
        public event EventHandler<EventArgs> OnMoveLeftClicked;
        public event EventHandler<EventArgs> OnMoveRightClicked;

        public LevelThumbnail(string levelPath, float width, float height, Language language) : base(Core.Render.Texture.White, width, height)
        {
            m_levelPath = levelPath;
            if (m_levelPath == "NEW")
            {
                m_levelTitle = language.Translate("menus.mod_select.create_new");
            }
            else if (Assets.Exists<LevelData>(levelPath))
            {
                m_levelTitle = Assets.Get<LevelData>(levelPath).Title;
            }
            else
            {
                m_levelTitle = "Untitled";
            }

            if (m_levelPath == "NEW")
            {
                m_texture = Core.Render.Texture.Get("gui/newlevel.png", false);
            }
            else
            {
                var thumbnailPath = AssetPath.ChangeExtension(levelPath, "png");
                if (Assets.Exists<Texture>(thumbnailPath))
                {
                    m_texture = Core.Render.Texture.Get(thumbnailPath, true);
                }
                else
                {
                    m_texture = Core.Render.Texture.Get("levels/template.png", true);
                }
            }
            m_geometry = new Geometry(Primitive.Triangles, 4, 6);

            m_deleteGeometry = new Geometry(Primitive.Triangles, 4, 6);
            m_moveGeometry = new Geometry(Primitive.Triangles, 8, 12);
            m_iconGeometry = new Geometry(Primitive.Triangles, 4, 6);

            m_highlight = false;
            m_locked = false;
            m_completed = false;
            m_canDelete = false;

            m_justUnlocked = false;
            m_justCompleted = false;
            m_animTime = 0.0f;

            Colour = BORDER_COLOUR;
        }

        protected override void OnInit()
        {
            base.OnInit();
            UpdateHover(true);
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            UpdateHover(false);

            if (m_highlight && Screen.Mouse.Buttons[MouseButton.Left].Pressed)
            {
                if (m_deleteHover)
                {
                    if (OnDeleteClicked != null)
                    {
                        OnDeleteClicked.Invoke(this, EventArgs.Empty);
                    }
                }
                else if (m_moveLeftHover)
                {
                    if (OnMoveLeftClicked != null)
                    {
                        OnMoveLeftClicked.Invoke(this, EventArgs.Empty);
                    }
                }
                else if (m_moveRightHover)
                {
                    if (OnMoveRightClicked != null)
                    {
                        OnMoveRightClicked.Invoke(this, EventArgs.Empty);
                    }
                }
                else
                {
                    if (OnClicked != null)
                    {
                        OnClicked.Invoke(this, EventArgs.Empty);
                    }
                }
                PlaySelectSound();
            }

            if (m_animTime < ANIM_DURATION)
            {
                var oldAnimTime = m_animTime;
                m_animTime = Math.Min(m_animTime + dt, ANIM_DURATION);
                if (m_justCompleted && oldAnimTime <= 0.0f && m_animTime > 0.0f)
                {
                    Screen.Audio.PlaySound("sound/trophy.wav");
                }
                if (m_justUnlocked && oldAnimTime <= 0.5f && m_animTime > 0.5f)
                {
                    Screen.Audio.PlaySound("sound/unlock.wav");
                }
                if (m_justUnlocked || m_justCompleted)
                {
                    RequestRebuild();
                }
            }
        }

        private void UpdateHover(bool silent)
        {
            var mousePos = Screen.MousePosition;
            bool deleteHover =
                m_canDelete &&
                Screen.ModalDialog == Parent &&
                mousePos.X >= Position.X + Width - BORDER_SIZE - DELETE_BUTTON_SIZE &&
                mousePos.X < Position.X + Width - BORDER_SIZE &&
                mousePos.Y >= Position.Y + Height - BORDER_SIZE - 3.0f - DELETE_BUTTON_SIZE &&
                mousePos.Y < Position.Y + Height - BORDER_SIZE - 3.0f;
            if (deleteHover != m_deleteHover)
            {
                m_deleteHover = deleteHover;
                if (deleteHover && !silent) PlayHighlightSound();
                RequestRebuild();
            }

            bool moveLeftHover =
                m_canMoveLeft &&
                Screen.ModalDialog == Parent &&
                mousePos.X >= Position.X + BORDER_SIZE &&
                mousePos.X < Position.X + BORDER_SIZE + MOVE_ARROW_SIZE &&
                mousePos.Y >= Position.Y + 0.5f * Height - 0.5f * MOVE_ARROW_SIZE &&
                mousePos.Y < Position.Y + 0.5f * Height + 0.5f * MOVE_ARROW_SIZE;
            if (moveLeftHover != m_moveLeftHover)
            {
                m_moveLeftHover = moveLeftHover;
                if (moveLeftHover && !silent) PlayHighlightSound();
            }

            bool moveRightHover =
                m_canMoveRight &&
                Screen.ModalDialog == Parent &&
                mousePos.X >= Position.X + Width - BORDER_SIZE - MOVE_ARROW_SIZE &&
                mousePos.X < Position.X + Width - BORDER_SIZE &&
                mousePos.Y >= Position.Y + 0.5f * Height - 0.5f * MOVE_ARROW_SIZE &&
                mousePos.Y < Position.Y + 0.5f * Height + 0.5f * MOVE_ARROW_SIZE;
            if (moveRightHover != m_moveRightHover)
            {
                m_moveRightHover = moveRightHover;
                if (moveRightHover && !silent) PlayHighlightSound();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            m_geometry.Dispose();
            m_deleteGeometry.Dispose();
            m_moveGeometry.Dispose();
            m_iconGeometry.Dispose();
        }

        private float GetAnimFraction()
        {
            var f = m_animTime / ANIM_DURATION;
            if (m_justCompleted)
            {
                if (f < 0.5f)
                {
                    f = f / 0.5f;
                    return MathUtils.Bounce(f);
                }
                else
                {
                    return 1.0f;
                }
            }
            else if (m_justUnlocked)
            {
                if (f > 0.5f)
                {
                    f = (f - 0.5f) / 0.5f;
                    return (1.0f - MathUtils.Ease(f));
                }
                else
                {
                    return 1.0f;
                }
            }
            else
            {
                return 0.0f;
            }
        }

        protected override void OnDraw()
        {
            base.OnDraw();

            float unlockedFraction;
            if (m_justUnlocked)
            {
                unlockedFraction = 1.0f - GetAnimFraction();
            }
            else
            {
                unlockedFraction = m_locked ? 0.0f : 1.0f;
            }
            Screen.Effect.Colour = Vector4.Lerp(new Vector4(0.3f, 0.3f, 0.3f, 1.0f), Vector4.One, unlockedFraction);
            Screen.Effect.Texture = m_texture;
            Screen.Effect.Bind();
            m_geometry.Draw();

            if (m_completed || m_locked || (!m_locked && m_justUnlocked))
            {
                Screen.Effect.Colour = Vector4.One;
                Screen.Effect.Texture = m_completed ?
                    Core.Render.Texture.Get("gui/completed.png", true) :
                    Core.Render.Texture.Get("gui/locked.png", true);
                Screen.Effect.Bind();
                m_iconGeometry.Draw();
            }

            if (m_highlight && m_canDelete)
            {
                Screen.Effect.Colour = Vector4.One;
                Screen.Effect.Texture = Core.Render.Texture.Get("gui/trash.png", true);
                Screen.Effect.Bind();
                m_deleteGeometry.Draw();
            }

            if (m_highlight && (m_canMoveLeft || m_canMoveRight))
            {
                Screen.Effect.Texture = Core.Render.Texture.Get("gui/arrows.png", true);
                Screen.Effect.Colour = m_moveLeftHover ? UIColours.Hover : (m_canMoveLeft ? UIColours.White : UIColours.Disabled);
                Screen.Effect.Bind();
                m_moveGeometry.DrawRange(0, 6);

                Screen.Effect.Texture = Core.Render.Texture.Get("gui/arrows.png", true);
                Screen.Effect.Colour = m_moveRightHover ? UIColours.Hover : (m_canMoveRight ? UIColours.White : UIColours.Disabled);
                Screen.Effect.Bind();
                m_moveGeometry.DrawRange(6, 6);
            }
        }

        protected override void OnRebuild()
        {
            base.OnRebuild();

            // Add the thumbnail
            m_geometry.Clear();

            float aspect = (Width - 2.0f * BORDER_SIZE) / (Height - 2.0f * BORDER_SIZE);
            float textureAspect = (float)m_texture.Width / (float)m_texture.Height;
            if (textureAspect > aspect)
            {
                float cutoff = ((textureAspect - aspect) * 0.5f) / textureAspect;
                m_geometry.Add2DQuad(
                    Position + new Vector2(BORDER_SIZE, BORDER_SIZE),
                    Position + new Vector2(Width - BORDER_SIZE, Height - BORDER_SIZE),
                    new Quad(cutoff, 0.0f, 1.0f - 2.0f * cutoff, 1.0f)
                );
            }
            else if (textureAspect < aspect)
            {
                float invAspect = 1.0f / aspect;
                float invTextureAspect = 1.0f / textureAspect;
                float cutoff = ((invTextureAspect - invAspect) * 0.5f) / invTextureAspect;
                m_geometry.Add2DQuad(
                    Position + new Vector2(BORDER_SIZE, BORDER_SIZE),
                    Position + new Vector2(Width - BORDER_SIZE, Height - BORDER_SIZE),
                    new Quad(0.0f, cutoff, 1.0f, 1.0f - 2.0f * cutoff)
                );
            }
            else
            {
                m_geometry.Add2DQuad(
                    Position + new Vector2(BORDER_SIZE, BORDER_SIZE),
                    Position + new Vector2(Width - BORDER_SIZE, Height - BORDER_SIZE),
                    Quad.UnitSquare
                );
            }
            m_geometry.Rebuild();

            // Add the icon
            m_iconGeometry.Clear();
            float iconSize;
            if (m_justCompleted || m_justUnlocked)
            {
                iconSize = GetAnimFraction() * ICON_SIZE;
            }
            else
            {
                iconSize = ICON_SIZE;
            }
            if (iconSize > 0.0f)
            {
                var iconCenter = Position + new Vector2(
                    BORDER_SIZE + ICON_PADDING_SIZE + 0.5f * ICON_SIZE,
                    Height - BORDER_SIZE - ICON_PADDING_SIZE - 0.5f * ICON_SIZE
                );
                m_iconGeometry.Add2DQuad(
                    iconCenter - 0.5f * new Vector2(iconSize, iconSize),
                    iconCenter + 0.5f * new Vector2(iconSize, iconSize),
                    Quad.UnitSquare
                );
            }
            m_iconGeometry.Rebuild();

            // Add the delete button
            m_deleteGeometry.Clear();
            m_deleteGeometry.Add2DQuad(
                Position + new Vector2(Width - BORDER_SIZE - DELETE_BUTTON_SIZE, Height - 2.0f * BORDER_SIZE - DELETE_BUTTON_SIZE),
                Position + new Vector2(Width - BORDER_SIZE, Height - 2.0f * BORDER_SIZE),
                m_deleteHover ? new Quad(0.5f, 0.0f, 0.5f, 1.0f) : new Quad(0.0f, 0.0f, 0.5f, 1.0f)
            );
            m_deleteGeometry.Rebuild();

            // Add move buttons
            m_moveGeometry.Clear();
            m_moveGeometry.Add2DQuad(
                Position + new Vector2(BORDER_SIZE, 0.5f * Height - 0.5f * MOVE_ARROW_SIZE),
                Position + new Vector2(BORDER_SIZE + MOVE_ARROW_SIZE, 0.5f * Height + 0.5f * MOVE_ARROW_SIZE),
                new Quad(0.0f, 0.5f, 0.5f, 0.5f)
            );
            m_moveGeometry.Add2DQuad(
                Position + new Vector2(Width - BORDER_SIZE - MOVE_ARROW_SIZE, 0.5f * Height - 0.5f * MOVE_ARROW_SIZE),
                Position + new Vector2(Width - BORDER_SIZE, 0.5f * Height + 0.5f * MOVE_ARROW_SIZE),
                new Quad(0.0f, 0.0f, 0.5f, 0.5f)
            );
            m_moveGeometry.Rebuild();
        }

        private void PlaySelectSound()
        {
            Screen.Audio.PlaySound("sound/menu_select.wav");
        }

        private void PlayHighlightSound()
        {
            Screen.Audio.PlaySound("sound/menu_highlight.wav");
        }
    }
}

