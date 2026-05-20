using System;
using System.Collections.Generic;
using System.Linq;
using DataAnalyzer.Models;

namespace DataAnalyzer.Services
{
    public partial class AnalysisEngine
    {
        private readonly StatisticSnapshot m_Current;
        private readonly List<StatisticSnapshot> m_History;
        private readonly int m_KUpdatesPerDay;
        private readonly int m_TotalSamples;

        public AnalysisEngine(StatisticSnapshot current, List<StatisticSnapshot> history, int kUpdatesPerDay = 8192)
        {
            m_Current = current;
            m_History = history;
            m_KUpdatesPerDay = kUpdatesPerDay;
            m_TotalSamples = current?.SampleCount ?? 0;
        }

        public CityAnalysisReport Analyze()
        {
            if (m_Current == null) return null;

            var report = new CityAnalysisReport
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
                Scores = ComputeScores()
            };

            report.Overview.Summary = GenerateOverviewSummary(report);
            return report;
        }

        private CityOverview AnalyzeOverview()
        {
            return new CityOverview
            {
                Population = m_Current.Population,
                Money = m_Current.Money,
                Happiness = m_Current.Wellbeing,
                Health = m_Current.Health,
                GameYear = m_Current.GameYear,
                GameMonth = m_Current.GameMonth,
                TotalSamples = m_TotalSamples,
                KUpdatesPerDay = m_KUpdatesPerDay
            };
        }

        private DemographicAnalysis AnalyzeDemographics()
        {
            var pop = m_Current.Population;
            return new DemographicAnalysis
            {
                Population = pop,
                PopulationWithMoveIn = m_Current.PopulationWithMoveIn,
                GrowthRate = Growth(s => s.Population),
                MovingAverage3 = MovingAverage(s => s.Population, 3),
                MovingAverage5 = MovingAverage(s => s.Population, 5),
                CitizensMovedIn = m_Current.CitizensMovedIn,
                CitizensMovedAway = m_Current.CitizensMovedAway,
                BirthRate = m_Current.BirthRate,
                DeathRate = m_Current.DeathRate,
                AdultsCount = m_Current.AdultsCount,
                Age = m_Current.Age,
                AdultsRatio = pop > 0 ? (double)m_Current.AdultsCount / pop * 100 : 0
            };
        }

        private EconomicAnalysis AnalyzeEconomy()
        {
            var pop = m_Current.Population;
            var totalTax = m_Current.ResidentialTaxableIncome + m_Current.CommercialTaxableIncome
                + m_Current.IndustrialTaxableIncome + m_Current.OfficeTaxableIncome;

            return new EconomicAnalysis
            {
                Money = m_Current.Money,
                MoneyGrowth = Growth(s => s.Money),
                Income = m_Current.Income,
                Expense = m_Current.Expense,
                ProfitMargin = m_Current.Income > 0 ? (double)(m_Current.Income - m_Current.Expense) / m_Current.Income * 100 : 0,
                Trade = m_Current.Trade,
                DevTreePoints = m_Current.DevTreePoints,
                PerCapitaIncome = pop > 0 ? (double)m_Current.Income / pop : 0,
                PerCapitaExpense = pop > 0 ? (double)m_Current.Expense / pop : 0,
                PerCapitaTax = pop > 0 ? (double)totalTax / pop : 0,
                ResidentialTax = m_Current.ResidentialTaxableIncome,
                CommercialTax = m_Current.CommercialTaxableIncome,
                IndustrialTax = m_Current.IndustrialTaxableIncome,
                OfficeTax = m_Current.OfficeTaxableIncome,
                ResidentialTaxPct = totalTax > 0 ? (double)m_Current.ResidentialTaxableIncome / totalTax * 100 : 0,
                CommercialTaxPct = totalTax > 0 ? (double)m_Current.CommercialTaxableIncome / totalTax * 100 : 0,
                IndustrialTaxPct = totalTax > 0 ? (double)m_Current.IndustrialTaxableIncome / totalTax * 100 : 0,
                OfficeTaxPct = totalTax > 0 ? (double)m_Current.OfficeTaxableIncome / totalTax * 100 : 0
            };
        }

        private SectorAnalysis AnalyzeSectors()
        {
            var totalWealth = m_Current.ServiceWealth + m_Current.ProcessingWealth + m_Current.OfficeWealth;
            return new SectorAnalysis
            {
                Service = new SectorInfo
                {
                    Wealth = m_Current.ServiceWealth,
                    Count = m_Current.ServiceCount,
                    Workers = m_Current.ServiceWorkers,
                    MaxWorkers = m_Current.ServiceMaxWorkers
                },
                Processing = new SectorInfo
                {
                    Wealth = m_Current.ProcessingWealth,
                    Count = m_Current.ProcessingCount,
                    Workers = m_Current.ProcessingWorkers,
                    MaxWorkers = m_Current.ProcessingMaxWorkers
                },
                Office = new SectorInfo
                {
                    Wealth = m_Current.OfficeWealth,
                    Count = m_Current.OfficeCount,
                    Workers = m_Current.OfficeWorkers,
                    MaxWorkers = m_Current.OfficeMaxWorkers
                },
                ServiceWealthPct = totalWealth > 0 ? (double)m_Current.ServiceWealth / totalWealth * 100 : 0,
                ProcessingWealthPct = totalWealth > 0 ? (double)m_Current.ProcessingWealth / totalWealth * 100 : 0,
                OfficeWealthPct = totalWealth > 0 ? (double)m_Current.OfficeWealth / totalWealth * 100 : 0,
                ServiceWorkerFillRate = m_Current.ServiceMaxWorkers > 0 ? (double)m_Current.ServiceWorkers / m_Current.ServiceMaxWorkers * 100 : 0,
                ProcessingWorkerFillRate = m_Current.ProcessingMaxWorkers > 0 ? (double)m_Current.ProcessingWorkers / m_Current.ProcessingMaxWorkers * 100 : 0,
                OfficeWorkerFillRate = m_Current.OfficeMaxWorkers > 0 ? (double)m_Current.OfficeWorkers / m_Current.OfficeMaxWorkers * 100 : 0
            };
        }

        private EmploymentAnalysis AnalyzeEmployment()
        {
            var laborForce = m_Current.WorkerCount + m_Current.Unemployed;
            var pop = m_Current.Population;
            return new EmploymentAnalysis
            {
                WorkerCount = m_Current.WorkerCount,
                Unemployed = m_Current.Unemployed,
                UnemploymentRate = laborForce > 0 ? (double)m_Current.Unemployed / laborForce * 100 : 0,
                WorkforceParticipation = pop > 0 ? (double)m_Current.WorkerCount / pop * 100 : 0,
                CityServiceWorkers = m_Current.CityServiceWorkers,
                CityServiceMaxWorkers = m_Current.CityServiceMaxWorkers,
                CityServiceFillRate = m_Current.CityServiceMaxWorkers > 0 ? (double)m_Current.CityServiceWorkers / m_Current.CityServiceMaxWorkers * 100 : 0,
                SeniorWorkerDemand = m_Current.SeniorWorkerInDemandPercentage
            };
        }

        private TransportAnalysis AnalyzeTransport()
        {
            var tp = m_Current.PassengerCountBus + m_Current.PassengerCountSubway + m_Current.PassengerCountTram
                   + m_Current.PassengerCountTrain + m_Current.PassengerCountTaxi
                   + m_Current.PassengerCountAirplane + m_Current.PassengerCountShip;
            var pub = m_Current.PassengerCountBus + m_Current.PassengerCountSubway
                    + m_Current.PassengerCountTram + m_Current.PassengerCountTrain;
            var tc = m_Current.CargoCountTruck + m_Current.CargoCountTrain + m_Current.CargoCountShip + m_Current.CargoCountAirplane;

            return new TransportAnalysis
            {
                Bus = new TransportMode { Passengers = m_Current.PassengerCountBus, Share = tp > 0 ? (double)m_Current.PassengerCountBus / tp * 100 : 0 },
                Subway = new TransportMode { Passengers = m_Current.PassengerCountSubway, Share = tp > 0 ? (double)m_Current.PassengerCountSubway / tp * 100 : 0 },
                Tram = new TransportMode { Passengers = m_Current.PassengerCountTram, Share = tp > 0 ? (double)m_Current.PassengerCountTram / tp * 100 : 0 },
                Train = new TransportMode { Passengers = m_Current.PassengerCountTrain, Share = tp > 0 ? (double)m_Current.PassengerCountTrain / tp * 100 : 0 },
                Taxi = new TransportMode { Passengers = m_Current.PassengerCountTaxi, Share = tp > 0 ? (double)m_Current.PassengerCountTaxi / tp * 100 : 0 },
                Airplane = new TransportMode { Passengers = m_Current.PassengerCountAirplane, Share = tp > 0 ? (double)m_Current.PassengerCountAirplane / tp * 100 : 0 },
                Ship = new TransportMode { Passengers = m_Current.PassengerCountShip, Share = tp > 0 ? (double)m_Current.PassengerCountShip / tp * 100 : 0 },
                TotalPassengers = tp,
                PublicTransitShare = tp > 0 ? (double)pub / tp * 100 : 0,
                CargoTruck = m_Current.CargoCountTruck,
                CargoTrain = m_Current.CargoCountTrain,
                CargoShip = m_Current.CargoCountShip,
                CargoAirplane = m_Current.CargoCountAirplane,
                TotalCargo = tc
            };
        }

        private SocialAnalysis AnalyzeSocial()
        {
            var pop = m_Current.Population;
            var adults = m_Current.AdultsCount;
            var workers = m_Current.WorkerCount;
            
            var employmentRate = pop > 0 && workers > 0 ? Math.Min((double)workers / pop * 100, 100) : 0;
            var educationRate = adults > 0 ? (double)m_Current.EducationCount / adults * 100 : 0;
            var homelessRate = pop > 0 ? (double)m_Current.HomelessCount / pop * 100 : 0;
            var naturalGrowth = m_Current.NaturalGrowthCount;
            
            return new SocialAnalysis
            {
                Wellbeing = m_Current.Wellbeing,
                Health = m_Current.Health,
                WellbeingLevel = m_Current.WellbeingLevel,
                HealthLevel = m_Current.HealthLevel,
                CrimeRate = m_Current.CrimeRate,
                CrimeCount = m_Current.CrimeCount,
                EscapedArrestCount = m_Current.EscapedArrestCount,
                HomelessCount = m_Current.HomelessCount,
                HomelessPerCapita = pop > 0 ? (double)m_Current.HomelessCount / pop * 1000 : 0,
                EducationCount = m_Current.EducationCount,
                EducationRate = educationRate,
                CollectedMail = m_Current.CollectedMail,
                DeliveredMail = m_Current.DeliveredMail,
                QualityOfLifeIndex = ComputeQoL(),
                CompositeHappinessIndex = ComputeCompositeHappinessIndex(employmentRate, educationRate, homelessRate),
                CompositeHealthIndex = ComputeCompositeHealthIndex(naturalGrowth, educationRate, homelessRate)
            };
        }

        private double ComputeCompositeHappinessIndex(double employmentRate, double educationRate, double homelessRate)
        {
            var baseWellbeing = Math.Min(m_Current.Wellbeing, 100) / 100.0;
            var employmentScore = Math.Min(employmentRate, 100) / 100.0;
            var crimeScore = Math.Max(0, (50 - m_Current.CrimeRate) / 50.0);
            var educationScore = Math.Min(educationRate, 100) / 100.0;
            var homelessScore = Math.Max(0, (10 - homelessRate) / 10.0);

            var happinessIndex = (baseWellbeing * 0.35 + 
                                  employmentScore * 0.25 + 
                                  crimeScore * 0.20 + 
                                  educationScore * 0.15 + 
                                  homelessScore * 0.05) * 100;

            return Math.Round(Math.Max(0, Math.Min(happinessIndex, 100)), 1);
        }

        private double ComputeCompositeHealthIndex(double naturalGrowth, double educationRate, double homelessRate)
        {
            var baseHealth = Math.Min(m_Current.Health, 100) / 100.0;
            var growthScore = Math.Max(0, Math.Min((naturalGrowth + 50) / 100.0, 1));
            var homelessScore = Math.Max(0, (10 - homelessRate) / 10.0);
            var educationScore = Math.Min(educationRate, 100) / 100.0;
            var serviceScore = m_Current.CityServiceWorkers > 0 ? 
                Math.Min((double)m_Current.CityServiceWorkers / Math.Max(m_Current.CityServiceMaxWorkers, 1) * 100 / 100.0, 1) : 0;

            var healthIndex = (baseHealth * 0.40 + 
                               growthScore * 0.20 + 
                               homelessScore * 0.15 + 
                               educationScore * 0.15 + 
                               serviceScore * 0.10) * 100;

            return Math.Round(Math.Max(0, Math.Min(healthIndex, 100)), 1);
        }

        private double ComputeQoL()
        {
            var wellScore = Math.Min(m_Current.Wellbeing, 100) / 100.0;
            var healthScore = Math.Min(m_Current.Health, 100) / 100.0;
            var crimePenalty = Math.Max(0, (10 - m_Current.CrimeRate) / 10.0);
            var homelessPenalty = Math.Max(0, 1 - (m_Current.Population > 0 ? (double)m_Current.HomelessCount / m_Current.Population * 100 : 0));
            return Math.Round((wellScore * 0.35 + healthScore * 0.35 + crimePenalty * 0.15 + homelessPenalty * 0.15) * 100, 1);
        }

        private FiscalAnalysis AnalyzeFiscal()
        {
            var totalTax = m_Current.ResidentialTaxableIncome + m_Current.CommercialTaxableIncome
                + m_Current.IndustrialTaxableIncome + m_Current.OfficeTaxableIncome;

            var ratio = m_Current.Expense > 0 ? (double)m_Current.Income / m_Current.Expense : 100;
            string status;
            if (ratio >= 1.5) status = "Highly Surplus";
            else if (ratio >= 1.1) status = "Surplus";
            else if (ratio >= 1.0) status = "Balanced";
            else if (ratio >= 0.9) status = "Mild Deficit";
            else if (ratio >= 0.7) status = "Deficit";
            else status = "Severe Deficit";

            return new FiscalAnalysis
            {
                RevenueExpenseRatio = Math.Round(ratio, 2),
                TaxToIncomeRatio = m_Current.Income > 0 ? Math.Round((double)totalTax / m_Current.Income * 100, 1) : 0,
                TradeToIncomeRatio = m_Current.Income > 0 ? Math.Round((double)m_Current.Trade / m_Current.Income * 100, 1) : 0,
                FiscalStatus = status
            };
        }

        private HouseholdAnalysis AnalyzeHouseholds()
        {
            var pop = m_Current.Population;
            return new HouseholdAnalysis
            {
                HouseholdCount = m_Current.HouseholdCount,
                HouseholdWealth = m_Current.HouseholdWealth,
                AvgWealthPerHousehold = m_Current.HouseholdCount > 0 ? (double)m_Current.HouseholdWealth / m_Current.HouseholdCount : 0,
                AvgPersonsPerHousehold = m_Current.HouseholdCount > 0 ? (double)pop / m_Current.HouseholdCount : 0
            };
        }

        private TrendSummary AnalyzeTrends()
        {
            return new TrendSummary
            {
                PopGrowthRate = Growth(s => s.Population),
                IncomeGrowthRate = Growth(s => s.Income),
                ExpenseGrowthRate = Growth(s => s.Expense),
                HappinessTrend = Growth(s => s.Wellbeing),
                HealthTrend = Growth(s => s.Health),
                CrimeTrend = Growth(s => s.CrimeRate),
                TourismTrend = Growth(s => s.TouristCount),
                PopMomentum = Momentum(Growth(s => s.Population)),
                EconomyMomentum = Momentum(Growth(s => s.Income)),
                SocialMomentum = SocialMomentumCalc()
            };
        }

        private string SocialMomentumCalc()
        {
            var h = Growth(s => s.Wellbeing);
            var c = Growth(s => s.CrimeRate);
            if (h > 2 && c < -2) return "Rising - social conditions improving";
            if (h > 0 && c < 5) return "Stable - social conditions steady";
            if (h < -5 || c > 10) return "Declining - social conditions worsening";
            return "Mixed - some indicators diverging";
        }

        private List<AlertItem> GenerateAlerts()
        {
            var alerts = new List<AlertItem>();

            var unemploymentRate = m_Current.WorkerCount + m_Current.Unemployed > 0
                ? (double)m_Current.Unemployed / (m_Current.WorkerCount + m_Current.Unemployed) * 100 : 0;

            if (m_Current.CrimeRate > 20)
                alerts.Add(new AlertItem { Level = "danger", Category = "Public Safety", Message = $"Crime rate at {m_Current.CrimeRate:F1}% exceeds safe threshold", Value = m_Current.CrimeRate, Threshold = 20 });
            else if (m_Current.CrimeRate > 10)
                alerts.Add(new AlertItem { Level = "warning", Category = "Public Safety", Message = $"Crime rate elevated at {m_Current.CrimeRate:F1}%", Value = m_Current.CrimeRate, Threshold = 10 });

            if (m_Current.HomelessCount > 0 && m_Current.Population > 0 && (double)m_Current.HomelessCount / m_Current.Population > 0.05)
                alerts.Add(new AlertItem { Level = "warning", Category = "Social Welfare", Message = $"Homeless rate {(double)m_Current.HomelessCount / m_Current.Population * 100:F1}% requires attention", Value = m_Current.HomelessCount, Threshold = m_Current.Population / 20 });

            if (Growth(s => s.Population) < -5)
                alerts.Add(new AlertItem { Level = "danger", Category = "Demographics", Message = "Population declining rapidly!", Value = Growth(s => s.Population), Threshold = -5 });

            if (m_Current.Income > 0 && m_Current.Expense > m_Current.Income * 1.1)
                alerts.Add(new AlertItem { Level = "danger", Category = "Fiscal", Message = "Severe budget deficit - expenses exceed revenue by over 10%", Value = m_Current.Expense - m_Current.Income, Threshold = m_Current.Income / 10 });

            if (unemploymentRate > 15)
                alerts.Add(new AlertItem { Level = "danger", Category = "Employment", Message = $"Unemployment at {unemploymentRate:F1}% - critical level", Value = unemploymentRate, Threshold = 15 });
            else if (unemploymentRate > 8)
                alerts.Add(new AlertItem { Level = "warning", Category = "Employment", Message = $"Unemployment elevated at {unemploymentRate:F1}%", Value = unemploymentRate, Threshold = 8 });

            if (m_Current.Wellbeing < 40)
                alerts.Add(new AlertItem { Level = "danger", Category = "Wellbeing", Message = $"Citizen happiness critically low at {m_Current.Wellbeing:F0}%", Value = m_Current.Wellbeing, Threshold = 40 });

            if (m_Current.Health < 40)
                alerts.Add(new AlertItem { Level = "danger", Category = "Health", Message = $"Citizen health critically low at {m_Current.Health:F0}%", Value = m_Current.Health, Threshold = 40 });

            if (m_Current.SeniorWorkerInDemandPercentage > 80)
                alerts.Add(new AlertItem { Level = "warning", Category = "Labor Market", Message = $"Senior worker demand at {m_Current.SeniorWorkerInDemandPercentage:F1}% - may indicate skills gap", Value = m_Current.SeniorWorkerInDemandPercentage, Threshold = 80 });

            return alerts;
        }

        private List<ScoreCard> ComputeScores()
        {
            var pop = m_Current.Population;
            return new List<ScoreCard>
            {
                Score("Economy", "Budget Health", m_Current.Expense > 0 ? Math.Min((double)m_Current.Income / m_Current.Expense * 50, 100) : 100, "Revenue-to-expense ratio"),
                Score("Economy", "Growth Momentum", Math.Max(0, Math.Min(Growth(s => s.Population) + 50, 100)), "Population growth trend"),
                Score("Society", "Wellbeing", Math.Min(m_Current.Wellbeing, 100), "Citizen happiness level"),
                Score("Society", "Public Health", Math.Min(m_Current.Health, 100), "Citizen health level"),
                Score("Society", "Education", Math.Min(pop > 0 ? (double)m_Current.EducationCount / pop * 200 : 0, 100), "Education coverage rate"),
                Score("Safety", "Crime Control", Math.Max(0, 100 - m_Current.CrimeRate), "Low crime = high score"),
                Score("Employment", "Job Market", Math.Max(0, 100 - (m_Current.WorkerCount + m_Current.Unemployed > 0 ? (double)m_Current.Unemployed / (m_Current.WorkerCount + m_Current.Unemployed) * 500 : 0)), "Low unemployment = high score"),
                Score("Living", "Housing", Math.Max(0, 100 - (pop > 0 ? (double)m_Current.HomelessCount / pop * 2000 : 0)), "Low homelessness = high score")
            };
        }

        private ScoreCard Score(string cat, string name, double score, string desc)
        {
            return new ScoreCard
            {
                Category = cat,
                Name = name,
                Score = Math.Round(Math.Clamp(score, 0, 100), 1),
                Grade = score >= 80 ? "A" : score >= 65 ? "B" : score >= 50 ? "C" : score >= 35 ? "D" : "F",
                Description = desc
            };
        }

        private string GenerateOverviewSummary(CityAnalysisReport r)
        {
            var parts = new List<string>();
            parts.Add($"{r.Overview.Population:N0} residents, {r.Overview.GameYear}Y {r.Overview.GameMonth}M");

            if (r.Economy.NetIncome > 0)
                parts.Add($"monthly surplus ₡{r.Economy.NetIncome:N0}");
            else
                parts.Add($"monthly deficit ₡{-r.Economy.NetIncome:N0}");

            parts.Add($"QoL index {r.Social.QualityOfLifeIndex:F1}/100");

            if (r.Alerts.Any(a => a.Level == "danger"))
                parts.Add($"⚠ {r.Alerts.Count(a => a.Level == "danger")} critical issues");
            else if (r.Alerts.Any(a => a.Level == "warning"))
                parts.Add($"⚡ {r.Alerts.Count(a => a.Level == "warning")} warnings");
            else
                parts.Add("all indicators normal");

            return string.Join(" | ", parts);
        }

        private double Growth(Func<StatisticSnapshot, double> selector)
        {
            if (m_History.Count < 2) return 0;
            var cur = selector(m_History[^1]);
            var prev = selector(m_History[^2]);
            if (Math.Abs(prev) < 1) return cur > 0 ? 100 : 0;
            return (cur - prev) / prev * 100;
        }

        private double MovingAverage(Func<StatisticSnapshot, double> selector, int window)
        {
            if (m_History.Count == 0) return 0;
            if (m_History.Count < window) return selector(m_History[^1]);
            var vals = m_History.TakeLast(window).Select(selector);
            return vals.Average();
        }

        private static string Momentum(double growth)
        {
            if (growth > 5) return "Strong Growth";
            if (growth > 1) return "Moderate Growth";
            if (growth > 0) return "Stable";
            if (growth > -1) return "Slight Decline";
            if (growth > -5) return "Declining";
            return "Rapid Decline";
        }
    }
}