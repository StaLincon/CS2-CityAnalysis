using System;
using System.Collections.Generic;
using System.Linq;
using DataAnalyzer.Models;

namespace DataAnalyzer.Services
{
    public partial class AnalysisEngine
    {
        public DevelopmentReport AnalyzeDevelopment()
        {
            if (m_Current == null || m_History.Count == 0) return null;

            var report = new DevelopmentReport
            {
                Overview = AnalyzeOverview(),
                Demographics = AnalyzeDemographics(),
                Economy = AnalyzeEconomy(),
                Sectors = AnalyzeSectors(),
                Employment = AnalyzeEmployment(),
                Transport = AnalyzeTransport(),
                Social = AnalyzeSocial(),
                Fiscal = AnalyzeFiscal(),
                Households = AnalyzeHouseholds(),
                Trends = AnalyzeTrends(),
                Alerts = GenerateAlerts(),
                Scores = ComputeScores(),
                History = BuildHistorySummary(),
                StageTransition = BuildStageTransition(),
                YearSnapshots = BuildYearSnapshots()
            };

            return report;
        }

        private HistorySummary BuildHistorySummary()
        {
            var first = m_History[0];
            var last = m_History[^1];
            var daysPerYear = last.DaysPerYear > 0 ? last.DaysPerYear : 12;

            var totalDays = 0.0;
            if (m_History.Count > 1)
            {
                var samplesPerDay = m_KUpdatesPerDay > 0 ? m_KUpdatesPerDay : 32;
                totalDays = (double)(m_History.Count - 1) / samplesPerDay;
            }

            var totalMonths = (int)(totalDays);

            // 寻找第一个非零人口的快照作为基准点（跳过建市前的空数据点）
            var baseline = m_History.FirstOrDefault(s => s.Population > 0) ?? first;

            var summary = new HistorySummary
            {
                FirstGameYear = first.GameYear,
                FirstGameMonth = first.GameMonth,
                LastGameYear = last.GameYear,
                LastGameMonth = last.GameMonth,
                TotalDays = Math.Max(1, totalMonths),
                TotalMonths = Math.Max(1, (int)(totalDays / daysPerYear * daysPerYear + totalDays % daysPerYear)),
                DataPoints = m_History.Count,
                FirstPopulation = baseline.Population,
                LastPopulation = last.Population,
                FirstMoney = baseline.Money,
                LastMoney = last.Money,
                PopGrowthTotal = baseline.Population > 0
                    ? (double)(last.Population - baseline.Population) / baseline.Population * 100 : 0,
                MoneyGrowthTotal = baseline.Money > 0
                    ? (double)(last.Money - baseline.Money) / baseline.Money * 100 : 0
            };

            // 使用归一化的幸福度/健康度（0-100范围）计算历史峰值
            var peakHappiness = m_History.OrderByDescending(s => s.Wellbeing).First();
            summary.PeakHappiness = peakHappiness.Wellbeing;
            summary.PeakHappinessYear = peakHappiness.GameYear;
            summary.PeakHappinessMonth = peakHappiness.GameMonth;

            var peakHealth = m_History.OrderByDescending(s => s.Health).First();
            summary.PeakHealth = peakHealth.Health;
            summary.PeakHealthYear = peakHealth.GameYear;
            summary.PeakHealthMonth = peakHealth.GameMonth;

            return summary;
        }

        private StageTransition BuildStageTransition()
        {
            var stages = new (int threshold, string name)[]
            {
                (1000, "初创期"),
                (5000, "发展初期"),
                (20000, "快速成长期"),
                (50000, "发展中期"),
                (100000, "成熟发展期"),
                (int.MaxValue, "大都市阶段")
            };

            var first = m_History[0];
            var currentStage = GetStageName(first.Population);
            var transition = new StageTransition
            {
                FromStage = currentStage,
                ToStage = currentStage
            };

            foreach (var snap in m_History)
            {
                var stage = GetStageName(snap.Population);
                if (stage != currentStage)
                {
                    transition.ToStage = stage;
                    transition.TransitionYear = snap.GameYear;
                    transition.TransitionMonth = snap.GameMonth;
                    transition.PopulationAtTransition = snap.Population;
                    break;
                }
            }

            return transition;
        }

        private static string GetStageName(int pop)
        {
            if (pop < 1000) return "初创期";
            if (pop < 5000) return "发展初期";
            if (pop < 20000) return "快速成长期";
            if (pop < 50000) return "发展中期";
            if (pop < 100000) return "成熟发展期";
            return "大都市阶段";
        }

        private List<YearSnapshot> BuildYearSnapshots()
        {
            var snapshots = new List<YearSnapshot>();
            var grouped = m_History.GroupBy(s => s.GameYear).OrderBy(g => g.Key);

            foreach (var yearGroup in grouped)
            {
                var last = yearGroup.Last();
                var laborForce = last.WorkerCount + last.Unemployed;
                var tp = last.PassengerCountBus + last.PassengerCountSubway + last.PassengerCountTram
                       + last.PassengerCountTrain + last.PassengerCountTaxi
                       + last.PassengerCountAirplane + last.PassengerCountShip;
                var tc = last.CargoCountTruck + last.CargoCountTrain + last.CargoCountShip + last.CargoCountAirplane;

                snapshots.Add(new YearSnapshot
                {
                    GameYear = yearGroup.Key,
                    GameMonth = last.GameMonth,
                    Population = last.Population,
                    Money = last.Money,
                    Happiness = last.Wellbeing,
                    Health = last.Health,
                    Income = last.Income,
                    Expense = last.Expense,
                    WorkerCount = last.WorkerCount,
                    UnemploymentRate = laborForce > 0 ? (double)last.Unemployed / laborForce * 100 : 0,
                    CrimeRate = last.CrimeRate,
                    TotalPassengers = tp,
                    TotalCargo = tc
                });
            }

            return snapshots;
        }

        }
}