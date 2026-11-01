using System.Collections.Generic;
using System.IO;
using Colossal.Logging;
using Game;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Economy;
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
        private readonly Dictionary<StatisticType, HashSet<int>> m_Parameters = new Dictionary<StatisticType, HashSet<int>>();

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
                RefreshParameters();
                m_StatisticsSystem.CompleteWriters();
                var snapshot = new StatisticSnapshot();

                snapshot.Population = ReadValue(StatisticType.Population);
                snapshot.CitizensMovedIn = ReadValue(StatisticType.CitizensMovedIn);
                snapshot.CitizensMovedAway = ReadValue(StatisticType.CitizensMovedAway);
                snapshot.BirthRate = ReadValue(StatisticType.BirthRate);
                snapshot.DeathRate = ReadValue(StatisticType.DeathRate);

                snapshot.Money = m_CitySystem.moneyAmount;
                snapshot.Income = ReadValue(StatisticType.Income);
                snapshot.Expense = ReadValue(StatisticType.Expense);
                snapshot.Trade = ReadValue(StatisticType.Trade);

                snapshot.AverageHappiness = StatisticMath.Average(ReadTotal(StatisticType.Wellbeing), snapshot.Population);
                snapshot.AverageHealth = StatisticMath.Average(ReadTotal(StatisticType.Health), snapshot.Population);
                snapshot.HomelessCount = ReadValue(StatisticType.HomelessCount);

                snapshot.WorkerCount = ReadValue(StatisticType.WorkerCount);
                snapshot.Unemployed = ReadValue(StatisticType.Unemployed);

                snapshot.TouristCount = ReadValue(StatisticType.TouristCount);
                snapshot.TouristIncome = ReadValue(StatisticType.TouristIncome);
                snapshot.LodgingUsed = ReadValue(StatisticType.LodgingUsed);
                snapshot.LodgingTotal = ReadValue(StatisticType.LodgingTotal);

                snapshot.CrimeRate = ReadValue(StatisticType.CrimeRate);
                snapshot.CrimeCount = ReadValue(StatisticType.CrimeCount);

                snapshot.PassengerCountBus = ReadValue(StatisticType.PassengerCountBus);
                snapshot.PassengerCountSubway = ReadValue(StatisticType.PassengerCountSubway);
                snapshot.PassengerCountTrain = ReadValue(StatisticType.PassengerCountTrain);
                snapshot.PassengerCountTram = ReadValue(StatisticType.PassengerCountTram);
                snapshot.PassengerCountAirplane = ReadValue(StatisticType.PassengerCountAirplane);

                snapshot.ResidentialTaxableIncome = ReadValue(StatisticType.ResidentialTaxableIncome);
                snapshot.CommercialTaxableIncome = ReadValue(StatisticType.CommercialTaxableIncome);
                snapshot.IndustrialTaxableIncome = ReadValue(StatisticType.IndustrialTaxableIncome);
                snapshot.OfficeTaxableIncome = ReadValue(StatisticType.OfficeTaxableIncome);

                snapshot.EducationCount = ReadValue(StatisticType.EducationCount);
                snapshot.AdultsCount = ReadValue(StatisticType.AdultsCount);
                snapshot.Age = ReadValue(StatisticType.Age);

                snapshot.CollectedMail = ReadValue(StatisticType.CollectedMail);
                snapshot.DeliveredMail = ReadValue(StatisticType.DeliveredMail);

                snapshot.PassengerCountTaxi = ReadValue(StatisticType.PassengerCountTaxi);
                snapshot.PassengerCountShip = ReadValue(StatisticType.PassengerCountShip);

                snapshot.CargoCountTruck = ReadValue(StatisticType.CargoCountTruck);
                snapshot.CargoCountTrain = ReadValue(StatisticType.CargoCountTrain);
                snapshot.CargoCountShip = ReadValue(StatisticType.CargoCountShip);
                snapshot.CargoCountAirplane = ReadValue(StatisticType.CargoCountAirplane);

                snapshot.ServiceWealth = ReadValue(StatisticType.ServiceWealth);
                snapshot.ServiceCount = ReadValue(StatisticType.ServiceCount);
                snapshot.ServiceWorkers = ReadValue(StatisticType.ServiceWorkers);
                snapshot.ServiceMaxWorkers = ReadValue(StatisticType.ServiceMaxWorkers);

                snapshot.ProcessingWealth = ReadValue(StatisticType.ProcessingWealth);
                snapshot.ProcessingCount = ReadValue(StatisticType.ProcessingCount);
                snapshot.ProcessingWorkers = ReadValue(StatisticType.ProcessingWorkers);
                snapshot.ProcessingMaxWorkers = ReadValue(StatisticType.ProcessingMaxWorkers);

                snapshot.OfficeWealth = ReadValue(StatisticType.OfficeWealth);
                snapshot.OfficeCount = ReadValue(StatisticType.OfficeCount);
                snapshot.OfficeWorkers = ReadValue(StatisticType.OfficeWorkers);
                snapshot.OfficeMaxWorkers = ReadValue(StatisticType.OfficeMaxWorkers);

                snapshot.CityServiceWorkers = ReadValue(StatisticType.CityServiceWorkers);
                snapshot.CityServiceMaxWorkers = ReadValue(StatisticType.CityServiceMaxWorkers);

                snapshot.HouseholdWealth = ReadValue(StatisticType.HouseholdWealth);
                snapshot.HouseholdCount = ReadValue(StatisticType.HouseholdCount);

                snapshot.SeniorWorkerInDemandPercentage = ReadValue(StatisticType.SeniorWorkerInDemandPercentage);
                snapshot.EscapedArrestCount = ReadValue(StatisticType.EscapedArrestCount);

                snapshot.WellbeingLevel = ReadValue(StatisticType.WellbeingLevel);
                snapshot.HealthLevel = ReadValue(StatisticType.HealthLevel);

                GetDate(m_SimulationSystem.frameIndex, out snapshot.GameYear, out snapshot.GameMonth, out snapshot.GameDay);
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

                RefreshParameters();
                m_StatisticsSystem.CompleteWriters();
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

        private void RefreshParameters()
        {
            m_Parameters.Clear();
            using (var query = m_World.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<StatisticsData>()))
            using (var entities = query.ToEntityArray(Allocator.Temp))
            {
                foreach (var entity in entities)
                {
                    var type = m_World.EntityManager.GetComponentData<StatisticsData>(entity).m_StatisticType;
                    if (!m_Parameters.TryGetValue(type, out var parameters))
                        m_Parameters[type] = parameters = new HashSet<int>();
                    if (m_World.EntityManager.HasBuffer<StatisticParameterData>(entity))
                    {
                        var buffer = m_World.EntityManager.GetBuffer<StatisticParameterData>(entity, true);
                        foreach (var parameter in buffer) parameters.Add(parameter.m_Value);
                    }
                    else parameters.Add(0);
                }
            }
        }

        private IEnumerable<int> Parameters(StatisticType type)
        {
            if (m_Parameters.TryGetValue(type, out var parameters)) return parameters;
            return new[] { 0 };
        }

        private long ReadTotal(StatisticType type)
        {
            long total = 0;
            foreach (int parameter in Parameters(type))
                total += m_StatisticsSystem.GetStatisticValueLong(type, parameter) * Multiplier(type, parameter);
            return total;
        }

        private int Multiplier(StatisticType type, int parameter)
        {
            if (type != StatisticType.Trade) return 1;
            // Match StatisticsUISystem: convert each traded resource to its market value.
            var prefabs = m_World.GetExistingSystemManaged<ResourceSystem>().GetPrefabs();
            var entity = prefabs[EconomyUtils.GetResource(parameter)];
            return (int)EconomyUtils.GetMarketPrice(m_World.EntityManager.GetComponentData<ResourceData>(entity));
        }

        private int ReadValue(StatisticType type) => StatisticMath.Saturate(ReadTotal(type));

        private long[] ReadHistory(StatisticType type)
        {
            var values = new long[m_StatisticsSystem.sampleCount];
            foreach (int parameter in Parameters(type))
            {
                using (var data = m_StatisticsSystem.GetStatisticDataArrayLong(type, parameter))
                {
                    var source = data.ToArray();
                    int multiplier = Multiplier(type, parameter);
                    for (int i = 0; i < source.Length; i++) source[i] *= multiplier;
                    StatisticMath.AddAligned(values, source);
                }
            }
            return values;
        }

        private void ExportStat(Dictionary<string, List<int>> target, string key, StatisticType type)
        {
            var data = ReadHistory(type);
            var population = type == StatisticType.Wellbeing || type == StatisticType.Health
                ? ReadHistory(StatisticType.Population) : null;
            var values = new List<int>(data.Length);
            for (int i = 0; i < data.Length; i++)
                values.Add(population == null ? StatisticMath.Saturate(data[i]) : StatisticMath.Average(data[i], population[i]));
            target[key] = values;
        }

        private void GetDate(uint frame, out int year, out int month, out int day)
        {
            using (var query = m_World.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeData>()))
            {
                var data = TimeData.GetSingleton(query);
                int days = m_TimeSystem.daysPerYear;
                int epochTicks = (int)Math.Round(data.TimeOffset * TimeSystem.kTicksPerDay)
                    + (int)Math.Round(data.GetDateOffset(days) * TimeSystem.kTicksPerDay * days);
                StatisticMath.GetDate(frame, data.m_FirstFrame, epochTicks, data.m_StartingYear,
                    days, TimeSystem.kTicksPerDay, out year, out month, out day);
            }
        }

        private void ExportTimeData(Dictionary<string, List<int>> target, int totalSamples)
        {
            var years = new List<int>(totalSamples);
            var months = new List<int>(totalSamples);
            var days = new List<int>(totalSamples);
            for (int i = 0; i < totalSamples; i++)
            {
                GetDate(m_StatisticsSystem.GetSampleFrameIndex(i), out int year, out int month, out int day);
                years.Add(year);
                months.Add(month);
                days.Add(day);
            }
            target["gameYear"] = years;
            target["gameMonth"] = months;
            target["gameDay"] = days;
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
