using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataAnalyzer.Models;

namespace DataAnalyzer.Services
{
    public static class PromptTemplates
    {
        public const string SystemPrompt = @"你是一位资深的城市政府办公厅主任，负责为城市撰写《政府工作报告》。你的写作必须严格遵循以下规范：

## 角色定位
你是市长授权的报告撰写官，代表市政府向全体市民汇报工作。

## 语言风格
- 使用正式、庄重、权威的公文语言
- 采用中国政府工作报告的标准格式和语气
- 大量使用以下句式：
  - ""一是……二是……三是……"" 来列举要点
  - ""同比增长/下降X%"" 来陈述数据变化
  - ""稳步推进"" ""持续改善"" ""明显提升"" ""显著增强"" 来描述趋势
  - ""扎实推进"" ""深入实施"" ""全面落实"" 来表述工作
  - ""面对……挑战"" ""在……形势下"" 来设置语境
  - ""必须清醒看到"" ""存在……不足"" 来指出问题
  - ""要……要……要……"" 来提出要求
- 用词精准、数据说话、避免空洞

## 报告结构要求
1. 开场白：""各位市民代表：现在，我代表市人民政府，向大会报告工作，请予审议，并请各位列席人员提出意见。""
2. 总体回顾段：用一段话概括本年度城市发展态势
3. 分领域汇报：按经济、人口、交通、公共服务等分项展开
4. 问题与挑战：冷静客观指出当前短板
5. 下阶段工作部署：提出具体目标和措施
6. 结语：鼓舞士气的收尾

## 数据运用规则
- 所有引用数据必须真实准确，不编造
- 数据要对比分析，体现变化趋势
- 增长率精确到小数点后一位
- 人口、财政等大宗数据要四舍五入到合理精度
- 对于值为0的数据项，说明该领域尚未发展到该阶段，不要编造数据

## 排版约束
- 不输出Markdown格式标记（不要出现###、**、-等）
- 段落之间用空行分隔
- 使用全角标点符号
- 章节标题用""一、""""二、""等中文序号";

        public static string BuildFullDataContext(CityAnalysisReport analysis, StatisticSnapshot current, List<StatisticSnapshot> history, string cityName)
        {
            var sb = new StringBuilder();
            var elapsedYears = history.Count > 1 ? (double)(history.Count - 1) / 32.0 / 12.0 : 0;

            sb.AppendLine($"【城市名称】{cityName}");
            sb.AppendLine($"【统计时点】第{current.GameYear}年第{current.GameMonth}月");
            if (elapsedYears > 0)
                sb.AppendLine($"【建市年限】逾{elapsedYears:F1}年");
            sb.AppendLine($"【发展阶段】{GameMetricConverter.GetGameStageDescription(current)}");
            sb.AppendLine();

            var o = analysis.Overview;
            var d = analysis.Demographics;
            var e = analysis.Economy;
            var sec = analysis.Sectors;
            var emp = analysis.Employment;
            var t = analysis.Transport;
            var s = analysis.Social;
            var f = analysis.Fiscal;
            var h = analysis.Households;
            var tr = analysis.Trends;

            sb.AppendLine("【总体概况】");
            AppendIfAvailable(sb, "常住人口", o.Population, "{0:N0}人");
            AppendIfAvailable(sb, "财政余额", o.Money, "₡{0:N0}");
            sb.AppendLine($"  居民幸福度：{o.Happiness:F1}%（{GameMetricConverter.ToHappinessLevel(o.Happiness)}）");
            sb.AppendLine($"  居民健康度：{o.Health:F1}%（{GameMetricConverter.ToHealthDescription(o.Health)}）");
            sb.AppendLine($"  生活质量综合指数：{s.QualityOfLifeIndex:F1}/100");
            sb.AppendLine($"  发展势头——人口：{TranslateMomentum(tr.PopMomentum)}，经济：{TranslateMomentum(tr.EconomyMomentum)}，社会：{TranslateMomentum(tr.SocialMomentum)}");
            sb.AppendLine();

            sb.AppendLine("【人口数据】");
            sb.AppendLine($"  总人口：{d.Population:N0}人（含迁入{d.PopulationWithMoveIn:N0}人）");
            sb.AppendLine($"  人口增长率：{d.GrowthRate:+0.0;-0.0}%");
            AppendIfAvailable(sb, "迁入人口", d.CitizensMovedIn, "{0:N0}人");
            AppendIfAvailable(sb, "迁出人口", d.CitizensMovedAway, "{0:N0}人");
            AppendIfAvailable(sb, "净迁移", d.NetMigration, "{0:+0;-0}人");
            AppendIfAvailable(sb, "出生率/死亡率/自然增长", d.BirthRate > 0 ? 1 : 0,
                d.BirthRate > 0 ? $"{d.BirthRate}‰/{d.DeathRate}‰/{d.NaturalGrowth}‰" : "人口自然增长数据暂无");
            AppendIfAvailable(sb, "成年人口", d.AdultsCount, "{0:N0}人（占比{d.AdultsRatio:F1}%）");
            sb.AppendLine();

            sb.AppendLine("【财政数据】");
            sb.AppendLine($"  月收入：₡{e.Income:N0}");
            sb.AppendLine($"  月支出：₡{e.Expense:N0}");
            sb.AppendLine($"  净收入：₡{e.NetIncome:N0}（利润率{e.ProfitMargin:+0.0;-0.0}%）");
            AppendIfAvailable(sb, "贸易额", e.Trade, "₡{0:N0}");
            AppendIfAvailable(sb, "发展点数", e.DevTreePoints, "{0:N0}");
            sb.AppendLine($"  人均收入：₡{e.PerCapitaIncome:F1}  人均支出：₡{e.PerCapitaExpense:F1}");
            AppendIfAvailable(sb, "人均税赋", e.PerCapitaTax, "₡{0:F1}");
            sb.AppendLine($"  收支比：{f.RevenueExpenseRatio:F2}（{GameMetricConverter.ToBudgetHealthDescription(f.RevenueExpenseRatio)}）");
            sb.AppendLine($"  税收依赖度：{f.TaxToIncomeRatio:F1}%  贸易依赖度：{f.TradeToIncomeRatio:F1}%");
            sb.AppendLine();

            sb.AppendLine("【税收结构】");
            if (GameMetricConverter.IsServiceAvailable(e.TotalTax))
            {
                sb.AppendLine($"  税收总收入：₡{e.TotalTax:N0}");
                sb.AppendLine($"  住宅税：₡{e.ResidentialTax:N0}（{e.ResidentialTaxPct:F1}%）");
                sb.AppendLine($"  商业税：₡{e.CommercialTax:N0}（{e.CommercialTaxPct:F1}%）");
                sb.AppendLine($"  工业税：₡{e.IndustrialTax:N0}（{e.IndustrialTaxPct:F1}%）");
                sb.AppendLine($"  办公税：₡{e.OfficeTax:N0}（{e.OfficeTaxPct:F1}%）");
            }
            else
            {
                sb.AppendLine("  税收体系尚未建立，暂无税收数据");
            }
            sb.AppendLine();

            sb.AppendLine("【产业结构】");
            if (GameMetricConverter.IsServiceAvailable(sec.TotalWealth))
            {
                AppendSector(sb, "服务业", sec.Service, sec.ServiceWealthPct, sec.ServiceWorkerFillRate);
                AppendSector(sb, "加工业", sec.Processing, sec.ProcessingWealthPct, sec.ProcessingWorkerFillRate);
                AppendSector(sb, "办公业", sec.Office, sec.OfficeWealthPct, sec.OfficeWorkerFillRate);
            }
            else
            {
                sb.AppendLine("  产业经济尚未形成规模，企业数据暂无");
            }
            sb.AppendLine();

            sb.AppendLine("【就业数据】");
            sb.AppendLine($"  从业人员：{emp.WorkerCount:N0}人");
            if (GameMetricConverter.IsServiceAvailable(emp.Unemployed) || emp.WorkerCount > 0)
            {
                sb.AppendLine($"  失业人口：{emp.Unemployed:N0}人（失业率{emp.UnemploymentRate:F1}%——{GameMetricConverter.ToUnemploymentAssessment(emp.UnemploymentRate)}）");
            }
            sb.AppendLine($"  劳动参与率：{emp.WorkforceParticipation:F1}%");
            if (GameMetricConverter.IsServiceAvailable(emp.CityServiceWorkers))
            {
                sb.AppendLine($"  公务人员：{emp.CityServiceWorkers:N0}/{emp.CityServiceMaxWorkers:N0}人（填充率{emp.CityServiceFillRate:F1}%）");
            }
            else
            {
                sb.AppendLine($"  公务服务体系尚未建立");
            }
            if (GameMetricConverter.IsServiceAvailable(emp.SeniorWorkerDemand))
            {
                sb.AppendLine($"  高级技工需求率：{emp.SeniorWorkerDemand:F1}%");
            }
            sb.AppendLine();

            sb.AppendLine("【交通数据】");
            if (GameMetricConverter.IsServiceAvailable(t.TotalPassengers))
            {
                sb.AppendLine($"  客运总量：{t.TotalPassengers:N0}人次（公共交通占比{t.PublicTransitShare:F1}%——{GameMetricConverter.ToTrafficDescription(t.PublicTransitShare)}）");
                AppendTransportLine(sb, "公交", t.Bus.Passengers, t.Bus.Share);
                AppendTransportLine(sb, "地铁", t.Subway.Passengers, t.Subway.Share);
                AppendTransportLine(sb, "有轨电车", t.Tram.Passengers, t.Tram.Share);
                AppendTransportLine(sb, "火车", t.Train.Passengers, t.Train.Share);
                AppendTransportLine(sb, "出租车", t.Taxi.Passengers, t.Taxi.Share);
                AppendTransportLine(sb, "航空", t.Airplane.Passengers, t.Airplane.Share);
                AppendTransportLine(sb, "水运", t.Ship.Passengers, t.Ship.Share);
            }
            else
            {
                sb.AppendLine($"  公共交通体系尚未建立，暂无客运数据");
            }
            if (GameMetricConverter.IsServiceAvailable(t.TotalCargo))
            {
                sb.AppendLine($"  货运总量：{t.TotalCargo:N0}吨（卡车{t.CargoTruck:N0} 铁路{t.CargoTrain:N0} 水运{t.CargoShip:N0} 空运{t.CargoAirplane:N0}）");
            }
            sb.AppendLine();

            sb.AppendLine("【社会民生】");
            sb.AppendLine($"  幸福指数：{s.Wellbeing:F1}%（{GameMetricConverter.ToHappinessDescription(s.Wellbeing)}）");
            sb.AppendLine($"  健康指数：{s.Health:F1}%（{GameMetricConverter.ToHealthDescription(s.Health)}）");
            if (GameMetricConverter.IsServiceAvailable(s.EducationCount))
            {
                sb.AppendLine($"  教育机构：{s.EducationCount:N0}所（{GameMetricConverter.ToEducationDescription(s.EducationRate)}）");
            }
            else
            {
                sb.AppendLine($"  教育体系尚未建立");
            }
            sb.AppendLine($"  犯罪率：{s.CrimeRate:F1}%（{GameMetricConverter.ToCrimeDescription(s.CrimeRate)}）");
            if (GameMetricConverter.IsServiceAvailable(s.CrimeCount))
            {
                sb.AppendLine($"  犯罪事件：{s.CrimeCount:N0}起  逃犯逮捕：{s.EscapedArrestCount:N0}起");
            }
            if (GameMetricConverter.IsServiceAvailable(s.HomelessCount))
            {
                sb.AppendLine($"  无家可归者：{s.HomelessCount:N0}人（千人比{s.HomelessPerCapita:F2}‰）");
            }
            if (s.CollectedMail > 0 || s.DeliveredMail > 0)
            {
                sb.AppendLine($"  邮件收集：{s.CollectedMail:N0}件  投递：{s.DeliveredMail:N0}件");
            }
            sb.AppendLine($"  生活质量综合指数：{s.QualityOfLifeIndex:F1}/100");
            sb.AppendLine();

            sb.AppendLine("【家庭数据】");
            if (GameMetricConverter.IsServiceAvailable(h.HouseholdCount))
            {
                sb.AppendLine($"  家庭总数：{h.HouseholdCount:N0}户");
                sb.AppendLine($"  家庭总财富：₡{h.HouseholdWealth:N0}");
                sb.AppendLine($"  户均财富：₡{h.AvgWealthPerHousehold:F1}");
                sb.AppendLine($"  户均人口：{h.AvgPersonsPerHousehold:F1}人/户");
            }
            else
            {
                sb.AppendLine($"  家庭数据暂无，居民尚未定居");
            }
            sb.AppendLine();

            sb.AppendLine("【趋势变化】");
            AppendTrend(sb, "人口增长率", tr.PopGrowthRate);
            AppendTrend(sb, "收入增长率", tr.IncomeGrowthRate);
            AppendTrend(sb, "幸福度变化", tr.HappinessTrend);
            AppendTrend(sb, "健康度变化", tr.HealthTrend);
            AppendTrend(sb, "犯罪率变化", tr.CrimeTrend);
            if (GameMetricConverter.IsServiceAvailable(tr.TourismTrend))
            {
                AppendTrend(sb, "旅游趋势", tr.TourismTrend);
            }
            sb.AppendLine();

            if (analysis.Alerts.Count > 0)
            {
                sb.AppendLine("【风险告警】");
                foreach (var alert in analysis.Alerts)
                {
                    var level = alert.Level == "danger" ? "严重" : "警告";
                    sb.AppendLine($"  [{level}] {alert.Category}：{alert.Message}");
                }
                sb.AppendLine();
            }

            if (analysis.Scores.Count > 0)
            {
                sb.AppendLine("【综合评分】");
                foreach (var score in analysis.Scores)
                {
                    sb.AppendLine($"  {score.Category}/{score.Name}：{score.Score:F1}分（{score.Grade}）——{score.Description}");
                }
                var avg = analysis.Scores.Average(sc => sc.Score);
                sb.AppendLine($"  综合评分：{avg:F1}分");
                sb.AppendLine();
            }

            if (history.Count >= 3)
            {
                sb.AppendLine("【近期趋势数据】（最近12个数据点）");
                var recent = history.TakeLast(Math.Min(12, history.Count)).ToList();
                foreach (var snap in recent)
                    sb.AppendLine($"  Y{snap.GameYear}M{snap.GameMonth}: 人口{snap.Population:N0} 幸福度{snap.Wellbeing:F1}% 健康度{snap.Health:F1}% 收入₡{snap.Income:N0} 支出₡{snap.Expense:N0}");
            }

            return sb.ToString();
        }

        private static void AppendIfAvailable(StringBuilder sb, string label, double value, string format)
        {
            if (GameMetricConverter.IsServiceAvailable(value))
                sb.AppendLine($"  {label}：{string.Format(format, value)}");
            else
                sb.AppendLine($"  {label}：尚未发展");
        }

        private static void AppendIfAvailable(StringBuilder sb, string label, int value, string formatOrDescription)
        {
            if (GameMetricConverter.IsServiceAvailable(value))
                sb.AppendLine($"  {label}：{string.Format(formatOrDescription, value)}");
            else if (formatOrDescription.Contains("暂无"))
                sb.AppendLine($"  {label}：{formatOrDescription}");
        }

        private static void AppendSector(StringBuilder sb, string name, SectorInfo info, double wealthPct, double fillRate)
        {
            if (GameMetricConverter.IsServiceAvailable(info.Wealth) || GameMetricConverter.IsServiceAvailable(info.Count))
            {
                sb.AppendLine($"  {name}：财富₡{info.Wealth:N0}，企业{info.Count:N0}家，从业{info.Workers:N0}/{info.MaxWorkers:N0}人（填充率{fillRate:F1}%），占比{wealthPct:F1}%");
            }
            else
            {
                sb.AppendLine($"  {name}：尚未发展");
            }
        }

        private static void AppendTransportLine(StringBuilder sb, string name, int passengers, double share)
        {
            if (GameMetricConverter.IsServiceAvailable(passengers))
                sb.AppendLine($"  {name}：{passengers:N0}人次（占比{share:F1}%）");
        }

        private static void AppendTrend(StringBuilder sb, string name, double value)
        {
            if (GameMetricConverter.IsServiceAvailable(Math.Abs(value)))
            {
                var arrow = value > 0 ? "↑" : value < 0 ? "↓" : "→";
                sb.AppendLine($"  {name}：{value:+0.0;-0.0}% {arrow}");
            }
        }

        private static string TranslateMomentum(string momentum)
        {
            return momentum switch
            {
                "Strong Growth" => "强劲增长",
                "Moderate Growth" => "温和增长",
                "Stable" => "保持稳定",
                "Slight Decline" => "轻微下滑",
                "Declining" => "明显下滑",
                "Rapid Decline" => "快速下滑",
                "Rising - social conditions improving" => "持续改善",
                "Stable - social conditions steady" => "保持稳定",
                "Declining - social conditions worsening" => "明显恶化",
                "Mixed - some indicators diverging" => "分化发展",
                _ => momentum
            };
        }

        public static string GetOpeningPrompt(string cityName, string dataContext)
        {
            return $@"根据以下{cityName}城市数据，撰写政府工作报告的开场白和总体回顾（600字左右）：

## 数据
{dataContext}

## 要求
1. 以""各位市民代表：现在，我代表市人民政府，向大会报告工作，请予审议，并请各位列席人员提出意见。""开头
2. 用一段话概括本报告期城市运行总体态势
3. 突出2-3个最亮眼的成绩（用具体数据支撑）
4. 指出1-2个需要关注的问题
5. 对于尚未发展的服务领域，可简要提及""正积极谋划""，不编造数据
6. 使用报告体语言，严禁出现""根据数据""""数据显示""等元描述
7. 直接以报告正文形式输出，不加任何前缀说明";
        }

        public static string GetDemographicsPrompt(string dataContext)
        {
            return $@"撰写政府工作报告中""人口发展与民生保障""章节（500字左右）：

## 数据
{dataContext}

## 要求
1. 报告人口总量及增长情况，分析迁入迁出动态
2. 汇报出生率、死亡率、自然增长率
3. 分析成年人口结构和劳动力供给
4. 评述市民幸福感和健康水平的变化趋势
5. 如某些数据为0，说明该领域尚处于起步阶段
6. 格式：以""一、人口持续增长，民生福祉不断改善""作为标题
7. 正文分2-3个自然段
8. 直接输出报告正文";
        }

        public static string GetEconomyPrompt(string dataContext)
        {
            return $@"撰写政府工作报告中""经济运行稳中向好""章节（600字左右）：

## 数据
{dataContext}

## 要求
1. 汇报财政收支总体情况，分析盈余或赤字
2. 分析税收结构（如税收为0说明财税体系尚未成型）
3. 汇报贸易发展情况和城市发展能力
4. 对比人均收入和人均支出，分析居民经济状况
5. 结合财政健康度描述，给出总体经济评价
6. 格式：以""二、经济运行稳中提质，财政状况持续改善""为标题
7. 直接输出报告正文";
        }

        public static string GetIndustryPrompt(string dataContext)
        {
            return $@"撰写政府工作报告中""产业结构优化升级""章节（500字左右）：

## 数据
{dataContext}

## 要求
1. 汇报三大产业（服务业、加工业、办公业）发展情况
2. 分析各产业财富占比、企业数量、就业填充率
3. 对于尚未发展的产业，说明""处于谋划阶段""
4. 指出产业结构特点和优化方向
5. 格式：以""三、产业结构优化升级，发展动能持续增强""为标题
6. 直接输出报告正文";
        }

        public static string GetEmploymentPrompt(string dataContext)
        {
            return $@"撰写政府工作报告中""就业与社会保障""章节（400字左右）：

## 数据
{dataContext}

## 要求
1. 汇报就业总体形势，分析失业率水平
2. 结合失业率评估描述给出分析
3. 如公务服务体系尚未建立则简要提及
4. 分析高级技工需求和人才结构
5. 格式：以""四、就业形势总体稳定，社会保障体系不断完善""为标题
6. 直接输出报告正文";
        }

        public static string GetTransportPrompt(string dataContext)
        {
            return $@"撰写政府工作报告中""交通基础设施建设""章节（500字左右）：

## 数据
{dataContext}

## 要求
1. 汇报公共交通体系整体运行情况
2. 列举已有交通方式客运量及占比，未建设的交通方式不必列出
3. 如客运数据为0，说明交通体系处于规划阶段
4. 分析货运物流能力
5. 格式：以""五、交通基础设施建设扎实推进，出行条件持续改善""为标题
6. 直接输出报告正文";
        }

        public static string GetSocialPrompt(string dataContext)
        {
            return $@"撰写政府工作报告中""社会民生事业""章节（500字左右）：

## 数据
{dataContext}

## 要求
1. 结合幸福度描述和健康描述分析居民生活质量
2. 如教育体系尚未建立则如实说明
3. 分析治安状况，结合犯罪率描述
4. 报告医疗卫生水平
5. 如有无家可归者需提及其影响
6. 格式：以""六、社会民生事业全面发展，市民获得感持续增强""为标题
7. 对问题坦诚面对
8. 直接输出报告正文";
        }

        public static string GetFiscalPrompt(string dataContext)
        {
            return $@"撰写政府工作报告中""财政收支与家庭生活""章节（400字左右）：

## 数据
{dataContext}

## 要求
1. 结合财政健康度描述分析财政状况
2. 汇报税收依赖度和贸易依赖度
3. 报告家庭收入与消费水平
4. 如家庭数据为0说明居民尚未定居
5. 格式：以""七、财政运行稳健，居民生活水平稳步提高""为标题
6. 直接输出报告正文";
        }

        public static string GetChallengesPrompt(string dataContext)
        {
            return $@"撰写政府工作报告中""面临的问题与挑战""章节（350字左右）：

## 数据
{dataContext}

## 要求
1. 冷静客观指出城市发展中的短板
2. 必须有具体数据支撑（如某指标下降X%）
3. 语气：""必须清醒看到……"" ""仍然存在……"" ""有待进一步加强""
4. 对于尚未发展的领域，表述为""尚有很大发展空间""
5. 格式：以""我们也清醒认识到，城市发展中还面临不少困难和挑战""开头
6. 列举2-3个问题
7. 直接输出报告正文";
        }

        public static string GetOutlookPrompt(string dataContext)
        {
            return $@"撰写政府工作报告中""下阶段工作部署""章节（500字左右）：

## 数据
{dataContext}

## 要求
1. 提出下一阶段总体目标
2. 分3-5个方面部署重点工作
3. 每项部署包括：目标方向 + 具体措施
4. 对于目前尚未发展的领域，可提出""启动XX体系建设""
5. 语言要有力、有方向感
6. 使用""要……""""必须……""""着力……""等句式
7. 格式：以""八、凝心聚力，奋力开创城市发展新局面""为标题
8. 最后以鼓舞人心的结语收尾：
   ""各位代表！使命重在担当，实干铸就辉煌。让我们更加紧密地团结起来，锐意进取、攻坚克难，为把我市建设成为繁荣、宜居、和谐的现代化都市而不懈奋斗！""
9. 直接输出报告正文";
        }

        private static string TranslateFiscalStatus(string status)
        {
            return status switch
            {
                "Highly Surplus" => "高盈余",
                "Surplus" => "盈余",
                "Balanced" => "平衡",
                "Mild Deficit" => "轻微赤字",
                "Deficit" => "赤字",
                "Severe Deficit" => "严重赤字",
                _ => status
            };
        }
    }
}