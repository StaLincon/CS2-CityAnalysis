using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DataAnalyzer.Models;

namespace DataAnalyzer.Services
{
    public class DataReader
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly string m_DataPath;

        public DataReader(string dataPath)
        {
            m_DataPath = dataPath;
        }

        public StatisticSnapshot ReadCurrentSnapshot()
        {
            var filePath = Path.Combine(m_DataPath, "current_snapshot.json");
            if (!File.Exists(filePath)) return null;

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<StatisticSnapshot>(json, JsonOptions);
        }

        public FullHistory ReadFullHistory()
        {
            var filePath = Path.Combine(m_DataPath, "full_history.json");
            if (!File.Exists(filePath)) return null;

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<FullHistory>(json, JsonOptions);
        }

        public List<StatisticSnapshot> ReadHistory()
        {
            var filePath = Path.Combine(m_DataPath, "history.json");
            if (!File.Exists(filePath)) return new List<StatisticSnapshot>();

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<StatisticSnapshot>>(json, JsonOptions) ?? new List<StatisticSnapshot>();
        }

        public List<StatisticSnapshot> BuildSnapshotsFromFullHistory(FullHistory full)
        {
            if (full?.Population == null || full.Population.Count == 0)
                return new List<StatisticSnapshot>();

            var count = full.Population.Count;
            var kUpdatesPerDay = full.KUpdatesPerDay > 0 ? full.KUpdatesPerDay : 32;
            var daysPerYear = full.DaysPerYear > 0 ? full.DaysPerYear : 12;
            // 使用统计系统每天更新次数计算每天的样本数
            var samplesPerDay = kUpdatesPerDay;

            // 检测 Mod 导出的时间数据是否有效（如果所有月份都相同，说明数据无效）
            bool hasValidTimeData = full.GameMonth != null && full.GameMonth.Count == count
                && full.GameMonth.Distinct().Count() > 1;

            var snaps = new List<StatisticSnapshot>(count);

            for (int i = 0; i < count; i++)
            {
                int gameYear, gameMonth, gameDay;

                if (hasValidTimeData && full.GameYear != null && i < full.GameYear.Count)
                {
                    // 使用 Mod 导出的真实时间数据
                    gameYear = full.GameYear[i];
                    gameMonth = i < full.GameMonth.Count ? full.GameMonth[i] : 1;
                    gameDay = full.GameDay != null && i < full.GameDay.Count ? full.GameDay[i] : gameMonth;
                }
                else
                {
                    // 回退：通过样本索引计算时间
                    int totalDays = i / samplesPerDay;
                    gameYear = totalDays / daysPerYear;
                    int dayInYear = totalDays % daysPerYear;
                    gameMonth = dayInYear + 1;
                    gameDay = dayInYear + 1;
                }

                var s = new StatisticSnapshot
                {
                    Population = Val(full.Population, i),
                    CitizensMovedIn = Val(full.CitizensMovedIn, i),
                    CitizensMovedAway = Val(full.CitizensMovedAway, i),
                    BirthRate = Val(full.BirthRate, i),
                    DeathRate = Val(full.DeathRate, i),
                    Income = Val(full.Income, i),
                    Expense = Val(full.Expense, i),
                    Trade = Val(full.Trade, i),
                    AverageHappiness = Val(full.Wellbeing, i),
                    AverageHealth = Val(full.Health, i),
                    HomelessCount = Val(full.HomelessCount, i),
                    WorkerCount = Val(full.WorkerCount, i),
                    Unemployed = Val(full.Unemployed, i),
                    TouristCount = Val(full.TouristCount, i),
                    TouristIncome = Val(full.TouristIncome, i),
                    LodgingUsed = Val(full.LodgingUsed, i),
                    LodgingTotal = Val(full.LodgingTotal, i),
                    CrimeRate = Val(full.CrimeRate, i),
                    CrimeCount = Val(full.CrimeCount, i),
                    PassengerCountBus = Val(full.PassengerCountBus, i),
                    PassengerCountSubway = Val(full.PassengerCountSubway, i),
                    PassengerCountTrain = Val(full.PassengerCountTrain, i),
                    PassengerCountTram = Val(full.PassengerCountTram, i),
                    PassengerCountAirplane = Val(full.PassengerCountAirplane, i),
                    ResidentialTaxableIncome = Val(full.ResidentialTaxableIncome, i),
                    CommercialTaxableIncome = Val(full.CommercialTaxableIncome, i),
                    IndustrialTaxableIncome = Val(full.IndustrialTaxableIncome, i),
                    OfficeTaxableIncome = Val(full.OfficeTaxableIncome, i),
                    EducationCount = Val(full.EducationCount, i),
                    AdultsCount = Val(full.AdultsCount, i),
                    Age = Val(full.Age, i),
                    CollectedMail = Val(full.CollectedMail, i),
                    DeliveredMail = Val(full.DeliveredMail, i),
                    PassengerCountTaxi = Val(full.PassengerCountTaxi, i),
                    PassengerCountShip = Val(full.PassengerCountShip, i),
                    CargoCountTruck = Val(full.CargoCountTruck, i),
                    CargoCountTrain = Val(full.CargoCountTrain, i),
                    CargoCountShip = Val(full.CargoCountShip, i),
                    CargoCountAirplane = Val(full.CargoCountAirplane, i),
                    ServiceWealth = Val(full.ServiceWealth, i),
                    ServiceCount = Val(full.ServiceCount, i),
                    ServiceWorkers = Val(full.ServiceWorkers, i),
                    ServiceMaxWorkers = Val(full.ServiceMaxWorkers, i),
                    ProcessingWealth = Val(full.ProcessingWealth, i),
                    ProcessingCount = Val(full.ProcessingCount, i),
                    ProcessingWorkers = Val(full.ProcessingWorkers, i),
                    ProcessingMaxWorkers = Val(full.ProcessingMaxWorkers, i),
                    OfficeWealth = Val(full.OfficeWealth, i),
                    OfficeCount = Val(full.OfficeCount, i),
                    OfficeWorkers = Val(full.OfficeWorkers, i),
                    OfficeMaxWorkers = Val(full.OfficeMaxWorkers, i),
                    CityServiceWorkers = Val(full.CityServiceWorkers, i),
                    CityServiceMaxWorkers = Val(full.CityServiceMaxWorkers, i),
                    HouseholdWealth = Val(full.HouseholdWealth, i),
                    HouseholdCount = Val(full.HouseholdCount, i),
                    SeniorWorkerInDemandPercentage = Val(full.SeniorWorkerInDemandPercentage, i),
                    EscapedArrestCount = Val(full.EscapedArrestCount, i),
                    WellbeingLevel = Val(full.WellbeingLevel, i),
                    HealthLevel = Val(full.HealthLevel, i),
                    SampleCount = full.TotalSamples,
                    GameDay = gameDay,
                    GameMonth = gameMonth,
                    GameYear = gameYear,
                    DaysPerYear = daysPerYear,
                };
                snaps.Add(s);
            }

            return snaps;
        }

        private static int Val(List<int> list, int i)
        {
            if (list == null || i >= list.Count) return 0;
            return list[i];
        }
    }
}