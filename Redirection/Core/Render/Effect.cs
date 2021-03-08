using Dan200.Core.Main;
using Dan200.Core.Assets;
using System.Text;
using System.IO;
using System.Collections.Generic;

using OpenTK.Graphics.OpenGL;

namespace Dan200.Core.Render
{
    public class Effect : IBasicAsset
    {
        public static Effect Get(string path)
        {
            return Assets.Assets.Get<Effect>(path);
        }

        private string m_path;
        private string m_vertexShaderPath;
        private string m_fragmentShaderPath;
        private BlendMode m_blendMode;

        private int m_vertexShader;
        private int m_fragmentShader;
        private int m_program;

        private Dictionary<string, int> m_knownUniformLocations;
        private Dictionary<string, int> m_knownAttributeLocations;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public int GLProgram
        {
            get
            {
                return m_program;
            }
        }

        public BlendMode BlendMode
        {
            get
            {
                return m_blendMode;
            }
        }

        public Effect(string path, IFileStore store)
        {
            m_path = path;
            m_knownUniformLocations = new Dictionary<string, int>();
            m_knownAttributeLocations = new Dictionary<string, int>();
            Load(store);
        }

        public void Reload(IFileStore store)
        {
            Unload();
            Load(store);
        }

        public void Dispose()
        {
            Unload();
        }

        private void Preprocess(StringBuilder output, IFileStore store, string directory, string path, int fileNumber)
        {
            int lineNumber = 0;
            output.AppendLine("#line " + lineNumber + " " + fileNumber);

            string fullPath = AssetPath.Combine(directory, path);
            using (var stream = store.OpenFile(fullPath))
            {
                var reader = new StreamReader(stream, Encoding.UTF8);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("#include"))
                    {
                        var includePath = line.Substring("#include".Length).Trim();
                        if (includePath.StartsWith("\"") && includePath.EndsWith("\""))
                        {
                            includePath = includePath.Substring(1, includePath.Length - 2);
                        }
                        Preprocess(output, store, directory, includePath, fileNumber + 1);
                        output.AppendLine("#line " + (lineNumber + 1) + " " + fileNumber);
                    }
                    else
                    {
                        output.AppendLine(line);
                    }
                    lineNumber++;
                }
            }
        }

        private string Preprocess(IFileStore store, string path, string[] defines)
        {
            var builder = new StringBuilder();
            if (defines != null)
            {
                for (int i = 0; i < defines.Length; ++i)
                {
                    builder.AppendLine("#define " + defines[i] + " 1");
                }
            }
            Preprocess(builder, store, AssetPath.GetDirectoryName(path), AssetPath.GetFileName(path), 0);
            return builder.ToString();
        }

        private void Load(IFileStore store)
        {
            var kvp = new KeyValuePairs();
            using (var stream = store.OpenTextFile(m_path))
            {
                kvp.Load(stream);
            }

            m_vertexShaderPath = kvp.GetString("vertex_shader");
            string[] vertexShaderDefines = kvp.ContainsKey("vertex_shader_defines") ? kvp.GetString("vertex_shader_defines").Split(',') : null;
            string vertexShaderCode = Preprocess(store, m_vertexShaderPath, vertexShaderDefines);

            m_fragmentShaderPath = kvp.GetString("fragment_shader");
            string[] fragmentShaderDefines = kvp.ContainsKey("fragment_shader_defines") ? kvp.GetString("fragment_shader_defines").Split(',') : null;
            string fragmentShaderCode = Preprocess(store, m_fragmentShaderPath, fragmentShaderDefines);

            m_vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(m_vertexShader, vertexShaderCode);
            GL.CompileShader(m_vertexShader);
            CheckCompileResult(m_vertexShader);

            m_fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(m_fragmentShader, fragmentShaderCode);
            GL.CompileShader(m_fragmentShader);
            CheckCompileResult(m_fragmentShader);

            m_program = GL.CreateProgram();
            GL.AttachShader(m_program, m_vertexShader);
            GL.AttachShader(m_program, m_fragmentShader);
            GL.LinkProgram(m_program);
            TestLink(m_program);

            m_blendMode = kvp.GetEnum("blend_mode", BlendMode.Overwrite);
        }

        private void Unload()
        {
            GL.DeleteProgram(m_program);
            GL.DeleteShader(m_fragmentShader);
            GL.DeleteShader(m_vertexShader);
            m_knownUniformLocations.Clear();
            m_knownAttributeLocations.Clear();
        }

        public int GetUniformLocation(string name)
        {
            int result;
            if (!m_knownUniformLocations.TryGetValue(name, out result))
            {
                result = GL.GetUniformLocation(m_program, name);
                m_knownUniformLocations[name] = result;
            }
            return result;
        }

        public int GetAttributeLocation(string name)
        {
            int result;
            if (!m_knownAttributeLocations.TryGetValue(name, out result))
            {
                result = GL.GetAttribLocation(m_program, name);
                m_knownAttributeLocations[name] = result;
            }
            return result;
        }

        private static void CheckCompileResult(int shader)
        {
            int param;
            GL.GetShader(shader, ShaderParameter.CompileStatus, out param);
            if (param == 0)
            {
                var log = GL.GetShaderInfoLog(shader);
                throw new OpenGLException("Errors compiling shader: " + log);
            }
        }

        private static void TestLink(int program)
        {
            int param;
            GL.GetProgram(program, ProgramParameter.LinkStatus, out param);
            if (param == 0)
            {
                var log = GL.GetProgramInfoLog(program);
                throw new OpenGLException("Errors linking shader: " + log);
            }
        }
    }
}
