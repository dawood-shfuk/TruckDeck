using System;
using System.Linq;

namespace Funbit.Ets.Telemetry.Server.Helpers
{
    public static class VersionComparer
    {
        public static int Compare(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(right)) return 0;
            if (string.IsNullOrWhiteSpace(left)) return -1;
            if (string.IsNullOrWhiteSpace(right)) return 1;

            var a = left.Split('.').Select(ParsePart).ToArray();
            var b = right.Split('.').Select(ParsePart).ToArray();
            var len = Math.Max(a.Length, b.Length);
            for (var i = 0; i < len; i++)
            {
                var av = i < a.Length ? a[i] : 0;
                var bv = i < b.Length ? b[i] : 0;
                if (av != bv) return av.CompareTo(bv);
            }
            return 0;
        }

        public static bool IsNewer(string remote, string local)
        {
            return Compare(remote, local) > 0;
        }

        static int ParsePart(string part)
        {
            return int.TryParse(part, out var n) ? n : 0;
        }
    }
}
