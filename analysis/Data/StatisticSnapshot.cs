using System;
using System.Collections.Generic;

namespace analysis.Data
{
    public class StatisticSnapshot
    {
        public DateTime RealTime;
        public ulong GameTick;
        public int GameDay;
        public int GameMonth;
        public int GameYear;
        public int DaysPerYear;
        public int SampleCount;

        public int Population;
        public int PopulationWithMoveIn;
        public int CurrentTourists;
        public int AverageTourists;
        public int Attractiveness;
        public int DevTreePoints;
        public int CitizensMovedIn;
        public int CitizensMovedAway;
        public int BirthRate;
        public int DeathRate;

        public int Money;
        public int Income;
        public int Expense;
        public int Trade;

        public int AverageHappiness;
        public int AverageHealth;
        public int HomelessCount;

        public int WorkerCount;
        public int Unemployed;

        public int TouristCount;
        public int TouristIncome;
        public int LodgingUsed;
        public int LodgingTotal;

        public int CrimeRate;
        public int CrimeCount;

        public int PassengerCountBus;
        public int PassengerCountSubway;
        public int PassengerCountTrain;
        public int PassengerCountTram;
        public int PassengerCountAirplane;

        public int ResidentialTaxableIncome;
        public int CommercialTaxableIncome;
        public int IndustrialTaxableIncome;
        public int OfficeTaxableIncome;

        public int EducationCount;
        public int AdultsCount;
        public int Age;

        public int CollectedMail;
        public int DeliveredMail;

        public int PassengerCountTaxi;
        public int PassengerCountShip;

        public int CargoCountTruck;
        public int CargoCountTrain;
        public int CargoCountShip;
        public int CargoCountAirplane;

        public int ServiceWealth;
        public int ServiceCount;
        public int ServiceWorkers;
        public int ServiceMaxWorkers;

        public int ProcessingWealth;
        public int ProcessingCount;
        public int ProcessingWorkers;
        public int ProcessingMaxWorkers;

        public int OfficeWealth;
        public int OfficeCount;
        public int OfficeWorkers;
        public int OfficeMaxWorkers;

        public int CityServiceWorkers;
        public int CityServiceMaxWorkers;

        public int HouseholdWealth;
        public int HouseholdCount;

        public int SeniorWorkerInDemandPercentage;
        public int EscapedArrestCount;

        public int WellbeingLevel;
        public int HealthLevel;

        public Dictionary<string, int> Extra;

        public StatisticSnapshot()
        {
            RealTime = DateTime.Now;
            Extra = new Dictionary<string, int>();
        }
    }
}