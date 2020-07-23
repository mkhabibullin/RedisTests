using System;
using System.Linq;

namespace RedisFTS.Core
{
    internal static class Extensions
    {
        public static TimeSpan TrimMilliseconds(this TimeSpan timespan)
        {
            return new TimeSpan(timespan.Hours, timespan.Minutes, timespan.Seconds);
        }

        public static TimeSpan TrimSeconds(this TimeSpan timespan)
        {
            return new TimeSpan(timespan.Hours, timespan.Minutes, 0);
        }

        public static T[][] Slice<T>(this T[] source, int chunkSize)
        {
            int i = 0;
            return
                source
                .GroupBy(s => i++ / chunkSize)
                .Select(g => g.ToArray()).ToArray();
        }
    }
}
