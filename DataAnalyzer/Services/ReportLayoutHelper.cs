using System;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DataAnalyzer.Services
{
    /// <summary>
    /// 严格遵循 GB/T 9704-2012《党政机关公文格式》国家标准的报告布局辅助类
    /// </summary>
    public static class ReportLayoutHelper
    {
        // ── GB/T 9704-2012 颜色常量 ──
        public const string RedColor = "FF0000";          // 版头红色（标准红）
        public const string BlackColor = "000000";        // 正文黑色
        public const string BodyColor = "000000";         // 正文颜色（标准黑）
        public const string SubColor = "000000";          // 辅助文字颜色
        public const string TableHeaderBg = "1F4E79";     // 表头背景色
        public const string TableBorderColor = "000000";  // 表格边框色（标准黑）
        public const string GreenColor = "00AA00";
        public const string RedWarn = "CC0000";
        public const string AltRowBg = "F2F7FC";
        public const string SummaryRowBg = "E7E6E6";

        // ── GB/T 9704-2012 字体常量 ──
        public const string FontXiaoBiaoSong = "FangSong";    // 小标宋体（发文机关标志）- 用仿宋替代
        public const string FontFangSong = "FangSong";        // 仿宋（正文、主送机关）
        public const string FontHeiTi = "SimHei";             // 黑体（一级标题、密级、紧急程度）
        public const string FontKaiTi = "KaiTi";              // 楷体（二级标题、签发人姓名）
        public const string FontSongTi = "SimSun";            // 宋体（页码）
        public const string FontTitle = "SimHei";             // 兼容旧代码
        public const string FontBody = "FangSong";            // 兼容旧代码
        public const string FontTableHeader = "SimHei";
        public const string FontTableBody = "FangSong";

        // ── GB/T 9704-2012 字号常量（半磅值，即 Word 的 w:sz 值） ──
        // 二号 = 22磅 = 44半磅
        public const int SizeErHao = 44;
        // 三号 = 16磅 = 32半磅
        public const int SizeSanHao = 32;
        // 四号 = 14磅 = 28半磅
        public const int SizeSiHao = 28;
        // 小四 = 12磅 = 24半磅
        public const int SizeXiaoSi = 24;

        // 兼容旧代码的字号映射
        public const int SizeHeaderTitle = 52;       // 发文机关标志（略大于三号，接近小标宋效果）
        public const int SizeHeaderSubtitle = 44;    // 政府工作报告（二号）
        public const int SizeSectionHeading = 32;    // 一级标题（三号黑体）
        public const int SizeBodyText = 32;          // 正文（三号仿宋）
        public const int SizeCoverInfo = 28;         // 封面信息（四号）
        public const int SizeCoverSmall = 24;        // 封面小字（小四）
        public const int SizeTableTitle = 28;        // 表标题（四号）
        public const int SizeTableCaption = 24;      // 表说明（小四）
        public const int SizeTableHeader = 28;       // 表头（四号）
        public const int SizeTableBody = 28;         // 表体（四号）
        public const int SizeFooter = 24;            // 页脚（小四）
        public const int SizeSeparator = 24;         // 分隔文字

        // ── GB/T 9704-2012 行距常量 ──
        // 固定值28磅 = 560缇（1磅=20缇）
        public const int LineSpacing28Pt = 560;
        // 每面22行，每行28字
        public const int CharsPerLine = 28;
        public const int LinesPerPage = 22;

        // ══════════════════════════════════════════════
        //  一、页面设置（GB/T 9704-2012 第5章）
        // ══════════════════════════════════════════════

        /// <summary>
        /// 创建符合 GB/T 9704-2012 的页面设置
        /// A4纸（210mm×297mm），页边距：上3.7cm 下3.5cm 左2.8cm 右2.6cm
        /// 版心：156mm×225mm
        /// 1cm = 567缇(twips)
        /// </summary>
        public static SectionProperties CreatePageSettings(string headerId = null, string footerId = null)
        {
            var props = new SectionProperties(
                // A4纸：210mm×297mm = 11906×16838缇
                new PageSize { Width = 11906, Height = 16838, Orient = PageOrientationValues.Portrait },
                // 页边距：上3.7cm=2098 下3.5cm=1985 左2.8cm=1588 右2.6cm=1474
                new PageMargin
                {
                    Top = 2098,
                    Bottom = 1985,
                    Left = 1588,
                    Right = 1474,
                    Header = 851,   // 页眉距版心
                    Footer = 992    // 页脚距版心
                },
                // 版心尺寸
                new Columns { Space = "709" },
                // 每行28字
                new DocGrid { Type = DocGridValues.LinesAndChars, LinePitch = 312 }
            );

            if (headerId != null)
                props.AppendChild(new HeaderReference { Id = headerId, Type = HeaderFooterValues.Default });
            if (footerId != null)
                props.AppendChild(new FooterReference { Id = footerId, Type = HeaderFooterValues.Default });

            return props;
        }

        // ══════════════════════════════════════════════
        //  二、版头部分（GB/T 9704-2012 第7章）
        // ══════════════════════════════════════════════

        /// <summary>
        /// 创建发文机关标志（居中排布，上边缘至版心上边缘35mm，红色小标宋体）
        /// </summary>
        public static Paragraph CreateRedHeader(string cityName, string reportTitle)
        {
            var para = new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { Before = "0", After = "0", Line = LineSpacing28Pt.ToString(), LineRule = LineSpacingRuleValues.Exact },
                    // 上边缘至版心上边缘35mm = 1984缇
                    new Indentation { FirstLine = "0" }),
                CreateRun($"{cityName}人民政府文件", SizeHeaderTitle, true, RedColor, FontXiaoBiaoSong));
            return para;
        }

        /// <summary>
        /// 创建发文机关标志（简化版，不含"文件"二字）
        /// </summary>
        public static Paragraph CreateRedHeaderSimple(string cityName)
        {
            return CreateRedHeader(cityName, "");
        }

        /// <summary>
        /// 创建发文字号（居中排布，发文机关标志下空二行）
        /// 格式：发文机关代字〔年份〕序号
        /// </summary>
        public static Paragraph CreateDocumentNumber(string cityName)
        {
            var year = DateTime.Now.Year;
            return new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { Before = "560", After = "0", Line = LineSpacing28Pt.ToString(), LineRule = LineSpacingRuleValues.Exact }),
                CreateRun($"{cityName}政发〔{year}〕1号", SizeSanHao, false, BlackColor, FontFangSong));
        }

        /// <summary>
        /// 创建版头红色分隔线（发文字号之下4mm处，与版心等宽）
        /// </summary>
        public static Paragraph CreateRedSeparatorLine()
        {
            // 4mm = 227缇
            return new Paragraph(
                new ParagraphProperties(
                    new SpacingBetweenLines { Before = "227", After = "0", Line = "40", LineRule = LineSpacingRuleValues.Exact },
                    new ParagraphBorders(
                        new BottomBorder { Val = BorderValues.Single, Size = 12, Space = 0, Color = RedColor })),
                new Run(new Text("") { Space = new EnumValue<SpaceProcessingModeValues>(SpaceProcessingModeValues.Preserve) }));
        }

        /// <summary>
        /// 创建完整的版头区域（发文机关标志 + 发文字号 + 红色分隔线）
        /// </summary>
        public static void BuildDocumentHeader(Body body, string cityName)
        {
            // 发文机关标志（居中，红色）
            body.AppendChild(CreateRedHeader(cityName, ""));
            // 发文字号（居中，发文机关标志下空二行）
            body.AppendChild(CreateDocumentNumber(cityName));
            // 版头红色分隔线
            body.AppendChild(CreateRedSeparatorLine());
        }

        // ══════════════════════════════════════════════
        //  三、主体部分（GB/T 9704-2012 第8章）
        // ══════════════════════════════════════════════

        /// <summary>
        /// 创建公文标题（红色分隔线下空二行，二号小标宋体，居中排布）
        /// </summary>
        public static Paragraph CreateDocumentTitle(string title)
        {
            return new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    // 红色分隔线下空二行
                    new SpacingBetweenLines { Before = "560", After = "0", Line = LineSpacing28Pt.ToString(), LineRule = LineSpacingRuleValues.Exact }),
                CreateRun(title, SizeErHao, true, BlackColor, FontXiaoBiaoSong));
        }

        /// <summary>
        /// 创建主送机关（标题下空一行，居左顶格，三号仿宋体）
        /// </summary>
        public static Paragraph CreateAddressee(string addressee)
        {
            return new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Left },
                    // 标题下空一行
                    new SpacingBetweenLines { Before = "280", After = "0", Line = LineSpacing28Pt.ToString(), LineRule = LineSpacingRuleValues.Exact }),
                CreateRun(addressee, SizeSanHao, false, BlackColor, FontFangSong));
        }

        /// <summary>
        /// 创建一级标题："一、"，三号黑体
        /// </summary>
        public static Paragraph CreateSectionHeading(string text)
        {
            return new Paragraph(
                new ParagraphProperties(
                    new SpacingBetweenLines { Before = "0", After = "0", Line = LineSpacing28Pt.ToString(), LineRule = LineSpacingRuleValues.Exact },
                    new Indentation { FirstLineChars = 200, FirstLine = "640" }),
                CreateRun(text, SizeSanHao, true, BlackColor, FontHeiTi));
        }

        /// <summary>
        /// 创建二级标题："(一)"，三号楷体
        /// </summary>
        public static Paragraph CreateSubSectionHeading(string text)
        {
            return new Paragraph(
                new ParagraphProperties(
                    new SpacingBetweenLines { Before = "0", After = "0", Line = LineSpacing28Pt.ToString(), LineRule = LineSpacingRuleValues.Exact },
                    new Indentation { FirstLineChars = 200, FirstLine = "640" }),
                CreateRun(text, SizeSanHao, true, BlackColor, FontKaiTi));
        }

        /// <summary>
        /// 创建正文段落（三号仿宋体，首行缩进二字，固定行距28磅）
        /// </summary>
        public static Paragraph CreateBodyParagraph(string text)
        {
            return new Paragraph(
                new ParagraphProperties(
                    new SpacingBetweenLines { After = "0", Line = LineSpacing28Pt.ToString(), LineRule = LineSpacingRuleValues.Exact },
                    // 首行缩进二字（约640缇）
                    new Indentation { FirstLineChars = 200, FirstLine = "640" }),
                CreateRun(text, SizeSanHao, false, BlackColor, FontFangSong));
        }

        /// <summary>
        /// 创建发文机关署名（成文日期之上居中）
        /// </summary>
        public static Paragraph CreateIssuingAuthority(string authority)
        {
            return new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { Before = "560", After = "0", Line = LineSpacing28Pt.ToString(), LineRule = LineSpacingRuleValues.Exact }),
                CreateRun(authority, SizeSanHao, false, BlackColor, FontFangSong));
        }

        /// <summary>
        /// 创建成文日期（阿拉伯数字，年月日标全，居中排布）
        /// </summary>
        public static Paragraph CreateIssueDate(DateTime? date = null)
        {
            var d = date ?? DateTime.Now;
            var dateText = $"{d.Year}年{d.Month}月{d.Day}日";
            return new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { Before = "0", After = "0", Line = LineSpacing28Pt.ToString(), LineRule = LineSpacingRuleValues.Exact }),
                CreateRun(dateText, SizeSanHao, false, BlackColor, FontFangSong));
        }

        // ══════════════════════════════════════════════
        //  四、版记部分（GB/T 9704-2012 第10章）
        // ══════════════════════════════════════════════

        /// <summary>
        /// 创建版记分隔线（与版心等宽）
        /// </summary>
        public static Paragraph CreateBanJiSeparator()
        {
            return new Paragraph(
                new ParagraphProperties(
                    new SpacingBetweenLines { Before = "0", After = "0", Line = "40", LineRule = LineSpacingRuleValues.Exact },
                    new ParagraphBorders(
                        new BottomBorder { Val = BorderValues.Single, Size = 4, Space = 0, Color = BlackColor })),
                new Run(new Text("") { Space = new EnumValue<SpaceProcessingModeValues>(SpaceProcessingModeValues.Preserve) }));
        }

        /// <summary>
        /// 创建抄送机关（四号仿宋体，左右各空一字）
        /// </summary>
        public static Paragraph CreateCopyTo(string copyToOrgs)
        {
            return new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Left },
                    new SpacingBetweenLines { Before = "0", After = "0", Line = "400", LineRule = LineSpacingRuleValues.Exact },
                    // 左空一字
                    new Indentation { Left = "560" }),
                CreateRun($"抄送：{copyToOrgs}。", SizeSiHao, false, BlackColor, FontFangSong));
        }

        /// <summary>
        /// 创建印发机关和印发日期（四号仿宋体，印发机关左空一字，印发日期右空一字）
        /// </summary>
        public static Paragraph CreatePrintInfo(string printOrg, DateTime? printDate = null)
        {
            var d = printDate ?? DateTime.Now;
            var dateText = $"{d.Year}年{d.Month}月{d.Day}日印发";
            return new Paragraph(
                new ParagraphProperties(
                    new SpacingBetweenLines { Before = "0", After = "0", Line = "400", LineRule = LineSpacingRuleValues.Exact },
                    // 使用制表符实现左右对齐
                    new Justification { Val = JustificationValues.Both }),
                CreateRun($"{printOrg}", SizeSiHao, false, BlackColor, FontFangSong),
                new Run(new TabChar()),
                CreateRun(dateText, SizeSiHao, false, BlackColor, FontFangSong));
        }

        /// <summary>
        /// 创建完整的版记区域
        /// </summary>
        public static void BuildBanJi(Body body, string cityName)
        {
            // 首条版记分隔线
            body.AppendChild(CreateBanJiSeparator());
            // 抄送机关
            body.AppendChild(CreateCopyTo("市发展改革委、市财政局、市统计局"));
            // 印发机关和印发日期
            body.AppendChild(CreatePrintInfo($"{cityName}人民政府办公室"));
            // 末条版记分隔线
            body.AppendChild(CreateBanJiSeparator());
        }

        // ══════════════════════════════════════════════
        //  五、页码（GB/T 9704-2012 第6章）
        // ══════════════════════════════════════════════

        /// <summary>
        /// 创建符合 GB/T 9704-2012 的页脚（4号半角宋体阿拉伯数字，一字线）
        /// 数字左右各加一条一字线，上距版心下边缘7mm
        /// </summary>
        public static Footer CreatePageNumberFooter()
        {
            var footer = new Footer();

            // 单页码居右空一字，双页码居左空一字
            // 使用居中排布作为简化实现（标准要求单双页不同位置，但Word中需分节实现）
            var para = new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { Before = "0", After = "0" }),
                // 一字线 + 页码 + 一字线
                CreateRun("— ", SizeSiHao, false, BlackColor, FontSongTi),
                new Run(
                    new RunProperties(
                        new RunFonts { EastAsia = FontSongTi, Ascii = FontSongTi, HighAnsi = FontSongTi },
                        new FontSize { Val = SizeSiHao.ToString() },
                        new FontSizeComplexScript { Val = SizeSiHao.ToString() }),
                    new FieldCode(" PAGE ") { Space = new EnumValue<SpaceProcessingModeValues>(SpaceProcessingModeValues.Preserve) }),
                CreateRun(" —", SizeSiHao, false, BlackColor, FontSongTi));

            footer.AppendChild(para);
            return footer;
        }

        // ══════════════════════════════════════════════
        //  兼容旧代码的方法（保持接口不变）
        // ══════════════════════════════════════════════

        public static Paragraph CreateRedSubHeader(string text)
        {
            return new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { After = "0", Line = LineSpacing28Pt.ToString(), LineRule = LineSpacingRuleValues.Exact }),
                CreateRun(text, SizeErHao, true, RedColor, FontXiaoBiaoSong));
        }

        public static Paragraph CreateCenteredText(string text, int fontSize, bool bold, string color)
        {
            return new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { After = "0", Line = LineSpacing28Pt.ToString(), LineRule = LineSpacingRuleValues.Exact }),
                CreateRun(text, fontSize, bold, color, bold ? FontHeiTi : FontFangSong));
        }

        public static Paragraph CreateTableTitle(string text)
        {
            return CreateCenteredText(text, SizeSiHao, true, BlackColor);
        }

        public static Paragraph CreateTableCaption(string text)
        {
            return new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { After = "0", Line = "400", LineRule = LineSpacingRuleValues.Exact }),
                CreateRun(text, SizeXiaoSi, false, SubColor, FontFangSong));
        }

        public static Paragraph CreateSpacer(int lines = 1)
        {
            // 每行28磅 = 560缇
            return new Paragraph(
                new ParagraphProperties(
                    new SpacingBetweenLines { After = (lines * LineSpacing28Pt).ToString(), Line = LineSpacing28Pt.ToString(), LineRule = LineSpacingRuleValues.Exact }));
        }

        public static Paragraph CreateHorizontalLine(string color, int size)
        {
            return new Paragraph(
                new ParagraphProperties(
                    new ParagraphBorders(new BottomBorder { Val = BorderValues.Single, Size = (uint)size, Space = 1, Color = color }),
                    new SpacingBetweenLines { After = "0", Line = "40", LineRule = LineSpacingRuleValues.Exact }));
        }

        public static Paragraph CreatePageBreak()
        {
            return new Paragraph(new Run(new Break { Type = BreakValues.Page }));
        }

        /// <summary>
        /// 报告结尾（版记格式）
        /// </summary>
        public static void AppendReportEnding(Body body)
        {
            // 成文日期和署名
            body.AppendChild(CreateSpacer(2));
            body.AppendChild(CreateHorizontalLine(BlackColor, 2));
            body.AppendChild(CreateCenteredText("—— 报告完 ——", SizeXiaoSi, false, SubColor));
            body.AppendChild(CreateCenteredText(
                $"本报告由都市天际线2城市数据分析系统自动生成 · {DateTime.Now:yyyy年MM月dd日 HH:mm}",
                SizeXiaoSi, false, "999999"));
        }

        // ══════════════════════════════════════════════
        //  低级构建块
        // ══════════════════════════════════════════════

        public static Run CreateRun(string text, int fontSize, bool bold, string color, string font)
        {
            var rPr = new RunProperties(
                new RunFonts { EastAsia = font, Ascii = font, HighAnsi = font },
                new FontSize { Val = fontSize.ToString() },
                new FontSizeComplexScript { Val = fontSize.ToString() },
                new DocumentFormat.OpenXml.Wordprocessing.Color { Val = color },
                new Languages { Val = "zh-CN", EastAsia = "zh-CN" });
            if (bold) rPr.AppendChild(new Bold());
            return new Run(rPr, new Text(text) { Space = new EnumValue<SpaceProcessingModeValues>(SpaceProcessingModeValues.Preserve) });
        }

        // ══════════════════════════════════════════════
        //  表格创建（GB/T 9704-2012 附件格式）
        // ══════════════════════════════════════════════

        public static Table CreateStyledTable(string[] headers)
        {
            var table = new Table();
            table.AppendChild(new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4, Color = TableBorderColor },
                    new BottomBorder { Val = BorderValues.Single, Size = 4, Color = TableBorderColor },
                    new LeftBorder { Val = BorderValues.Single, Size = 4, Color = TableBorderColor },
                    new RightBorder { Val = BorderValues.Single, Size = 4, Color = TableBorderColor },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 2, Color = TableBorderColor },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 2, Color = TableBorderColor }),
                new TableLook { Val = "04A0" },
                new TableCellMarginDefault(
                    new TopMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
                    new BottomMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
                    new TableCellLeftMargin { Width = 60, Type = TableWidthValues.Dxa },
                    new TableCellRightMargin { Width = 60, Type = TableWidthValues.Dxa })));

            var grid = new TableGrid();
            foreach (var _ in headers) grid.AppendChild(new GridColumn());
            table.AppendChild(grid);

            var headerRow = new TableRow();
            headerRow.AppendChild(new TableRowProperties(new TableRowHeight { Val = 400, HeightType = HeightRuleValues.AtLeast }));
            foreach (var h in headers)
            {
                headerRow.AppendChild(new TableCell(
                    new TableCellProperties(
                        new Shading { Fill = TableHeaderBg, Val = ShadingPatternValues.Clear },
                        new TableCellWidth { Width = (5000 / headers.Length).ToString(), Type = TableWidthUnitValues.Pct },
                        new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }),
                    new Paragraph(
                        new ParagraphProperties(
                            new Justification { Val = JustificationValues.Center },
                            new SpacingBetweenLines { Before = "40", After = "40", Line = "280", LineRule = LineSpacingRuleValues.Exact }),
                        new Run(
                            new RunProperties(
                                new Bold(), new DocumentFormat.OpenXml.Wordprocessing.Color { Val = "FFFFFF" },
                                new RunFonts { EastAsia = FontTableHeader, Ascii = "Arial", HighAnsi = "Arial" },
                                new FontSize { Val = SizeTableHeader.ToString() }, new FontSizeComplexScript { Val = SizeTableHeader.ToString() },
                                new Languages { Val = "zh-CN", EastAsia = "zh-CN" }),
                            new Text(h) { Space = new EnumValue<SpaceProcessingModeValues>(SpaceProcessingModeValues.Preserve) }))));
            }
            table.AppendChild(headerRow);
            return table;
        }

        public static void AddTableRow(Table table, string[] cells, bool alt = false)
        {
            var row = new TableRow();
            row.AppendChild(new TableRowProperties(new TableRowHeight { Val = 340, HeightType = HeightRuleValues.AtLeast }));
            for (int i = 0; i < cells.Length; i++)
            {
                var props = new TableCellProperties(
                    new TableCellWidth { Width = (5000 / cells.Length).ToString(), Type = TableWidthUnitValues.Pct },
                    new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
                if (alt) props.AppendChild(new Shading { Fill = AltRowBg, Val = ShadingPatternValues.Clear });
                var justify = i == 0 ? JustificationValues.Left : JustificationValues.Center;
                row.AppendChild(new TableCell(props, new Paragraph(
                    new ParagraphProperties(
                        new Justification { Val = justify },
                        new SpacingBetweenLines { Before = "20", After = "20", Line = "280", LineRule = LineSpacingRuleValues.Exact }),
                    new Run(
                        new RunProperties(
                            new RunFonts { EastAsia = FontTableBody, Ascii = "Arial", HighAnsi = "Arial" },
                            new FontSize { Val = SizeTableBody.ToString() }, new FontSizeComplexScript { Val = SizeTableBody.ToString() },
                            new DocumentFormat.OpenXml.Wordprocessing.Color { Val = BodyColor },
                            new Languages { Val = "zh-CN", EastAsia = "zh-CN" }),
                        new Text(cells[i]) { Space = new EnumValue<SpaceProcessingModeValues>(SpaceProcessingModeValues.Preserve) }))));
            }
            table.AppendChild(row);
        }

        /// <summary>
        /// 添加带颜色编码的环比/同比数据行
        /// </summary>
        public static void AddColoredTableRow(Table table, string[] cells)
        {
            var row = new TableRow();
            row.AppendChild(new TableRowProperties(new TableRowHeight { Val = 340, HeightType = HeightRuleValues.AtLeast }));
            for (int i = 0; i < cells.Length; i++)
            {
                var props = new TableCellProperties(
                    new TableCellWidth { Width = (5000 / cells.Length).ToString(), Type = TableWidthUnitValues.Pct },
                    new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
                var justify = i == 0 ? JustificationValues.Left : JustificationValues.Center;

                var textColor = BodyColor;
                if (i >= 2 && cells[i].Contains("↓")) textColor = RedWarn;
                else if (i >= 2 && cells[i].Contains("↑")) textColor = GreenColor;

                row.AppendChild(new TableCell(props, new Paragraph(
                    new ParagraphProperties(
                        new Justification { Val = justify },
                        new SpacingBetweenLines { Before = "20", After = "20", Line = "280", LineRule = LineSpacingRuleValues.Exact }),
                    new Run(
                        new RunProperties(
                            new RunFonts { EastAsia = FontTableBody, Ascii = "Arial", HighAnsi = "Arial" },
                            new FontSize { Val = SizeTableBody.ToString() }, new FontSizeComplexScript { Val = SizeTableBody.ToString() },
                            new DocumentFormat.OpenXml.Wordprocessing.Color { Val = textColor },
                            new Languages { Val = "zh-CN", EastAsia = "zh-CN" }),
                        new Text(cells[i]) { Space = new EnumValue<SpaceProcessingModeValues>(SpaceProcessingModeValues.Preserve) }))));
            }
            table.AppendChild(row);
        }
    }
}
