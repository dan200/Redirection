using System;
using Dan200.Core.Main;


#if OPENGLES
using OpenTK.Graphics.ES20;
using TextureUnit=OpenTK.Graphics.ES20.All;
using TextureTarget=OpenTK.Graphics.ES20.All;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace Dan200.Core.Render
{
    public class RenderTexture : ITexture, IDisposable
    {
        public static RenderTexture Current
        {
            get;
            private set;
        }

        private int m_width;
        private int m_height;

        private int m_texture;
        private int m_depthStencilBuffer;
        private int m_frameBuffer;

        public int GLTexture
        {
            get
            {
                return m_texture;
            }
        }

        public int Width
        {
            get
            {
                return m_width;
            }
        }

        public int Height
        {
            get
            {
                return m_height;
            }
        }

        public RenderTexture(int width, int height, bool filter)
        {
            m_width = width;
            m_height = height;

            // Generate texture (for colour data)
            m_texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, m_texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)(filter ? TextureMagFilter.Linear : TextureMagFilter.Nearest));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)(filter ? TextureMinFilter.Linear : TextureMinFilter.Nearest));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, m_width, m_height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            // Generate depth buffer (for depth data)
            GL.Ext.GenRenderbuffers(1, out m_depthStencilBuffer);
            GL.Ext.BindRenderbuffer(RenderbufferTarget.RenderbufferExt, m_depthStencilBuffer);
            GL.Ext.RenderbufferStorage(RenderbufferTarget.RenderbufferExt, RenderbufferStorage.Depth24Stencil8, m_width, m_height);
            GL.Ext.BindRenderbuffer(RenderbufferTarget.RenderbufferExt, 0);

            // Generate Framebuffer (links the buffers together)
            GL.Ext.GenFramebuffers(1, out m_frameBuffer);
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, m_frameBuffer);
            GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment0Ext, TextureTarget.Texture2D, m_texture, 0);

            //GL.Ext.FramebufferRenderbuffer( FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.RenderbufferExt, m_depthStencilBuffer );
            GL.Ext.FramebufferRenderbuffer(FramebufferTarget.FramebufferExt, FramebufferAttachment.DepthAttachmentExt, RenderbufferTarget.RenderbufferExt, m_depthStencilBuffer);
            GL.Ext.FramebufferRenderbuffer(FramebufferTarget.FramebufferExt, FramebufferAttachment.StencilAttachmentExt, RenderbufferTarget.RenderbufferExt, m_depthStencilBuffer);

            try
            {
                // Check everything worked
                var status = GL.Ext.CheckFramebufferStatus(FramebufferTarget.FramebufferExt);
                if (status != FramebufferErrorCode.FramebufferCompleteExt)
                {
                    throw new OpenGLException("Error creating framebuffer: " + status);
                }
            }
            finally
            {
                GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0);
            }
        }

        public void Dispose()
        {
            GL.Ext.DeleteFramebuffers(1, ref m_frameBuffer);
            m_frameBuffer = -1;

            GL.Ext.DeleteRenderbuffers(1, ref m_depthStencilBuffer);
            m_depthStencilBuffer = -1;

            GL.DeleteTexture(m_texture);
            m_texture = -1;
        }

        public void Resize(int width, int height)
        {
            if (m_width != width || m_height != height)
            {
                m_width = width;
                m_height = height;

                // Rescale texture
                GL.BindTexture(TextureTarget.Texture2D, m_texture);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, m_width, m_height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
                GL.BindTexture(TextureTarget.Texture2D, 0);

                // Rescale depth buffer
                GL.Ext.BindRenderbuffer(RenderbufferTarget.RenderbufferExt, m_depthStencilBuffer);
                GL.Ext.RenderbufferStorage(RenderbufferTarget.RenderbufferExt, RenderbufferStorage.Depth24Stencil8, m_width, m_height);
                GL.Ext.BindRenderbuffer(RenderbufferTarget.RenderbufferExt, 0);
            }
        }

        public void Bind()
        {
            GL.Viewport(0, 0, m_width, m_height);
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, m_frameBuffer);
            Current = this;
        }

        public void Unbind()
        {
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0);
            Current = null;
        }
    }
}
