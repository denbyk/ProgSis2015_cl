using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ProgettoClient
{
    public static class MyConverter
    {
        private const int sizeLength = 8;
        private const int hashLength = 16;
        private const int timestampLength = 8;


        private static Encoding utf8 = Encoding.UTF8;

        //public static string toExadecimal(long toConvert, int numDigits)
        //{
        //    return toConvert.ToString("X" + numDigits.ToString());
        //}

        /// <summary>
        /// estrae nome e directory dal path
        /// </summary>
        /// <param name="path"></param>
        /// <returns>ret[0] = dir (with final //); ret[1] = name</returns>
        public static string[] extractNameAndFolder (string path)
        {
            string[] tot = path.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
            string[] ret = new string[2];
            foreach (var s in tot.Take<string>(tot.Length - 1))
                ret[0] += s + "\\";
            ret[1] = tot.Last<string>();
            return ret;
        }

        public static long DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return Convert.ToInt64((TimeZoneInfo.ConvertTimeToUtc(dateTime) -
                new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
        }

        public static DateTime UnixTimestampToDateTime(long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }


        //public static byte[] stringToFixedLengthByteArray(string str)
        //{
        //    if (str.Length != hashLength) throw new ConvertingException();
        //    //TODO: verificare Default!!!!
        //    return Encoding.Default.GetBytes(str);
        //}

    }

    [Serializable]
    internal class ConvertingException : Exception
    {
        public ConvertingException()
        {
        }

        public ConvertingException(string message) : base(message)
        {
        }

        public ConvertingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ConvertingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
