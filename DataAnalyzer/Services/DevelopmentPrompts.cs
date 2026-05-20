using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataAnalyzer.Models;

namespace DataAnalyzer.Services
{
    public static class DevelopmentPrompts
    {
        public const string SystemPrompt = @"你是一位资深的城市政府办公厅主任，负责撰写《城市发展工作报告》。本报告面向市民代表大会，回顾城市从建市之初到当前阶段的发展历程。

## 角色定位
你是市长授权的报告撰写官，代表市人民政府向市民代表大会报告工作。

## 时间视角
本报告是""自建市以来的全面回顾""，需要从头到尾讲述城市的发展故事：
- 重点突出从建市初期到现在的对比变化
- 引用历史极值（人口最高点、幸福度峰值等）展示成就
- 引用人口里程碑（首次突破1000人、5000人、10000人等）展现跨越式发展
- 描述发展阶段的转变（从初创期→发展初期→快速成长期等）

## 语言风格
- 使用正式、庄重的政府工作报告语言
- 句式参考：
  - ""建市以来，我市……"" 开场
  - ""从XX人到XX人，增长了XX倍"" 强调跨越
  - ""先后突破……XX、XX人口大关"" 回顾里程碑
  - ""由XX期迈入XX期"" 描述发展阶段跃升
  - ""较建市之初增长XX%"" 展示总量变化
  - ""在XX年XX月达到历史峰值"" 引用极值
- 数据要对比分析，体现长期发展轨迹

## 排版约束
- 不输出Markdown格式标记（不要出现###、**、-等）
- 段落之间用空行分隔
- 使用全角标点符号
- 章节标题用""一、""""二、""等中文序号";

        public static string BuildDevelopmentContext(DevelopmentReport report, string cityName)
        {
            var sb = new StringBuilder();
            var h = report.History;
            var c = report.Overview;
            var st = report.StageTransition;

            sb.AppendLine($"【城市名称】{cityName}");
            sb.AppendLine($"【时间跨度】第{h.FirstGameYear}年第{h.FirstGameMonth}月 ~ 第{h.LastGameYear}年第{h.LastGameMonth}月（共{h.TotalDays}天，约{h.LastGameYear - h.FirstGameYear + 1}年）");
            sb.AppendLine($"【当前时点】第{c.GameYear}年第{c.GameMonth}月");
            sb.AppendLine($"【发展阶段】{GameMetricConverter.GetGameStageDescription(new StatisticSnapshot { Population = c.Population })}");
            sb.AppendLine();

            sb.AppendLine("【建市以来总体变化】");
            sb.AppendLine($"  人口：{h.FirstPopulation:N0}人 → {h.LastPopulation:N0}人（累计增长{h.PopGrowthTotal:+0.0;-0.0}%）");
            sb.AppendLine($"  财政：₡{h.FirstMoney:N0} → ₡{h.LastMoney:N0}（累计增长{h.MoneyGrowthTotal:+0.0;-0.0}%）");
            sb.AppendLine($"  当前人口：{c.Population:N0}人");
            sb.AppendLine($"  当前财政余额：₡{c.Money:N0}");
            sb.AppendLine($"  当前幸福度：{c.Happiness:F1}%");
            sb.AppendLine($"  历史最高幸福度：{h.PeakHappiness:F1}%（第{h.PeakHappinessYear}年第{h.PeakHappinessMonth}月）");
            sb.AppendLine($"  历史最高健康度：{h.PeakHealth:F1}%（第{h.PeakHealthYear}年第{h.PeakHealthMonth}月）");
            sb.AppendLine();

            if (st.FromStage != st.ToStage)
            {
                sb.AppendLine("【阶段跨越】");
                sb.AppendLine($"  城市由「{st.FromStage}」迈入「{st.ToStage}」（第{st.TransitionYear}年第{st.TransitionMonth}月，人口{st.PopulationAtTransition:N0}人时实现跨越）");
                sb.AppendLine();
            }

            AppendDemographics(sb, report.Demographics);
            AppendEconomy(sb, report.Economy, report.Fiscal);
            AppendSectors(sb, report.Sectors);
            AppendEmployment(sb, report.Employment);
            AppendTransport(sb, report.Transport);
            AppendSocial(sb, report.Social);
            AppendFiscal(sb, report.Fiscal);
            AppendHouseholds(sb, report.Households);
            AppendTrends(sb, report.Trends);
            AppendAlerts(sb, report.Alerts);
            AppendScores(sb, report.Scores);
            AppendYearSnapshots(sb, report.YearSnapshots);

            return sb.ToString();
        }

        private static void AppendDemographics(StringBuilder sb, DemographicAnalysis d)
        {
            sb.AppendLine("【人口数据】");
            sb.AppendLine($"  常住人口：{d.Population:N0}人（含迁入{d.PopulationWithMoveIn:N0}人）");
            sb.AppendLine($"  人口增长率：{d.GrowthRate:+0.0;-0.0}%");
            if (d.CitizensMovedIn > 0 || d.CitizensMovedAway > 0)
                sb.AppendLine($"  迁入：{d.CitizensMovedIn:N0}人  迁出：{d.CitizensMovedAway:N0}人  净迁移：{d.NetMigration:+0;-0}人");
            if (d.BirthRate > 0)
                sb.AppendLine($"  月出生人数：{d.BirthRate}人  月死亡人数：{d.DeathRate}人  月净增：{d.NaturalGrowth:+0;-0}人");
            if (d.AdultsCount > 0)
                sb.AppendLine($"  成年人口：{d.AdultsCount:N0}人（占比{d.AdultsRatio:F1}%）");
            sb.AppendLine();
        }

        private static void AppendEconomy(StringBuilder sb, EconomicAnalysis e, FiscalAnalysis f)
        {
            sb.AppendLine("【财政经济数据】");
            sb.AppendLine($"  月收入：₡{e.Income:N0}  月支出：₡{e.Expense:N0}  净收入：₡{e.NetIncome:N0}");
            sb.AppendLine($"  收支比：{f.RevenueExpenseRatio:F2}（{GameMetricConverter.ToBudgetHealthDescription(f.RevenueExpenseRatio)}）");
            if (e.Trade > 0) sb.AppendLine($"  贸易额：₡{e.Trade:N0}");
            sb.AppendLine($"  人均收入：₡{e.PerCapitaIncome:F1}  人均支出：₡{e.PerCapitaExpense:F1}");
            if (e.TotalTax > 0)
            {
                sb.AppendLine($"  税收总收入：₡{e.TotalTax:N0}");
                sb.AppendLine($"  住宅税{e.ResidentialTaxPct:F1}%  商业税{e.CommercialTaxPct:F1}%  工业税{e.IndustrialTaxPct:F1}%  办公税{e.OfficeTaxPct:F1}%");
            }
            sb.AppendLine();
        }

        private static void AppendSectors(StringBuilder sb, SectorAnalysis sec)
        {
            if (sec.TotalWealth <= 0) return;
            sb.AppendLine("【产业结构】");
            sb.AppendLine($"  服务业：财富₡{sec.Service.Wealth:N0}，企业{sec.Service.Count:N0}家，占比{sec.ServiceWealthPct:F1}%");
            sb.AppendLine($"  加工业：财富₡{sec.Processing.Wealth:N0}，企业{sec.Processing.Count:N0}家，占比{sec.ProcessingWealthPct:F1}%");
            sb.AppendLine($"  办公业：财富₡{sec.Office.Wealth:N0}，企业{sec.Office.Count:N0}家，占比{sec.OfficeWealthPct:F1}%");
            sb.AppendLine($"  产业合计：财富₡{sec.TotalWealth:N0}，企业{sec.TotalCount:N0}家，从业{sec.TotalWorkers:N0}人");
            sb.AppendLine();
        }

        private static void AppendEmployment(StringBuilder sb, EmploymentAnalysis emp)
        {
            sb.AppendLine("【就业数据】");
            sb.AppendLine($"  从业人员：{emp.WorkerCount:N0}人  失业率：{emp.UnemploymentRate:F1}%");
            sb.AppendLine($"  劳动参与率：{emp.WorkforceParticipation:F1}%");
            if (emp.CityServiceWorkers > 0)
                sb.AppendLine($"  公务人员填充率：{emp.CityServiceFillRate:F1}%");
            if (emp.SeniorWorkerDemand > 0)
                sb.AppendLine($"  高级技工需求率：{emp.SeniorWorkerDemand:F1}%");
            sb.AppendLine();
        }

        private static void AppendTransport(StringBuilder sb, TransportAnalysis t)
        {
            sb.AppendLine("【交通数据】");
            if (t.TotalPassengers > 0)
            {
                sb.AppendLine($"  客运总量：{t.TotalPassengers:N0}人次（公共交通占比{t.PublicTransitShare:F1}%）");
                if (t.Bus.Passengers > 0) sb.AppendLine($"  公交：{t.Bus.Passengers:N0}人次");
                if (t.Subway.Passengers > 0) sb.AppendLine($"  地铁：{t.Subway.Passengers:N0}人次");
                if (t.Train.Passengers > 0) sb.AppendLine($"  火车：{t.Train.Passengers:N0}人次");
                if (t.Tram.Passengers > 0) sb.AppendLine($"  有轨电车：{t.Tram.Passengers:N0}人次");
                if (t.Airplane.Passengers > 0) sb.AppendLine($"  航空：{t.Airplane.Passengers:N0}人次");
            }
            if (t.TotalCargo > 0)
                sb.AppendLine($"  货运总量：{t.TotalCargo:N0}吨");
            sb.AppendLine();
        }

        private static void AppendSocial(StringBuilder sb, SocialAnalysis s)
        {
            sb.AppendLine("【社会民生】");
            sb.AppendLine($"  幸福度：{s.Wellbeing:F1}%（{GameMetricConverter.ToHappinessLevel(s.Wellbeing)}）");
            sb.AppendLine($"  健康度：{s.Health:F1}%（{GameMetricConverter.ToHealthDescription(s.Health)}）");
            if (s.EducationCount > 0) sb.AppendLine($"  教育机构：{s.EducationCount:N0}所（覆盖率{s.EducationRate:F1}%）");
            sb.AppendLine($"  犯罪率：{s.CrimeRate:F1}%（{GameMetricConverter.ToCrimeDescription(s.CrimeRate)}）");
            if (s.HomelessCount > 0) sb.AppendLine($"  无家可归者：{s.HomelessCount:N0}人");
            sb.AppendLine($"  生活质量指数：{s.QualityOfLifeIndex:F1}/100");
            sb.AppendLine();
        }

        private static void AppendFiscal(StringBuilder sb, FiscalAnalysis f)
        {
            sb.AppendLine("【财政健康】");
            sb.AppendLine($"  收支比：{f.RevenueExpenseRatio:F2}（状态：{f.FiscalStatus}）");
            sb.AppendLine($"  税收依赖度：{f.TaxToIncomeRatio:F1}%  贸易依赖度：{f.TradeToIncomeRatio:F1}%");
            sb.AppendLine();
        }

        private static void AppendHouseholds(StringBuilder sb, HouseholdAnalysis h)
        {
            if (h.HouseholdCount <= 0) return;
            sb.AppendLine("【家庭经济】");
            sb.AppendLine($"  家庭总数：{h.HouseholdCount:N0}户  户均人口：{h.AvgPersonsPerHousehold:F1}人");
            sb.AppendLine($"  家庭总财富：₡{h.HouseholdWealth:N0}  户均财富：₡{h.AvgWealthPerHousehold:F1}");
            sb.AppendLine();
        }

        private static void AppendTrends(StringBuilder sb, TrendSummary tr)
        {
            sb.AppendLine("【趋势变化】");
            AppendT(sb, "人口增长率", tr.PopGrowthRate);
            AppendT(sb, "收入增长率", tr.IncomeGrowthRate);
            AppendT(sb, "幸福度变化", tr.HappinessTrend);
            AppendT(sb, "健康度变化", tr.HealthTrend);
            AppendT(sb, "犯罪率变化", tr.CrimeTrend);
            sb.AppendLine($"  发展势头——人口：{TransM(tr.PopMomentum)}，经济：{TransM(tr.EconomyMomentum)}，社会：{TransM(tr.SocialMomentum)}");
            sb.AppendLine();
        }

        private static void AppendT(StringBuilder sb, string name, double value)
        {
            if (Math.Abs(value) > 0.01)
                sb.AppendLine($"  {name}：{value:+0.0;-0.0}% {(value > 0 ? "↑" : "↓")}");
        }

        private static string TransM(string m) => m switch
        {
            "Strong Growth" => "强劲增长",
            "Moderate Growth" => "温和增长",
            "Stable" => "保持稳定",
            "Slight Decline" => "轻微下滑",
            "Declining" => "明显下滑",
            "Rapid Decline" => "快速下滑",
            _ => m
        };

        private static void AppendAlerts(StringBuilder sb, List<AlertItem> alerts)
        {
            if (alerts.Count == 0) return;
            sb.AppendLine("【风险告警】");
            foreach (var a in alerts)
                sb.AppendLine($"  [{a.Level}] {a.Category}：{a.Message}");
            sb.AppendLine();
        }

        private static void AppendScores(StringBuilder sb, List<ScoreCard> scores)
        {
            if (scores.Count == 0) return;
            sb.AppendLine("【综合评分】");
            foreach (var s in scores)
                sb.AppendLine($"  {s.Category}/{s.Name}：{s.Score:F1}分（{s.Grade}）");
            sb.AppendLine($"  综合均分：{scores.Average(s => s.Score):F1}分");
            sb.AppendLine();
        }

        private static void AppendYearSnapshots(StringBuilder sb, List<YearSnapshot> ys)
        {
            if (ys.Count < 2) return;
            sb.AppendLine("【历年关键指标】");
            foreach (var y in ys)
                sb.AppendLine($"  第{y.GameYear}年（第{y.GameMonth}月）：人口{y.Population:N0} 资金₡{y.Money:N0} 幸福{y.Happiness:F1}% 健康{y.Health:F1}% 收入₡{y.Income:N0} 支出₡{y.Expense:N0}");
        }

        public static string GetOpeningPrompt(string cityName, string ctx)
        {
            return $@"根据以下{cityName}城市数据，撰写《城市发展工作报告》的开场白和总体回顾章节（600字左右）：

## 数据
{ctx}

## 要求
1. 以""各位代表：现在，我代表市人民政府，向大会报告建市以来的城市发展工作，请予审议。""开头
2. 概括从建市之初到当前的总体发展态势，使用""建市以来""的时间视角
3. 引用人口增长数据（从XX人增长到XX人）
4. 提及发展阶段的变化（从XX期迈入XX期）
5. 点出2-3个最亮眼的成就（引用历史峰值数据）
6. 指出1-2个需要持续关注的问题
7. 严禁使用""根据数据""""数据显示""等元描述
8. 直接以报告正文形式输出";
        }

        public static string GetDemographicsPrompt(string ctx)
        {
            return $@"撰写""人口发展与城镇化建设""章节（500字）：

## 数据
{ctx}

## 要求
1. 回顾建市以来人口总量的跨越式增长
2. 引用人口增长数据（从建市初到当前的变化幅度）
3. 分析迁入迁出的长期趋势
4. 汇报出生率和死亡率变化
5. 描述劳动力年龄结构的变化
6. 格式：以""一、人口规模持续壮大，城镇化水平显著提升""为标题
7. 直接输出报告正文";
        }

        public static string GetEconomyPrompt(string ctx)
        {
            return $@"撰写""经济发展与财政运行""章节（600字）：

## 数据
{ctx}

## 要求
1. 回顾财政收入从建市之初到现在的增长历程
2. 分析税收结构的演变（从单一到多元）
3. 汇报首次实现财政盈余的标志性意义
4. 点评贸易发展和产业基础建设
5. 用人均指标反映居民经济水平提升
6. 格式：以""二、经济实力显著增强，财政运行稳健有力""为标题
7. 直接输出报告正文";
        }

        public static string GetIndustryPrompt(string ctx)
        {
            return $@"撰写""产业体系构建与优化升级""章节（500字）：

## 数据
{ctx}

## 要求
1. 回顾三大产业从无到有、从小到大的发展历程
2. 分析当前产业结构特点和主导产业
3. 汇报产业对就业的吸纳作用
4. 对于尚未充分发展的产业，指出发展空间
5. 格式：以""三、产业体系日趋完善，发展动能持续增强""为标题
6. 直接输出报告正文";
        }

        public static string GetEmploymentPrompt(string ctx)
        {
            return $@"撰写""就业促进与社会保障""章节（400字）：

## 数据
{ctx}

## 要求
1. 回顾就业市场从建市之初到现在的变化
2. 分析当前失业率水平和劳动参与率
3. 汇报人才培养和技能供给情况
4. 如公务体系初建，如实说明建设进展
5. 格式：以""四、就业形势稳定向好，社会保障逐步健全""为标题
6. 直接输出报告正文";
        }

        public static string GetTransportPrompt(string ctx)
        {
            return $@"撰写""基础设施建设与交通发展""章节（500字）：

## 数据
{ctx}

## 要求
1. 回顾交通基础设施从零起步的建设历程
2. 汇报现有公共交通方式及运力
3. 分析公共交通分担率的变化趋势
4. 如某类交通尚未建设，说明处于规划阶段
5. 汇报货运物流体系发展
6. 格式：以""五、基础设施日臻完善，交通网络初具规模""为标题
7. 直接输出报告正文";
        }

        public static string GetSocialPrompt(string ctx)
        {
            return $@"撰写""民生福祉与社会事业""章节（500字）：

## 数据
{ctx}

## 要求
1. 回顾幸福度和健康度的长期变化趋势
2. 引用历史峰值展示民生改善成就
3. 汇报教育、医疗等社会事业建设进展
4. 分析治安状况的长期演变
5. 对于存在的问题如实报告
6. 格式：以""六、民生福祉持续改善，社会事业全面发展""为标题
7. 直接输出报告正文";
        }

        public static string GetFiscalPrompt(string ctx)
        {
            return $@"撰写""财政管理与家庭经济""章节（400字）：

## 数据
{ctx}

## 要求
1. 分析财政健康度的演变
2. 汇报税收结构变化和财政自主能力
3. 报告家庭财富积累情况
4. 如有风险信号要如实指出
5. 格式：以""七、财政管理规范有序，居民生活水平稳步提高""为标题
6. 直接输出报告正文";
        }

        public static string GetChallengesPrompt(string ctx)
        {
            return $@"撰写""面临的问题与挑战""章节（350字）：

## 数据
{ctx}

## 要求
1. 冷静客观分析城市发展中长期存在的结构性问题
2. 结合当前数据和风险告警，指出紧迫短板
3. 语气：""必须清醒看到……"" ""仍然存在……"" ""有待进一步加强""
4. 对于发展初期的领域表示""还有很大发展空间""
5. 列举2-3个问题，每个问题有数据支撑
6. 直接输出报告正文";
        }

        public static string GetOutlookPrompt(string ctx)
        {
            return $@"撰写""下一阶段发展目标与工作部署""章节（500字）：

## 数据
{ctx}

## 要求
1. 结合当前发展阶段提出下一阶段总体目标
2. 分3-5个方面部署重点工作
3. 对未发展的领域提出""启动XX建设""
4. 语言有力，使用""要……""""必须……""""着力……""
5. 以鼓舞人心的结语收尾
6. 格式：以""八、凝心聚力，奋力开创城市发展新局面""为标题
7. 直接输出报告正文";
        }
    }
}