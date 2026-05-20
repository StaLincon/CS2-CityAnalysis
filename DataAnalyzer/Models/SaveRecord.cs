using System;

namespace DataAnalyzer.Models
{
    public class SaveRecord
    {
        public string FolderName { get; set; } = "";
        public string FolderPath { get; set; } = "";
        public int Population { get; set; }
        public int GameYear { get; set; }
        public int GameMonth { get; set; }
        public int GameDay { get; set; }
        public double Happiness { get; set; }
        public double Health { get; set; }
        public int Income { get; set; }
        public int Expense { get; set; }
        public DateTime LastExportTime { get; set; }
        public bool HasSnapshot { get; set; }
        public bool HasHistory { get; set; }
        public int HistoryRecordCount { get; set; }

        public string GameDateDisplay =>
            GameYear > 0 ? $"第{GameYear}年 第{GameMonth}月" : "未知";

        public string PopulationDisplay =>
            Population > 0 ? $"{Population:N0}人" : "未知";

        public string HappinessDisplay =>
            Happiness > 0 ? $"{Happiness:F1}%" : "未知";

        public string HealthDisplay =>
            Health > 0 ? $"{Health:F1}%" : "未知";

        public string FinanceDisplay
        {
            get
            {
                if (Income == 0 && Expense == 0) return "无数据";
                var balance = Income - Expense;
                var sign = balance >= 0 ? "+" : "";
                return $"₡{sign}{balance:N0}";
            }
        }

        public string Summary =>
            $"{GameDateDisplay} | 人口: {PopulationDisplay} | 幸福度: {HappinessDisplay} | 财政: {FinanceDisplay}";
    }
}