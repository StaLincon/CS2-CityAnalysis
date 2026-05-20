using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DataAnalyzer.Models;

namespace DataAnalyzer.Services
{
    public class SaveFolderManager
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public List<SaveRecord> Scan(string baseDataPath)
        {
            var records = new List<SaveRecord>();

            if (string.IsNullOrEmpty(baseDataPath) || !Directory.Exists(baseDataPath))
                return records;

            // 扫描子文件夹（多存档模式）
            foreach (var dir in Directory.GetDirectories(baseDataPath))
            {
                var record = ReadSaveFolder(dir);
                if (record != null)
                    records.Add(record);
            }

            // 如果子文件夹中没有找到存档，检查基础路径本身是否有数据（单存档/扁平模式）
            if (records.Count == 0)
            {
                var flatRecord = ReadFlatSave(baseDataPath);
                if (flatRecord != null)
                    records.Add(flatRecord);
            }

            records = records
                .OrderByDescending(r => r.LastExportTime)
                .ThenByDescending(r => r.GameYear)
                .ThenByDescending(r => r.GameMonth)
                .ToList();

            return records;
        }

        private static SaveRecord ReadFlatSave(string folderPath)
        {
            var snapshotPath = Path.Combine(folderPath, "current_snapshot.json");
            var historyPath = Path.Combine(folderPath, "full_history.json");

            if (!File.Exists(snapshotPath))
                return null;

            var record = new SaveRecord
            {
                FolderName = "(当前数据)",
                FolderPath = folderPath,
                HasSnapshot = true,
                HasHistory = File.Exists(historyPath),
                LastExportTime = GetLatestFileTime(folderPath)
            };

            try
            {
                var json = File.ReadAllText(snapshotPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                record.Population = GetInt(root, "population");
                record.GameYear = GetInt(root, "gameYear");
                record.GameMonth = GetInt(root, "gameMonth");
                record.GameDay = GetInt(root, "gameDay");
                record.Happiness = GetInt(root, "averageHappiness");
                record.Health = GetInt(root, "averageHealth");
                record.Income = GetInt(root, "income");
                record.Expense = GetInt(root, "expense");
            }
            catch
            {
                return record;
            }

            if (record.HasHistory)
            {
                try
                {
                    var historyJson = File.ReadAllText(historyPath);
                    using var doc = JsonDocument.Parse(historyJson);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("totalSamples", out var samples))
                        record.HistoryRecordCount = samples.GetInt32();
                }
                catch { }
            }

            return record;
        }

        private static SaveRecord ReadSaveFolder(string folderPath)
        {
            var snapshotPath = Path.Combine(folderPath, "current_snapshot.json");
            var historyPath = Path.Combine(folderPath, "full_history.json");

            if (!File.Exists(snapshotPath))
                return null;

            var record = new SaveRecord
            {
                FolderName = Path.GetFileName(folderPath),
                FolderPath = folderPath,
                HasSnapshot = File.Exists(snapshotPath),
                HasHistory = File.Exists(historyPath),
                LastExportTime = GetLatestFileTime(folderPath)
            };

            try
            {
                var json = File.ReadAllText(snapshotPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                record.Population = GetInt(root, "population");
                record.GameYear = GetInt(root, "gameYear");
                record.GameMonth = GetInt(root, "gameMonth");
                record.GameDay = GetInt(root, "gameDay");
                record.Happiness = GetInt(root, "averageHappiness");
                record.Health = GetInt(root, "averageHealth");
                record.Income = GetInt(root, "income");
                record.Expense = GetInt(root, "expense");
            }
            catch
            {
                return record;
            }

            if (record.HasHistory)
            {
                try
                {
                    var historyJson = File.ReadAllText(historyPath);
                    using var doc = JsonDocument.Parse(historyJson);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("totalSamples", out var samples))
                        record.HistoryRecordCount = samples.GetInt32();
                }
                catch { }
            }

            return record;
        }

        private static int GetInt(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
            {
                return prop.GetInt32();
            }
            return 0;
        }

        private static DateTime GetLatestFileTime(string folderPath)
        {
            var latest = DateTime.MinValue;
            try
            {
                foreach (var file in Directory.GetFiles(folderPath, "*.json"))
                {
                    var writeTime = File.GetLastWriteTime(file);
                    if (writeTime > latest)
                        latest = writeTime;
                }
            }
            catch { }
            return latest;
        }

        public bool HasValidSaves(string baseDataPath)
        {
            if (string.IsNullOrEmpty(baseDataPath) || !Directory.Exists(baseDataPath))
                return false;

            foreach (var dir in Directory.GetDirectories(baseDataPath))
            {
                if (File.Exists(Path.Combine(dir, "current_snapshot.json")))
                    return true;
            }

            // 扁平模式：检查基础路径本身
            return File.Exists(Path.Combine(baseDataPath, "current_snapshot.json"));
        }
    }
}