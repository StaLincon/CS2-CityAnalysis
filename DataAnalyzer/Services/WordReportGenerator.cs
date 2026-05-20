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
    public class WordReportGenerator
    {
        private readonly List<ReportChapter> m_Chapters;
        private readonly CityAnalysisReport m_Analysis;
        private readonly StatisticSnapshot m_Current;
        private readonly List<StatisticSnapshot> m_History;
        private readonly int m_KUpdatesPerDay;
        private readonly string m_CityName;
        private readonly string m_OutputPath;
        private readonly ChartRenderer m_ChartRenderer;

        public WordReportGenerator(List<ReportChapter> chapters, CityAnalysisReport analysis,
            StatisticSnapshot current, List<StatisticSnapshot> history,
            int kUpdatesPerDay, string cityName, string outputPath)
        {
            m_Chapters = chapters;
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

            // ── 正文各章 ──
            BuildChapter(body, mainPart, "opening");

            BuildChapter(body, mainPart, "demographics",
                ("图1 人口变化趋势图", () => m_ChartRenderer.GeneratePopulationChart(), "chart_pop.png"));

            BuildChapter(body, mainPart, "economy",
                ("图2 财政收支趋势图", () => m_ChartRenderer.GenerateIncomeExpenseChart(), "chart_income.png"),
                extraCharts: new (string, Func<byte[]>, string)[] {
                    ("图3 税收结构趋势图", () => m_ChartRenderer.GenerateTaxChart(), "chart_tax.png")
                });

            BuildChapter(body, mainPart, "industry",
                ("图4 产业结构趋势图", () => m_ChartRenderer.GenerateSectorChart(), "chart_sector.png"),
                tableBuilder: () => AddIndustryTable(body));

            BuildChapter(body, mainPart, "employment",
                tableBuilder: () => AddEmploymentTable(body));

            BuildChapter(body, mainPart, "transport",
                ("图5 公共交通客流量分布", () => m_ChartRenderer.GenerateTransportChart(), "chart_transport.png"));

            BuildChapter(body, mainPart, "social",
                ("图6 市民幸福度与健康趋势", () => m_ChartRenderer.GenerateWellbeingChart(), "chart_wellbeing.png"));

            BuildChapter(body, mainPart, "fiscal",
                tableBuilder: () => AddFiscalTable(body));

            BuildChapter(body, mainPart, "challenges");
            BuildChapter(body, mainPart, "outlook");

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
        //  逐章构建（文本 + 图表 + 表格 + 分页）
        // ══════════════════════════════════════════════
        private void BuildChapter(Body body, MainDocumentPart mainPart, string chapterId,
            (string title, Func<byte[]> generator, string imageName)? chart = null,
            (string title, Func<byte[]> generator, string imageName)[] extraCharts = null,
            Action tableBuilder = null)
        {
            WriteChapterText(body, chapterId);

            // 主图表
            if (chart.HasValue)
                AddChartToBody(body, mainPart, chart.Value.title, chart.Value.generator, chart.Value.imageName);

            // 额外图表
            if (extraCharts != null)
            {
                foreach (var ec in extraCharts)
                    AddChartToBody(body, mainPart, ec.title, ec.generator, ec.imageName);
            }

            tableBuilder?.Invoke();

            body.AppendChild(CreatePageBreak());
        }

        private void WriteChapterText(Body body, string chapterId)
        {
            var chapter = m_Chapters.FirstOrDefault(c => c.Id == chapterId);
            if (chapter == null) return;

            var paragraphs = chapter.Content
                .Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim()).Where(p => p.Length > 0).ToList();

            foreach (var paraText in paragraphs)
            {
                if (IsSectionMarker(paraText))
                    body.AppendChild(CreateSectionHeading(paraText));
                else
                    body.AppendChild(CreateBodyParagraph(paraText));
            }
        }

        private static bool IsSectionMarker(string text)
        {
            if (text.Length > 30) return false;
            return text.StartsWith("一、") || text.StartsWith("二、") || text.StartsWith("三、") ||
                   text.StartsWith("四、") || text.StartsWith("五、") || text.StartsWith("六、") ||
                   text.StartsWith("七、") || text.StartsWith("八、") || text.StartsWith("九、") ||
                   text.StartsWith("十、");
        }

        // ══════════════════════════════════════════════
        //  图表插入
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

            var leader = AnalyzeIndustryLeader(sec);
            body.AppendChild(CreateBodyParagraph($"从上表可以看出，{leader}"));
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

            body.AppendChild(CreateBodyParagraph($"综合来看，本市{GameMetricConverter.ToUnemploymentAssessment(emp.UnemploymentRate)}。"));
        }

        private void AddFiscalTable(Body body)
        {
            var f = m_Analysis.Fiscal;
            var e = m_Analysis.Economy;
            body.AppendChild(CreateSpacer(1));
            body.AppendChild(CreateTableTitle("表3 财政数据"));
            body.AppendChild(CreateTableCaption("反映本市财政运行及居民经济状况"));
            body.AppendChild(CreateSpacer(1));

            var table = CreateStyledTable(new[] { "指标", "数值", "说明" });
            AddTableRow(table, new[] { "收支比", $"{f.RevenueExpenseRatio:F2}", f.IsSurplus ? "盈余" : "赤字" });
            AddTableRow(table, new[] { "税收依赖度", $"{f.TaxToIncomeRatio:F1}%", f.TaxToIncomeRatio > 80 ? "高度依赖" : "健康" }, alt: true);
            AddTableRow(table, new[] { "贸易依赖度", $"{f.TradeToIncomeRatio:F1}%", "贸易收入占总收入比重" });
            AddTableRow(table, new[] { "人均收入", $"₡{e.PerCapitaIncome:F1}", "每位居民平均贡献" }, alt: true);
            AddTableRow(table, new[] { "人均支出", $"₡{e.PerCapitaExpense:F1}", "每位居民平均享受" });
            body.AppendChild(table);
            body.AppendChild(CreateSpacer(1));

            body.AppendChild(CreateBodyParagraph($"当前财政状况：{GameMetricConverter.ToBudgetHealthDescription(f.RevenueExpenseRatio)}。"));
        }

        // ══════════════════════════════════════════════
        //  附录
        // ══════════════════════════════════════════════
        private void AddScorecardSection(Body body)
        {
            body.AppendChild(CreateSectionHeading("综合评分卡"));
            body.AppendChild(CreateSpacer(1));

            var scores = m_Analysis.Scores;
            var table = CreateStyledTable(new[] { "类别", "指标", "得分", "等级", "说明" });
            bool alt = false;
            foreach (var score in scores)
            {
                AddTableRow(table, new[] { score.Category, score.Name, $"{score.Score:F1}", score.Grade, score.Description }, alt);
                alt = !alt;
            }
            body.AppendChild(table);
            body.AppendChild(CreateSpacer(2));

            var avgScore = scores.Count > 0 ? scores.Average(s => s.Score) : 0;
            var overallGrade = avgScore >= 80 ? "A（优秀）" : avgScore >= 65 ? "B（良好）" : avgScore >= 50 ? "C（合格）" : avgScore >= 35 ? "D（待改善）" : "F（不合格）";
            body.AppendChild(CreateBodyParagraph($"综合评分：{avgScore:F1}分，总体等级：{overallGrade}。" +
                $"{(avgScore >= 65 ? "城市发展状况良好，各项指标处于健康水平。" : "城市发展存在一定问题，建议重点关注低分领域。")}"));

            body.AppendChild(CreatePageBreak());
        }

        private void AddAppendixTable(Body body)
        {
            body.AppendChild(CreateSectionHeading("附表：主要指标一览"));
            body.AppendChild(CreateSpacer(1));

            var table = CreateStyledTable(new[] { "序号", "指标名称", "数值", "单位", "状态" });

            var rows = new List<string[]>
            {
                new[] { "1", "常住人口", $"{m_Analysis.Overview.Population:N0}", "人", "—" },
                new[] { "2", "人口增长率", $"{m_Analysis.Demographics.GrowthRate:+0.0;-0.0}", "%", GetStatus(m_Analysis.Demographics.GrowthRate > 0) },
                new[] { "3", "财政收入", $"₡{m_Analysis.Economy.Income:N0}", "₡", "—" },
                new[] { "4", "财政支出", $"₡{m_Analysis.Economy.Expense:N0}", "₡", "—" },
                new[] { "5", "净收入", $"₡{m_Analysis.Economy.NetIncome:N0}", "₡", GetStatus(m_Analysis.Economy.NetIncome >= 0) },
                new[] { "6", "居民幸福度", $"{m_Analysis.Social.Wellbeing:F1}%", "%", GameMetricConverter.ToHappinessLevel(m_Analysis.Social.Wellbeing) },
                new[] { "7", "居民健康度", $"{m_Analysis.Social.Health:F1}%", "%", GetStatus(m_Analysis.Social.Health >= 50) },
                new[] { "8", "犯罪率", $"{m_Analysis.Social.CrimeRate:F1}%", "%", GetCrimeStatus(m_Analysis.Social.CrimeRate) },
                new[] { "9", "失业率", $"{m_Analysis.Employment.UnemploymentRate:F1}%", "%", GetUnemploymentStatus(m_Analysis.Employment.UnemploymentRate) },
            };

            if (GameMetricConverter.IsServiceAvailable(m_Analysis.Economy.Trade))
                rows.Add(new[] { "10", "贸易额", $"₡{m_Analysis.Economy.Trade:N0}", "₡", "—" });
            if (GameMetricConverter.IsServiceAvailable(m_Analysis.Transport.TotalPassengers))
                rows.Add(new[] { "11", "客运总量", $"{m_Analysis.Transport.TotalPassengers:N0}", "人次", "—" });
            if (GameMetricConverter.IsServiceAvailable(m_Analysis.Transport.TotalCargo))
                rows.Add(new[] { "12", "货运总量", $"{m_Analysis.Transport.TotalCargo:N0}", "吨", "—" });
            rows.Add(new[] { "13", "生活质量指数", $"{m_Analysis.Social.QualityOfLifeIndex:F1}", "/100", GetQualityGrade(m_Analysis.Social.QualityOfLifeIndex) });

            for (int i = 0; i < rows.Count; i++)
            {
                rows[i][0] = (i + 1).ToString();
                AddTableRow(table, rows[i], i % 2 == 1);
            }

            body.AppendChild(table);
        }

        private static string GetStatus(bool positive) => positive ? "正常" : "关注";
        private static string GetCrimeStatus(double rate) => rate <= 5 ? "良好" : rate <= 10 ? "一般" : "关注";
        private static string GetUnemploymentStatus(double rate) => rate <= 5 ? "良好" : rate <= 10 ? "一般" : "关注";
        private static string GetQualityGrade(double score)
        {
            if (score >= 80) return "优秀";
            if (score >= 65) return "良好";
            if (score >= 50) return "合格";
            if (score >= 35) return "待改善";
            return "不合格";
        }
    }
}