using Dan200.Core.Util;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Dan200.Core.Assets
{
    public class KeyValuePairs
    {
        private string m_comment;
        private IDictionary<string, string> m_pairs;
        private bool m_modified;

        public string Comment
        {
            get
            {
                return m_comment;
            }
            set
            {
                if (m_comment != value)
                {
                    m_comment = value;
                    m_modified = true;
                }
            }
        }

        public IReadOnlyCollection<string> Keys
        {
            get
            {
                return m_pairs.Keys.ToReadOnly();
            }
        }

        public int Count
        {
            get
            {
                return m_pairs.Count;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return m_pairs.Count == 0;
            }
        }

        public bool Modified
        {
            get
            {
                return m_modified;
            }
            set
            {
                m_modified = value;
            }
        }

        public KeyValuePairs()
        {
            m_comment = null;
            m_pairs = new SortedDictionary<string, string>();
            m_modified = false;
        }

        public void Load(TextReader reader)
        {
            string line = null;
            while ((line = reader.ReadLine()) != null)
            {
                int commentIndex;
                if (line.StartsWith("//"))
                {
                    commentIndex = 0;
                }
                else
                {
                    commentIndex = line.IndexOf(" //");
                    commentIndex = (commentIndex >= 0) ? (commentIndex + 1) : -1;
                }
                if (commentIndex >= 0)
                {
                    if (m_pairs.Count == 0 && Comment == null)
                    {
                        Comment = line.Substring(commentIndex + 2).Trim();
                    }
                    line = line.Substring(0, commentIndex);
                }

                int equalsIndex = line.IndexOf('=');
                if (equalsIndex >= 0)
                {
                    string key = line.Substring(0, equalsIndex).Trim();
                    string value = line.Substring(equalsIndex + 1).Replace("\\n", "\n").Trim();
                    Set(key, value);
                }
            }
        }

        public void Save(TextWriter writer)
        {
            if (Comment != null)
            {
                writer.WriteLine(string.Format("// {0}", Comment));
            }
            foreach (var pair in m_pairs)
            {
                writer.WriteLine(string.Format("{0}={1}", pair.Key, pair.Value.Replace("\n", "\\n")));
            }
        }

        public bool ContainsKey(string key)
        {
            return m_pairs.ContainsKey(key);
        }

        public void Remove(string key)
        {
            if (m_pairs.ContainsKey(key))
            {
                m_pairs.Remove(key);
                m_modified = true;
            }
        }

        public void Clear()
        {
            if (m_pairs.Count > 0)
            {
                m_pairs.Clear();
                m_modified = true;
            }
        }

        public string GetString(string key, string _default = null)
        {
            if (m_pairs.ContainsKey(key))
            {
                return m_pairs[key];
            }
            return _default;
        }

        public int GetInteger(string key, int _default = 0)
        {
            if (m_pairs.ContainsKey(key))
            {
                int result;
                if (int.TryParse(m_pairs[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                {
                    return result;
                }
            }
            return _default;
        }

        public ulong GetULong(string key, ulong _default = 0)
        {
            if (m_pairs.ContainsKey(key))
            {
                ulong result;
                if (ulong.TryParse(m_pairs[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                {
                    return result;
                }
            }
            return _default;
        }

        public float GetFloat(string key, float _default = 0.0f)
        {
            if (m_pairs.ContainsKey(key))
            {
                float result;
                if (float.TryParse(m_pairs[key], NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                {
                    return result;
                }
            }
            return _default;
        }

        public bool GetBool(string key, bool _default = false)
        {
            if (m_pairs.ContainsKey(key))
            {
                string value = m_pairs[key];
                if (value == "true")
                {
                    return true;
                }
                if (value == "false")
                {
                    return false;
                }
            }
            return _default;
        }

        public Version GetVersion(string key, Version _default)
        {
            if (m_pairs.ContainsKey(key))
            {
                Version result;
                if (Version.TryParse(m_pairs[key], out result))
                {
                    return result;
                }
            }
            return _default;
        }

        private bool TryParseEnum<TEnum>(string s, out TEnum o_result) where TEnum : struct
        {
            foreach (TEnum value in Enum.GetValues(typeof(TEnum)))
            {
                if (s == value.ToString().ToLowerUnderscored())
                {
                    o_result = value;
                    return true;
                }
            }
            o_result = default(TEnum);
            return false;
        }

        public TEnum? GetEnum<TEnum>(string key) where TEnum : struct
        {
            if (ContainsKey(key))
            {
                return GetEnum<TEnum>(key, default(TEnum));
            }
            return null;
        }

        public TEnum GetEnum<TEnum>(string key, TEnum _default) where TEnum : struct
        {
            if (m_pairs.ContainsKey(key))
            {
                string value = m_pairs[key];
                TEnum result;
                if (TryParseEnum<TEnum>(value, out result))
                {
                    return result;
                }
            }
            return _default;
        }

        public void Set(string key, string value)
        {
            if (m_pairs.ContainsKey(key))
            {
                if (value != null)
                {
                    if (m_pairs[key] != value)
                    {
                        m_pairs[key] = value;
                        m_modified = true;
                    }
                }
                else
                {
                    m_pairs.Remove(key);
                    m_modified = true;
                }
            }
            else if (value != null)
            {
                m_pairs.Add(key, value);
                m_modified = true;
            }
        }

        public void Set(string key, int value)
        {
            Set(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public void Set(string key, ulong value)
        {
            Set(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public void Set(string key, float value)
        {
            Set(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public void Set(string key, bool value)
        {
            Set(key, value ? "true" : "false");
        }

        public void Set(string key, Version version)
        {
            if (version != null)
            {
                Set(key, version.ToString());
            }
            else
            {
                Remove(key);
            }
        }

        public void Set<TEnum>(string key, TEnum value) where TEnum : struct
        {
            if (!Enum.IsDefined(typeof(TEnum), value))
            {
                throw new InvalidOperationException();
            }
            Set(key, value.ToString().ToLowerUnderscored());
        }

        public string Ensure(string key, string _default)
        {
            var value = GetString(key, _default);
            Set(key, value);
            return value;
        }

        public int Ensure(string key, int _default)
        {
            var value = GetInteger(key, _default);
            Set(key, value);
            return value;
        }

        public ulong Ensure(string key, ulong _default)
        {
            var value = GetULong(key, _default);
            Set(key, value);
            return value;
        }

        public float Ensure(string key, float _default)
        {
            var value = GetFloat(key, _default);
            Set(key, value);
            return value;
        }

        public bool Ensure(string key, bool _default)
        {
            var value = GetBool(key, _default);
            Set(key, value);
            return value;
        }

        public TEnum Ensure<TEnum>(string key, TEnum _default) where TEnum : struct
        {
            var value = GetEnum<TEnum>(key, _default);
            Set(key, value);
            return value;
        }
    }

    public static class KVPExtensions
    {
        public static Vector3 GetColour(this KeyValuePairs kvp, string key, Vector3 _default)
        {
            int colourHex;
            if (kvp.ContainsKey(key) &&
                int.TryParse(kvp.GetString(key).Replace("#", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out colourHex))
            {
                float r = (float)((colourHex & 0xff0000) >> 16) / 255.0f;
                float g = (float)((colourHex & 0x00ff00) >> 8) / 255.0f;
                float b = (float)(colourHex & 0x0000ff) / 255.0f;
                return new Vector3(r, g, b);
            }
            else
            {
                return _default;
            }
        }

        public static void SetColour(this KeyValuePairs kvp, string key, Vector3 value)
        {
            int colourHex =
                (((int)(value.X * 255.0f) & 0xff) << 16) +
                (((int)(value.Y * 255.0f) & 0xff) << 8) +
                ((int)(value.Z * 255.0f) & 0xff);

            var colourString =
                "#" + colourHex.ToString("X6");

            kvp.Set(key, colourString);
        }

        public static Vector3 GetVector(this KeyValuePairs kvp, string key, Vector3 _default)
        {
            if (kvp.ContainsKey(key))
            {
                string[] str = kvp.GetString(key).Split(',');
                if (str.Length >= 3)
                {
                    float x, y, z;
                    if (float.TryParse(str[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                        float.TryParse(str[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out y) &&
                        float.TryParse(str[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out z))
                    {
                        return new Vector3(x, y, z);
                    }
                }
            }
            return _default;
        }

        public static void SetVector(this KeyValuePairs kvp, string key, Vector3 value)
        {
            var vectorString =
                value.X.ToString(CultureInfo.InvariantCulture) + "," +
                value.Y.ToString(CultureInfo.InvariantCulture) + "," +
                value.Z.ToString(CultureInfo.InvariantCulture);

            kvp.Set(key, vectorString);
        }

        public static Vector3 GetUnitVector(this KeyValuePairs kvp, string key, Vector3 _default)
        {
            var vector = kvp.GetVector(key, _default);
            if (vector.Length >= 0.01f)
            {
                vector.Normalize();
                return vector;
            }
            return _default;
        }

        public static string[] GetStringArray(this KeyValuePairs kvp, string key, string[] _default)
        {
            if (kvp.ContainsKey(key))
            {
                var value = kvp.GetString(key);
                if (value.Length > 0)
                {
                    var array = kvp.GetString(key).Split(',');
                    for (int i = 0; i < array.Length; ++i)
                    {
                        array[i] = array[i].Trim();
                    }
                    return array;
                }
                else
                {
                    return new string[0];
                }
            }
            return _default;
        }

        public static void SetStringArray(this KeyValuePairs kvp, string key, string[] value)
        {
            if (value != null)
            {
                kvp.Set(key, string.Join(",", value));
            }
            else
            {
                kvp.Remove(key);
            }
        }

        public static int[] GetIntegerArray(this KeyValuePairs kvp, string key, int[] _default)
        {
            if (kvp.ContainsKey(key))
            {
                var value = kvp.GetString(key);
                if (value.Length > 0)
                {
                    var strArray = kvp.GetString(key).Split(',');
                    var intArray = new int[strArray.Length];
                    for (int i = 0; i < strArray.Length; ++i)
                    {
                        if (!int.TryParse(strArray[i], out intArray[i]))
                        {
                            return _default;
                        }
                    }
                    return intArray;
                }
                else
                {
                    return new int[0];
                }
            }
            return _default;
        }

        public static void SetIntegerArray(this KeyValuePairs kvp, string key, int[] value)
        {
            if (value != null)
            {
                var joined = new StringBuilder();
                for (int i = 0; i < value.Length; ++i)
                {
                    joined.Append(value[i].ToString(CultureInfo.InvariantCulture));
                    if (i < value.Length - 1)
                    {
                        joined.Append(',');
                    }
                }
                kvp.Set(key, joined.ToString());
            }
            else
            {
                kvp.Remove(key);
            }
        }

        public static Guid GetGUID(this KeyValuePairs kvp, string key, Guid _default)
        {
            if (kvp.ContainsKey(key))
            {
                Guid result;
                if (Guid.TryParse(kvp.GetString(key), out result))
                {
                    return result;
                }
            }
            return _default;
        }

        public static void SetGUID(this KeyValuePairs kvp, string key, Guid value)
        {
            kvp.Set(key, value.ToString());
        }
    }
}

