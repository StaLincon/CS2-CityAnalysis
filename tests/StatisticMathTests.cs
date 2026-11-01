using System;
using analysis.Data;

internal static class StatisticMathTests
{
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        Console.WriteLine("PASS " + name);
    }

    public static void Main()
    {
        const int ticks = 262144;
        StatisticMath.GetDate(999, 1000, 0, 2026, 12, ticks, out int y, out int m, out int d);
        Check(y == 2026 && m == 1 && d == 1, "initial sample before first frame cannot underflow");
        StatisticMath.GetDate(1000 + ticks * 12, 1000, 0, 2026, 12, ticks, out y, out m, out d);
        Check(y == 2027 && m == 1 && d == 1, "year rollover");
        StatisticMath.GetDate(1000 + ticks * 4, 1000, ticks * 6, 2026, 12, ticks, out y, out m, out d);
        Check(y == 2026 && m == 11 && d == 11, "non-January starting date");
        StatisticMath.GetDate(1000 + ticks * 3, 1000, 0, 2026, 24, ticks, out y, out m, out d);
        Check(y == 2026 && m == 2 && d == 4, "custom days per year");
        StatisticMath.GetDate(50, uint.MaxValue - 49, 0, 2026, 12, ticks, out y, out m, out d);
        Check(y == 2026 && m == 1 && d == 1, "frame counter wrap");
        Check(StatisticMath.Average(1536033, 16937) == 90, "real exported wellbeing total becomes per-capita value");
        Check(StatisticMath.Average(1005487, 16937) == 59, "real exported health total becomes per-capita value");
        Check(StatisticMath.Average(123, 0) == 0, "empty city");
        var history = new long[4];
        StatisticMath.AddAligned(history, new long[] { 10, 20 });
        StatisticMath.AddAligned(history, new long[] { 1, 2, 3, 4, 5 });
        Check(string.Join(",", history) == "2,3,14,25", "parameter histories align at newest sample");
        Check(StatisticMath.Saturate((long)int.MaxValue + 10) == int.MaxValue, "large aggregate does not overflow");
    }
}
