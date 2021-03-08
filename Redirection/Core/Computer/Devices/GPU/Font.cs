using System;
using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Computer.Devices.GPU
{
    public unsafe class Font
    {
        public readonly Image Image;
        public readonly string Characters;
        public readonly int CharacterWidth;
        public readonly int CharacterHeight;
        private Dictionary<int, int> m_characterPositions; // codepoint -> position
        private Dictionary<int, int> m_characterWidths; // position -> width

        public Font(Image image, string characters, int characterWidth, int characterHeight, bool fixedWidth)
        {
            if (characterWidth <= 0 || characterWidth > image.Width)
            {
                throw new IOException("Character width must be between 0 and the image width");
            }
            if (characterHeight <= 0 || characterHeight > image.Height)
            {
                throw new IOException("Character height must be between 0 and the image height");
            }

            Image = image;
            Characters = characters;
            CharacterWidth = characterWidth;
            CharacterHeight = characterHeight;

            // Build character position map
            var rows = image.Height / characterHeight;
            var columns = image.Width / characterWidth;
            var limit = (rows * columns);
            int positionsUsed = 0;
            m_characterPositions = new Dictionary<int, int>(Math.Min(limit, characters.Length));
            for (int i = 0; i < characters.Length; ++i)
            {
                if (!char.IsLowSurrogate(characters, i))
                {
                    var codepoint = char.ConvertToUtf32(characters, i);
                    if (!m_characterPositions.ContainsKey(codepoint))
                    {
                        m_characterPositions[codepoint] = positionsUsed;
                    }
                    if (++positionsUsed >= limit)
                    {
                        break;
                    }
                }
            }

            // Build character width map
            m_characterWidths = new Dictionary<int, int>();
            if (fixedWidth)
            {
                // Fixed
                for (int i = 0; i < positionsUsed; ++i)
                {
                    m_characterWidths[i] = characterWidth;
                }
            }
            else
            {
                // Variable
                var data = Image.Data;
                var start = Image.Start;
                var stride = Image.Stride;
                fixed (byte* pData = data)
                {
                    var bgColor = pData[start];
                    for (int i = 0; i < positionsUsed; ++i)
                    {
                        var cx = i % columns;
                        var cy = i / columns;

                        var width = 0;
                        for (var px = characterWidth - 1; px >= 0; --px)
                        {
                            for (var py = 0; py < characterHeight; ++py)
                            {
                                var color = pData[
                                    start +
                                    (cx * characterWidth + px) +
                                    (cy * characterHeight + py) * stride
                                ];
                                if (color != bgColor)
                                {
                                    width = px + 1;
                                    break;
                                }
                            }
                            if (width > 0)
                            {
                                break;
                            }
                        }
                        if (width == 0)
                        {
                            width = characterWidth;
                        }
                        m_characterWidths[i] = width;
                    }
                }
            }
        }

        private int GetCharacterIndex(int codepoint)
        {
            var positions = m_characterPositions;
            int position;
            if (positions.TryGetValue(codepoint, out position))
            {
                return position;
            }
            else if (positions.TryGetValue('?', out position))
            {
                return position;
            }
            else
            {
                return 0;
            }
        }

        public void GetCharacterPosition(int codepoint, out int o_x, out int o_y, out int o_width)
        {
            var pos = GetCharacterIndex(codepoint);
            var image = Image;
            var charWidth = CharacterWidth;
            var charHeight = CharacterHeight;
            var columns = image.Width / charWidth;
            o_x = (pos % columns) * charWidth;
            o_y = (pos / columns) * charHeight;
            o_width = m_characterWidths[pos];
        }

        public Image GetCharacterImage(int codepoint)
        {
            int x, y, width;
            GetCharacterPosition(codepoint, out x, out y, out width);
            return Image.Sub(
                x, y, width, CharacterHeight
            );
        }

        public void MeasureText(string text, out int o_width, out int o_height)
        {
            var width = 0;
            for (int i = 0; i < text.Length; ++i)
            {
                if (!char.IsLowSurrogate(text, i))
                {
                    var codepoint = char.ConvertToUtf32(text, i);
                    var position = GetCharacterIndex(codepoint);
                    var charWidth = m_characterWidths[position];
                    width += charWidth;
                }
            }
            o_width = width;
            o_height = CharacterHeight;
        }
    }
}
