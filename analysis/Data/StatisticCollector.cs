using System.Collections.Generic;
using System.IO;
using Colossal.Logging;
using Game;
using Game.City;
using Game.Common;
using Game.Simulation;
using System;
using Unity.Collections;
using Unity.Entities;

namespace analysis.Data
{
    public class StatisticCollector
    {
        private readonly ILog m_Log;
        private World m_World;
        private CityStatisticsSystem m_StatisticsSystem;
        private CitySystem m_CitySystem;
        private TimeSystem m_TimeSystem;
        private SimulationSystem m_SimulationSystem;
        private EntityQuery m_CityQuery;
        private bool m_Initialized;

        public StatisticSnapshot CurrentSnapshot { get; private set; }

        public StatisticCollector()
        {
            m_Log = LogManager.GetLogger($"{nameof(analysis)}.{nameof(StatisticCollector)}");
        }

        public void Initialize(World world)
        {
            m_World = world;

            m_StatisticsSystem = world.GetExistingSystemManaged<CityStatisticsSystem>();
            m_CitySystem = world.GetExistingSystemManaged<CitySystem>();
            m_TimeSystem = world.GetExistingSystemManaged<TimeSystem>();
            m_SimulationSystem = world.GetExistingSystemManaged<SimulationSystem>();

            var entityManager = world.EntityManager;
            m_CityQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<City>(),
                ComponentType.ReadOnly<Population>(),
                ComponentType.ReadOnly<Tourism>(),
                ComponentType.ReadOnly<DevTreePoints>()
            );

            if (m_StatisticsSystem != null && m_CitySystem != null && m_TimeSystem != null)
            {
                m_Initialized = true;
                m_Log.Info("StatisticCollector initialized - using game CityStatisticsSystem");
            }
            else
            {
                m_Log.Error($"Init failed: StatsSys={m_StatisticsSystem != null} CitySys={m_CitySystem != null} TimeSys={m_TimeSystem != null}");
            }
        }

        public StatisticSnapshot Collect()
        {
            if (!m_Initialized || m_World == null || !m_World.IsCreated)
                return null;

            try
            {
                var snapshot = new StatisticSnapshot();

                snapshot.Population = m_StatisticsSystem.GetStatisticValue(StatisticType.Population);
                snapshot.CitizensMovedIn = m_StatisticsSystem.GetStatisticValue(StatisticType.CitizensMovedIn);
                snapshot.CitizensMovedAway = m_StatisticsSystem.GetStatisticValue(StatisticType.CitizensMovedAway);
                snapshot.BirthRate = m_StatisticsSystem.GetStatisticValue(StatisticType.BirthRate);
                snapshot.DeathRate = m_StatisticsSystem.GetStatisticValue(StatisticType.DeathRate);

                snapshot.Money = m_CitySystem.moneyAmount;
                snapshot.Income = m_StatisticsSystem.GetStatisticValue(StatisticType.Income);
                snapshot.Expense = m_StatisticsSystem.GetStatisticValue(StatisticType.Expense);
                snapshot.Trade = m_StatisticsSystem.GetStatisticValue(StatisticType.Trade);

                snapshot.AverageHappiness = m_StatisticsSystem.GetStatisticValue(StatisticType.Wellbeing);
                snapshot.AverageHealth = m_StatisticsSystem.GetStatisticValue(StatisticType.Health);
                snapshot.HomelessCount = m_StatisticsSystem.GetStatisticValue(StatisticType.HomelessCount);

                snapshot.WorkerCount = m_StatisticsSystem.GetStatisticValue(StatisticType.WorkerCount);
                snapshot.Unemployed = m_StatisticsSystem.GetStatisticValue(StatisticType.Unemployed);

                snapshot.TouristCount = m_StatisticsSystem.GetStatisticValue(StatisticType.TouristCount);
                snapshot.TouristIncome = m_StatisticsSystem.GetStatisticValue(StatisticType.TouristIncome);
                snapshot.LodgingUsed = m_StatisticsSystem.GetStatisticValue(StatisticType.LodgingUsed);
                snapshot.LodgingTotal = m_StatisticsSystem.GetStatisticValue(StatisticType.LodgingTotal);

                snapshot.CrimeRate = m_StatisticsSystem.GetStatisticValue(StatisticType.CrimeRate);
                snapshot.CrimeCount = m_StatisticsSystem.GetStatisticValue(StatisticType.CrimeCount);

                snapshot.PassengerCountBus = m_StatisticsSystem.GetStatisticValue(StatisticType.PassengerCountBus);
                snapshot.PassengerCountSubway = m_StatisticsSystem.GetStatisticValue(StatisticType.PassengerCountSubway);
                snapshot.PassengerCountTrain = m_StatisticsSystem.GetStatisticValue(StatisticType.PassengerCountTrain);
                snapshot.PassengerCountTram = m_StatisticsSystem.GetStatisticValue(StatisticType.PassengerCountTram);
                snapshot.PassengerCountAirplane = m_StatisticsSystem.GetStatisticValue(StatisticType.PassengerCountAirplane);

                snapshot.ResidentialTaxableIncome = m_StatisticsSystem.GetStatisticValue(StatisticType.ResidentialTaxableIncome);
                snapshot.CommercialTaxableIncome = m_StatisticsSystem.GetStatisticValue(StatisticType.CommercialTaxableIncome);
                snapshot.IndustrialTaxableIncome = m_StatisticsSystem.GetStatisticValue(StatisticType.IndustrialTaxableIncome);
                snapshot.OfficeTaxableIncome = m_StatisticsSystem.GetStatisticValue(StatisticType.OfficeTaxableIncome);

                snapshot.EducationCount = m_StatisticsSystem.GetStatisticValue(StatisticType.EducationCount);
                snapshot.AdultsCount = m_StatisticsSystem.GetStatisticValue(StatisticType.AdultsCount);
                snapshot.Age = m_StatisticsSystem.GetStatisticValue(StatisticType.Age);

                snapshot.CollectedMail = m_StatisticsSystem.GetStatisticValue(StatisticType.CollectedMail);
                snapshot.DeliveredMail = m_StatisticsSystem.GetStatisticValue(StatisticType.DeliveredMail);

                snapshot.PassengerCountTaxi = m_StatisticsSystem.GetStatisticValue(StatisticType.PassengerCountTaxi);
                snapshot.PassengerCountShip = m_StatisticsSystem.GetStatisticValue(StatisticType.PassengerCountShip);

                snapshot.CargoCountTruck = m_StatisticsSystem.GetStatisticValue(StatisticType.CargoCountTruck);
                snapshot.CargoCountTrain = m_StatisticsSystem.GetStatisticValue(StatisticType.CargoCountTrain);
                snapshot.CargoCountShip = m_StatisticsSystem.GetStatisticValue(StatisticType.CargoCountShip);
                snapshot.CargoCountAirplane = m_StatisticsSystem.GetStatisticValue(StatisticType.CargoCountAirplane);

                snapshot.ServiceWealth = m_StatisticsSystem.GetStatisticValue(StatisticType.ServiceWealth);
                snapshot.ServiceCount = m_StatisticsSystem.GetStatisticValue(StatisticType.ServiceCount);
                snapshot.ServiceWorkers = m_StatisticsSystem.GetStatisticValue(StatisticType.ServiceWorkers);
                snapshot.ServiceMaxWorkers = m_StatisticsSystem.GetStatisticValue(StatisticType.ServiceMaxWorkers);

                snapshot.ProcessingWealth = m_StatisticsSystem.GetStatisticValue(StatisticType.ProcessingWealth);
                snapshot.ProcessingCount = m_StatisticsSystem.GetStatisticValue(StatisticType.ProcessingCount);
                snapshot.ProcessingWorkers = m_StatisticsSystem.GetStatisticValue(StatisticType.ProcessingWorkers);
                snapshot.ProcessingMaxWorkers = m_StatisticsSystem.GetStatisticValue(StatisticType.ProcessingMaxWorkers);

                snapshot.OfficeWealth = m_StatisticsSystem.GetStatisticValue(StatisticType.OfficeWealth);
                snapshot.OfficeCount = m_StatisticsSystem.GetStatisticValue(StatisticType.OfficeCount);
                snapshot.OfficeWorkers = m_StatisticsSystem.GetStatisticValue(StatisticType.OfficeWorkers);
                snapshot.OfficeMaxWorkers = m_StatisticsSystem.GetStatisticValue(StatisticType.OfficeMaxWorkers);

                snapshot.CityServiceWorkers = m_StatisticsSystem.GetStatisticValue(StatisticType.CityServiceWorkers);
                snapshot.CityServiceMaxWorkers = m_StatisticsSystem.GetStatisticValue(StatisticType.CityServiceMaxWorkers);

                snapshot.HouseholdWealth = m_StatisticsSystem.GetStatisticValue(StatisticType.HouseholdWealth);
                snapshot.HouseholdCount = m_StatisticsSystem.GetStatisticValue(StatisticType.HouseholdCount);

                snapshot.SeniorWorkerInDemandPercentage = m_StatisticsSystem.GetStatisticValue(StatisticType.SeniorWorkerInDemandPercentage);
                snapshot.EscapedArrestCount = m_StatisticsSystem.GetStatisticValue(StatisticType.EscapedArrestCount);

                snapshot.WellbeingLevel = m_StatisticsSystem.GetStatisticValue(StatisticType.WellbeingLevel);
                snapshot.HealthLevel = m_StatisticsSystem.GetStatisticValue(StatisticType.HealthLevel);

                // 使用TimeSystem直接获取精确的游戏时间
                var gameDateTime = m_TimeSystem.GetCurrentDateTime();
                snapshot.GameYear = gameDateTime.Year;
                snapshot.GameMonth = gameDateTime.Month;
                snapshot.GameDay = gameDateTime.Day;
                snapshot.DaysPerYear = m_TimeSystem.daysPerYear;
                snapshot.GameTick = m_SimulationSystem.frameIndex;

                if (!m_CityQuery.IsEmptyIgnoreFilter)
                {
                    var entityManager = m_World.EntityManager;
                    var cityEntity = m_CityQuery.GetSingletonEntity();
                    var population = entityManager.GetComponentData<Population>(cityEntity);
                    var tourism = entityManager.GetComponentData<Tourism>(cityEntity);
                    var devPoints = entityManager.GetComponentData<DevTreePoints>(cityEntity);

                    snapshot.PopulationWithMoveIn = population.m_PopulationWithMoveIn;
                    snapshot.CurrentTourists = tourism.m_CurrentTourists;
                    snapshot.AverageTourists = tourism.m_AverageTourists;
                    snapshot.Attractiveness = tourism.m_Attractiveness;
                    snapshot.DevTreePoints = devPoints.m_Points;
                }

                snapshot.RealTime = DateTime.Now;
                snapshot.SampleCount = m_StatisticsSystem.sampleCount;

                CurrentSnapshot = snapshot;
                return snapshot;
            }
            catch (System.Exception ex)
            {
                m_Log.Error($"Collect failed: {ex.Message}");
                return null;
            }
        }

        public void ExportFullHistory(string dataPath)
        {
            if (!m_Initialized) return;

            try
            {
                m_Log.Info($"Exporting full history... (sampleCount={m_StatisticsSystem.sampleCount})");

                var allHistory = new Dictionary<string, List<int>>();
                var totalSamples = m_StatisticsSystem.sampleCount;

                ExportStat(allHistory, "population", StatisticType.Population);
                ExportStat(allHistory, "citizensMovedIn", StatisticType.CitizensMovedIn);
                ExportStat(allHistory, "citizensMovedAway", StatisticType.CitizensMovedAway);
                ExportStat(allHistory, "birthRate", StatisticType.BirthRate);
                ExportStat(allHistory, "deathRate", StatisticType.DeathRate);
                ExportStat(allHistory, "income", StatisticType.Income);
                ExportStat(allHistory, "expense", StatisticType.Expense);
                ExportStat(allHistory, "trade", StatisticType.Trade);
                ExportStat(allHistory, "wellbeing", StatisticType.Wellbeing);
                ExportStat(allHistory, "health", StatisticType.Health);
                ExportStat(allHistory, "homelessCount", StatisticType.HomelessCount);
                ExportStat(allHistory, "workerCount", StatisticType.WorkerCount);
                ExportStat(allHistory, "unemployed", StatisticType.Unemployed);
                ExportStat(allHistory, "touristCount", StatisticType.TouristCount);
                ExportStat(allHistory, "touristIncome", StatisticType.TouristIncome);
                ExportStat(allHistory, "lodgingUsed", StatisticType.LodgingUsed);
                ExportStat(allHistory, "lodgingTotal", StatisticType.LodgingTotal);
                ExportStat(allHistory, "crimeRate", StatisticType.CrimeRate);
                ExportStat(allHistory, "crimeCount", StatisticType.CrimeCount);
                ExportStat(allHistory, "passengerCountBus", StatisticType.PassengerCountBus);
                ExportStat(allHistory, "passengerCountSubway", StatisticType.PassengerCountSubway);
                ExportStat(allHistory, "passengerCountTrain", StatisticType.PassengerCountTrain);
                ExportStat(allHistory, "passengerCountTram", StatisticType.PassengerCountTram);
                ExportStat(allHistory, "passengerCountAirplane", StatisticType.PassengerCountAirplane);
                ExportStat(allHistory, "residentialTaxableIncome", StatisticType.ResidentialTaxableIncome);
                ExportStat(allHistory, "commercialTaxableIncome", StatisticType.CommercialTaxableIncome);
                ExportStat(allHistory, "industrialTaxableIncome", StatisticType.IndustrialTaxableIncome);
                ExportStat(allHistory, "officeTaxableIncome", StatisticType.OfficeTaxableIncome);

                ExportStat(allHistory, "educationCount", StatisticType.EducationCount);
                ExportStat(allHistory, "adultsCount", StatisticType.AdultsCount);
                ExportStat(allHistory, "age", StatisticType.Age);
                ExportStat(allHistory, "collectedMail", StatisticType.CollectedMail);
                ExportStat(allHistory, "deliveredMail", StatisticType.DeliveredMail);
                ExportStat(allHistory, "passengerCountTaxi", StatisticType.PassengerCountTaxi);
                ExportStat(allHistory, "passengerCountShip", StatisticType.PassengerCountShip);
                ExportStat(allHistory, "cargoCountTruck", StatisticType.CargoCountTruck);
                ExportStat(allHistory, "cargoCountTrain", StatisticType.CargoCountTrain);
                ExportStat(allHistory, "cargoCountShip", StatisticType.CargoCountShip);
                ExportStat(allHistory, "cargoCountAirplane", StatisticType.CargoCountAirplane);
                ExportStat(allHistory, "serviceWealth", StatisticType.ServiceWealth);
                ExportStat(allHistory, "serviceCount", StatisticType.ServiceCount);
                ExportStat(allHistory, "serviceWorkers", StatisticType.ServiceWorkers);
                ExportStat(allHistory, "serviceMaxWorkers", StatisticType.ServiceMaxWorkers);
                ExportStat(allHistory, "processingWealth", StatisticType.ProcessingWealth);
                ExportStat(allHistory, "processingCount", StatisticType.ProcessingCount);
                ExportStat(allHistory, "processingWorkers", StatisticType.ProcessingWorkers);
                ExportStat(allHistory, "processingMaxWorkers", StatisticType.ProcessingMaxWorkers);
                ExportStat(allHistory, "officeWealth", StatisticType.OfficeWealth);
                ExportStat(allHistory, "officeCount", StatisticType.OfficeCount);
                ExportStat(allHistory, "officeWorkers", StatisticType.OfficeWorkers);
                ExportStat(allHistory, "officeMaxWorkers", StatisticType.OfficeMaxWorkers);
                ExportStat(allHistory, "cityServiceWorkers", StatisticType.CityServiceWorkers);
                ExportStat(allHistory, "cityServiceMaxWorkers", StatisticType.CityServiceMaxWorkers);
                ExportStat(allHistory, "householdWealth", StatisticType.HouseholdWealth);
                ExportStat(allHistory, "householdCount", StatisticType.HouseholdCount);
                ExportStat(allHistory, "seniorWorkerInDemandPercentage", StatisticType.SeniorWorkerInDemandPercentage);
                ExportStat(allHistory, "escapedArrestCount", StatisticType.EscapedArrestCount);
                ExportStat(allHistory, "wellbeingLevel", StatisticType.WellbeingLevel);
                ExportStat(allHistory, "healthLevel", StatisticType.HealthLevel);

                // 导出时间数据
                ExportTimeData(allHistory, totalSamples);

                var json = SerializeHistory(allHistory, totalSamples);
                Directory.CreateDirectory(dataPath);
                File.WriteAllText(Path.Combine(dataPath, "full_history.json"), json);

                m_Log.Info($"Full history exported: {totalSamples} samples, {allHistory.Count} metrics");
            }
            catch (Exception ex)
            {
                m_Log.Error($"Export full history failed: {ex.Message}");
            }
        }

        private void ExportStat(Dictionary<string, List<int>> target, string key, StatisticType type, int parameter = 0)
        {
            var data = m_StatisticsSystem.GetStatisticDataArray(type, parameter);
            if (data.Length > 0)
                target[key] = new List<int>(data);
        }

        private void ExportTimeData(Dictionary<string, List<int>> target, int totalSamples)
        {
            if (totalSamples <= 0) return;

            var gameYears = new List<int>(totalSamples);
            var gameMonths = new List<int>(totalSamples);
            var gameDays = new List<int>(totalSamples);

            // 获取 TimeData 用于自行计算历史时间
            // 注意：TimeSystem.GetDateTime(frameIndex) 的 renderingFrame 参数被内部忽略，
            // 始终返回当前时间，因此不能使用它来获取历史样本的时间。
            // 我们使用 TimeSystem.GetDay 静态方法根据帧索引计算经过的天数，再推算年月日。
            var timeDataQuery = m_World.EntityManager.CreateEntityQuery(
                Unity.Entities.ComponentType.ReadOnly<Game.Common.TimeData>()
            );
            var timeData = Game.Common.TimeData.GetSingleton(timeDataQuery);
            timeDataQuery.Dispose();

            int daysPerYear = m_TimeSystem.daysPerYear;
            int startingYear = timeData.m_StartingYear;
            int startingMonth = timeData.m_StartingMonth;

            for (int i = 0; i < totalSamples; i++)
            {
                uint frameIndex = m_StatisticsSystem.GetSampleFrameIndex(i);

                // 使用 TimeSystem.GetDay 计算从起始帧开始经过的天数
                int totalDays = TimeSystem.GetDay(frameIndex, timeData);

                // 计算年份和月份
                int yearOffset = totalDays / daysPerYear;
                int dayInYear = totalDays % daysPerYear;
                int month = dayInYear + 1; // dayInYear 是 0-based，month 是 1-based

                int gameYear = startingYear + yearOffset;
                int gameMonth = month;
                int gameDay = dayInYear + 1; // 简化：用年内天数作为日

                gameYears.Add(gameYear);
                gameMonths.Add(gameMonth);
                gameDays.Add(gameDay);
            }

            target["gameYear"] = gameYears;
            target["gameMonth"] = gameMonths;
            target["gameDay"] = gameDays;

            m_Log.Info($"Exported time data: {totalSamples} samples, year range {gameYears[0]}-{gameYears[gameYears.Count - 1]}, month range {gameMonths[0]}-{gameMonths[gameMonths.Count - 1]}");
        }

        private string SerializeHistory(Dictionary<string, List<int>> history, int totalSamples)
        {
            var parts = new List<string>();
            parts.Add($"\"totalSamples\":{totalSamples}");
            parts.Add($"\"kUpdatesPerDay\":{CityStatisticsSystem.kUpdatesPerDay}");
            parts.Add($"\"kTicksPerDay\":{TimeSystem.kTicksPerDay}");
            parts.Add($"\"daysPerYear\":{m_TimeSystem.daysPerYear}");
            parts.Add($"\"exportTime\":\"{DateTime.Now:O}\"");

            foreach (var kv in history)
            {
                var values = string.Join(",", kv.Value);
                parts.Add($"\"{kv.Key}\":[{values}]");
            }

            return "{" + string.Join(",", parts) + "}";
        }

        public void Dispose()
        {
            m_World = null;
            m_StatisticsSystem = null;
            m_CitySystem = null;
            m_TimeSystem = null;
            m_Initialized = false;
        }
    }
}