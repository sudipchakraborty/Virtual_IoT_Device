using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Virtual_IoT_Device.IoT_Sinric_Pro
{
    public static class Util_IoT
    {
        
            // https://stackoverflow.com/questions/11743160/how-do-i-encode-and-decode-a-base64-string
            public static string Base64Encode(this string plainText)
            {
                var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                return Convert.ToBase64String(plainTextBytes);
            }

            public static string Base64Decode(this string base64EncodedData)
            {
                var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
                return Encoding.UTF8.GetString(base64EncodedBytes);
            }

            public static string aDescriptionAttr<T>(this T source)
            {
                if (source is Type sourceType)
                {
                    // attribute for a class, struct or enum
                    var attributes = (DescriptionAttribute[])sourceType.GetCustomAttributes(typeof(DescriptionAttribute), false);

                    if (attributes.Length > 0)
                        return attributes[0].Description;
                }
                else
                {
                    // attribute for a member field
                    var fieldInfo = source.GetType().GetField(source.ToString());
                    var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

                    if (attributes.Length > 0)
                        return attributes[0].Description;
                }

                return source.ToString();
            }

            public static string MessageID()
            {
                string _id = "";
                for (byte i = 0; i < 16; i++)
                {
                    Random random = new Random();
                    byte rnd = (byte)random.Next(1, 255);

                    if (i == 4) _id += "-";
                    if (i == 6) { _id += "-"; rnd = (byte)(0x40 | 0x0F & rnd); } // 0100xxxx to set version 4
                    if (i == 8) { _id += "-"; rnd = (byte)(0x80 | 0x3F & rnd); } // 10xxxxxx to set reserved bits
                    if (i == 10) _id += "-";
                    byte high_nibble = (byte)(rnd >> 4);
                    byte low_nibble = (byte)(rnd & 0x0f);
                    _id += "0123456789abcdef"[high_nibble];
                    _id += "0123456789abcdef"[low_nibble];
                }
                return _id;
            }

            public static DateTime ConvertFromUnixTimestamp(uint timestamp)
            {
                DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                return origin.AddSeconds(timestamp);
            }

            public static uint ConvertToUnixTimestamp(DateTime date)
            {
                DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                TimeSpan diff = date.ToUniversalTime() - origin;
                return (uint)Math.Floor(diff.TotalSeconds);
            }
        







    }
}
