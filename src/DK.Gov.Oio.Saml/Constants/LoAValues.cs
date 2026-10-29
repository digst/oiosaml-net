using System;
using System.Collections.Immutable;
using System.Linq;

namespace DK.Gov.Oio.Saml.Constants
{
    public static class LoAValues
    {
        public const string Low = "Low";
        public const string Substantial = "Substantial";
        public const string High = "High";

        // eIDAS LoA values are URIs: http://eidas.europa.eu/LoA/low, .../substantial, .../high
        public const string EidasPrefix = "http://eidas.europa.eu/LoA/";

        public static readonly ImmutableArray<string> AllValuesRanked = new[] { Low, Substantial, High }.ToImmutableArray();

        // 0 for Low, 1 for Substantial, 2 for High, -1 if not recognized; accepts NSIS/generic values and eIDAS URIs.
        public static int GetLoARank(string? loaValue)
        {
            if (loaValue == null) return -1;
            if (!loaValue.StartsWith(EidasPrefix, StringComparison.Ordinal)) return AllValuesRanked.IndexOf(loaValue);
            var eidasLevel = loaValue.Substring(EidasPrefix.Length);
            return AllValuesRanked.Select((level, rank) => (level, rank)).FirstOrDefault(x => string.Equals(x.level, eidasLevel, StringComparison.OrdinalIgnoreCase)) is { level: not null } match
                ? match.rank
                : -1;
        }
    }
}
