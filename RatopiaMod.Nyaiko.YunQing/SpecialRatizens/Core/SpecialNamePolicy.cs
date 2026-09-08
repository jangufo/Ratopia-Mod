using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpecialRatizens.Core
{
    internal static class SpecialNamePolicy
    {
        private static readonly Regex ColorTag = new Regex("<color(?:=[^>]*)?>|</color>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsTaken(string name, IEnumerable<string> existingNames)
        {
            if (existingNames == null)
                throw new ArgumentNullException(nameof(existingNames));

            string normalized = Normalize(name);
            return normalized.Length > 0 && existingNames.Any(existing => Normalize(existing) == normalized);
        }

        public static string Normalize(string name)
        {
            return ColorTag.Replace(name ?? string.Empty, string.Empty).Trim();
        }
    }
}
