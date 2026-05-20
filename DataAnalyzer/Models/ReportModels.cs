using System;
using System.Collections.Generic;

namespace DataAnalyzer.Models
{
    public enum ReportTemplate
    {
        Development,
        Quarterly
    }

    public class DevelopmentReport
    {
        public CityOverview Overview { get; set; } = new();
        public HistorySummary History { get; set; } = new();
        public StageTransition StageTransition { get; set; } = new();
        public DemographicAnalysis Demographics { get; set; } = new();
        public EconomicAnalysis Economy { get; set; } = new();
        public SectorAnalysis Sectors { get; set; } = new();
        public EmploymentAnalysis Employment { get; set; } = new();
        public TransportAnalysis Transport { get; set; } = new();
        public SocialAnalysis Social { get; set; } = new();
        public FiscalAnalysis Fiscal { get; set; } = new();
        public HouseholdAnalysis Households { get; set; } = new();
        public TrendSummary Trends { get; set; } = new();
        public List<AlertItem> Alerts { get; set; } = new();
        public List<ScoreCard> Scores { get; set; } = new();
        public List<YearSnapshot> YearSnapshots { get; set; } = new();
    }

    public class HistorySummary
    {
        public int FirstGameYear { get; set; }
        public int FirstGameMonth { get; set; }
        public int LastGameYear { get; set; }
        public int LastGameMonth { get; set; }
        public int TotalDays { get; set; }
        public int TotalMonths { get; set; }
        public int DataPoints { get; set; }
        public int FirstPopulation { get; set; }
        public int LastPopulation { get; set; }
        public long FirstMoney { get; set; }
        public long LastMoney { get; set; }
        public double PopGrowthTotal { get; set; }
        public double MoneyGrowthTotal { get; set; }
        public double PeakHappiness { get; set; }
        public int PeakHappinessYear { get; set; }
        public int PeakHappinessMonth { get; set; }
        public double PeakHealth { get; set; }
        public int PeakHealthYear { get; set; }
        public int PeakHealthMonth { get; set; }
    }

    public class StageTransition
    {
        public string FromStage { get; set; }
        public string ToStage { get; set; }
        public int TransitionYear { get; set; }
        public int TransitionMonth { get; set; }
        public int PopulationAtTransition { get; set; }
    }

    public class YearSnapshot
    {
        public int GameYear { get; set; }
        public int GameMonth { get; set; }
        public int Population { get; set; }
        public long Money { get; set; }
        public double Happiness { get; set; }
        public double Health { get; set; }
        public int Income { get; set; }
        public int Expense { get; set; }
        public int WorkerCount { get; set; }
        public double UnemploymentRate { get; set; }
        public double CrimeRate { get; set; }
        public int TotalPassengers { get; set; }
        public int TotalCargo { get; set; }
    }

    public class ReportContent
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
    }

    public class ReportGenerationResult
    {
        public string OutputPath { get; set; }
        public string CityName { get; set; }
        public ReportTemplate Template { get; set; }
        public string Summary { get; set; }
    }
}