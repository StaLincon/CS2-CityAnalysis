using System;
using System.Collections.Generic;

namespace analysis.Data
{
    internal static class StatisticMath
    {
        public static int Saturate(long value) => (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, value));

        public static int Average(long total, long population) => population > 0 ? Saturate(total / population) : 0;

        // Statistics introduced after founding have shorter buffers. Align their newest samples.
        public static void AddAligned(long[] target, IList<long> source)
        {
            int count = Math.Min(target.Length, source.Count);
            for (int i = 0; i < count; i++)
                target[target.Length - count + i] += source[source.Count - count + i];
        }

        public static void GetDate(uint frame, uint firstFrame, int epochTicks, int startingYear,
            int daysPerYear, int ticksPerDay, out int year, out int month, out int day)
        {
            // The initial statistics sample can precede the city's first simulation frame.
            long elapsed = Math.Max(0, unchecked((int)(frame - firstFrame)));
            long ticks = elapsed + epochTicks;
            long ticksPerYear = (long)ticksPerDay * daysPerYear;
            long yearOffset = (long)Math.Floor((double)ticks / ticksPerYear);
            long tickInYear = ticks - yearOffset * ticksPerYear;
            year = startingYear + (int)yearOffset;
            month = 1 + (int)(tickInYear * 12 / ticksPerYear);
            day = 1 + (int)(tickInYear / ticksPerDay);
        }
    }
}
