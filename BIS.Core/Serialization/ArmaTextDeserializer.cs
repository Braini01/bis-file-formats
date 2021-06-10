using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace BIS.Core.Serialization
{
    /// <summary>
    /// Utilities to parse text representation of engine simple types
    /// </summary>
    public class ArmaTextDeserializer
    {
        /// <summary>
        /// Converts given, formatted as simple array, String into a valid Array
        /// https://community.bistudio.com/wiki/parseSimpleArray
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static object[] ParseSimpleArray(string str)
        {
            return ReadArray(new StringReader(str));
        }

        public static T[] ParseNumberArray<T>(string str)
            where T : unmanaged
        {
            return ParseSimpleArray(str).Cast<double?>().Select(n => (T)Convert.ChangeType(n, typeof(T))).ToArray();
        }

        public static double? ParseDouble(string str)
        {
            if (string.IsNullOrEmpty(str) || IsNullToken(str))
            {
                return null;
            }
            return double.Parse(str.Trim(), CultureInfo.InvariantCulture);
        }

        public static string ParseString(string str)
        {
            if (IsNullToken(str))
            {
                return null;
            }
            return ReadString(new StringReader(str));
        }

        private static bool IsNullToken(string token)
        {
            return token == "null" || token == "<null>" || token == "nil" || token == "any";
        }

        private static string ReadString(StringReader str)
        {
            if (str.Peek() == '"')
            {
                var sb = new StringBuilder();
                str.Read(); // Consume '"'
                while (str.Peek() != -1)
                {
                    char c = (char)str.Read();
                    if (c == '"')
                    {
                        if (str.Peek() == '"')
                        {
                            str.Read(); // Consume second '"'
                            sb.Append(c);
                        }
                        else
                        {
                            return sb.ToString();
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                return sb.ToString();
            }
            return null;
        }

        private static double? ReadNumber(StringReader str)
        {
            var sb = new StringBuilder();
            int i;
            while ((i = str.Peek()) != -1)
            {
                char c = (char)i;
                if (char.IsDigit(c) || c == '.' || c == '-')
                {
                    str.Read();
                    sb.Append(c);
                }
                else
                {
                    return ParseDouble(sb.ToString());
                }
            }
            return ParseDouble(sb.ToString());
        }

        private static object[] ReadArray(StringReader str)
        {
            if (str.Peek() == '[')
            {
                var data = new List<object>();
                var expectItem = true;
                str.Read(); // Consume '['

                int i;
                while ((i = str.Peek()) != -1)
                {
                    char c = (char)i;
                    if (c == ']')
                    {
                        str.Read();
                        return data.ToArray();
                    }
                    if (c == ',')
                    {
                        str.Read();
                        expectItem = true;
                    }
                    else if (c != ' ' && expectItem)
                    {
                        if (c == '"')
                        {
                            data.Add(ReadString(str));
                        }
                        else if (c == '[')
                        {
                            data.Add(ReadArray(str));
                        }
                        else if (char.IsDigit(c) || c == '-')
                        {
                            data.Add(ReadNumber(str));
                        }
                        else if (c == 'n' || c == '<' || c == 'a') // null, <null>, nil or any
                        {
                            str.Read();
                            data.Add(null);
                        }
                        else if (c == 't') // true
                        {
                            str.Read();
                            data.Add(true);
                        }
                        else if (c == 'f') // false
                        {
                            str.Read();
                            data.Add(false);
                        }
                        expectItem = false;
                    }
                    else
                    {
                        str.Read();
                    }
                }
                return data.ToArray();
            }
            return null;
        }
    }
}
