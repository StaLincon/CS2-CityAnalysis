using System;
using System.Collections.Generic;
using System.Linq;

namespace analysis.Data
{
    public static class SnapshotSerializer
    {
        public static string SerializeSnapshot(StatisticSnapshot s)
        {
            return "{" +
                $"\"realTime\":\"{s.RealTime:O}\"," +
                $"\"gameTick\":{s.GameTick}," +
                $"\"gameDay\":{s.GameDay}," +
                $"\"gameMonth\":{s.GameMonth}," +
                $"\"gameYear\":{s.GameYear}," +
                $"\"daysPerYear\":{s.DaysPerYear}," +
                $"\"sampleCount\":{s.SampleCount}," +
                $"\"population\":{s.Population}," +
                $"\"populationWithMoveIn\":{s.PopulationWithMoveIn}," +
                $"\"currentTourists\":{s.CurrentTourists}," +
                $"\"averageTourists\":{s.AverageTourists}," +
                $"\"attractiveness\":{s.Attractiveness}," +
                $"\"devTreePoints\":{s.DevTreePoints}," +
                $"\"citizensMovedIn\":{s.CitizensMovedIn}," +
                $"\"citizensMovedAway\":{s.CitizensMovedAway}," +
                $"\"birthRate\":{s.BirthRate}," +
                $"\"deathRate\":{s.DeathRate}," +
                $"\"money\":{s.Money}," +
                $"\"income\":{s.Income}," +
                $"\"expense\":{s.Expense}," +
                $"\"trade\":{s.Trade}," +
                $"\"averageHappiness\":{s.AverageHappiness}," +
                $"\"averageHealth\":{s.AverageHealth}," +
                $"\"homelessCount\":{s.HomelessCount}," +
                $"\"workerCount\":{s.WorkerCount}," +
                $"\"unemployed\":{s.Unemployed}," +
                $"\"touristCount\":{s.TouristCount}," +
                $"\"touristIncome\":{s.TouristIncome}," +
                $"\"lodgingUsed\":{s.LodgingUsed}," +
                $"\"lodgingTotal\":{s.LodgingTotal}," +
                $"\"crimeRate\":{s.CrimeRate}," +
                $"\"crimeCount\":{s.CrimeCount}," +
                $"\"passengerCountBus\":{s.PassengerCountBus}," +
                $"\"passengerCountSubway\":{s.PassengerCountSubway}," +
                $"\"passengerCountTrain\":{s.PassengerCountTrain}," +
                $"\"passengerCountTram\":{s.PassengerCountTram}," +
                $"\"passengerCountAirplane\":{s.PassengerCountAirplane}," +
                $"\"residentialTaxableIncome\":{s.ResidentialTaxableIncome}," +
                $"\"commercialTaxableIncome\":{s.CommercialTaxableIncome}," +
                $"\"industrialTaxableIncome\":{s.IndustrialTaxableIncome}," +
                $"\"officeTaxableIncome\":{s.OfficeTaxableIncome}," +
                $"\"educationCount\":{s.EducationCount}," +
                $"\"adultsCount\":{s.AdultsCount}," +
                $"\"age\":{s.Age}," +
                $"\"collectedMail\":{s.CollectedMail}," +
                $"\"deliveredMail\":{s.DeliveredMail}," +
                $"\"passengerCountTaxi\":{s.PassengerCountTaxi}," +
                $"\"passengerCountShip\":{s.PassengerCountShip}," +
                $"\"cargoCountTruck\":{s.CargoCountTruck}," +
                $"\"cargoCountTrain\":{s.CargoCountTrain}," +
                $"\"cargoCountShip\":{s.CargoCountShip}," +
                $"\"cargoCountAirplane\":{s.CargoCountAirplane}," +
                $"\"serviceWealth\":{s.ServiceWealth}," +
                $"\"serviceCount\":{s.ServiceCount}," +
                $"\"serviceWorkers\":{s.ServiceWorkers}," +
                $"\"serviceMaxWorkers\":{s.ServiceMaxWorkers}," +
                $"\"processingWealth\":{s.ProcessingWealth}," +
                $"\"processingCount\":{s.ProcessingCount}," +
                $"\"processingWorkers\":{s.ProcessingWorkers}," +
                $"\"processingMaxWorkers\":{s.ProcessingMaxWorkers}," +
                $"\"officeWealth\":{s.OfficeWealth}," +
                $"\"officeCount\":{s.OfficeCount}," +
                $"\"officeWorkers\":{s.OfficeWorkers}," +
                $"\"officeMaxWorkers\":{s.OfficeMaxWorkers}," +
                $"\"cityServiceWorkers\":{s.CityServiceWorkers}," +
                $"\"cityServiceMaxWorkers\":{s.CityServiceMaxWorkers}," +
                $"\"householdWealth\":{s.HouseholdWealth}," +
                $"\"householdCount\":{s.HouseholdCount}," +
                $"\"seniorWorkerInDemandPercentage\":{s.SeniorWorkerInDemandPercentage}," +
                $"\"escapedArrestCount\":{s.EscapedArrestCount}," +
                $"\"wellbeingLevel\":{s.WellbeingLevel}," +
                $"\"healthLevel\":{s.HealthLevel}" +
                "}";
        }

        public static string SerializeHistory(List<StatisticSnapshot> history)
        {
            var entries = history.Select(SerializeSnapshot);
            return "[" + string.Join(",", entries) + "]";
        }
    }
}