/*
 * report-docx.js — 浏览器内生成 Word(.docx) 报告（复用 docx.js UMD 全局 docx）
 * 结构对齐桌面端 WordReportGenerator：标题/主送/十章/署名/评分卡/附表/版记
 * chartImages: { pop, income, tax, sector, transport, wellbeing } 为 canvas.toDataURL('image/png') 结果
 */
(function (root) {
  'use strict';

  const D = () => root.docx;
  const A = () => root.Analysis;

  function b64ToUint8(b64) {
    const base64 = b64.split(',')[1] || b64;
    const bin = atob(base64);
    const len = bin.length;
    const bytes = new Uint8Array(len);
    for (let i = 0; i < len; i++) bytes[i] = bin.charCodeAt(i);
    return bytes;
  }

  function run(text, o) {
    o = o || {};
    return new (D().TextRun)({ text: text || '', font: o.font || '宋体', size: o.size || 21, bold: !!o.bold, color: o.color });
  }
  function para(text, o) {
    o = o || {};
    return new (D().Paragraph)({
      children: [run(text || '', o)],
      alignment: o.alignment || D().AlignmentType.JUSTIFIED,
      spacing: { line: o.line || 320, after: o.after != null ? o.after : 120 },
    });
  }
  function heading(text) {
    return new (D().Paragraph)({
      children: [run(text || '', { bold: true, size: 28, font: '黑体' })],
      spacing: { before: 240, after: 160 },
    });
  }

  const borders = {
    top: { style: D().BorderStyle.SINGLE, size: 4, color: '999999' },
    bottom: { style: D().BorderStyle.SINGLE, size: 4, color: '999999' },
    left: { style: D().BorderStyle.SINGLE, size: 4, color: '999999' },
    right: { style: D().BorderStyle.SINGLE, size: 4, color: '999999' },
    insideHorizontal: { style: D().BorderStyle.SINGLE, size: 2, color: 'cccccc' },
    insideVertical: { style: D().BorderStyle.SINGLE, size: 2, color: 'cccccc' },
  };

  function makeTable(headers, rows, widths) {
    const cell = (text, bold, align) => new (D().TableCell)({
      borders, width: { size: (widths && widths[0]) || 2000, type: D().WidthType.DXA },
      children: [new (D().Paragraph)({ alignment: align || D().AlignmentType.LEFT, children: [run(text || '', { bold: !!bold, size: 20 })] })],
    });
    const headerRow = new (D().TableRow)({
      tableHeader: true,
      children: headers.map((h, i) => new (D().TableCell)({
        borders, shading: { fill: 'E8EEF7' },
        children: [new (D().Paragraph)({ alignment: D().AlignmentType.CENTER, children: [run(h, { bold: true, size: 20 })] })],
      })),
    });
    const bodyRows = rows.map((r, ri) => new (D().TableRow)({
      children: r.map((c, ci) => new (D().TableCell)({
        borders, shading: (ri % 2 === 1) ? { fill: 'F5F7FA' } : undefined,
        children: [new (D().Paragraph)({ alignment: ci === 0 ? D().AlignmentType.LEFT : D().AlignmentType.CENTER, children: [run(c, { size: 20 })] })],
      })),
    }));
    return new (D().Table)({ width: { size: 100, type: D().WidthType.PERCENTAGE }, rows: [headerRow, ...bodyRows] });
  }

  function chartFigure(caption, dataUrl) {
    if (!dataUrl) return null;
    try {
      const img = new (D().ImageRun)({
        type: 'png', data: b64ToUint8(dataUrl),
        transformation: { width: 560, height: 320 },
      });
      return [
        para(caption, { alignment: D().AlignmentType.CENTER, size: 20, after: 60 }),
        new (D().Paragraph)({ alignment: D().AlignmentType.CENTER, children: [img] }),
        para('', { after: 120 }),
      ];
    } catch (e) { return [para(`（图表生成失败：${e.message}）`)]; }
  }

  function isSectionMarker(text) {
    if (!text || text.length > 30) return false;
    return /^[一二三四五六七八九十]、/.test(text);
  }

  function renderChapterContent(content) {
    const blocks = (content || '').split(/\n\s*\n/).map(s => s.trim()).filter(Boolean);
    const out = [];
    blocks.forEach(b => {
      if (isSectionMarker(b)) out.push(heading(b));
      else out.push(para(b));
    });
    return out;
  }

  function statusOk(b) { return b ? '正常' : '关注'; }
  function crimeStatus(r) { return r <= 5 ? '良好' : r <= 10 ? '一般' : '关注'; }
  function unempStatus(r) { return r <= 5 ? '良好' : r <= 10 ? '一般' : '关注'; }
  function qualityGrade(s) { return s >= 80 ? '优秀' : s >= 65 ? '良好' : s >= 50 ? '合格' : s >= 35 ? '待改善' : '不合格'; }

  async function buildReportBlob(report, chapters, analysis, cityName, chartImages) {
    const docx = D();
    const A_ = A();
    const chapterMap = {};
    chapters.forEach(c => chapterMap[c.id] = c.content || '');

    const children = [];
    // 版头标题
    children.push(new docx.Paragraph({ alignment: docx.AlignmentType.CENTER, spacing: { before: 200, after: 80 }, children: [run(`关于${cityName}政府工作的报告`, { bold: true, size: 36, font: '黑体' })] }));
    children.push(para('各位代表：', { alignment: docx.AlignmentType.LEFT, after: 120 }));

    const pushChapter = (id, extra) => {
      renderChapterContent(chapterMap[id]).forEach(p => children.push(p));
      if (extra) extra.forEach(p => { if (p) children.push(p); });
      children.push(new docx.Paragraph({ children: [new docx.PageBreak()] }));
    };

    pushChapter('opening');
    pushChapter('demographics', chartFigure('图1 人口变化趋势图', chartImages && chartImages.pop));
    pushChapter('economy', [
      ...(chartFigure('图2 财政收支趋势图', chartImages && chartImages.income) || []),
      ...(chartFigure('图3 税收结构趋势图', chartImages && chartImages.tax) || []),
    ]);
    // 产业 + 表1
    {
      const sec = analysis.sectors;
      const rows = [
        ['服务业', A_.yuan(sec.service.wealth), `${A_.n0(sec.service.count)}`, `${A_.n0(sec.service.workers)}/${A_.n0(sec.service.maxWorkers)}`, `${A_.n1(sec.service.fillRate)}%`, `${A_.n1(sec.service.pct)}%`],
        ['加工业', A_.yuan(sec.processing.wealth), `${A_.n0(sec.processing.count)}`, `${A_.n0(sec.processing.workers)}/${A_.n0(sec.processing.maxWorkers)}`, `${A_.n1(sec.processing.fillRate)}%`, `${A_.n1(sec.processing.pct)}%`],
        ['办公业', A_.yuan(sec.office.wealth), `${A_.n0(sec.office.count)}`, `${A_.n0(sec.office.workers)}/${A_.n0(sec.office.maxWorkers)}`, `${A_.n1(sec.office.fillRate)}%`, `${A_.n1(sec.office.pct)}%`],
        ['合计', A_.yuan(sec.totalWealth), `${A_.n0(sec.totalCount)}`, `${A_.n0(sec.totalWorkers)}/${A_.n0(sec.totalMaxWorkers)}`, '—', '100%'],
      ];
      const extra = [makeTable(['产业', '财富（₡）', '企业数', '从业/满编', '填充率', '财富占比'], rows)];
      pushChapter('industry', [...(chartFigure('图4 产业结构趋势图', chartImages && chartImages.sector) || []), ...extra]);
    }
    // 就业 + 表2
    {
      const emp = analysis.employment;
      const rows = [
        ['从业人员', `${A_.n0(emp.workerCount)}人`, '—'],
        ['失业率', `${A_.n1(emp.unemploymentRate)}%`, emp.unemploymentRate < 5 ? '良好' : emp.unemploymentRate < 10 ? '一般' : '需要关注'],
        ['劳动参与率', `${A_.n1(emp.workforceParticipation)}%`, emp.workforceParticipation > 50 ? '积极参与' : '偏低'],
      ];
      if (A_.isAvail(emp.cityServiceWorkers)) rows.push(['公务人员填充率', `${A_.n1(emp.cityServiceFillRate)}%`, emp.cityServiceFillRate > 80 ? '充足' : '不足']);
      if (A_.isAvail(emp.seniorWorkerDemand)) rows.push(['高级技工需求', `${A_.n1(emp.seniorWorkerDemand)}%`, emp.seniorWorkerDemand > 70 ? '紧缺' : '正常']);
      pushChapter('employment', [makeTable(['指标', '数值', '评估'], rows)]);
    }
    pushChapter('transport', chartFigure('图5 公共交通客流量分布', chartImages && chartImages.transport));
    pushChapter('social', chartFigure('图6 市民幸福度与健康趋势', chartImages && chartImages.wellbeing));
    // 财政 + 表3
    {
      const f = analysis.fiscal, e = analysis.economy;
      const rows = [
        ['收支比', `${A_.n1(f.revenueExpenseRatio)}`, f.revenueExpenseRatio >= 1 ? '盈余' : '赤字'],
        ['税收依赖度', `${A_.n1(f.taxToIncomeRatio)}%`, f.taxToIncomeRatio > 80 ? '高度依赖' : '健康'],
        ['贸易依赖度', `${A_.n1(f.tradeToIncomeRatio)}%`, '贸易收入占总收入比重'],
        ['人均收入', `₡${A_.n1(e.perCapitaIncome)}`, '每位居民平均贡献'],
        ['人均支出', `₡${A_.n1(e.perCapitaExpense)}`, '每位居民平均享受'],
      ];
      pushChapter('fiscal', [makeTable(['指标', '数值', '说明'], rows)]);
    }
    pushChapter('challenges');
    pushChapter('outlook');

    // 发文机关署名 + 日期
    children.push(para(`${cityName}人民政府`, { alignment: docx.AlignmentType.RIGHT, after: 0 }));
    const now = new Date();
    children.push(para(`${now.getFullYear()}年${now.getMonth() + 1}月${now.getDate()}日`, { alignment: docx.AlignmentType.RIGHT, after: 200 }));

    // 综合评分卡
    children.push(heading('综合评分卡'));
    {
      const rows = analysis.scores.map(s => [s.category, s.name, A_.n1(s.score), s.grade, s.description]);
      children.push(makeTable(['类别', '指标', '得分', '等级', '说明'], rows));
      const avg = analysis.scores.reduce((a, s) => a + s.score, 0) / analysis.scores.length;
      const grade = avg >= 80 ? 'A（优秀）' : avg >= 65 ? 'B（良好）' : avg >= 50 ? 'C（合格）' : avg >= 35 ? 'D（待改善）' : 'F（不合格）';
      children.push(para(`综合评分：${A_.n1(avg)}分，总体等级：${grade}。${avg >= 65 ? '城市发展状况良好，各项指标处于健康水平。' : '城市发展存在一定问题，建议重点关注低分领域。'}`, { after: 200 }));
      children.push(new docx.Paragraph({ children: [new docx.PageBreak()] }));
    }

    // 附表
    children.push(heading('附表：主要指标一览'));
    {
      const o = analysis.overview, d = analysis.demographics, e = analysis.economy, s = analysis.social, emp = analysis.employment, t = analysis.transport;
      const rows = [
        ['1', '常住人口', `${A_.n0(o.population)}`, '人', '—'],
        ['2', '人口增长率', `${d.growthRate >= 0 ? '+' : ''}${A_.n1(d.growthRate)}`, '%', statusOk(d.growthRate > 0)],
        ['3', '财政收入', `₡${A_.n0(e.income)}`, '₡', '—'],
        ['4', '财政支出', `₡${A_.n0(e.expense)}`, '₡', '—'],
        ['5', '净收入', `₡${A_.n0(e.netIncome)}`, '₡', statusOk(e.netIncome >= 0)],
        ['6', '居民幸福度', `${A_.n1(s.wellbeing)}%`, '%', A_.Desc.happinessLevel(s.wellbeing)],
        ['7', '居民健康度', `${A_.n1(s.health)}%`, '%', statusOk(s.health >= 50)],
        ['8', '犯罪率', `${A_.n1(s.crimeRate)}%`, '%', crimeStatus(s.crimeRate)],
        ['9', '失业率', `${A_.n1(emp.unemploymentRate)}%`, '%', unempStatus(emp.unemploymentRate)],
      ];
      if (A_.isAvail(e.trade)) rows.push([`${rows.length + 1}`, '贸易额', `₡${A_.n0(e.trade)}`, '₡', '—']);
      if (A_.isAvail(t.totalPassengers)) rows.push([`${rows.length + 1}`, '客运总量', `${A_.n0(t.totalPassengers)}`, '人次', '—']);
      if (A_.isAvail(t.totalCargo)) rows.push([`${rows.length + 1}`, '货运总量', `${A_.n0(t.totalCargo)}`, '吨', '—']);
      rows.push([`${rows.length + 1}`, '生活质量指数', `${A_.n1(s.qualityOfLifeIndex)}`, '/100', qualityGrade(s.qualityOfLifeIndex)]);
      children.push(makeTable(['序号', '指标名称', '数值', '单位', '状态'], rows));
    }

    const doc = new docx.Document({
      creator: 'CS2 City Analysis (Web)',
      title: `关于${cityName}政府工作的报告`,
      styles: { default: { document: { run: { font: '宋体', size: 21 } } } },
      sections: [{ children }],
    });
    return await docx.Packer.toBlob(doc);
  }

  const ReportDocx = { buildReportBlob, b64ToUint8 };
  if (typeof module !== 'undefined' && module.exports) module.exports = ReportDocx;
  if (root) root.ReportDocx = ReportDocx;
})(typeof window !== 'undefined' ? window : (typeof globalThis !== 'undefined' ? globalThis : this));
