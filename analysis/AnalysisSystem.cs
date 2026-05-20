using System;
using System.IO;
using analysis.Data;
using Colossal.Logging;
using Game;
using Game.SceneFlow;
using Unity.Entities;

namespace analysis
{
    public partial class AnalysisSystem : GameSystemBase
    {
        private readonly ILog m_Log;
        private StatisticCollector m_Collector;
        private uint m_TickCounter;
        private bool m_Initialized;
        private string m_BaseDataPath;
        private string m_CurrentSaveName;
        private string m_SaveDataPath;
        private string m_LastCityName; // 缓存最后检测到的城市名称

        private const uint SNAPSHOT_INTERVAL = 1024;

        public AnalysisSystem()
        {
            m_Log = LogManager.GetLogger($"{nameof(analysis)}.{nameof(AnalysisSystem)}");
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log.Info("AnalysisSystem created");

            var docsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            m_BaseDataPath = Path.Combine(docsPath, "Cities Skylines II", "analysis");
            Directory.CreateDirectory(m_BaseDataPath);

            m_Collector = new StatisticCollector();
            m_Collector.Initialize(World);

            Mod.Instance.System = this;

            UpdateSaveInfo();

            m_Initialized = true;
            m_Log.Info($"AnalysisSystem ready - base data path: {m_BaseDataPath}");
        }

        protected override void OnUpdate()
        {
            if (!m_Initialized) return;

            m_TickCounter++;
            if (m_TickCounter % SNAPSHOT_INTERVAL != 0) return;

            try
            {
                // 定期检查存档是否切换
                CheckForSaveChange();
                
                var snapshot = m_Collector.Collect();
                if (snapshot != null && snapshot.Population > 0)
                    SaveCurrentSnapshot(snapshot);
            }
            catch (System.Exception ex)
            {
                m_Log.Error($"OnUpdate error: {ex.Message}");
            }
        }

        public void ExportData()
        {
            if (!m_Initialized) return;

            try
            {
                // 导出前检查存档是否切换
                CheckForSaveChange();
                
                m_Log.Info("Export started...");

                var snapshot = m_Collector.Collect();
                if (snapshot != null)
                    SaveCurrentSnapshot(snapshot);

                m_Collector.ExportFullHistory(m_SaveDataPath);
                m_Log.Info($"Data exported to: {m_SaveDataPath}, SampleCount={m_Collector.CurrentSnapshot?.SampleCount}");
            }
            catch (System.Exception ex)
            {
                m_Log.Error($"Export failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查存档是否发生变化
        /// </summary>
        private void CheckForSaveChange()
        {
            string currentCityName = GetCityNameFromGame();
            
            if (!string.IsNullOrEmpty(currentCityName) && currentCityName != m_LastCityName)
            {
                m_Log.Info($"City changed from '{m_LastCityName}' to '{currentCityName}'");
                m_LastCityName = currentCityName;
                UpdateSaveInfo();
            }
        }

        /// <summary>
        /// 更新当前存档信息并创建对应的存储目录
        /// </summary>
        public void UpdateSaveInfo()
        {
            try
            {
                string saveName = GetSaveNameFromGame();
                
                if (!string.IsNullOrEmpty(saveName))
                {
                    m_CurrentSaveName = saveName;
                    m_SaveDataPath = Path.Combine(m_BaseDataPath, SanitizeFileName(saveName));
                    
                    if (!Directory.Exists(m_SaveDataPath))
                        Directory.CreateDirectory(m_SaveDataPath);
                    
                    m_Log.Info($"Save data path updated: {m_SaveDataPath} (save name: {saveName})");
                }
                else
                {
                    m_CurrentSaveName = "Unknown";
                    m_SaveDataPath = m_BaseDataPath;
                    m_Log.Warn("Could not determine save name, using base path");
                }
            }
            catch (System.Exception ex)
            {
                m_Log.Error($"UpdateSaveInfo error: {ex.Message}");
                m_CurrentSaveName = "Error";
                m_SaveDataPath = m_BaseDataPath;
            }
        }

        /// <summary>
        /// 从游戏中获取当前城市名称（用于检测存档切换）
        /// </summary>
        private string GetCityNameFromGame()
        {
            try
            {
                // 通过 GameManager 获取最后保存的存档信息
                if (GameManager.instance != null && 
                    GameManager.instance.settings != null && 
                    GameManager.instance.settings.userState != null)
                {
                    var lastSave = GameManager.instance.settings.userState.lastSaveGameMetadata;
                    if (lastSave != null && !string.IsNullOrEmpty(lastSave.target?.cityName))
                    {
                        return lastSave.target.cityName;
                    }
                }
                return null;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 从游戏中获取当前存档名称
        /// </summary>
        private string GetSaveNameFromGame()
        {
            try
            {
                // 优先获取城市名称
                string cityName = GetCityNameFromGame();
                if (!string.IsNullOrEmpty(cityName))
                {
                    m_Log.Info($"Got city name: {cityName}");
                    return cityName;
                }

                // 尝试从GameManager获取存档信息
                if (GameManager.instance != null)
                {
                    var mode = GameManager.instance.gameMode;
                    m_Log.Info($"Current game mode: {mode}");
                }

                // 备用方案: 使用时间戳作为存档标识（确保每个游戏会话数据独立）
                return $"Session_{DateTime.Now:yyyyMMdd_HHmmss}";
            }
            catch (System.Exception ex)
            {
                m_Log.Error($"GetSaveNameFromGame error: {ex.Message}");
                return $"Session_{DateTime.Now:yyyyMMdd_HHmmss}";
            }
        }

        /// <summary>
        /// 清理文件名中的非法字符
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "Unknown";
            
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
                fileName = fileName.Replace(c, '_');
            
            fileName = fileName.Trim('.', ' ');
            
            if (fileName.Length > 50)
                fileName = fileName.Substring(0, 50);
            
            return string.IsNullOrEmpty(fileName) ? "Unknown" : fileName;
        }

        private void SaveCurrentSnapshot(StatisticSnapshot snapshot)
        {
            try
            {
                var json = SnapshotSerializer.SerializeSnapshot(snapshot);
                File.WriteAllText(Path.Combine(m_SaveDataPath, "current_snapshot.json"), json);
                m_Log.Debug($"Saved snapshot to {m_SaveDataPath}");
            }
            catch (System.Exception ex)
            {
                m_Log.Error($"Save snapshot failed: {ex.Message}");
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_Collector?.Dispose();
        }
    }
}