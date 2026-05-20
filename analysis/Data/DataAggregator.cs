using System;
using System.Collections.Generic;
using System.Linq;

namespace analysis.Data
{
    public class DataAggregator
    {
        private readonly List<StatisticSnapshot> m_History;

        public DataAggregator(List<StatisticSnapshot> history)
        {
            m_History = history;
        }

        public double GetGrowthRate(Func<StatisticSnapshot, double> selector, int periods = 1)
        {
            if (m_History.Count < periods + 1) return 0;
            var current = selector(m_History[m_History.Count - 1]);
            var previous = selector(m_History[m_History.Count - 1 - periods]);
            if (Math.Abs(previous) < 0.001) return current > 0 ? 100 : 0;
            return (current - previous) / previous * 100.0;
        }

        public double GetAverage(Func<StatisticSnapshot, double> selector, int recentPeriods = -1)
        {
            var items = recentPeriods > 0 && recentPeriods < m_History.Count
                ? m_History.Skip(m_History.Count - recentPeriods)
                : m_History;
            var list = items.ToList();
            if (list.Count == 0) return 0;
            return list.Average(selector);
        }

        public double GetMax(Func<StatisticSnapshot, double> selector, int recentPeriods = -1)
        {
            var items = recentPeriods > 0 && recentPeriods < m_History.Count
                ? m_History.Skip(m_History.Count - recentPeriods)
                : m_History;
            var list = items.ToList();
            if (list.Count == 0) return 0;
            return list.Max(selector);
        }

        public double GetMin(Func<StatisticSnapshot, double> selector, int recentPeriods = -1)
        {
            var items = recentPeriods > 0 && recentPeriods < m_History.Count
                ? m_History.Skip(m_History.Count - recentPeriods)
                : m_History;
            var list = items.ToList();
            if (list.Count == 0) return 0;
            return list.Min(selector);
        }

        public double GetTotal(Func<StatisticSnapshot, double> selector, int recentPeriods = -1)
        {
            var items = recentPeriods > 0 && recentPeriods < m_History.Count
                ? m_History.Skip(m_History.Count - recentPeriods)
                : m_History;
            return items.Sum(selector);
        }

        public List<double> GetSeries(Func<StatisticSnapshot, double> selector, int recentPeriods = -1)
        {
            var items = recentPeriods > 0 && recentPeriods < m_History.Count
                ? m_History.Skip(m_History.Count - recentPeriods)
                : m_History;
            return items.Select(selector).ToList();
        }

        public List<string> GetTimeLabels(int recentPeriods = -1)
        {
            var items = recentPeriods > 0 && recentPeriods < m_History.Count
                ? m_History.Skip(m_History.Count - recentPeriods)
                : m_History;
            return items.Select(s => $"Y{s.GameYear}M{s.GameMonth}").ToList();
        }

        public string GetTrendDescription(Func<StatisticSnapshot, double> selector)
        {
            var rate = GetGrowthRate(selector);
            if (rate > 10) return "显著增长";
            if (rate > 3) return "稳步增长";
            if (rate > 0) return "略有增长";
            if (rate > -3) return "基本持平";
            if (rate > -10) return "有所下降";
            return "显著下降";
        }
    }
}