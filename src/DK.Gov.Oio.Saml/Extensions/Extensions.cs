using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DK.Gov.Oio.Saml.Exceptions;

namespace DK.Gov.Oio.Saml.Extensions;

internal static class Extensions
{
    public static string ToQueryString(this List<(string Name, string Value)> tuples) => string.Join("&", tuples.Select(x => $"{x.Name}={x.Value}"));

    public static bool WithinAllowedPeriod(this DateTimeOffset now, DateTime? notBefore, DateTime? notOnOrAfter, TimeSpan allowedClockSkew) =>
        (notBefore == null || notBefore.Value.Subtract(allowedClockSkew) <= now) && (notOnOrAfter == null || now < notOnOrAfter.Value.Add(allowedClockSkew));

    // A redirect-binding message is a few kilobytes; the limit stops a small query string inflating into megabytes.
    public const int MaximumInflatedMessageLength = 256 * 1024;
    // A posted SAML response, even with many privileges, is well under this; the /auth POST endpoints refuse a larger body.
    public const int MaximumPostedMessageBytes = 1024 * 1024;

    extension(string base64)
    {
        public string DeflateDecompress()
        {
            try
            {
                using var stream = new DeflateStream(new MemoryStream(Convert.FromBase64String(base64)), CompressionMode.Decompress);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var buffer = new char[MaximumInflatedMessageLength + 1];
                var length = 0;
                for (int read; length < buffer.Length && (read = reader.Read(buffer, length, buffer.Length - length)) > 0;) length += read;
                if (length > MaximumInflatedMessageLength)
                    throw new Saml20FormatException($"The message inflates to more than {MaximumInflatedMessageLength} characters.");
                return new string(buffer, 0, length);
            }
            catch (Exception e) when (e is FormatException or InvalidDataException) { throw new Saml20FormatException("The message is not base64-encoded DEFLATE data.", e); }
        }

        public X509Certificate2 ToX509Certificate2() => new(Convert.FromBase64String(base64));
    }
}
