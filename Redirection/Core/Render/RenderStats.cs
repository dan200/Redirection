namespace Dan200.Core.Render
{
    public static class RenderStats
    {
        private static int m_trianglesLastFrame;
        private static int m_drawCallsLastFrame;

        private static int m_trianglesThisFrame;
        private static int m_drawCallsThisFrame;

        public static int Triangles
        {
            get
            {
                return m_trianglesLastFrame;
            }
        }

        public static int DrawCalls
        {
            get
            {
                return m_drawCallsLastFrame;
            }
        }

        public static void EndFrame()
        {
            m_trianglesLastFrame = m_trianglesThisFrame;
            m_trianglesThisFrame = 0;
            m_drawCallsLastFrame = m_drawCallsThisFrame;
            m_drawCallsThisFrame = 0;
        }

        public static void AddDrawCall(int triangles)
        {
            m_drawCallsThisFrame++;
            m_trianglesThisFrame += triangles;
        }
    }
}
