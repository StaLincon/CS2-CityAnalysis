using System.Collections.Generic;

namespace DataAnalyzer.Models
{
    public class CityAnalysisReport
    {
        public CityOverview Overview { get; set; } = new();
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
    }

    public class CityOverview
    {
        public int Population { get; set; }
        public long Money { get; set; }
        public double Happiness { get; set; }
        public double Health { get; set; }
        public int GameYear { get; set; }
        public int GameMonth { get; set; }
        public int TotalSamples { get; set; }
        public int KUpdatesPerDay { get; set; }
        public string Summary { get; set; }
    }

    public class DemographicAnalysis
    {
        public int Population { get; set; }
        public int PopulationWithMoveIn { get; set; }
        public double GrowthRate { get; set; }
        public double MovingAverage3 { get; set; }
        public double MovingAverage5 { get; set; }
        public int CitizensMovedIn { get; set; }
        public int CitizensMovedAway { get; set; }
        public int NetMigration => CitizensMovedIn - CitizensMovedAway;
        public int BirthRate { get; set; }
        public int DeathRate { get; set; }
        public int NaturalGrowth => BirthRate - DeathRate;
        public int AdultsCount { get; set; }
        public int Age { get; set; }
        public double AdultsRatio { get; set; }
    }

    public class EconomicAnalysis
    {
        public long Money { get; set; }
        public double MoneyGrowth { get; set; }
        public int Income { get; set; }
        public int Expense { get; set; }
        public int NetIncome => Income - Expense;
        public double ProfitMargin { get; set; }
        public int Trade { get; set; }
        public int DevTreePoints { get; set; }

        public double PerCapitaIncome { get; set; }
        public double PerCapitaExpense { get; set; }
        public double PerCapitaTax { get; set; }

        public int ResidentialTax { get; set; }
        public int CommercialTax { get; set; }
        public int IndustrialTax { get; set; }
        public int OfficeTax { get; set; }
        public int TotalTax => ResidentialTax + CommercialTax + IndustrialTax + OfficeTax;

        public double ResidentialTaxPct { get; set; }
        public double CommercialTaxPct { get; set; }
        public double IndustrialTaxPct { get; set; }
        public double OfficeTaxPct { get; set; }
    }

    public class SectorAnalysis
    {
        public SectorInfo Service { get; set; } = new();
        public SectorInfo Processing { get; set; } = new();
        public SectorInfo Office { get; set; } = new();

        public int TotalWealth => Service.Wealth + Processing.Wealth + Office.Wealth;
        public int TotalCount => Service.Count + Processing.Count + Office.Count;
        public int TotalWorkers => Service.Workers + Processing.Workers + Office.Workers;
        public int TotalMaxWorkers => Service.MaxWorkers + Processing.MaxWorkers + Office.MaxWorkers;

        public double ServiceWealthPct { get; set; }
        public double ProcessingWealthPct { get; set; }
        public double OfficeWealthPct { get; set; }

        public double ServiceWorkerFillRate { get; set; }
        public double ProcessingWorkerFillRate { get; set; }
        public double OfficeWorkerFillRate { get; set; }
    }

    public class SectorInfo
    {
        public int Wealth { get; set; }
        public int Count { get; set; }
        public int Workers { get; set; }
        public int MaxWorkers { get; set; }
    }

    public class EmploymentAnalysis
    {
        public int WorkerCount { get; set; }
        public int Unemployed { get; set; }
        public double UnemploymentRate { get; set; }
        public double WorkforceParticipation { get; set; }
        public int CityServiceWorkers { get; set; }
        public int CityServiceMaxWorkers { get; set; }
        public double CityServiceFillRate { get; set; }
        public double SeniorWorkerDemand { get; set; }
    }

    public class TransportAnalysis
    {
        public TransportMode Bus { get; set; } = new();
        public TransportMode Subway { get; set; } = new();
        public TransportMode Tram { get; set; } = new();
        public TransportMode Train { get; set; } = new();
        public TransportMode Taxi { get; set; } = new();
        public TransportMode Airplane { get; set; } = new();
        public TransportMode Ship { get; set; } = new();

        public int TotalPassengers { get; set; }
        public double PublicTransitShare { get; set; }

        public int CargoTruck { get; set; }
        public int CargoTrain { get; set; }
        public int CargoShip { get; set; }
        public int CargoAirplane { get; set; }
        public int TotalCargo { get; set; }
    }

    public class TransportMode
    {
        public int Passengers { get; set; }
        public double Share { get; set; }
    }

    public class SocialAnalysis
    {
        public double Wellbeing { get; set; }
        public double Health { get; set; }
        public int WellbeingLevel { get; set; }
        public int HealthLevel { get; set; }
        public double CrimeRate { get; set; }
        public int CrimeCount { get; set; }
        public int EscapedArrestCount { get; set; }
        public int HomelessCount { get; set; }
        public double HomelessPerCapita { get; set; }

        public int EducationCount { get; set; }
        public double EducationRate { get; set; }
        public int CollectedMail { get; set; }
        public int DeliveredMail { get; set; }

        public double QualityOfLifeIndex { get; set; }

        public double CompositeHappinessIndex { get; set; }

        public double CompositeHealthIndex { get; set; }
    }

    public class FiscalAnalysis
    {
        public double RevenueExpenseRatio { get; set; }
        public double TaxToIncomeRatio { get; set; }
        public double TradeToIncomeRatio { get; set; }
        public bool IsSurplus => RevenueExpenseRatio > 1.0;
        public string FiscalStatus { get; set; }
    }

    public class HouseholdAnalysis
    {
        public int HouseholdCount { get; set; }
        public int HouseholdWealth { get; set; }
        public double AvgWealthPerHousehold { get; set; }
        public double AvgPersonsPerHousehold { get; set; }
    }

    public class TrendSummary
    {
        public double PopGrowthRate { get; set; }
        public double IncomeGrowthRate { get; set; }
        public double ExpenseGrowthRate { get; set; }
        public double HappinessTrend { get; set; }
        public double HealthTrend { get; set; }
        public double CrimeTrend { get; set; }
        public double TourismTrend { get; set; }

        public string PopMomentum { get; set; }
        public string EconomyMomentum { get; set; }
        public string SocialMomentum { get; set; }
    }

    public class AlertItem
    {
        public string Level { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public double Value { get; set; }
        public double Threshold { get; set; }
    }

    public class ScoreCard
    {
        public string Category { get; set; }
        public string Name { get; set; }
        public double Score { get; set; }
        public string Grade { get; set; }
        public string Description { get; set; }
    }
}