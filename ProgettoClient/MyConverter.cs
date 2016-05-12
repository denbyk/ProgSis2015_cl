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
        /// Nome completo file\r\n | Dimensione file (8 Byte) | Hash del file (16 Byte) | Timestamp (8 Byte)
        private const int sizeLength = 8;
        private const int hashLength = 16;
        private const int timestampLength = 8;


        private static Encoding utf8 = Encoding.UTF8;

        public static byte[] toFixedLengthByteArray(long x)
        {
            byte[] buf = BitConverter.GetBytes(x);
            if (buf.Length != sizeLength)
                throw new ConvertingException();
            return buf;
        }

        public static byte[] toFixedLengthByteArray(double x)
        {
            byte[] buf = BitConverter.GetBytes(x);
            if (buf.Length != timestampLength)
                throw new ConvertingException();
            return buf;
        }

        public static byte[] UnicodeToByteArray(string x)
        {
            return utf8.GetBytes(x.ToCharArray());
        }


        public static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (TimeZoneInfo.ConvertTimeToUtc(dateTime) -
                new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        public static DateTime UnixTimestampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }


        public static byte[] stringToFixedLengthByteArray(string str)
        {
            if (str.Length != hashLength) throw new ConvertingException();
            //TODO: verificare Default!!!!
            return Encoding.Default.GetBytes(str);
        }

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
