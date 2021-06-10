using System;
using System.Collections;
using System.Globalization;
using System.Text;

namespace BIS.Core.Serialization
{
    /// <summary>
    /// Utilities to serialize to text representation of engine simple types
    /// </summary>
    public static class ArmaTextSerializer
    {
        private static string ToArmaString(object obj)
        {
            if (obj == null)
            {
                return "null";
            }
            if (obj is string str)
            {
                return ToArmaString(str);
            }
            if (obj is double dnum)
            {
                return ToArmaString(dnum);
            }
            if (obj is float fnum)
            {
                return ToArmaString(fnum);
            }
            if (obj is int inum)
            {
                return ToArmaString(inum);
            }
            if (obj is long lnum)
            {
                return ToArmaString(lnum);
            }
            if (obj is bool boolean)
            {
                return ToArmaString(boolean);
            }
            if (obj is IEnumerable list)
            {
                return ToSimpleArrayString(list);
            }
            throw new ArgumentException($"Sorry, type '{obj.GetType().FullName}' is not supported");
        }
        
        private static string Escape(string str)
        {
            return str.Replace("\"", "\"\"");
        }

        private static string ToArmaString(string str)
        {
            return $"\"{Escape(str)}\"";
        }

        private static string ToArmaString(double num)
        {
            return num.ToString(CultureInfo.InvariantCulture);
        }

        private static string ToArmaString(bool boolean)
        {
            return boolean ? "true" : "false";
        }

        private static string ToArmaString(int num)
        {
            return num.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Serialize an array for parseSimpleArray
        /// 
        /// https://community.bistudio.com/wiki/parseSimpleArray
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToSimpleArrayString(IEnumerable list)
        {
            var sb = new StringBuilder("[");
            foreach(var item in list)
            {
                if (sb.Length > 1)
                {
                    sb.Append(",");
                }
                sb.Append(ToArmaString(item));
            }
            sb.Append("]");
            return sb.ToString();
        }



    }
}
