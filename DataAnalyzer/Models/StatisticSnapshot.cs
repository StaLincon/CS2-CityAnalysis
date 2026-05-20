using System;
using System.Collections.Generic;

namespace DataAnalyzer.Models
{
    public class StatisticSnapshot
    {
        public DateTime RealTime { get; set; }
        public ulong GameTick { get; set; }
        public int GameDay { get; set; }
        public int GameMonth { get; set; }
        public int GameYear { get; set; }
        public int DaysPerYear { get; set; }
        public int SampleCount { get; set; }

        public int Population { get; set; }
        public int PopulationWithMoveIn { get; set; }
        public int CitizensMovedIn { get; set; }
        public int CitizensMovedAway { get; set; }
        public int BirthRate { get; set; }
        public int DeathRate { get; set; }

        public int Money { get; set; }
        public int Income { get; set; }
        public int Expense { get; set; }
        public int Trade { get; set; }

        public int AverageHappiness { get; set; }
        public int AverageHealth { get; set; }
        public int HomelessCount { get; set; }

        public int WorkerCount { get; set; }
        public int Unemployed { get; set; }

        public int CurrentTourists { get; set; }
        public int AverageTourists { get; set; }
        public int TouristCount { get; set; }
        public int TouristIncome { get; set; }
        public int LodgingUsed { get; set; }
        public int LodgingTotal { get; set; }
        public int Attractiveness { get; set; }
        public int DevTreePoints { get; set; }

        public int CrimeRate { get; set; }
        public int CrimeCount { get; set; }

        public int PassengerCountBus { get; set; }
        public int PassengerCountSubway { get; set; }
        public int PassengerCountTrain { get; set; }
        public int PassengerCountTram { get; set; }
        public int PassengerCountAirplane { get; set; }

        public int ResidentialTaxableIncome { get; set; }
        public int CommercialTaxableIncome { get; set; }
        public int IndustrialTaxableIncome { get; set; }
        public int OfficeTaxableIncome { get; set; }

        public int EducationCount { get; set; }
        public int AdultsCount { get; set; }
        public int Age { get; set; }

        public int CollectedMail { get; set; }
        public int DeliveredMail { get; set; }

        public int PassengerCountTaxi { get; set; }
        public int PassengerCountShip { get; set; }

        public int CargoCountTruck { get; set; }
        public int CargoCountTrain { get; set; }
        public int CargoCountShip { get; set; }
        public int CargoCountAirplane { get; set; }

        public int ServiceWealth { get; set; }
        public int ServiceCount { get; set; }
        public int ServiceWorkers { get; set; }
        public int ServiceMaxWorkers { get; set; }

        public int ProcessingWealth { get; set; }
        public int ProcessingCount { get; set; }
        public int ProcessingWorkers { get; set; }
        public int ProcessingMaxWorkers { get; set; }

        public int OfficeWealth { get; set; }
        public int OfficeCount { get; set; }
        public int OfficeWorkers { get; set; }
        public int OfficeMaxWorkers { get; set; }

        public int CityServiceWorkers { get; set; }
        public int CityServiceMaxWorkers { get; set; }

        public int HouseholdWealth { get; set; }
        public int HouseholdCount { get; set; }

        public int SeniorWorkerInDemandPercentage { get; set; }
        public int EscapedArrestCount { get; set; }

        public int WellbeingLevel { get; set; }
        public int HealthLevel { get; set; }

        // AverageHappiness/AverageHealth 是游戏内所有市民幸福/健康值的累加和
        // 需要除以人口数才能得到真实的 0-100 百分比值
        public double Wellbeing => Population > 0 ? (double)AverageHappiness / Population : 0;
        public double Health => Population > 0 ? (double)AverageHealth / Population : 0;
        public double Education => EducationCount;

        // BirthRate/DeathRate 是月度绝对人数，不是千分比
        public int MonthlyBirths => BirthRate;
        public int MonthlyDeaths => DeathRate;
        // 真实千分比年率（‰）
        public double BirthRatePerMille => Population > 0 ? (double)BirthRate / Population * 1000 : 0;
        public double DeathRatePerMille => Population > 0 ? (double)DeathRate / Population * 1000 : 0;

        public double EmploymentRate => WorkerCount > 0 && Population > 0 ? Math.Min((double)WorkerCount / Population * 100, 100) : 0;

        // 月度净自然增长人数
        public int NaturalGrowthCount => BirthRate - DeathRate;

        public double HomelessRate => Population > 0 ? (double)HomelessCount / Population * 100 : 0;

        public double EducationRate => AdultsCount > 0 ? (double)EducationCount / AdultsCount * 100 : 0;
    }
}