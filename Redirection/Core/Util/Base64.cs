using System.Collections.Generic;

namespace Dan200.Core.Util
{
    public static class Base64
    {
        private static string s_hexAlphabet;
        private static Dictionary<char, int> s_hexAlphabetLookup;

        static Base64()
        {
            s_hexAlphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ+/";
            s_hexAlphabetLookup = new Dictionary<char, int>();
            for (int i = 0; i < 64; ++i)
            {
                char c = s_hexAlphabet[i];
                s_hexAlphabetLookup[c] = i;
            }
        }

        public static int ParseInt(string str)
        {
            int result = 0;
            for (int i = 0; i < str.Length; ++i)
            {
                char c = str[i];
                if (s_hexAlphabetLookup.ContainsKey(c))
                {
                    int value = s_hexAlphabetLookup[c];
                    result = (result * 64) + value;
                }
                else
                {
                    result *= 64;
                }
            }
            return result;
        }

        public static string ToString(int value, int length)
        {
            string result = "";
            while (result.Length < length)
            {
                char c = s_hexAlphabet[value % 64];
                result = c + result;
                value /= 64;
            }
            return result;
        }
    }
}

