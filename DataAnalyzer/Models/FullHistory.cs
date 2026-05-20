using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DataAnalyzer.Models
{
    public class FullHistory
    {
        public int TotalSamples { get; set; }
        [JsonPropertyName("kUpdatesPerDay")]
        public int KUpdatesPerDay { get; set; }
        [JsonPropertyName("kTicksPerDay")]
        public int KTicksPerDay { get; set; }
        public int DaysPerYear { get; set; }
        public string ExportTime { get; set; }

        public List<int> Population { get; set; }
        public List<int> CitizensMovedIn { get; set; }
        public List<int> CitizensMovedAway { get; set; }
        public List<int> BirthRate { get; set; }
        public List<int> DeathRate { get; set; }
        public List<int> Income { get; set; }
        public List<int> Expense { get; set; }
        public List<int> Trade { get; set; }
        public List<int> Wellbeing { get; set; }
        public List<int> Health { get; set; }
        public List<int> HomelessCount { get; set; }
        public List<int> WorkerCount { get; set; }
        public List<int> Unemployed { get; set; }
        public List<int> TouristCount { get; set; }
        public List<int> TouristIncome { get; set; }
        public List<int> LodgingUsed { get; set; }
        public List<int> LodgingTotal { get; set; }
        public List<int> CrimeRate { get; set; }
        public List<int> CrimeCount { get; set; }
        public List<int> PassengerCountBus { get; set; }
        public List<int> PassengerCountSubway { get; set; }
        public List<int> PassengerCountTrain { get; set; }
        public List<int> PassengerCountTram { get; set; }
        public List<int> PassengerCountAirplane { get; set; }
        public List<int> ResidentialTaxableIncome { get; set; }
        public List<int> CommercialTaxableIncome { get; set; }
        public List<int> IndustrialTaxableIncome { get; set; }
        public List<int> OfficeTaxableIncome { get; set; }

        public List<int> EducationCount { get; set; }
        public List<int> AdultsCount { get; set; }
        public List<int> Age { get; set; }
        public List<int> CollectedMail { get; set; }
        public List<int> DeliveredMail { get; set; }
        public List<int> PassengerCountTaxi { get; set; }
        public List<int> PassengerCountShip { get; set; }
        public List<int> CargoCountTruck { get; set; }
        public List<int> CargoCountTrain { get; set; }
        public List<int> CargoCountShip { get; set; }
        public List<int> CargoCountAirplane { get; set; }
        public List<int> ServiceWealth { get; set; }
        public List<int> ServiceCount { get; set; }
        public List<int> ServiceWorkers { get; set; }
        public List<int> ServiceMaxWorkers { get; set; }
        public List<int> ProcessingWealth { get; set; }
        public List<int> ProcessingCount { get; set; }
        public List<int> ProcessingWorkers { get; set; }
        public List<int> ProcessingMaxWorkers { get; set; }
        public List<int> OfficeWealth { get; set; }
        public List<int> OfficeCount { get; set; }
        public List<int> OfficeWorkers { get; set; }
        public List<int> OfficeMaxWorkers { get; set; }
        public List<int> CityServiceWorkers { get; set; }
        public List<int> CityServiceMaxWorkers { get; set; }
        public List<int> HouseholdWealth { get; set; }
        public List<int> HouseholdCount { get; set; }
        public List<int> SeniorWorkerInDemandPercentage { get; set; }
        public List<int> EscapedArrestCount { get; set; }
        public List<int> WellbeingLevel { get; set; }
        public List<int> HealthLevel { get; set; }
        
        // 时间数据字段
        [JsonPropertyName("gameYear")]
        public List<int> GameYear { get; set; }
        [JsonPropertyName("gameMonth")]
        public List<int> GameMonth { get; set; }
        [JsonPropertyName("gameDay")]
        public List<int> GameDay { get; set; }
    }
}