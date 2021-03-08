using Dan200.Core.Assets;
using Dan200.Core.Main;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Dan200.Core.Render
{
    public class Font : IBasicAsset
    {
        private const float FONT_SCALE = 0.5f;

        public static Font Get(string path)
        {
            return Assets.Assets.Get<Font>(path);
        }

        public static int AdvanceGlyph(string s, bool parseImages)
        {
            return AdvanceGlyph(s, 0, parseImages);
        }

        public static int AdvanceGlyph(string s, int start, bool parseImages)
        {
            return AdvanceGlyph(s, start, s.Length - start, parseImages);
        }

        public static int AdvanceGlyph(string s, int start, int length, bool parseImages)
        {
            if (start < 0 || length < 0 || start + length > s.Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            if (length == 0)
            {
                return 0;
            }

            var c = s[start];
            if (parseImages && c == '[')
            {
                int newPos = start + 1;
                while (newPos < start + length)
                {
                    if (s[newPos] == ']')
                    {
                        return (newPos + 1) - start; // Eat the image
                    }
                    ++newPos;
                }
                return 1; // Eat the bracket
            }
            else
            {
                if (!char.IsSurrogate(c))
                {
                    return 1; // Eat the char
                }
                else if (start + 1 < start + length && char.IsSurrogatePair(c, s[start + 1]))
                {
                    return 2; // Eat the pair
                }
                else
                {
                    return 1; // Eat the (invalid) char
                }
            }
        }

        public static int AdvanceWhitespace(string s)
        {
            return AdvanceWhitespace(s, 0);
        }

        public static int AdvanceWhitespace(string s, int start)
        {
            return AdvanceWhitespace(s, start, s.Length - start);
        }

        public static int AdvanceWhitespace(string s, int start, int length)
        {
            if (start < 0 || length < 0 || start + length > s.Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            int pos = start;
            while (pos < start + length)
            {
                if (s[pos] == '\n')
                {
                    ++pos;
                    break;
                }
                else if (s[pos] == ' ')
                {
                    ++pos;
                }
                else
                {
                    break;
                }
            }
            return pos - start;
        }

        public static int AdvanceSentence(string s)
        {
            return AdvanceSentence(s, 0);
        }

        public static int AdvanceSentence(string s, int start)
        {
            return AdvanceSentence(s, start, s.Length - start);
        }

        public static int AdvanceSentence(string s, int start, int length)
        {
            if (start < 0 || length < 0 || start + length > s.Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            int pos = start;
            char lastChar = '\0';
            while (pos < start + length)
            {
                var thisChar = s[pos];
                if (thisChar == '\n')
                {
                    break;
                }
                else if (thisChar == ' ')
                {
                    if (lastChar == '.' ||
                        lastChar == '!' ||
                        lastChar == '?' ||
                        lastChar == (char)161 || // Inverted exclamation mark 
                        lastChar == (char)191 // Inverted question mark
                       )
                    {
                        break;
                    }
                }
                ++pos;
                lastChar = thisChar;
            }
            return pos - start;
        }

        private string m_path;
        private int m_lineHeight;
        private int m_base;
        private int m_scaleW;
        private int m_scaleH;

        private class Page
        {
            public string Path;
        }
        private Page[] m_pages;

        private class Char
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;
            public int XOffset;
            public int YOffset;
            public int XAdvance;
            public int Page;
        }
        private Dictionary<int, Char> m_chars;

        private struct KerningPair
        {
            int FirstChar;
            int SecondChar;

            public KerningPair(int firstChar, int secondChar)
            {
                FirstChar = firstChar;
                SecondChar = secondChar;
            }

            public override bool Equals(object other)
            {
                if (other is KerningPair)
                {
                    return Equals((KerningPair)other);
                }
                return false;
            }

            public bool Equals(KerningPair other)
            {
                return other.FirstChar == FirstChar && other.SecondChar == SecondChar;
            }

            public override int GetHashCode()
            {
                return FirstChar * 31 + SecondChar;
            }

            public override string ToString()
            {
                return char.ConvertFromUtf32(FirstChar) + char.ConvertFromUtf32(SecondChar);
            }
        }
        private class Kerning
        {
            public int Amount;
        }
        private Dictionary<KerningPair, Kerning> m_kernings;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public float Height
        {
            get
            {
                return m_lineHeight * FONT_SCALE;
            }
        }

        public float Base
        {
            get
            {
                return m_base * FONT_SCALE;
            }
        }

        public int PageCount
        {
            get
            {
                return m_pages.Length;
            }
        }

        public Font(string path, IFileStore store)
        {
            m_path = path;
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

        private void Load(IFileStore store)
        {
            using (var stream = store.OpenFile(m_path))
            {
                var reader = new StreamReader(stream, Encoding.UTF8);
                string line;
                string type = null;
                var options = new KeyValuePairs();
                while ((line = reader.ReadLine()) != null)
                {
                    // Parse each line
                    string[] parts = line.Split(' ');
                    if (parts.Length < 1)
                    {
                        continue;
                    }

                    // Extract type and options
                    type = parts[0];
                    options.Clear();
                    for (int i = 1; i < parts.Length; ++i)
                    {
                        string part = parts[i];
                        int equalsIndex = part.IndexOf('=');
                        if (equalsIndex >= 0)
                        {
                            string key = part.Substring(0, equalsIndex);
                            string value = part.Substring(equalsIndex + 1);
                            int intValue;
                            if (value.StartsWith("\""))
                            {
                                if (value.EndsWith("\"") && value.Length >= 2)
                                {
                                    value = value.Substring(1, value.Length - 2);
                                }
                                else
                                {
                                    value = value.Substring(1) + " ";
                                    i++;
                                    while (!parts[i].EndsWith("\""))
                                    {
                                        value += parts[i] + " ";
                                        i++;
                                    }
                                    value += parts[i].Substring(0, parts[i].Length - 1);
                                }
                                options.Set(key, value);
                            }
                            else if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue))
                            {
                                options.Set(key, intValue);
                            }
                        }
                    }

                    // Interpret
                    switch (type)
                    {
                        case "common":
                            {
                                m_lineHeight = options.GetInteger("lineHeight");
                                m_base = options.GetInteger("base");
                                m_scaleW = options.GetInteger("scaleW");
                                m_scaleH = options.GetInteger("scaleH");
                                m_pages = new Page[options.GetInteger("pages")];
                                break;
                            }
                        case "page":
                            {
                                int id = options.GetInteger("id");
                                if (id >= PageCount)
                                {
                                    Array.Resize(ref m_pages, id + 1);
                                }
                                m_pages[id] = new Page();
                                m_pages[id].Path = AssetPath.Combine(AssetPath.GetDirectoryName(m_path), options.GetString("file"));
                                break;
                            }
                        case "chars":
                            {
                                m_chars = new Dictionary<int, Char>(options.GetInteger("count"));
                                break;
                            }
                        case "char":
                            {
                                var id = options.GetInteger("id");
                                m_chars[id] = new Char();
                                m_chars[id].X = options.GetInteger("x");
                                m_chars[id].Y = options.GetInteger("y");
                                m_chars[id].Width = options.GetInteger("width");
                                m_chars[id].Height = options.GetInteger("height");
                                m_chars[id].XOffset = options.GetInteger("xoffset");
                                m_chars[id].YOffset = options.GetInteger("yoffset");
                                m_chars[id].XAdvance = options.GetInteger("xadvance");
                                m_chars[id].Page = options.GetInteger("page");
                                if (m_chars[id].Page < 0 || m_chars[id].Page >= PageCount)
                                {
                                    m_chars[id].Page = 0;
                                    //throw new IOException( "Page count out of range" );
                                }
                                break;
                            }
                        case "kernings":
                            {
                                m_kernings = new Dictionary<KerningPair, Kerning>(options.GetInteger("count"));
                                break;
                            }
                        case "kerning":
                            {
                                var first = options.GetInteger("first");
                                var second = options.GetInteger("second");
                                var pair = new KerningPair(first, second);
                                m_kernings[pair] = new Kerning();
                                m_kernings[pair].Amount = options.GetInteger("amount");
                                break;
                            }
                    }
                }
            }
        }

        private void Unload()
        {
            m_chars = null;
            m_pages = null;
            m_kernings = null;
        }

        public Texture GetPageTexture(int page)
        {
            return Texture.Get(m_pages[page].Path, true);
        }

        private struct PositionedGlyph
        {
            public int Start;
            public int Length;

            public float X;
            public float Y;
            public float Width;
            public float Height;

            public int PageTexture;
            public Texture ImageTexture;
            public float U;
            public float V;
            public float UVWidth;
            public float UVHeight;
        }

        private IEnumerable<PositionedGlyph> EnumerateGlyphs(String s, int start, int length, bool parseImages)
        {
            if (start < 0 || length < 0 || start + length > s.Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            if (length > 0)
            {
                // Measure text
                float xPos = 0.0f;
                float yPos = 0.0f;
                int previousDisplayChar = -1;
                int pos = start;
                while (pos < start + length)
                {
                    int glyphLength = AdvanceGlyph(s, pos, (start + length) - pos, parseImages);
                    if (glyphLength >= 2 && s[pos] == '[' && s[pos + glyphLength - 1] == ']')
                    {
                        // Emit image
                        var imagePath = s.Substring(pos + 1, glyphLength - 2);
                        var texture = Texture.Get(imagePath, true);
                        var imageHeight = this.Height;
                        var imageWidth = (this.Height * texture.Width) / texture.Height;
                        var imageAdvance = imageWidth;

                        float expand = -0.05f;
                        float startX = (float)(xPos) - (float)imageWidth * expand;
                        float startY = (float)(yPos) - (float)imageHeight * expand;
                        float endX = (float)(xPos + imageWidth) + (float)imageWidth * expand;
                        float endY = (float)(yPos + imageHeight) + (float)imageHeight * expand;

                        var glyph = new PositionedGlyph();
                        glyph.Start = pos;
                        glyph.Length = glyphLength;
                        glyph.PageTexture = -1;
                        glyph.ImageTexture = texture;
                        glyph.X = startX;
                        glyph.Y = startY;
                        glyph.Width = (endX - startX);
                        glyph.Height = (endY - startY);
                        glyph.U = 0.0f;
                        glyph.V = 0.0f;
                        glyph.UVWidth = 1.0f;
                        glyph.UVHeight = 1.0f;
                        yield return glyph;

                        xPos += imageAdvance;
                        previousDisplayChar = -1;
                    }
                    else
                    {
                        // Emit character
                        int thisChar;
                        if (glyphLength == 1)
                        {
                            thisChar = s[pos];
                        }
                        else if (glyphLength == 2)
                        {
                            thisChar = char.ConvertToUtf32(s[pos], s[pos + 1]);
                        }
                        else
                        {
                            thisChar = '?';
                        }

                        int displayChar = thisChar;
                        if (!m_chars.ContainsKey(displayChar))
                        {
                            displayChar = '?';
                            if (!m_chars.ContainsKey(displayChar))
                            {
                                throw new IOException(m_path + " does not contain " + char.ConvertFromUtf32(displayChar) + " character");
                            }
                        }
                        if (m_kernings != null && previousDisplayChar >= 0)
                        {
                            var pair = new KerningPair(previousDisplayChar, displayChar);
                            if (m_kernings.ContainsKey(pair))
                            {
                                var kerning = m_kernings[pair];
                                xPos += (float)kerning.Amount * FONT_SCALE;
                            }
                        }
                        {
                            var letter = m_chars[displayChar];
                            if (xPos == 0.0f)
                            {
                                xPos = -(float)(letter.XOffset) * FONT_SCALE;
                            }
                            float startX = xPos + (float)(letter.XOffset) * FONT_SCALE;
                            float startY = yPos + (float)(letter.YOffset) * FONT_SCALE;
                            float endX = xPos + (float)(letter.XOffset + letter.Width) * FONT_SCALE;
                            float endY = yPos + (float)(letter.YOffset + letter.Height) * FONT_SCALE;
                            float texStartX = (float)(letter.X) / (float)m_scaleW;
                            float texStartY = (float)(letter.Y) / (float)m_scaleH;
                            float texWidth = (float)(letter.Width) / (float)m_scaleW;
                            float texHeight = (float)(letter.Height) / (float)m_scaleH;

                            var glyph = new PositionedGlyph();
                            glyph.Start = pos;
                            glyph.Length = glyphLength;
                            glyph.PageTexture = letter.Page;
                            glyph.ImageTexture = null;
                            glyph.X = startX;
                            glyph.Y = startY;
                            glyph.Width = endX - startX;
                            glyph.Height = endY - startY;
                            glyph.U = texStartX;
                            glyph.V = texStartY;
                            glyph.UVWidth = texWidth;
                            glyph.UVHeight = texHeight;
                            yield return glyph;

                            xPos += (float)letter.XAdvance * FONT_SCALE;
                            previousDisplayChar = displayChar;
                        }
                    }
                    pos += glyphLength;
                }
            }
        }

        public float Measure(string s, bool parseImages)
        {
            return Measure(s, 0, s.Length, parseImages);
        }

        public float Measure(string s, int start, int length, bool parseImages)
        {
            float width = 0.0f;
            foreach (var glyph in EnumerateGlyphs(s, start, length, parseImages))
            {
                width = glyph.X + glyph.Width;
            }
            return width;
        }

        public int WordWrap(string s, bool parseImages, float maxWidth)
        {
            return WordWrap(s, 0, parseImages, maxWidth);
        }

        public int WordWrap(string s, int start, bool parseImages, float maxWidth)
        {
            return WordWrap(s, start, s.Length - start, parseImages, maxWidth);
        }

        public int WordWrap(string s, int start, int length, bool parseImages, float maxWidth)
        {
            var currentLineLen = 0;
            var currentWordLen = 0;
            foreach (var glyph in EnumerateGlyphs(s, start, length, parseImages))
            {
                if (glyph.Length == 1)
                {
                    var c = s[glyph.Start];
                    if (c == '\n')
                    {
                        currentLineLen += currentWordLen;
                        return currentLineLen;
                    }
                    else if (c == ' ')
                    {
                        currentLineLen += currentWordLen;
                        currentWordLen = 0;
                    }
                }
                if (glyph.X + glyph.Width > maxWidth)
                {
                    if (currentLineLen > 0)
                    {
                        return currentLineLen;
                    }
                    else if (currentWordLen > 0)
                    {
                        return currentWordLen;
                    }
                    else
                    {
                        return glyph.Length;
                    }
                }
                currentWordLen += glyph.Length;
            }
            return length;
        }

        public void Render(string s, float xPos, float yPos, Geometry[] o_geometry, Texture[] o_textures, bool parseImages, float scale)
        {
            Render(s, 0, s.Length, xPos, yPos, o_geometry, o_textures, parseImages, scale);
        }

        public void Render(string s, int start, int length, float xPos, float yPos, Geometry[] o_geometry, Texture[] o_textures, bool parseImages, float scale)
        {
            // Clear the output
            for (int i = 0; i < o_geometry.Length; ++i)
            {
                o_geometry[i].Clear();
                o_textures[i] = (i < PageCount) ? GetPageTexture(i) : null;
            }
            int usedPages = PageCount;

            // Build the geometries
            foreach (var glyph in EnumerateGlyphs(s, start, length, parseImages))
            {
                Geometry geometry = null;
                if (glyph.ImageTexture != null)
                {
                    if (usedPages >= o_geometry.Length)
                    {
                        App.Log("Error: No geometry left to render image {0}", glyph.ImageTexture.Path);
                    }
                    else
                    {
                        geometry = o_geometry[usedPages];
                        o_textures[usedPages] = glyph.ImageTexture;
                        usedPages++;
                    }
                }
                else
                {
                    geometry = o_geometry[glyph.PageTexture];
                }
                if (geometry != null)
                {
                    geometry.Add2DQuad(
                        new Vector2(xPos + glyph.X * scale, yPos + glyph.Y * scale),
                        new Vector2(xPos + (glyph.X + glyph.Width) * scale, yPos + (glyph.Y + glyph.Height) * scale),
                        new Quad(glyph.U, glyph.V, glyph.UVWidth, glyph.UVHeight),
                        Vector4.One
                    );
                }
            }

            // Rebuild the output
            for (int i = 0; i < o_geometry.Length; ++i)
            {
                o_geometry[i].Rebuild();
            }
        }
    }
}
