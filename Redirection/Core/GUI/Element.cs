using OpenTK;
using System;


namespace Dan200.Core.GUI
{
    public abstract class Element : IDisposable
    {
        private Screen m_screen;
        private Anchor m_anchor;
        private Vector2 m_position;
        private bool m_visible;
        private bool m_rebuildRequested;

        public Screen Screen
        {
            get
            {
                return m_screen;
            }
        }

        public Element Parent;

        public Anchor Anchor
        {
            get
            {
                return m_anchor;
            }
            set
            {
                if (m_anchor != value)
                {
                    m_anchor = value;
                    RequestRebuild();
                }
            }
        }

        public Vector2 LocalPosition
        {
            get
            {
                return m_position;
            }
            set
            {
                m_position = value;
                RequestRebuild();
            }
        }

        public Vector2 Position
        {
            get
            {
                return m_screen.GetAnchorPosition(m_anchor) + m_position;
            }
        }

        public bool Visible
        {
            get
            {
                return m_visible;
            }
            set
            {
                m_visible = value;
            }
        }

        public Element()
        {
            m_screen = null;
            m_anchor = Anchor.TopLeft;
            m_position = Vector2.Zero;
            m_visible = true;
            m_rebuildRequested = false;
        }

        public virtual void Dispose()
        {
        }

        protected abstract void OnInit();
        protected abstract void OnUpdate(float dt);
        protected abstract void OnDraw();
        protected virtual void OnDraw3D() { }
        protected abstract void OnRebuild();

        public void Init(Screen screen)
        {
            m_screen = screen;
            OnInit();
            RequestRebuild();
        }

        public void Update(float dt)
        {
            OnUpdate(dt);
        }

        public void Draw()
        {
            RebuildIfRequested();
            if (m_visible)
            {
                OnDraw();
            }
        }

        public void Draw3D()
        {
            if (m_visible)
            {
                OnDraw3D();
            }
        }

        public void RequestRebuild()
        {
            m_rebuildRequested = true;
        }

        protected void RebuildIfRequested()
        {
            if (m_rebuildRequested)
            {
                OnRebuild();
                m_rebuildRequested = false;
            }
        }
    }
}

