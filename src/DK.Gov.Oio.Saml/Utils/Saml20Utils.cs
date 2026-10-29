using System;
using System.Xml;
using DK.Gov.Oio.Saml.Exceptions;

namespace DK.Gov.Oio.Saml.Utils
{
    internal static class Saml20Utils
    {
        public static DateTime FromUTCString(string value)
        {
            try
            {
                return XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.Utc);
            }
            catch (FormatException fe)
            {
                throw new Saml20FormatException("Invalid DateTime-string (non-UTC) found in saml:Assertion", fe);
            }
        }

        public static string ToUTCString(DateTime value)
        {
            return XmlConvert.ToString(value, XmlDateTimeSerializationMode.Utc);
        }
    }
}