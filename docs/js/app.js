/*
 * app.js — CS2 城市数据分析在线报告生成器（主逻辑）
 */
(function () {
  'use strict';

  const $ = (id) => document.getElementById(id);
  const state = {
    snapshots: [], current: null, analysis: null, dataContext: '',
    chapters: {}, charts: {}, parsed: null,
  };

  const PROVIDERS = {
    deepseek: { url: 'https://api.deepseek.com/v1/chat/completions', model: 'deepseek-v4-flash' },
    openai: { url: 'https://api.openai.com/v1/chat/completions', model: 'gpt-4o-mini' },
    siliconflow: { url: 'https://api.siliconflow.cn/v1/chat/completions', model: 'deepseek-ai/DeepSeek-V3' },
    ollama: { url: 'http://localhost:11434/v1/chat/completions', model: 'llama3' },
    custom: { url: '', model: '' },
  };

  // ── 数据加载 ──
  function readFiles(fileList) {
    const promises = Array.from(fileList).map(f => f.text().then(t => ({ name: f.name, text: t })));
    return Promise.all(promises);
  }

  function ingest(files) {
    const res = window.Analysis.parseFiles(files);
    state.parsed = res;
    state.snapshots = res.snapshots;
    state.current = res.current;
    if (!state.snapshots.length && res.current) state.snapshots = [res.current];
    renderDataSummary(res);
    setStatus('dataStatus', `已解析 ${res.snapshots.length} 个时间点${res.current ? `，当前人口 ${window.Analysis.n0(res.current.population)}` : ''}`, 'ok');
  }

  function renderDataSummary(res) {
    const el = $('dataSummary');
    const rows = res.meta.sources.map(s => `<li>${s.name} — ${s.ok ? (s.kind + (s.count ? ` (${s.count} 点)` : '')) : '失败: ' + (s.err || '')}</li>`).join('');
    const c = res.current || {};
    el.innerHTML = `<div class="summary-grid">
      <div><b>时间点</b><br>${res.snapshots.length}</div>
      <div><b>当前人口</b><br>${window.Analysis.n0(c.population || 0)}</div>
      <div><b>幸福度</b><br>${window.Analysis.n1(c.wellbeing || 0)}%</div>
      <div><b>健康度</b><br>${window.Analysis.n1(c.health || 0)}%</div>
    </div>
    <ul class="src-list">${rows}</ul>`;
    el.hidden = false;
  }

  // ── API 配置 ──
  function applyProvider() {
    const p = PROVIDERS[$('provider').value];
    if (p && p.url) { $('apiUrl').value = p.url; $('model').value = p.model; }
  }

  function getConfig() {
    return {
      apiUrl: $('apiUrl').value.trim(),
      apiKey: $('apiKey').value.trim(),
      model: $('model').value.trim(),
      proxy: $('proxy').value.trim(),
      systemPrompt: window.Analysis.SYSTEM_PROMPT,
    };
  }

  // ── 生成 ──
  async function generate() {
    if (!state.snapshots.length) { setStatus('genStatus', '请先上传数据或加载示例', 'err'); return; }
    if (!getConfig().apiKey) { setStatus('genStatus', '请填写 API Key', 'err'); return; }

    const city = $('cityName').value.trim() || '示范市';
    state.analysis = window.Analysis.analyze(state.snapshots, state.current);
    state.dataContext = window.Analysis.buildDataContext(state.analysis, city);

    const defs = window.Analysis.CHAPTER_DEFS;
    state.chapters = {};
    const prog = $('progress');
    prog.hidden = false; prog.innerHTML = '';

    for (let i = 0; i < defs.length; i++) {
      const def = defs[i];
      const item = document.createElement('div');
      item.className = 'prog-item';
      item.textContent = `⏳ (${i + 1}/${defs.length}) ${def.title}`;
      prog.appendChild(item);
      try {
        const prompt = def.prompt(state.dataContext, city);
        const text = await window.LLM.callLLM(getConfig(), prompt);
        state.chapters[def.id] = text;
        item.textContent = `✅ (${i + 1}/${defs.length}) ${def.title}`;
        item.className = 'prog-item ok';
      } catch (e) {
        state.chapters[def.id] = `（本章生成失败：${e.message}）`;
        item.textContent = `❌ (${i + 1}/${defs.length}) ${def.title} — ${e.message}`;
        item.className = 'prog-item err';
      }
    }
    setStatus('genStatus', '报告已生成，可在下方预览 / 下载', 'ok');
    renderPreview(city);
  }

  // ── 预览渲染 ──
  const CHART_MAP = {
    demographics: [{ key: 'pop', id: 'chart-pop', type: 'line', title: '图1 人口变化趋势图' }],
    economy: [
      { key: 'income', id: 'chart-income', type: 'line', title: '图2 财政收支趋势图' },
      { key: 'tax', id: 'chart-tax', type: 'line', title: '图3 税收结构趋势图' },
    ],
    industry: [{ key: 'sector', id: 'chart-sector', type: 'line', title: '图4 产业结构趋势图' }],
    transport: [{ key: 'transport', id: 'chart-transport', type: 'bar', title: '图5 公共交通客流量分布' }],
    social: [{ key: 'wellbeing', id: 'chart-wellbeing', type: 'line', title: '图6 市民幸福度与健康趋势' }],
  };

  function renderPreview(city) {
    const a = state.analysis;
    const report = $('report');
    report.innerHTML = '';
    const title = document.createElement('h1');
    title.className = 'report-title';
    title.textContent = `关于${city}政府工作的报告`;
    report.appendChild(title);
    report.appendChild(paraEl('各位代表：'));

    const order = window.Analysis.CHAPTER_DEFS.map(d => d.id);
    order.forEach(id => {
      const sec = document.createElement('section');
      sec.className = 'report-chapter';
      renderText(sec, state.chapters[id] || '');
      const charts = CHART_MAP[id];
      if (charts) {
        charts.forEach(c => {
          const cap = document.createElement('div'); cap.className = 'chart-caption'; cap.textContent = c.title;
          const cv = document.createElement('canvas'); cv.id = c.id; cv.height = 260;
          sec.appendChild(cap); sec.appendChild(cv);
          state.charts[c.id] = c; // placeholder; real instance set after attach
        });
      }
      report.appendChild(sec);
    });

    // 署名
    const sign = document.createElement('div'); sign.className = 'sign';
    sign.innerHTML = `<div>${city}人民政府</div><div>${new Date().getFullYear()}年${new Date().getMonth() + 1}月${new Date().getDate()}日</div>`;
    report.appendChild(sign);

    // 评分卡 + 附表
    report.appendChild(buildScoreTable(a));
    report.appendChild(buildAppendixTable(a));

    $('previewCard').hidden = false;
    // 实例化图表
    setTimeout(() => drawCharts(), 50);
  }

  function paraEl(text) { const p = document.createElement('p'); p.textContent = text; return p; }

  function renderText(container, content) {
    const blocks = (content || '').split(/\n\s*\n/).map(s => s.trim()).filter(Boolean);
    blocks.forEach(b => {
      if (/^[一二三四五六七八九十]、/.test(b) && b.length <= 30) {
        const h = document.createElement('h3'); h.textContent = b; container.appendChild(h);
      } else {
        container.appendChild(paraEl(b));
      }
    });
  }

  function buildScoreTable(a) {
    const sec = document.createElement('section');
    const h = document.createElement('h2'); h.textContent = '综合评分卡'; sec.appendChild(h);
    const tbl = document.createElement('table'); tbl.className = 'data-table';
    tbl.innerHTML = '<tr><th>类别</th><th>指标</th><th>得分</th><th>等级</th><th>说明</th></tr>' +
      a.scores.map(s => `<tr><td>${s.category}</td><td>${s.name}</td><td>${window.Analysis.n1(s.score)}</td><td>${s.grade}</td><td>${s.description}</td></tr>`).join('');
    sec.appendChild(tbl);
    const avg = a.scores.reduce((x, s) => x + s.score, 0) / a.scores.length;
    const grade = avg >= 80 ? 'A（优秀）' : avg >= 65 ? 'B（良好）' : avg >= 50 ? 'C（合格）' : avg >= 35 ? 'D（待改善）' : 'F（不合格）';
    sec.appendChild(paraEl(`综合评分：${window.Analysis.n1(avg)}分，总体等级：${grade}。${avg >= 65 ? '城市发展状况良好，各项指标处于健康水平。' : '城市发展存在一定问题，建议重点关注低分领域。'}`));
    return sec;
  }

  function buildAppendixTable(a) {
    const o = a.overview, d = a.demographics, e = a.economy, s = a.social, emp = a.employment, t = a.transport;
    const rows = [
      ['1', '常住人口', `${window.Analysis.n0(o.population)}`, '人', '—'],
      ['2', '人口增长率', `${d.growthRate >= 0 ? '+' : ''}${window.Analysis.n1(d.growthRate)}`, '%', d.growthRate > 0 ? '正常' : '关注'],
      ['3', '财政收入', `₡${window.Analysis.n0(e.income)}`, '₡', '—'],
      ['4', '财政支出', `₡${window.Analysis.n0(e.expense)}`, '₡', '—'],
      ['5', '净收入', `₡${window.Analysis.n0(e.netIncome)}`, '₡', e.netIncome >= 0 ? '正常' : '关注'],
      ['6', '居民幸福度', `${window.Analysis.n1(s.wellbeing)}%`, '%', window.Analysis.Desc.happinessLevel(s.wellbeing)],
      ['7', '居民健康度', `${window.Analysis.n1(s.health)}%`, '%', s.health >= 50 ? '正常' : '关注'],
      ['8', '犯罪率', `${window.Analysis.n1(s.crimeRate)}%`, '%', s.crimeRate <= 5 ? '良好' : s.crimeRate <= 10 ? '一般' : '关注'],
      ['9', '失业率', `${window.Analysis.n1(emp.unemploymentRate)}%`, '%', emp.unemploymentRate <= 5 ? '良好' : emp.unemploymentRate <= 10 ? '一般' : '关注'],
    ];
    if (window.Analysis.isAvail(e.trade)) rows.push([`${rows.length + 1}`, '贸易额', `₡${window.Analysis.n0(e.trade)}`, '₡', '—']);
    if (window.Analysis.isAvail(t.totalPassengers)) rows.push([`${rows.length + 1}`, '客运总量', `${window.Analysis.n0(t.totalPassengers)}`, '人次', '—']);
    if (window.Analysis.isAvail(t.totalCargo)) rows.push([`${rows.length + 1}`, '货运总量', `${window.Analysis.n0(t.totalCargo)}`, '吨', '—']);
    rows.push([`${rows.length + 1}`, '生活质量指数', `${window.Analysis.n1(s.qualityOfLifeIndex)}`, '/100', s.qualityOfLifeIndex >= 80 ? '优秀' : s.qualityOfLifeIndex >= 65 ? '良好' : s.qualityOfLifeIndex >= 50 ? '合格' : s.qualityOfLifeIndex >= 35 ? '待改善' : '不合格']);

    const sec = document.createElement('section');
    const h = document.createElement('h2'); h.textContent = '附表：主要指标一览'; sec.appendChild(h);
    const tbl = document.createElement('table'); tbl.className = 'data-table';
    tbl.innerHTML = '<tr><th>序号</th><th>指标名称</th><th>数值</th><th>单位</th><th>状态</th></tr>' +
      rows.map(r => `<tr><td>${r[0]}</td><td>${r[1]}</td><td>${r[2]}</td><td>${r[3]}</td><td>${r[4]}</td></tr>`).join('');
    sec.appendChild(tbl);
    return sec;
  }

  // ── 图表绘制 ──
  function labels() {
    return state.snapshots.map((s, i) => `Y${s.gameYear}M${s.gameMonth}`);
  }
  function drawCharts() {
    const L = labels();
    const map = {
      'chart-pop': () => lineChart('chart-pop', L, [{ label: '人口', data: state.snapshots.map(s => s.population), color: '#2563eb' }]),
      'chart-income': () => lineChart('chart-income', L, [
        { label: '收入', data: state.snapshots.map(s => s.income), color: '#16a34a' },
        { label: '支出', data: state.snapshots.map(s => s.expense), color: '#dc2626' },
      ]),
      'chart-tax': () => lineChart('chart-tax', L, [
        { label: '住宅税', data: state.snapshots.map(s => s.residentialTaxableIncome), color: '#2563eb' },
        { label: '商业税', data: state.snapshots.map(s => s.commercialTaxableIncome), color: '#16a34a' },
        { label: '工业税', data: state.snapshots.map(s => s.industrialTaxableIncome), color: '#d97706' },
        { label: '办公税', data: state.snapshots.map(s => s.officeTaxableIncome), color: '#9333ea' },
      ]),
      'chart-sector': () => lineChart('chart-sector', L, [
        { label: '服务业', data: state.snapshots.map(s => s.serviceWealth), color: '#2563eb' },
        { label: '加工业', data: state.snapshots.map(s => s.processingWealth), color: '#d97706' },
        { label: '办公业', data: state.snapshots.map(s => s.officeWealth), color: '#9333ea' },
      ]),
      'chart-transport': () => barChart('chart-transport', ['公交', '地铁', '有轨电车', '火车', '出租车', '航空', '水运'],
        [state.current.passengerCountBus, state.current.passengerCountSubway, state.current.passengerCountTram, state.current.passengerCountTrain, state.current.passengerCountTaxi, state.current.passengerCountAirplane, state.current.passengerCountShip]),
      'chart-wellbeing': () => lineChart('chart-wellbeing', L, [
        { label: '幸福度', data: state.snapshots.map(s => Math.round(s.wellbeing * 10) / 10), color: '#16a34a' },
        { label: '健康度', data: state.snapshots.map(s => Math.round(s.health * 10) / 10), color: '#dc2626' },
      ]),
    };
    state.charts = {};
    Object.keys(map).forEach(id => {
      const cv = document.getElementById(id);
      if (cv) { try { state.charts[id] = map[id](); } catch (e) { console.error(e); } }
    });
  }

  function lineChart(id, labels, datasets) {
    return new Chart(document.getElementById(id), {
      type: 'line',
      data: { labels, datasets: datasets.map(d => ({ label: d.label, data: d.data, borderColor: d.color, backgroundColor: d.color + '22', fill: true, tension: 0.3, pointRadius: 1, borderWidth: 2 })) },
      options: { responsive: true, plugins: { legend: { position: 'bottom' } }, scales: { y: { beginAtZero: true } } },
    });
  }
  function barChart(id, labels, data) {
    return new Chart(document.getElementById(id), {
      type: 'bar',
      data: { labels, datasets: [{ label: '客运量（人次）', data, backgroundColor: '#2563eb' }] },
      options: { responsive: true, plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true } } },
    });
  }

  // ── 下载 ──
  function downloadBlob(blob, filename) {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a'); a.href = url; a.download = filename; a.click();
    setTimeout(() => URL.revokeObjectURL(url), 2000);
  }

  async function downloadWord() {
    if (!state.analysis) return;
    const city = $('cityName').value.trim() || '示范市';
    const imgs = {};
    [['pop', 'chart-pop'], ['income', 'chart-income'], ['tax', 'chart-tax'], ['sector', 'chart-sector'], ['transport', 'chart-transport'], ['wellbeing', 'chart-wellbeing']]
      .forEach(([k, id]) => { const c = state.charts[id]; if (c) { try { imgs[k] = c.toBase64Image(); } catch (e) {} } });
    const chaptersArr = window.Analysis.CHAPTER_DEFS.map(d => ({ id: d.id, content: state.chapters[d.id] || '' }));
    setStatus('genStatus', '正在生成 Word…', '');
    const blob = await window.ReportDocx.buildReportBlob(state.analysis, chaptersArr, state.analysis, city, imgs);
    downloadBlob(blob, `${city}政府工作报告.docx`);
    setStatus('genStatus', 'Word 已下载', 'ok');
  }

  function setStatus(id, msg, kind) {
    const el = $(id); if (!el) return;
    el.textContent = msg; el.className = 'status' + (kind ? ' ' + kind : '');
  }

  // ── 事件绑定 ──
  document.addEventListener('DOMContentLoaded', () => {
    $('fileInput').addEventListener('change', async (e) => {
      if (!e.target.files.length) return;
      setStatus('dataStatus', '解析中…', '');
      ingest(await readFiles(e.target.files));
    });
    $('demoBtn').addEventListener('click', () => {
      setStatus('dataStatus', '加载示例…', '');
      ingest(window.Sample.getDemoFiles());
    });
    $('provider').addEventListener('change', applyProvider);
    applyProvider();
    $('testBtn').addEventListener('click', async () => {
      setStatus('testStatus', '测试中…', '');
      const r = await window.LLM.testConnection(getConfig());
      setStatus('testStatus', r.success ? `✅ 连接成功（${r.latency}ms, ${r.model}）` : `❌ ${r.error}`, r.success ? 'ok' : 'err');
    });
    $('genBtn').addEventListener('click', generate);
    $('downloadWord').addEventListener('click', downloadWord);
  });
})();
