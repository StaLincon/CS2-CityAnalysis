using System;
using DataAnalyzer.Models;

namespace DataAnalyzer.Services
{
    public static class GameMetricConverter
    {
        public static string ToHappinessDescription(double wellbeing)
        {
            return wellbeing switch
            {
                >= 90 => "安居乐业，市民生活满意度极高",
                >= 80 => "城市宜居，市民普遍感到满意",
                >= 65 => "生活稳定，市民情绪积极",
                >= 50 => "基本满意，城市运行平稳",
                >= 35 => "部分不满，需要关注民生改善",
                >= 20 => "较多不满，民生问题亟待解决",
                _ => "严重不满，城市面临信任危机"
            };
        }

        public static string ToHappinessLevel(double wellbeing)
        {
            return wellbeing switch
            {
                >= 90 => "优秀",
                >= 80 => "良好",
                >= 65 => "一般",
                >= 50 => "及格",
                >= 35 => "较差",
                _ => "很差"
            };
        }

        public static string ToHealthDescription(double health)
        {
            return health switch
            {
                >= 90 => "医疗体系完善，居民健康水平优良",
                >= 80 => "医疗服务充足，居民身体状态良好",
                >= 65 => "基本医疗保障到位，整体健康达标",
                >= 50 => "医疗资源有待扩充，部分居民健康欠佳",
                >= 35 => "医疗服务不足，需要加大医疗投入",
                _ => "医疗危机，居民健康严重恶化"
            };
        }

        public static string ToCrimeDescription(double crimeRate)
        {
            return crimeRate switch
            {
                <= 2 => "治安优良，市民安全感强",
                <= 5 => "治安稳定，城市秩序井然",
                <= 10 => "治安基本可控，偶有轻微案件",
                <= 20 => "犯罪率偏高，需加强警力部署",
                <= 35 => "治安形势严峻，公共安全受到挑战",
                _ => "治安恶化严重，城市面临安全危机"
            };
        }

        public static string ToUnemploymentAssessment(double unemploymentRate)
        {
            return unemploymentRate switch
            {
                <= 3 => "充分就业，劳动力市场供需平衡",
                <= 5 => "就业形势良好，失业率处于健康水平",
                <= 8 => "就业基本稳定，需关注结构性失业",
                <= 12 => "就业压力较大，应出台稳就业措施",
                <= 20 => "失业问题突出，急需就业扶持政策",
                _ => "就业危机，大量居民面临生计困难"
            };
        }

        public static string ToEducationDescription(double educationRate)
        {
            return educationRate switch
            {
                >= 80 => "教育资源充裕，满足市民求学需求",
                >= 60 => "教育覆盖较广，基本满足需求",
                >= 40 => "教育资源适中，尚有提升空间",
                >= 20 => "教育设施不足，部分市民就学困难",
                _ => "教育资源匮乏，急需建设学校"
            };
        }

        public static string ToTrafficDescription(double transitShare)
        {
            return transitShare switch
            {
                >= 70 => "公共交通主导，绿色出行成效显著",
                >= 50 => "公共交通分担率较高，出行结构合理",
                >= 30 => "公交与私车并行，交通体系均衡",
                >= 15 => "公共交通分担不足，道路压力较大",
                _ => "公共交通薄弱，依赖私家车出行"
            };
        }

        public static string ToBudgetHealthDescription(double ratio)
        {
            return ratio switch
            {
                >= 1.5 => "财政高度充裕，有大量资金可供投资",
                >= 1.1 => "财政盈余，政府财力充裕",
                >= 1.0 => "收支平衡，财政运行稳健",
                >= 0.9 => "略微赤字，需关注支出控制",
                >= 0.7 => "赤字运行，应开源节流",
                _ => "严重赤字，财政状况亟待改善"
            };
        }

        public static bool IsServiceAvailable(int value)
        {
            return value > 0;
        }

        public static bool IsServiceAvailable(double value)
        {
            return value > 0.01;
        }

        public static string GetGameStageDescription(StatisticSnapshot current)
        {
            var pop = current.Population;
            if (pop < 1000) return "初创期——城市刚刚起步，各项基础设施正在建设中";
            if (pop < 5000) return "发展初期——城市初具规模，公共服务体系逐步建立";
            if (pop < 20000) return "快速成长期——人口加速流入，城市功能日趋完善";
            if (pop < 50000) return "发展中期——城市规模不断扩大，多元产业协同发展";
            if (pop < 100000) return "成熟发展期——城市功能完善，综合承载力稳步提升";
            return "大都市阶段——城市高度发达，区域影响力显著增强";
        }
    }
}