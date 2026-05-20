using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DataAnalyzer.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using static DataAnalyzer.Services.ReportLayoutHelper;

namespace DataAnalyzer.Services
{
    public class DevelopmentReportGenerator
    {
        private readonly List<ReportChapter> m_Chapters;
        private readonly DevelopmentReport m_Report;
        private readonly StatisticSnapshot m_Current;
        private readonly List<StatisticSnapshot> m_History;
        private readonly int m_KUpdatesPerDay;
        private readonly string m_CityName;
        private readonly string m_OutputPath;
        private readonly ChartRenderer m_ChartRenderer;

        public DevelopmentReportGenerator(List<ReportChapter> chapters, DevelopmentReport report,
            StatisticSnapshot current, List<StatisticSnapshot> history,
            int kUpdatesPerDay, string cityName, string outputPath)
        {
            m_Chapters = chapters;
            m_Report = report;
            m_Current = current;
            m_History = history;
            m_KUpdatesPerDay = kUpdatesPerDay;
            m_CityName = cityName;
            m_OutputPath = outputPath;
            var analysis = new CityAnalysisReport
            {
                Overview = report.Overview,
                Demographics = report.Demographics,
                Economy = report.Economy,
                Sectors = report.Sectors,
                Employment = report.Employment,
                Transport = report.Transport,
                Social = report.Social,
                Fiscal = report.Fiscal,
                Households = report.Households,
                Trends = report.Trends,
                Alerts = report.Alerts,
                Scores = report.Scores
            };
            m_ChartRenderer = new ChartRenderer(m_History, m_Current, analysis);
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
            body.AppendChild(CreateDocumentTitle($"关于{m_CityName}城市发展工作的报告"));

            // ── 正文各章 ──
            BuildChapter(body, mainPart, "opening");

            BuildChapter(body, mainPart, "demographics",
                ("图1 建市以来人口增长全景图", () => m_ChartRenderer.GeneratePopulationChart(), "chart_pop.png"));

            BuildChapter(body, mainPart, "economy",
                ("图2 财政收支历史趋势图", () => m_ChartRenderer.GenerateIncomeExpenseChart(), "chart_income.png"));

            BuildChapter(body, mainPart, "industry",
                tableBuilder: () => AddIndustryTable(body));

            BuildChapter(body, mainPart, "employment",
                tableBuilder: () => AddEmploymentTable(body));

            BuildChapter(body, mainPart, "transport",
                ("图3 公共交通客流量分布", () => m_ChartRenderer.GenerateTransportChart(), "chart_transport.png"));

            BuildChapter(body, mainPart, "social",
                ("图4 市民幸福度与健康趋势", () => m_ChartRenderer.GenerateWellbeingChart(), "chart_wellbeing.png"));

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
            AddYearHistoryTable(body);

            // ── 版记部分（GB/T 9704-2012 第10章）──
            BuildBanJi(body, m_CityName);

            mainPart.Document.Save();
        }

        // ══════════════════════════════════════════════
        //  核心指标仪表盘
        // ══════════════════════════════════════════════
        private void BuildDashboard(Body body)
        {
            body.AppendChild(CreateSpacer(1));

            var h = m_Report.History;
            var elapsedYears = h.TotalDays / 12.0;

            var sb = new StringBuilder();
            sb.AppendLine($"截至第{m_Current.GameYear}年第{m_Current.GameMonth}月，{m_CityName}建市已逾{elapsedYears:F1}年。");
            sb.AppendLine();

            sb.AppendLine($"全市常住人口{m_Current.Population:N0}人，居民幸福度{m_Current.Wellbeing:F1}%（{GameMetricConverter.ToHappinessLevel(m_Current.Wellbeing)}），健康度{m_Current.Health:F1}%（{(m_Current.Health >= 80 ? "优秀" : m_Current.Health >= 60 ? "良好" : "需关注")}）。");
            
            if (m_Current.Money > 0)
                sb.AppendLine($"财政余额₡{m_Current.Money:N0}，{(h.MoneyGrowthTotal > 0 ? "财政状况稳健" : "财政压力较大")}。");

            if (m_Current.CrimeRate > 0)
                sb.AppendLine($"社会治安方面，犯罪率{m_Current.CrimeRate:F1}%，{GameMetricConverter.ToCrimeDescription(m_Current.CrimeRate)}。");

            if (m_Current.HomelessCount > 0 && m_Current.Population > 0)
            {
                var homelessPct = (double)m_Current.HomelessCount / m_Current.Population * 100;
                sb.AppendLine($"住房保障方面，无家可归者{m_Current.HomelessCount:N0}人（占人口{homelessPct:F2}%），{(homelessPct > 1 ? "住房问题较为突出" : "住房保障基本到位")}。");
            }

            body.AppendChild(CreateBodyParagraph(sb.ToString()));
            body.AppendChild(CreateSpacer(1));
            body.AppendChild(CreatePageBreak());
        }

        // ══════════════════════════════════════════════
        //  逐章构建（文本 + 图表/表格 + 分页）
        // ══════════════════════════════════════════════
        private void BuildChapter(Body body, MainDocumentPart mainPart, string chapterId,
            (string title, Func<byte[]> generator, string imageName)? chart = null,
            Action tableBuilder = null)
        {
            // 写入 AI 生成的章节文本
            WriteChapterText(body, chapterId);

            // 插入图表（位于文本之后、分页之前）
            if (chart.HasValue)
                AddChartToBody(body, mainPart, chart.Value.title, chart.Value.generator, chart.Value.imageName);

            // 插入数据表格
            tableBuilder?.Invoke();

            // 分页
            body.AppendChild(CreatePageBreak());
        }

        /// <summary>
        /// 仅写入AI章节文本内容（不含图表、表格、分页符）
        /// </summary>
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
                var bytes = chartFunc();
                if (bytes != null && bytes.Length > 0)
                {
                    var imagePart = mainPart.AddImagePart(ImagePartType.Png);
                    using (var ms = new MemoryStream(bytes)) imagePart.FeedData(ms);
                    var imageId = mainPart.GetIdOfPart(imagePart);
                    body.AppendChild(new Paragraph(new Run(ImageHelper.CreateDrawing(imageId, title))));
                }
            }
            catch (Exception ex) { body.AppendChild(CreateBodyParagraph($"（图表生成失败：{ex.Message}）")); }
            body.AppendChild(CreateSpacer(1));
        }

        // ══════════════════════════════════════════════
        //  数据表格
        // ══════════════════════════════════════════════
        private void AddIndustryTable(Body body)
        {
            var sec = m_Report.Sectors;
            if (!GameMetricConverter.IsServiceAvailable(sec.TotalWealth)) return;

            body.AppendChild(CreateSpacer(1));
            body.AppendChild(CreateTableTitle("表1 产业结构数据"));
            body.AppendChild(CreateTableCaption("反映本市三大产业部门的当前发展状况"));
            body.AppendChild(CreateSpacer(1));

            var table = CreateStyledTable(new[] { "产业", "财富（₡）", "企业数", "从业/满编", "填充率", "财富占比" });
            AddTableRow(table, new[] { "服务业", $"{sec.Service.Wealth:N0}", $"{sec.Service.Count:N0}", $"{sec.Service.Workers}/{sec.Service.MaxWorkers}", $"{sec.ServiceWorkerFillRate:F1}%", $"{sec.ServiceWealthPct:F1}%" });
            AddTableRow(table, new[] { "加工业", $"{sec.Processing.Wealth:N0}", $"{sec.Processing.Count:N0}", $"{sec.Processing.Workers}/{sec.Processing.MaxWorkers}", $"{sec.ProcessingWorkerFillRate:F1}%", $"{sec.ProcessingWealthPct:F1}%" }, alt: true);
            AddTableRow(table, new[] { "办公业", $"{sec.Office.Wealth:N0}", $"{sec.Office.Count:N0}", $"{sec.Office.Workers}/{sec.Office.MaxWorkers}", $"{sec.OfficeWorkerFillRate:F1}%", $"{sec.OfficeWealthPct:F1}%" });
            AddTableRow(table, new[] { "合计", $"{sec.TotalWealth:N0}", $"{sec.TotalCount:N0}", $"{sec.TotalWorkers}/{sec.TotalMaxWorkers}", "—", "100%" }, alt: true);
            body.AppendChild(table);
            body.AppendChild(CreateSpacer(1));
        }

        private void AddEmploymentTable(Body body)
        {
            var emp = m_Report.Employment;
            body.AppendChild(CreateSpacer(1));
            body.AppendChild(CreateTableTitle("表2 就业数据"));
            body.AppendChild(CreateTableCaption("反映本市劳动力市场运行状况"));
            body.AppendChild(CreateSpacer(1));

            var table = CreateStyledTable(new[] { "指标", "数值", "评估" });
            AddTableRow(table, new[] { "从业人员", $"{emp.WorkerCount:N0}人", "—" });
            AddTableRow(table, new[] { "失业率", $"{emp.UnemploymentRate:F1}%", emp.UnemploymentRate < 5 ? "良好" : emp.UnemploymentRate < 10 ? "一般" : "需关注" }, alt: true);
            AddTableRow(table, new[] { "劳动参与率", $"{emp.WorkforceParticipation:F1}%", emp.WorkforceParticipation > 50 ? "积极参与" : "偏低" });
            if (emp.CityServiceWorkers > 0)
                AddTableRow(table, new[] { "公务人员填充率", $"{emp.CityServiceFillRate:F1}%", emp.CityServiceFillRate > 80 ? "充足" : "不足" }, alt: true);
            if (emp.SeniorWorkerDemand > 0)
                AddTableRow(table, new[] { "高级技工需求", $"{emp.SeniorWorkerDemand:F1}%", emp.SeniorWorkerDemand > 70 ? "紧缺" : "正常" });
            body.AppendChild(table);
            body.AppendChild(CreateSpacer(1));
        }

        private void AddFiscalTable(Body body)
        {
            var f = m_Report.Fiscal;
            var e = m_Report.Economy;
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
        }

        // ══════════════════════════════════════════════
        //  附录
        // ══════════════════════════════════════════════
        private void AddScorecardSection(Body body)
        {
            body.AppendChild(CreateSectionHeading("附表一：综合评分卡"));
            body.AppendChild(CreateSpacer(1));

            var scores = m_Report.Scores;
            var table = CreateStyledTable(new[] { "类别", "指标", "得分", "等级", "说明" });
            bool alt = false;
            foreach (var score in scores)
            {
                AddTableRow(table, new[] { score.Category, score.Name, $"{score.Score:F1}", score.Grade, score.Description }, alt);
                alt = !alt;
            }
            body.AppendChild(table);
            body.AppendChild(CreateSpacer(1));

            var avgScore = scores.Count > 0 ? scores.Average(s => s.Score) : 0;
            var overallGrade = avgScore >= 80 ? "A（优秀）" : avgScore >= 65 ? "B（良好）" : avgScore >= 50 ? "C（合格）" : avgScore >= 35 ? "D（待改善）" : "F（不合格）";
            body.AppendChild(CreateBodyParagraph($"综合评分：{avgScore:F1}分，总体等级：{overallGrade}。"));
            body.AppendChild(CreatePageBreak());
        }

        private void AddYearHistoryTable(Body body)
        {
            if (m_Report.YearSnapshots.Count < 2) return;

            body.AppendChild(CreateSectionHeading("附表二：历年关键指标一览"));
            body.AppendChild(CreateSpacer(1));

            var table = CreateStyledTable(new[] { "年度", "时间点", "人口", "财政余额", "幸福度", "健康度", "收入", "支出" });
            foreach (var y in m_Report.YearSnapshots)
            {
                AddTableRow(table, new[] {
                    $"第{y.GameYear}年",
                    $"第{y.GameMonth}月",
                    $"{y.Population:N0}",
                    $"₡{y.Money:N0}",
                    $"{y.Happiness:F1}%",
                    $"{y.Health:F1}%",
                    $"₡{y.Income:N0}",
                    $"₡{y.Expense:N0}"
                }, y.GameYear % 2 == 0);
            }
            body.AppendChild(table);
        }
    }
}