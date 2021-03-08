using Dan200.Core.GUI;
using Dan200.Core.Render;
using Dan200.Game.Level;
using OpenTK;
using System;

namespace Dan200.Game.GUI
{
    public class InWorldInputPrompt : InputPrompt
    {
        private const float ARROW_SIZE = 20.0f;
        private const double BOUNCE_PERIOD = 2.0;

        private ILevel m_level;
        private Camera m_camera;
        private Image m_downArrow;

        private bool m_usePosition3D;
        private Vector3 m_position3D;
        private Vector2 m_position2D;
        private Anchor m_position2DAnchor;

        public bool UsePosition3D
        {
            get
            {
                return m_usePosition3D;
            }
            set
            {
                m_usePosition3D = value;
            }
        }

        public Vector3 Position3D
        {
            get
            {
                return m_position3D;
            }
            set
            {
                m_position3D = value;
            }
        }

        public Vector2 Position2D
        {
            get
            {
                return m_position2D;
            }
            set
            {
                m_position2D = value;
            }
        }

        public Anchor Position2DAnchor
        {
            get
            {
                return m_position2DAnchor;
            }
            set
            {
                m_position2DAnchor = value;
            }
        }

        public InWorldInputPrompt(ILevel level, Camera camera, Font font, string str, TextAlignment alignment) : base(font, str, alignment)
        {
            m_level = level;
            m_camera = camera;
            m_position3D = Vector3.Zero;
            m_downArrow = new Image(Texture.Get("gui/arrows.png", true), new Quad(0.5f, 0.5f, 0.5f, 0.5f), ARROW_SIZE, ARROW_SIZE);
            m_downArrow.Colour = UIColours.White;

            m_usePosition3D = true;
            m_position3D = Vector3.Zero;
            m_position2D = Vector2.Zero;
            m_position2DAnchor = Anchor.TopLeft;
        }

        public override void Dispose()
        {
            m_downArrow.Dispose();
            base.Dispose();
        }

        protected override void OnInit()
        {
            base.OnInit();

            m_downArrow.Anchor = Anchor;
            m_downArrow.LocalPosition = LocalPosition + new Vector2(-0.5f * m_downArrow.Width, Height);
            m_downArrow.Parent = this.Parent;
            m_downArrow.Init(Screen);
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);

            m_downArrow.Visible = Visible;
            m_downArrow.Update(dt);
        }

        protected override void OnDraw()
        {
            UpdatePosition();
            RebuildIfRequested();
            base.OnDraw();
            if (Screen.ModalDialog == this.Parent ||
                (Screen.ModalDialog is DialogueBox && !((DialogueBox)Screen.ModalDialog).BlockInput))
            {
                m_downArrow.Draw();
            }
        }

        protected override void OnRebuild()
        {
            base.OnRebuild();
            m_downArrow.Anchor = Anchor;
            m_downArrow.LocalPosition = LocalPosition + new Vector2(-0.5f * m_downArrow.Width, Height);
            m_downArrow.Visible = Visible;
            m_downArrow.RequestRebuild();
        }

        private void UpdatePosition()
        {
            // Convert level space to camera space
            Vector2 posSS;
            if (m_usePosition3D)
            {
                var posLS = m_position3D;
                var posWS = Vector3.Transform(posLS, m_level.Transform);
                var posCS = Vector3.Transform(posWS, m_camera.Transform);

                // Convert camera space to screen space
                float screenAspect = Screen.Width / Screen.Height;
                posSS = new Vector2(
                    ((float)Math.Atan2(posCS.X, -posCS.Z) / (m_camera.FOV * 0.5f * screenAspect)) * (0.5f * Screen.Width),
                    ((float)Math.Atan2(-posCS.Y, -posCS.Z) / (m_camera.FOV * 0.5f)) * (0.5f * Screen.Height)
                );
                Anchor = Anchor.CentreMiddle;
            }
            else
            {
                posSS = m_position2D;
                Anchor = m_position2DAnchor;
            }

            float bob = (float)Math.Abs(16.0 * Math.Sin((m_level.TimeMachine.RealTime / BOUNCE_PERIOD) * 2.0 * Math.PI));
            posSS += new Vector2(0.0f, -bob - m_downArrow.Height - Font.Height);
            LocalPosition = posSS;
        }
    }
}

