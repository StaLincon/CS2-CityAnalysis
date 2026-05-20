using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataAnalyzer.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using static DataAnalyzer.Services.ReportLayoutHelper;

namespace DataAnalyzer.Services
{
    public class PreviewReportGenerator
    {
        private readonly CityAnalysisReport m_Analysis;
        private readonly StatisticSnapshot m_Current;
        private readonly List<StatisticSnapshot> m_History;
        private readonly int m_KUpdatesPerDay;
        private readonly string m_CityName;
        private readonly string m_OutputPath;
        private readonly ChartRenderer m_ChartRenderer;

        public PreviewReportGenerator(CityAnalysisReport analysis,
            StatisticSnapshot current, List<StatisticSnapshot> history,
            int kUpdatesPerDay, string cityName, string outputPath)
        {
            m_Analysis = analysis;
            m_Current = current;
            m_History = history;
            m_KUpdatesPerDay = kUpdatesPerDay;
            m_CityName = cityName;
            m_OutputPath = outputPath;
            m_ChartRenderer = new ChartRenderer(m_History, m_Current, m_Analysis);
        }

        public void Generate()
        {
            var dir = Path.GetDirectoryName(m_OutputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using var doc = WordprocessingDocument.Create(m_OutputPath, WordprocessingDocumentType.Document);
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // 页脚（GB/T 9704-2012 页码）
            var footerPart = mainPart.AddNewPart<FooterPart>();
            footerPart.Footer = CreatePageNumberFooter();
            var footerId = mainPart.GetIdOfPart(footerPart);

            // 页面设置（GB/T 9704-2012）
            body.AppendChild(CreatePageSettings(null, footerId));

            // ── 版头部分（GB/T 9704-2012 第7章）──
            BuildDocumentHeader(body, m_CityName);

            // ── 主体部分（GB/T 9704-2012 第8章）──
            // 公文标题
            body.AppendChild(CreateDocumentTitle($"关于{m_CityName}政府工作的报告"));
            // 主送机关
            body.AppendChild(CreateAddressee("各位代表："));

            // 正文各章
            AddOpening(body);
            AddDemographics(body, mainPart);
            AddEconomy(body, mainPart);
            AddIndustry(body);
            AddEmployment(body);
            AddTransport(body, mainPart);
            AddSocial(body, mainPart);
            AddFiscal(body);
            AddChallenges(body);
            AddOutlook(body);

            // 发文机关署名
            body.AppendChild(CreateIssuingAuthority($"{m_CityName}人民政府"));
            // 成文日期
            body.AppendChild(CreateIssueDate());

            // ── 附录 ──
            AddScorecardSection(body);
            AddAppendixTable(body);

            // ── 版记部分（GB/T 9704-2012 第10章）──
            BuildBanJi(body, m_CityName);

            mainPart.Document.Save();
        }

        // ══════════════════════════════════════════════
        //  各章内容（预览版使用硬编码文本模板）
        // ══════════════════════════════════════════════
        private void AddOpening(Body body)
        {
            body.AppendChild(CreateSectionHeading("一、开场致辞"));
            body.AppendChild(CreateBodyParagraph($"现在，我代表{m_CityName}人民政府，向大会报告政府工作，请予审议。"));
            body.AppendChild(CreateBodyParagraph($"过去一段时期，{m_CityName}在各位市民的支持下，取得了显著发展。本报告基于城市运行数据，全面回顾各项工作进展，分析当前形势，明确下一阶段发展目标。"));
            body.AppendChild(CreatePageBreak());
        }

        private void AddDemographics(Body body, MainDocumentPart mainPart)
        {
            body.AppendChild(CreateSectionHeading("二、人口发展概况"));

            var pop = m_Analysis.Demographics;
            body.AppendChild(CreateBodyParagraph($"截至第{m_Current.GameYear}年{m_Current.GameMonth}月，{m_CityName}常住人口达到{m_Current.Population:N0}人。"));

            if (pop.GrowthRate != 0)
            {
                var trend = pop.GrowthRate > 0 ? "增长" : "下降";
                body.AppendChild(CreateBodyParagraph($"人口{trend}趋势明显，增长率为{Math.Abs(pop.GrowthRate):F2}%。"));
            }

            if (m_History.Count >= 2)
            {
                var first = m_History.First();
                var last = m_History.Last();
                var netChange = last.Population - first.Population;
                body.AppendChild(CreateBodyParagraph($"统计周期内，城市人口净{(netChange >= 0 ? "增加" : "减少")}{Math.Abs(netChange):N0}人，显示出城市{(netChange >= 0 ? "吸引力持续增强" : "面临人口流失压力")}。"));
            }

            body.AppendChild(CreateBodyParagraph($"迁入人口{m_Current.CitizensMovedIn:N0}人，迁出人口{m_Current.CitizensMovedAway:N0}人，净迁移{(m_Current.CitizensMovedIn - m_Current.CitizensMovedAway):N0}人。"));

            AddChartToBody(body, mainPart, "图1 人口变化趋势图", () => m_ChartRenderer.GeneratePopulationChart(), "chart_pop.png");
            body.AppendChild(CreatePageBreak());
        }

        private void AddEconomy(Body body, MainDocumentPart mainPart)
        {
            body.AppendChild(CreateSectionHeading("三、经济运行情况"));

            var eco = m_Analysis.Economy;
            body.AppendChild(CreateBodyParagraph($"本市经济运行总体平稳。财政收入方面，月度收入达到₡{m_Current.Income:N0}，支出₡{m_Current.Expense:N0}，{(eco.NetIncome >= 0 ? "财政盈余" : "财政赤字")}₡{Math.Abs(eco.NetIncome):N0}。"));

            if (eco.MoneyGrowth != 0)
            {
                body.AppendChild(CreateBodyParagraph($"财政收入同比{(eco.MoneyGrowth >= 0 ? "增长" : "下降")}{Math.Abs(eco.MoneyGrowth):F1}%，{(eco.MoneyGrowth >= 0 ? "显示出良好的增收势头" : "需要关注收入来源")}。"));
            }

            var taxTotal = eco.TotalTax;
            if (taxTotal > 0)
            {
                body.AppendChild(CreateBodyParagraph($"税收结构方面，住宅税占比{eco.ResidentialTaxPct:F1}%，商业税{eco.CommercialTaxPct:F1}%，工业税{eco.IndustrialTaxPct:F1}%，办公税{eco.OfficeTaxPct:F1}%。"));
            }

            AddChartToBody(body, mainPart, "图2 财政收支趋势图", () => m_ChartRenderer.GenerateIncomeExpenseChart(), "chart_income.png");
            AddChartToBody(body, mainPart, "图3 税收结构趋势图", () => m_ChartRenderer.GenerateTaxChart(), "chart_tax.png");
            body.AppendChild(CreatePageBreak());
        }

        private void AddIndustry(Body body)
        {
            body.AppendChild(CreateSectionHeading("四、产业发展情况"));

            var sec = m_Analysis.Sectors;
            if (GameMetricConverter.IsServiceAvailable(sec.TotalWealth))
            {
                body.AppendChild(CreateBodyParagraph($"本市产业经济蓬勃发展，三大产业部门合计创造财富₡{sec.TotalWealth:N0}。"));

                AddIndustryTable(body);

                var leader = AnalyzeIndustryLeader(sec);
                body.AppendChild(CreateBodyParagraph(leader));
                body.AppendChild(CreateBodyParagraph($"从就业角度看，三大产业共吸纳就业{sec.TotalWorkers:N0}人，平均就业填充率{(sec.TotalWorkers / (double)sec.TotalMaxWorkers * 100):F1}%，{(sec.TotalWorkers / (double)sec.TotalMaxWorkers > 0.8 ? "劳动力市场活跃" : "仍有招工空间")}。"));
            }
            else
            {
                body.AppendChild(CreateBodyParagraph("本市产业经济尚处于起步阶段，各项产业指标有待进一步发展。"));
            }

            body.AppendChild(CreatePageBreak());
        }

        private void AddEmployment(Body body)
        {
            body.AppendChild(CreateSectionHeading("五、就业与劳动力"));

            var emp = m_Analysis.Employment;
            body.AppendChild(CreateBodyParagraph($"本市劳动力市场运行总体{GameMetricConverter.ToUnemploymentAssessment(emp.UnemploymentRate)}。"));
            body.AppendChild(CreateBodyParagraph($"全市从业人员{emp.WorkerCount:N0}人，失业人口{m_Current.Unemployed:N0}人，失业率为{emp.UnemploymentRate:F1}%。"));

            if (GameMetricConverter.IsServiceAvailable(emp.CityServiceWorkers))
            {
                body.AppendChild(CreateBodyParagraph($"城市公共服务部门从业人员{emp.CityServiceWorkers:N0}人，编制填充率{emp.CityServiceFillRate:F1}%，{(emp.CityServiceFillRate > 80 ? "公共服务力量充足" : "需要加强人员配备")}。"));
            }

            if (emp.SeniorWorkerDemand > 0)
            {
                body.AppendChild(CreateBodyParagraph($"高级技工需求指数{emp.SeniorWorkerDemand:F1}%，{(emp.SeniorWorkerDemand > 70 ? "技能人才紧缺，需加大引进力度" : "技能人才供给相对充足")}。"));
            }

            AddEmploymentTable(body);
            body.AppendChild(CreatePageBreak());
        }

        private void AddTransport(Body body, MainDocumentPart mainPart)
        {
            body.AppendChild(CreateSectionHeading("六、交通运行情况"));

            var trans = m_Analysis.Transport;
            body.AppendChild(CreateBodyParagraph($"城市交通系统运行平稳，公共交通日均客流量{trans.TotalPassengers:N0}人次。"));

            var modes = new List<string>();
            if (m_Current.PassengerCountBus > 0) modes.Add($"公交{m_Current.PassengerCountBus:N0}人次");
            if (m_Current.PassengerCountSubway > 0) modes.Add($"地铁{m_Current.PassengerCountSubway:N0}人次");
            if (m_Current.PassengerCountTrain > 0) modes.Add($"火车{m_Current.PassengerCountTrain:N0}人次");
            if (m_Current.PassengerCountTram > 0) modes.Add($"有轨电车{m_Current.PassengerCountTram:N0}人次");
            if (m_Current.PassengerCountAirplane > 0) modes.Add($"航空{m_Current.PassengerCountAirplane:N0}人次");
            if (m_Current.PassengerCountShip > 0) modes.Add($"船舶{m_Current.PassengerCountShip:N0}人次");
            if (m_Current.PassengerCountTaxi > 0) modes.Add($"出租车{m_Current.PassengerCountTaxi:N0}人次");

            if (modes.Count > 0)
                body.AppendChild(CreateBodyParagraph($"分方式来看，{string.Join("、", modes)}。"));

            if (trans.TotalCargo > 0)
                body.AppendChild(CreateBodyParagraph($"货运方面，各类交通工具共运送货物{trans.TotalCargo:N0}单位，物流体系运转良好。"));

            AddChartToBody(body, mainPart, "图4 公共交通客流量分布", () => m_ChartRenderer.GenerateTransportChart(), "chart_transport.png");
            body.AppendChild(CreatePageBreak());
        }

        private void AddSocial(Body body, MainDocumentPart mainPart)
        {
            body.AppendChild(CreateSectionHeading("七、社会民生情况"));

            var soc = m_Analysis.Social;
            body.AppendChild(CreateBodyParagraph($"市民生活质量持续改善。幸福度达到{m_Current.Wellbeing:F1}%，处于{GameMetricConverter.ToHappinessLevel(m_Current.Wellbeing)}水平。"));
            body.AppendChild(CreateBodyParagraph($"健康指标方面，市民健康度为{m_Current.Health:F1}%，{GameMetricConverter.ToHealthDescription(m_Current.Health)}。"));

            if (m_Current.HomelessCount > 0)
            {
                body.AppendChild(CreateBodyParagraph($"住房保障方面，全市无家可归者{m_Current.HomelessCount:N0}人，{(m_Current.HomelessCount > m_Current.Population * 0.01 ? "住房问题较为突出，需加大保障力度" : "住房保障体系基本完善")}。"));
            }

            if (m_Current.CrimeCount > 0 || m_Current.CrimeRate > 0)
            {
                body.AppendChild(CreateBodyParagraph($"社会治安方面，犯罪率{m_Current.CrimeRate:F1}%，{GameMetricConverter.ToCrimeDescription(m_Current.CrimeRate)}。"));
            }

            body.AppendChild(CreateBodyParagraph($"综合生活质量指数{soc.QualityOfLifeIndex:F1}分（满分100分），{GameMetricConverter.ToBudgetHealthDescription((int)soc.QualityOfLifeIndex)}。"));

            AddChartToBody(body, mainPart, "图5 市民幸福度变化趋势", () => m_ChartRenderer.GenerateWellbeingChart(), "chart_wellbeing.png");
            body.AppendChild(CreatePageBreak());
        }

        private void AddFiscal(Body body)
        {
            body.AppendChild(CreateSectionHeading("八、财政收支明细"));

            var eco = m_Analysis.Economy;
            var fiscalHealth = eco.NetIncome >= 0 ? "健康" : "需关注";
            body.AppendChild(CreateBodyParagraph($"本市财政运行{fiscalHealth}。"));
            body.AppendChild(CreateBodyParagraph($"本期财政收入₡{m_Current.Income:N0}，财政支出₡{m_Current.Expense:N0}，{(eco.NetIncome >= 0 ? "实现盈余" : "出现赤字")}₡{Math.Abs(eco.NetIncome):N0}。"));

            if (m_Current.Money != 0)
            {
                body.AppendChild(CreateBodyParagraph($"财政储备余额₡{m_Current.Money:N0}，{(m_Current.Money > m_Current.Expense * 3 ? "财政储备充足，抗风险能力强" : m_Current.Money > 0 ? "财政储备尚可，需关注收支平衡" : "财政储备不足，需采取措施改善")}。"));
            }

            AddFiscalTable(body);
            body.AppendChild(CreatePageBreak());
        }

        private void AddChallenges(Body body)
        {
            body.AppendChild(CreateSectionHeading("九、面临的挑战"));

            if (m_Analysis.Alerts.Count > 0)
            {
                body.AppendChild(CreateBodyParagraph("当前城市发展面临以下主要挑战："));
                foreach (var alert in m_Analysis.Alerts.Take(5))
                {
                    body.AppendChild(CreateBodyParagraph($"- {alert.Message}"));
                }
            }
            else
            {
                body.AppendChild(CreateBodyParagraph("当前城市运行总体平稳，各项主要指标处于合理区间。但仍需持续关注人口增长、产业升级、环境保护等长期议题，确保城市可持续发展。"));
            }

            body.AppendChild(CreatePageBreak());
        }

        private void AddOutlook(Body body)
        {
            body.AppendChild(CreateSectionHeading("十、未来展望"));

            body.AppendChild(CreateBodyParagraph($"展望未来，{m_CityName}将继续坚持高质量发展道路。"));
            body.AppendChild(CreateBodyParagraph("下一阶段重点工作包括：一是持续优化产业结构，提升经济发展质量；二是完善公共服务体系，增进民生福祉；三是加强基础设施建设，提升城市承载能力；四是推进绿色发展，建设生态宜居城市。"));
            body.AppendChild(CreateBodyParagraph($"我们坚信，在全体市民的共同努力下，{m_CityName}必将迎来更加美好的明天！"));
            body.AppendChild(CreateSpacer(2));

            body.AppendChild(CreateCenteredText($"{m_CityName}人民政府", SizeCoverInfo, false, BodyColor));
            body.AppendChild(CreateCenteredText($"{DateTime.Now:yyyy年MM月dd日}", SizeCoverInfo, false, BodyColor));

            body.AppendChild(CreatePageBreak());
        }

        // ══════════════════════════════════════════════
        //  图表
        // ══════════════════════════════════════════════
        private void AddChartToBody(Body body, MainDocumentPart mainPart, string title, Func<byte[]> chartFunc, string imageName)
        {
            body.AppendChild(CreateSpacer(1));
            body.AppendChild(CreateTableTitle(title));
            body.AppendChild(CreateSpacer(1));
            try
            {
                var imageBytes = chartFunc();
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    var imagePart = mainPart.AddImagePart(ImagePartType.Png);
                    using (var ms = new MemoryStream(imageBytes)) imagePart.FeedData(ms);
                    var imageId = mainPart.GetIdOfPart(imagePart);
                    var element = ImageHelper.CreateDrawing(imageId, title);
                    body.AppendChild(new Paragraph(new Run(element)));
                }
            }
            catch (Exception ex)
            {
                body.AppendChild(CreateBodyParagraph($"（图表生成失败：{ex.Message}）"));
            }
            body.AppendChild(CreateSpacer(1));
        }

        // ══════════════════════════════════════════════
        //  数据表格
        // ══════════════════════════════════════════════
        private void AddIndustryTable(Body body)
        {
            var sec = m_Analysis.Sectors;
            if (!GameMetricConverter.IsServiceAvailable(sec.TotalWealth)) return;

            body.AppendChild(CreateSpacer(1));
            body.AppendChild(CreateTableTitle("表1 产业结构数据"));
            body.AppendChild(CreateTableCaption("反映了本市三大产业部门的财富贡献、企业规模及就业吸纳情况"));
            body.AppendChild(CreateSpacer(1));

            var table = CreateStyledTable(new[] { "产业", "财富（₡）", "企业数", "从业/满编", "填充率", "财富占比" });
            AddTableRow(table, new[] { "服务业", $"{sec.Service.Wealth:N0}", $"{sec.Service.Count:N0}", $"{sec.Service.Workers}/{sec.Service.MaxWorkers}", $"{sec.ServiceWorkerFillRate:F1}%", $"{sec.ServiceWealthPct:F1}%" });
            AddTableRow(table, new[] { "加工业", $"{sec.Processing.Wealth:N0}", $"{sec.Processing.Count:N0}", $"{sec.Processing.Workers}/{sec.Processing.MaxWorkers}", $"{sec.ProcessingWorkerFillRate:F1}%", $"{sec.ProcessingWealthPct:F1}%" }, alt: true);
            AddTableRow(table, new[] { "办公业", $"{sec.Office.Wealth:N0}", $"{sec.Office.Count:N0}", $"{sec.Office.Workers}/{sec.Office.MaxWorkers}", $"{sec.OfficeWorkerFillRate:F1}%", $"{sec.OfficeWealthPct:F1}%" });
            AddTableRow(table, new[] { "合计", $"{sec.TotalWealth:N0}", $"{sec.TotalCount:N0}", $"{sec.TotalWorkers}/{sec.TotalMaxWorkers}", "—", "100%" }, alt: true);
            body.AppendChild(table);
            body.AppendChild(CreateSpacer(1));
        }

        private string AnalyzeIndustryLeader(SectorAnalysis sec)
        {
            if (sec.ServiceWealthPct >= sec.ProcessingWealthPct && sec.ServiceWealthPct >= sec.OfficeWealthPct)
                return $"服务业以{sec.ServiceWealthPct:F1}%的财富占比成为本市第一大产业，企业数量达{sec.Service.Count:N0}家，就业填充率{sec.ServiceWorkerFillRate:F1}%，展现出强劲的发展态势。";
            if (sec.ProcessingWealthPct >= sec.OfficeWealthPct)
                return $"加工业以{sec.ProcessingWealthPct:F1}%的财富占比成为本市第一大产业，企业数量达{sec.Processing.Count:N0}家，就业填充率{sec.ProcessingWorkerFillRate:F1}%，是城市经济的重要支柱。";
            return $"办公业以{sec.OfficeWealthPct:F1}%的财富占比成为本市第一大产业，企业数量达{sec.Office.Count:N0}家，就业填充率{sec.OfficeWorkerFillRate:F1}%，高端商务经济蓬勃发展。";
        }

        private void AddEmploymentTable(Body body)
        {
            var emp = m_Analysis.Employment;
            body.AppendChild(CreateSpacer(1));
            body.AppendChild(CreateTableTitle("表2 就业数据"));
            body.AppendChild(CreateTableCaption("反映本市劳动力市场运行状况"));
            body.AppendChild(CreateSpacer(1));

            var table = CreateStyledTable(new[] { "指标", "数值", "评估" });
            AddTableRow(table, new[] { "从业人员", $"{emp.WorkerCount:N0}人", "—" });
            AddTableRow(table, new[] { "失业率", $"{emp.UnemploymentRate:F1}%", emp.UnemploymentRate < 5 ? "良好" : emp.UnemploymentRate < 10 ? "一般" : "需要关注" }, alt: true);
            AddTableRow(table, new[] { "劳动参与率", $"{emp.WorkforceParticipation:F1}%", emp.WorkforceParticipation > 50 ? "积极参与" : "偏低" });
            if (GameMetricConverter.IsServiceAvailable(emp.CityServiceWorkers))
                AddTableRow(table, new[] { "公务人员填充率", $"{emp.CityServiceFillRate:F1}%", emp.CityServiceFillRate > 80 ? "充足" : "不足" }, alt: true);
            if (emp.SeniorWorkerDemand > 0)
                AddTableRow(table, new[] { "高级技工需求", $"{emp.SeniorWorkerDemand:F1}%", emp.SeniorWorkerDemand > 70 ? "紧缺" : "正常" });
            body.AppendChild(table);
            body.AppendChild(CreateSpacer(1));
        }

        private void AddFiscalTable(Body body)
        {
            var eco = m_Analysis.Economy;
            body.AppendChild(CreateSpacer(1));
            body.AppendChild(CreateTableTitle("表3 财政收支明细"));
            body.AppendChild(CreateTableCaption("反映本期财政运行情况"));
            body.AppendChild(CreateSpacer(1));

            var table = CreateStyledTable(new[] { "项目", "金额（₡）", "说明" });
            AddTableRow(table, new[] { "财政收入", $"₡{m_Current.Income:N0}", "本期收入总额" });
            AddTableRow(table, new[] { "财政支出", $"₡{m_Current.Expense:N0}", "本期支出总额" }, alt: true);
            AddTableRow(table, new[] { "收支差额", $"₡{eco.NetIncome:N0}", eco.NetIncome >= 0 ? "盈余" : "赤字" });
            AddTableRow(table, new[] { "财政储备", $"₡{m_Current.Money:N0}", "当前资金余额" }, alt: true);
            body.AppendChild(table);
            body.AppendChild(CreateSpacer(1));
        }

        // ══════════════════════════════════════════════
        //  附录
        // ══════════════════════════════════════════════
        private void AddScorecardSection(Body body)
        {
            body.AppendChild(CreateSectionHeading("附录：城市运行评分卡"));
            body.AppendChild(CreateSpacer(1));
            body.AppendChild(CreateTableTitle("综合评估指标一览"));
            body.AppendChild(CreateSpacer(1));

            var table = CreateStyledTable(new[] { "维度", "指标", "数值", "评级" });

            AddTableRow(table, new[] { "人口", "总人口", $"{m_Analysis.Demographics.Population:N0}", "—" });
            if (m_Analysis.Demographics.GrowthRate != 0)
                AddTableRow(table, new[] { "人口", "增长率", $"{m_Analysis.Demographics.GrowthRate:F2}%", m_Analysis.Demographics.GrowthRate > 0 ? "↑" : "↓" }, alt: true);

            AddTableRow(table, new[] { "经济", "财政余额", $"₡{m_Analysis.Economy.NetIncome:N0}", m_Analysis.Economy.NetIncome >= 0 ? "健康" : "警示" });
            if (m_Analysis.Economy.MoneyGrowth != 0)
                AddTableRow(table, new[] { "经济", "收入增长", $"{m_Analysis.Economy.MoneyGrowth:F1}%", m_Analysis.Economy.MoneyGrowth >= 0 ? "良好" : "需关注" }, alt: true);

            AddTableRow(table, new[] { "就业", "失业率", $"{m_Analysis.Employment.UnemploymentRate:F1}%", GameMetricConverter.ToUnemploymentAssessment(m_Analysis.Employment.UnemploymentRate) });
            AddTableRow(table, new[] { "就业", "劳动参与率", $"{m_Analysis.Employment.WorkforceParticipation:F1}%", m_Analysis.Employment.WorkforceParticipation > 50 ? "积极" : "偏低" }, alt: true);

            AddTableRow(table, new[] { "社会", "幸福度", $"{m_Analysis.Social.Wellbeing:F1}%", GameMetricConverter.ToHappinessLevel(m_Analysis.Social.Wellbeing) });
            AddTableRow(table, new[] { "社会", "健康度", $"{m_Analysis.Social.Health:F1}%", GameMetricConverter.ToHealthDescription(m_Analysis.Social.Health) }, alt: true);
            AddTableRow(table, new[] { "社会", "生活质量指数", $"{m_Analysis.Social.QualityOfLifeIndex:F1}/100", GameMetricConverter.ToBudgetHealthDescription((int)m_Analysis.Social.QualityOfLifeIndex) });

            if (GameMetricConverter.IsServiceAvailable(m_Analysis.Sectors.TotalWealth))
            {
                AddTableRow(table, new[] { "产业", "总财富", $"₡{m_Analysis.Sectors.TotalWealth:N0}", "—" }, alt: true);
                AddTableRow(table, new[] { "产业", "企业总数", $"{m_Analysis.Sectors.TotalCount:N0}", "—" });
            }

            body.AppendChild(table);
            body.AppendChild(CreateSpacer(1));
            body.AppendChild(CreateBodyParagraph("注：本评分卡基于城市运行数据自动生成，供决策参考。"));

            body.AppendChild(CreatePageBreak());
        }

        private void AddAppendixTable(Body body)
        {
            body.AppendChild(CreateSectionHeading("数据附录"));
            body.AppendChild(CreateSpacer(1));
            body.AppendChild(CreateTableTitle("主要统计指标原始数据"));
            body.AppendChild(CreateSpacer(1));

            var table = CreateStyledTable(new[] { "类别", "指标", "数值" });

            AddTableRow(table, new[] { "基础", "游戏时间", $"第{m_Current.GameYear}年 第{m_Current.GameMonth}月" });
            AddTableRow(table, new[] { "基础", "人口", $"{m_Current.Population:N0}" }, alt: true);
            AddTableRow(table, new[] { "基础", "资金", $"₡{m_Current.Money:N0}" });

            AddTableRow(table, new[] { "人口", "迁入", $"{m_Current.CitizensMovedIn:N0}" }, alt: true);
            AddTableRow(table, new[] { "人口", "迁出", $"{m_Current.CitizensMovedAway:N0}" });
            AddTableRow(table, new[] { "人口", "出生率", $"{m_Current.BirthRate}" }, alt: true);
            AddTableRow(table, new[] { "人口", "死亡率", $"{m_Current.DeathRate}" });

            AddTableRow(table, new[] { "财政", "收入", $"₡{m_Current.Income:N0}" }, alt: true);
            AddTableRow(table, new[] { "财政", "支出", $"₡{m_Current.Expense:N0}" });
            AddTableRow(table, new[] { "财政", "贸易", $"₡{m_Current.Trade:N0}" }, alt: true);

            AddTableRow(table, new[] { "社会", "幸福度", $"{m_Analysis.Social.Wellbeing:F1}%" });
            AddTableRow(table, new[] { "社会", "健康度", $"{m_Analysis.Social.Health:F1}%" }, alt: true);
            AddTableRow(table, new[] { "社会", "无家可归者", $"{m_Current.HomelessCount:N0}" });

            if (GameMetricConverter.IsServiceAvailable(m_Current.TouristCount))
            {
                AddTableRow(table, new[] { "旅游", "游客数", $"{m_Current.TouristCount:N0}" }, alt: true);
                AddTableRow(table, new[] { "旅游", "旅游收入", $"₡{m_Current.TouristIncome:N0}" });
            }

            body.AppendChild(table);
        }
    }
}