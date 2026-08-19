/*
 * analysis.js — CS2 城市数据分析核心（纯前端移植自 DataAnalyzer）
 * 负责：解析 Mod 导出的 JSON → 计算分析指标 → 拼装喂给 LLM 的数据上下文 → 提供章节 prompt
 * 不依赖任何浏览器 API，可在 Node 下做单元测试。
 */
(function (root) {
  'use strict';

  // ───────────────────────── 数值格式化 ─────────────────────────
  const n0 = (v) => (v == null ? '0' : Math.round(v).toLocaleString('en-US'));
  const n1 = (v) => (v == null ? '0.0' : Number(v).toFixed(1));
  const pct = (v) => (v == null ? '0.0' : Number(v).toFixed(1));
  const yuan = (v) => '₡' + n0(v);

  // 与桌面端 GameMetricConverter.IsServiceAvailable 对齐
  const isAvail = (v) => v != null && Math.abs(v) > 0.01;

  // ───────────────────────── 指标描述（中文等级） ─────────────────────────
  const Desc = {
    happiness: (w) => w >= 90 ? '安居乐业，市民生活满意度极高' : w >= 80 ? '城市宜居，市民普遍感到满意' :
      w >= 65 ? '生活稳定，市民情绪积极' : w >= 50 ? '基本满意，城市运行平稳' :
      w >= 35 ? '部分不满，需要关注民生改善' : w >= 20 ? '较多不满，民生问题亟待解决' : '严重不满，城市面临信任危机',
    happinessLevel: (w) => w >= 90 ? '优秀' : w >= 80 ? '良好' : w >= 65 ? '一般' : w >= 50 ? '及格' : w >= 35 ? '较差' : '很差',
    health: (h) => h >= 90 ? '医疗体系完善，居民健康水平优良' : h >= 80 ? '医疗服务充足，居民身体状态良好' :
      h >= 65 ? '基本医疗保障到位，整体健康达标' : h >= 50 ? '医疗资源有待扩充，部分居民健康欠佳' :
      h >= 35 ? '医疗服务不足，需要加大医疗投入' : '医疗危机，居民健康严重恶化',
    crime: (r) => r <= 2 ? '治安优良，市民安全感强' : r <= 5 ? '治安稳定，城市秩序井然' :
      r <= 10 ? '治安基本可控，偶有轻微案件' : r <= 20 ? '犯罪率偏高，需加强警力部署' :
      r <= 35 ? '治安形势严峻，公共安全受到挑战' : '治安恶化严重，城市面临安全危机',
    unemployment: (r) => r <= 3 ? '充分就业，劳动力市场供需平衡' : r <= 5 ? '就业形势良好，失业率处于健康水平' :
      r <= 8 ? '就业基本稳定，需关注结构性失业' : r <= 12 ? '就业压力较大，应出台稳就业措施' :
      r <= 20 ? '失业问题突出，急需就业扶持政策' : '就业危机，大量居民面临生计困难',
    education: (r) => r >= 80 ? '教育资源充裕，满足市民求学需求' : r >= 60 ? '教育覆盖较广，基本满足需求' :
      r >= 40 ? '教育资源适中，尚有提升空间' : r >= 20 ? '教育设施不足，部分市民就学困难' : '教育资源匮乏，急需建设学校',
    traffic: (s) => s >= 70 ? '公共交通主导，绿色出行成效显著' : s >= 50 ? '公共交通分担率较高，出行结构合理' :
      s >= 30 ? '公交与私车并行，交通体系均衡' : s >= 15 ? '公共交通分担不足，道路压力较大' : '公共交通薄弱，依赖私家车出行',
    budget: (r) => r >= 1.5 ? '财政高度充裕，有大量资金可供投资' : r >= 1.1 ? '财政盈余，政府财力充裕' :
      r >= 1.0 ? '收支平衡，财政运行稳健' : r >= 0.9 ? '略微赤字，需关注支出控制' :
      r >= 0.7 ? '赤字运行，应开源节流' : '严重赤字，财政状况亟待改善',
    stage: (pop) => pop < 1000 ? '初创期——城市刚刚起步，各项基础设施正在建设中' :
      pop < 5000 ? '发展初期——城市初具规模，公共服务体系逐步建立' : pop < 20000 ? '快速成长期——人口加速流入，城市功能日趋完善' :
      pop < 50000 ? '发展中期——城市规模不断扩大，多元产业协同发展' : pop < 100000 ? '成熟发展期——城市功能完善，综合承载力稳步提升' : '大都市阶段——城市高度发达，区域影响力显著增强',
    momentum: (g) => g > 5 ? '强劲增长' : g > 1 ? '温和增长' : g > 0 ? '保持稳定' : g > -1 ? '轻微下滑' : g > -5 ? '明显下滑' : '快速下滑',
    socialMomentum: (h, c) => (h > 2 && c < -2) ? '持续改善' : (h > 0 && c < 5) ? '保持稳定' : (h < -5 || c > 10) ? '明显恶化' : '分化发展',
  };

  // ───────────────────────── 原始快照 → 归一化快照 ─────────────────────────
  // 兼容两种导出：history.json（数组，字段较精简）与 current_snapshot.json / full_history.json（字段完整）
  function normalize(raw, idx, total) {
    const g = (k) => (raw && raw[k] != null ? raw[k] : 0);
    const population = g('population') || 0;
    const avgH = g('averageHappiness') || 0;
    const avgHe = g('averageHealth') || 0;
    const birth = g('birthRate') || 0;
    const death = g('deathRate') || 0;
    const adult = g('adultsCount') || 0;

    const snap = {
      realTime: raw.realTime || raw.gameTime || '',
      gameYear: raw.gameYear || 1,
      gameMonth: raw.gameMonth || 1,
      gameDay: raw.gameDay || 1,
      population,
      populationWithMoveIn: g('populationWithMoveIn'),
      citizensMovedIn: g('citizensMovedIn'),
      citizensMovedAway: g('citizensMovedAway'),
      birthRate: birth,
      deathRate: death,
      money: g('money'),
      income: g('income'),
      expense: g('expense'),
      trade: g('trade'),
      averageHappiness: avgH,
      averageHealth: avgHe,
      wellbeing: population > 0 ? avgH / population : 0,
      health: population > 0 ? avgHe / population : 0,
      homelessCount: g('homelessCount'),
      workerCount: g('workerCount'),
      unemployed: g('unemployed'),
      currentTourists: g('currentTourists'),
      averageTourists: g('averageTourists'),
      touristCount: g('touristCount'),
      touristIncome: g('touristIncome'),
      attractiveness: g('attractiveness'),
      devTreePoints: g('devTreePoints'),
      crimeRate: g('crimeRate'),
      crimeCount: g('crimeCount'),
      passengerCountBus: g('passengerCountBus'),
      passengerCountSubway: g('passengerCountSubway'),
      passengerCountTrain: g('passengerCountTrain'),
      passengerCountTram: g('passengerCountTram'),
      passengerCountAirplane: g('passengerCountAirplane'),
      passengerCountTaxi: g('passengerCountTaxi'),
      passengerCountShip: g('passengerCountShip'),
      cargoCountTruck: g('cargoCountTruck'),
      cargoCountTrain: g('cargoCountTrain'),
      cargoCountShip: g('cargoCountShip'),
      cargoCountAirplane: g('cargoCountAirplane'),
      residentialTaxableIncome: g('residentialTaxableIncome'),
      commercialTaxableIncome: g('commercialTaxableIncome'),
      industrialTaxableIncome: g('industrialTaxableIncome'),
      officeTaxableIncome: g('officeTaxableIncome'),
      educationCount: g('educationCount'),
      adultsCount: adult,
      age: g('age'),
      collectedMail: g('collectedMail'),
      deliveredMail: g('deliveredMail'),
      serviceWealth: g('serviceWealth'), serviceCount: g('serviceCount'),
      serviceWorkers: g('serviceWorkers'), serviceMaxWorkers: g('serviceMaxWorkers'),
      processingWealth: g('processingWealth'), processingCount: g('processingCount'),
      processingWorkers: g('processingWorkers'), processingMaxWorkers: g('processingMaxWorkers'),
      officeWealth: g('officeWealth'), officeCount: g('officeCount'),
      officeWorkers: g('officeWorkers'), officeMaxWorkers: g('officeMaxWorkers'),
      cityServiceWorkers: g('cityServiceWorkers'), cityServiceMaxWorkers: g('cityServiceMaxWorkers'),
      householdWealth: g('householdWealth'), householdCount: g('householdCount'),
      seniorWorkerInDemandPercentage: g('seniorWorkerInDemandPercentage'),
      escapedArrestCount: g('escapedArrestCount'),
      wellbeingLevel: g('wellbeingLevel'), healthLevel: g('healthLevel'),
      stats: raw.stats || null,
    };
    // 派生指标
    snap.naturalGrowth = birth - death;
    snap.birthRatePerMille = population > 0 ? birth / population * 1000 : 0;
    snap.deathRatePerMille = population > 0 ? death / population * 1000 : 0;
    snap.netMigration = snap.citizensMovedIn - snap.citizensMovedAway;
    snap.homelessRate = population > 0 ? snap.homelessCount / population * 100 : 0;
    snap.educationRate = adult > 0 ? snap.educationCount / adult * 100 : 0;
    snap.employmentRate = population > 0 ? Math.min(snap.workerCount / population * 100, 100) : 0;
    snap.adultsRatio = population > 0 ? adult / population * 100 : 0;
    return snap;
  }

  // full_history.json：并行数组 → 快照序列（与 DataReader.BuildSnapshotsFromFullHistory 对齐）
  function buildFromFullHistory(full) {
    if (!full || typeof full !== 'object') return [];
    // 真实 Mod 导出的 full_history.json 为纯小驼峰（camelCase）并行数组；
    // 建立大小写无关的索引，兼容 PascalCase 与小驼峰两种命名（修复 P('Population') 永远取不到 'population' 的 bug）。
    const _idx = {};
    Object.keys(full).forEach((k) => { _idx[k.toLowerCase()] = full[k]; });
    const P = (k) => full[k] || full[capitalize(k)] || _idx[k.toLowerCase()] || [];
    const pop = P('Population');
    const count = pop.length;
    if (count === 0) return [];
    const kUpdatesPerDay = full.kUpdatesPerDay || full.KUpdatesPerDay || 32;
    const daysPerYear = full.daysPerYear || full.DaysPerYear || 12;
    const samplesPerDay = kUpdatesPerDay;
    const gm = P('GameMonth'), gy = P('GameYear'), gd = P('GameDay');
    const hasValidTime = gm.length === count && new Set(gm).size > 1;
    const arr = (k) => P(k);
    const val = (list, i) => {
      if (!list || !list.length) return 0;
      return i < list.length ? list[i] : list[list.length - 1];
    };
    const snaps = [];
    for (let i = 0; i < count; i++) {
      let gameYear, gameMonth, gameDay;
      if (hasValidTime) {
        gameYear = val(gy, i); gameMonth = val(gm, i); gameDay = gd.length === count ? val(gd, i) : gameMonth;
      } else {
        const totalDays = Math.floor(i / samplesPerDay);
        gameYear = Math.floor(totalDays / daysPerYear);
        const dayInYear = totalDays % daysPerYear;
        gameMonth = dayInYear + 1; gameDay = dayInYear + 1;
      }
      const raw = {
        gameYear, gameMonth, gameDay,
        population: val(pop, i),
        populationWithMoveIn: val(P('CitizensMovedIn'), i),
        citizensMovedIn: val(P('CitizensMovedIn'), i),
        citizensMovedAway: val(P('CitizensMovedAway'), i),
        birthRate: val(P('BirthRate'), i),
        deathRate: val(P('DeathRate'), i),
        money: val(P('Income'), i) /* placeholder */,
        income: val(P('Income'), i),
        expense: val(P('Expense'), i),
        trade: val(P('Trade'), i),
        averageHappiness: val(P('Wellbeing'), i),
        averageHealth: val(P('Health'), i),
        homelessCount: val(P('HomelessCount'), i),
        workerCount: val(P('WorkerCount'), i),
        unemployed: val(P('Unemployed'), i),
        touristCount: val(P('TouristCount'), i),
        touristIncome: val(P('TouristIncome'), i),
        attractiveness: val(P('Attractiveness'), i),
        devTreePoints: val(P('DevTreePoints'), i),
        crimeRate: val(P('CrimeRate'), i),
        crimeCount: val(P('CrimeCount'), i),
        passengerCountBus: val(P('PassengerCountBus'), i),
        passengerCountSubway: val(P('PassengerCountSubway'), i),
        passengerCountTrain: val(P('PassengerCountTrain'), i),
        passengerCountTram: val(P('PassengerCountTram'), i),
        passengerCountAirplane: val(P('PassengerCountAirplane'), i),
        passengerCountTaxi: val(P('PassengerCountTaxi'), i),
        passengerCountShip: val(P('PassengerCountShip'), i),
        cargoCountTruck: val(P('CargoCountTruck'), i),
        cargoCountTrain: val(P('CargoCountTrain'), i),
        cargoCountShip: val(P('CargoCountShip'), i),
        cargoCountAirplane: val(P('CargoCountAirplane'), i),
        residentialTaxableIncome: val(P('ResidentialTaxableIncome'), i),
        commercialTaxableIncome: val(P('CommercialTaxableIncome'), i),
        industrialTaxableIncome: val(P('IndustrialTaxableIncome'), i),
        officeTaxableIncome: val(P('OfficeTaxableIncome'), i),
        educationCount: val(P('EducationCount'), i),
        adultsCount: val(P('AdultsCount'), i),
        age: val(P('Age'), i),
        collectedMail: val(P('CollectedMail'), i),
        deliveredMail: val(P('DeliveredMail'), i),
        serviceWealth: val(P('ServiceWealth'), i), serviceCount: val(P('ServiceCount'), i),
        serviceWorkers: val(P('ServiceWorkers'), i), serviceMaxWorkers: val(P('ServiceMaxWorkers'), i),
        processingWealth: val(P('ProcessingWealth'), i), processingCount: val(P('ProcessingCount'), i),
        processingWorkers: val(P('ProcessingWorkers'), i), processingMaxWorkers: val(P('ProcessingMaxWorkers'), i),
        officeWealth: val(P('OfficeWealth'), i), officeCount: val(P('OfficeCount'), i),
        officeWorkers: val(P('OfficeWorkers'), i), officeMaxWorkers: val(P('OfficeMaxWorkers'), i),
        cityServiceWorkers: val(P('CityServiceWorkers'), i), cityServiceMaxWorkers: val(P('CityServiceMaxWorkers'), i),
        householdWealth: val(P('HouseholdWealth'), i), householdCount: val(P('HouseholdCount'), i),
        seniorWorkerInDemandPercentage: val(P('SeniorWorkerInDemandPercentage'), i),
        escapedArrestCount: val(P('EscapedArrestCount'), i),
        wellbeingLevel: val(P('WellbeingLevel'), i), healthLevel: val(P('HealthLevel'), i),
        money: val(P('Money'), i),
      };
      snaps.push(normalize(raw, i, count));
    }
    return snaps;
  }

  function capitalize(s) { return s.charAt(0).toUpperCase() + s.slice(1); }

  // ───────────────────────── 解析上传的文件集合 ─────────────────────────
  // files: [{name, text}]  ；返回 { snapshots, current, meta }
  function parseFiles(files) {
    let snapshots = [];
    let currentCandidate = null;
    const meta = { sources: [] };

    for (const f of files) {
      let json;
      try { json = JSON.parse(f.text); } catch (e) { meta.sources.push({ name: f.name, ok: false, err: 'JSON 解析失败' }); continue; }
      if (Array.isArray(json)) {
        const arr = json.map((r, i) => normalize(r, i, json.length)).filter(s => s.population >= 0);
        snapshots = snapshots.concat(arr);
        meta.sources.push({ name: f.name, ok: true, kind: 'history', count: arr.length });
      } else if (json && (Array.isArray(json.Population) || Array.isArray(json.population))) {
        const arr = buildFromFullHistory(json);
        snapshots = snapshots.concat(arr);
        meta.sources.push({ name: f.name, ok: true, kind: 'full_history', count: arr.length });
      } else if (json && typeof json.population === 'number') {
        currentCandidate = normalize(json, 0, 1);
        meta.sources.push({ name: f.name, ok: true, kind: 'current_snapshot' });
      } else {
        meta.sources.push({ name: f.name, ok: false, err: '无法识别的 JSON 结构' });
      }
    }

    // 按时间排序（用游戏年/月/日）
    snapshots.sort((a, b) => (a.gameYear - b.gameYear) || (a.gameMonth - b.gameMonth) || (a.gameDay - b.gameDay));

    let current = null;
    if (snapshots.length) {
      current = Object.assign({}, snapshots[snapshots.length - 1]);
    }
    // current_snapshot.json 字段更全，叠加覆盖
    if (currentCandidate) {
      current = current ? Object.assign({}, current, currentCandidate) : currentCandidate;
    }
    if (!current) current = normalize({}, 0, 1);

    return { snapshots, current, meta };
  }

  // ───────────────────────── 指标分析（对齐 AnalysisEngine） ─────────────────────────
  function analyze(snapshots, current) {
    const history = snapshots.length ? snapshots : [current];
    const sel = (fn) => history.map(fn);
    const last = (fn) => history.length ? fn(history[history.length - 1]) : 0;
    const prev = (fn) => history.length > 1 ? fn(history[history.length - 2]) : 0;
    const growth = (fn) => {
      if (history.length < 2) return 0;
      const c = last(fn), p = prev(fn);
      if (Math.abs(p) < 1) return c > 0 ? 100 : 0;
      return (c - p) / p * 100;
    };

    const pop = current.population;
    const totalTax = current.residentialTaxableIncome + current.commercialTaxableIncome + current.industrialTaxableIncome + current.officeTaxableIncome;

    const overview = {
      population: pop, money: current.money, happiness: current.wellbeing, health: current.health,
      gameYear: current.gameYear, gameMonth: current.gameMonth, totalSamples: snapshots.length,
    };

    const demographics = {
      population: pop, populationWithMoveIn: current.populationWithMoveIn,
      growthRate: growth(s => s.population), citizensMovedIn: current.citizensMovedIn,
      citizensMovedAway: current.citizensMovedAway, netMigration: current.netMigration,
      birthRate: current.birthRatePerMille, deathRate: current.deathRatePerMille,
      naturalGrowth: Math.round((current.birthRatePerMille - current.deathRatePerMille) * 10) / 10, adultsCount: current.adultsCount, adultsRatio: current.adultsRatio,
    };

    const expenseAvail = current.expense > 0.01;
    const economy = {
      money: current.money, income: current.income, expense: current.expense,
      netIncome: expenseAvail ? current.income - current.expense : null,
      profitMargin: current.income > 0 ? (current.income - current.expense) / current.income * 100 : 0,
      trade: current.trade, devTreePoints: current.devTreePoints,
      perCapitaIncome: pop > 0 ? current.income / pop : 0,
      perCapitaExpense: pop > 0 ? current.expense / pop : 0,
      perCapitaTax: pop > 0 ? totalTax / pop : 0,
      totalTax,
      residentialTax: current.residentialTaxableIncome, commercialTax: current.commercialTaxableIncome,
      industrialTax: current.industrialTaxableIncome, officeTax: current.officeTaxableIncome,
      residentialTaxPct: totalTax > 0 ? current.residentialTaxableIncome / totalTax * 100 : 0,
      commercialTaxPct: totalTax > 0 ? current.commercialTaxableIncome / totalTax * 100 : 0,
      industrialTaxPct: totalTax > 0 ? current.industrialTaxableIncome / totalTax * 100 : 0,
      officeTaxPct: totalTax > 0 ? current.officeTaxableIncome / totalTax * 100 : 0,
    };

    const totalWealth = current.serviceWealth + current.processingWealth + current.officeWealth;
    const sector = (w, c, wk, mw) => ({
      wealth: w, count: c, workers: wk, maxWorkers: mw,
      pct: totalWealth > 0 ? w / totalWealth * 100 : 0,
      fillRate: mw > 0 ? wk / mw * 100 : 0,
    });
    const sectors = {
      service: sector(current.serviceWealth, current.serviceCount, current.serviceWorkers, current.serviceMaxWorkers),
      processing: sector(current.processingWealth, current.processingCount, current.processingWorkers, current.processingMaxWorkers),
      office: sector(current.officeWealth, current.officeCount, current.officeWorkers, current.officeMaxWorkers),
      totalWealth, totalCount: current.serviceCount + current.processingCount + current.officeCount,
      totalWorkers: current.serviceWorkers + current.processingWorkers + current.officeWorkers,
      totalMaxWorkers: current.serviceMaxWorkers + current.processingMaxWorkers + current.officeMaxWorkers,
    };
    sectors.serviceWealthPct = sectors.service.pct; sectors.processingWealthPct = sectors.processing.pct; sectors.officeWealthPct = sectors.office.pct;
    sectors.serviceWorkerFillRate = sectors.service.fillRate; sectors.processingWorkerFillRate = sectors.processing.fillRate; sectors.officeWorkerFillRate = sectors.office.fillRate;

    const laborForce = current.workerCount + current.unemployed;
    const employment = {
      workerCount: current.workerCount, unemployed: current.unemployed,
      unemploymentRate: laborForce > 0 ? current.unemployed / laborForce * 100 : 0,
      workforceParticipation: pop > 0 ? current.workerCount / pop * 100 : 0,
      cityServiceWorkers: current.cityServiceWorkers, cityServiceMaxWorkers: current.cityServiceMaxWorkers,
      cityServiceFillRate: current.cityServiceMaxWorkers > 0 ? current.cityServiceWorkers / current.cityServiceMaxWorkers * 100 : 0,
      seniorWorkerDemand: current.seniorWorkerInDemandPercentage,
    };

    const tp = current.passengerCountBus + current.passengerCountSubway + current.passengerCountTram + current.passengerCountTrain + current.passengerCountTaxi + current.passengerCountAirplane + current.passengerCountShip;
    const pub = current.passengerCountBus + current.passengerCountSubway + current.passengerCountTram + current.passengerCountTrain;
    const tc = current.cargoCountTruck + current.cargoCountTrain + current.cargoCountShip + current.cargoCountAirplane;
    const mode = (p) => ({ passengers: p, share: tp > 0 ? p / tp * 100 : 0 });
    const transport = {
      bus: mode(current.passengerCountBus), subway: mode(current.passengerCountSubway), tram: mode(current.passengerCountTram),
      train: mode(current.passengerCountTrain), taxi: mode(current.passengerCountTaxi), airplane: mode(current.passengerCountAirplane),
      ship: mode(current.passengerCountShip), totalPassengers: tp, publicTransitShare: tp > 0 ? pub / tp * 100 : 0,
      cargoTruck: current.cargoCountTruck, cargoTrain: current.cargoCountTrain, cargoShip: current.cargoCountShip, cargoAirplane: current.cargoCountAirplane, totalCargo: tc,
    };

    const employmentRate = pop > 0 && current.workerCount > 0 ? Math.min(current.workerCount / pop * 100, 100) : 0;
    const educationRate = current.adultsCount > 0 ? current.educationCount / current.adultsCount * 100 : 0;
    const homelessRate = pop > 0 ? current.homelessCount / pop * 100 : 0;
    const qoL = (Math.min(current.wellbeing, 100) / 100 * 0.35 + Math.min(current.health, 100) / 100 * 0.35 +
      Math.max(0, (10 - current.crimeRate) / 10) * 0.15 + Math.max(0, 1 - homelessRate / 100) * 0.15) * 100;
    const social = {
      wellbeing: current.wellbeing, health: current.health, crimeRate: current.crimeRate, crimeCount: current.crimeCount,
      escapedArrestCount: current.escapedArrestCount, homelessCount: current.homelessCount, homelessPerCapita: current.homelessRate * 10,
      educationCount: current.educationCount, educationRate, collectedMail: current.collectedMail, deliveredMail: current.deliveredMail,
      qualityOfLifeIndex: Math.round(Math.max(0, Math.min(qoL, 100)) * 10) / 10,
    };

    const ratio = expenseAvail ? (current.income > 0 ? current.income / current.expense : 100) : null;
    let fiscalStatus = !expenseAvail ? 'Data Missing' :
      ratio >= 1.5 ? 'Highly Surplus' : ratio >= 1.1 ? 'Surplus' : ratio >= 1.0 ? 'Balanced' :
      ratio >= 0.9 ? 'Mild Deficit' : ratio >= 0.7 ? 'Deficit' : 'Severe Deficit';
    const fiscal = {
      revenueExpenseRatio: ratio != null ? Math.round(ratio * 100) / 100 : null,
      expenseAvailable: expenseAvail,
      taxToIncomeRatio: current.income > 0 ? Math.round(totalTax / current.income * 100 * 10) / 10 : 0,
      tradeToIncomeRatio: current.income > 0 ? Math.round(current.trade / current.income * 100 * 10) / 10 : 0,
      fiscalStatus,
    };

    const households = {
      householdCount: current.householdCount, householdWealth: current.householdWealth,
      avgWealthPerHousehold: current.householdCount > 0 ? current.householdWealth / current.householdCount : 0,
      avgPersonsPerHousehold: current.householdCount > 0 ? pop / current.householdCount : 0,
    };

    const trends = {
      popGrowthRate: growth(s => s.population), incomeGrowthRate: growth(s => s.income), expenseGrowthRate: growth(s => s.expense),
      happinessTrend: growth(s => s.wellbeing), healthTrend: growth(s => s.health), crimeTrend: growth(s => s.crimeRate),
      tourismTrend: growth(s => s.touristCount),
      popMomentum: Desc.momentum(growth(s => s.population)),
      economyMomentum: Desc.momentum(growth(s => s.income)),
      socialMomentum: Desc.socialMomentum(growth(s => s.wellbeing), growth(s => s.crimeRate)),
    };

    // 告警
    const alerts = [];
    const unempRate = laborForce > 0 ? current.unemployed / laborForce * 100 : 0;
    if (current.crimeRate > 20) alerts.push({ level: 'danger', category: 'Public Safety', message: `犯罪率 ${n1(current.crimeRate)}% 超出安全阈值` });
    else if (current.crimeRate > 10) alerts.push({ level: 'warning', category: 'Public Safety', message: `犯罪率偏高 ${n1(current.crimeRate)}%` });
    if (current.homelessCount > 0 && pop > 0 && current.homelessCount / pop > 0.05) alerts.push({ level: 'warning', category: 'Social Welfare', message: `无家可归率 ${n1(current.homelessCount / pop * 100)}% 需要关注` });
    if (growth(s => s.population) < -5) alerts.push({ level: 'danger', category: 'Demographics', message: '人口快速下降！' });
    if (current.income > 0 && current.expense > current.income * 1.1) alerts.push({ level: 'danger', category: 'Fiscal', message: '严重财政赤字——支出超过收入 10% 以上' });
    if (unempRate > 15) alerts.push({ level: 'danger', category: 'Employment', message: `失业率 ${n1(unempRate)}%——危急水平` });
    else if (unempRate > 8) alerts.push({ level: 'warning', category: 'Employment', message: `失业率偏高 ${n1(unempRate)}%` });
    if (current.wellbeing < 40) alerts.push({ level: 'danger', category: 'Wellbeing', message: `市民幸福度极低 ${Math.round(current.wellbeing)}%` });
    if (current.health < 40) alerts.push({ level: 'danger', category: 'Health', message: `市民健康度极低 ${Math.round(current.health)}%` });
    if (current.seniorWorkerInDemandPercentage > 80) alerts.push({ level: 'warning', category: 'Labor Market', message: `高级技工需求 ${n1(current.seniorWorkerInDemandPercentage)}%——可能存在技能缺口` });

    // 评分卡
    const clamp = (v) => Math.max(0, Math.min(v, 100));
    const score = (cat, name, s, desc) => ({ category: cat, name, score: Math.round(clamp(s) * 10) / 10, grade: s >= 80 ? 'A' : s >= 65 ? 'B' : s >= 50 ? 'C' : s >= 35 ? 'D' : 'F', description: desc });
    const scores = [
      score('Economy', 'Budget Health', expenseAvail ? Math.min(current.income / current.expense * 50, 100) : 50, expenseAvail ? '收入支出比' : '支出数据缺失'),
      score('Economy', 'Growth Momentum', Math.max(0, Math.min(growth(s => s.population) + 50, 100)), '人口增长趋势'),
      score('Society', 'Wellbeing', Math.min(current.wellbeing, 100), '市民幸福水平'),
      score('Society', 'Public Health', Math.min(current.health, 100), '市民健康水平'),
      score('Society', 'Education', Math.min(pop > 0 ? current.educationCount / pop * 200 : 0, 100), '教育覆盖率'),
      score('Safety', 'Crime Control', Math.max(0, 100 - current.crimeRate), '犯罪越低分越高'),
      score('Employment', 'Job Market', Math.max(0, 100 - (laborForce > 0 ? current.unemployed / laborForce * 500 : 0)), '失业越低分越高'),
      score('Living', 'Housing', Math.max(0, 100 - (pop > 0 ? current.homelessCount / pop * 2000 : 0)), '无家可归越低分越高'),
    ];

    const fiscalTxt = economy.netIncome != null
      ? (economy.netIncome >= 0 ? `月盈余 ${yuan(economy.netIncome)}` : `月赤字 ${yuan(-economy.netIncome)}`)
      : '财政支出数据缺失';
    overview.summary = `${n0(pop)} 居民 | 第${current.gameYear}年${current.gameMonth}月 | ` +
      fiscalTxt + ` | QoL ${n1(social.qualityOfLifeIndex)}/100 | ` +
      (alerts.some(a => a.level === 'danger') ? `⚠ ${alerts.filter(a => a.level === 'danger').length} 项严重问题` :
        alerts.some(a => a.level === 'warning') ? `⚡ ${alerts.filter(a => a.level === 'warning').length} 项警告` : '各项指标正常');

    return {
      overview, demographics, economy, sectors, employment, transport, social, fiscal, households, trends, alerts, scores,
      current, history,
    };
  }

  // ───────────────────────── 拼装喂给 LLM 的数据上下文 ─────────────────────────
  function buildDataContext(analysis, cityName) {
    const a = analysis;
    const o = a.overview, d = a.demographics, e = a.economy, sec = a.sectors, emp = a.employment,
      t = a.transport, s = a.social, f = a.fiscal, h = a.households, tr = a.trends, cur = a.current;
    const lines = [];
    const ap = (label, value, fmt) => {
      if (isAvail(value)) lines.push(`  ${label}：${typeof fmt === 'function' ? fmt(value) : value}`);
      else lines.push(`  ${label}：尚未发展`);
    };
    const elapsedYears = a.history.length > 1 ? (a.history.length - 1) / 32.0 / 12.0 : 0;

    lines.push(`【城市名称】${cityName}`);
    lines.push(`【统计时点】第${cur.gameYear}年第${cur.gameMonth}月`);
    if (elapsedYears > 0) lines.push(`【建市年限】逾${elapsedYears.toFixed(1)}年`);
    lines.push(`【发展阶段】${Desc.stage(cur.population)}`);
    lines.push('');

    lines.push('【总体概况】');
    ap('常住人口', o.population, v => `${n0(v)}人`);
    ap('财政余额', o.money, v => yuan(v));
    lines.push(`  居民幸福度：${n1(o.happiness)}%（${Desc.happinessLevel(o.happiness)}）`);
    lines.push(`  居民健康度：${n1(o.health)}%（${Desc.health(o.health)}）`);
    lines.push(`  生活质量综合指数：${n1(s.qualityOfLifeIndex)}/100`);
    lines.push(`  发展势头——人口：${Desc.translateMomentum ? Desc.translateMomentum(tr.popMomentum) : tr.popMomentum}，经济：${tr.economyMomentum}，社会：${tr.socialMomentum}`);
    lines.push('');

    lines.push('【人口数据】');
    lines.push(`  总人口：${n0(d.population)}人（含迁入${n0(d.populationWithMoveIn)}人）`);
    lines.push(`  人口增长率：${d.growthRate >= 0 ? '+' : ''}${n1(d.growthRate)}%`);
    ap('迁入人口', d.citizensMovedIn, v => `${n0(v)}人`);
    ap('迁出人口', d.citizensMovedAway, v => `${n0(v)}人`);
    ap('净迁移', d.netMigration, v => `${v >= 0 ? '+' : ''}${n0(v)}人`);
    ap('出生率/死亡率/自然增长', d.birthRate > 0 ? 1 : 0, () => `${n1(d.birthRate)}‰/${n1(d.deathRate)}‰/${n1(d.naturalGrowth)}‰`);
    ap('成年人口', d.adultsCount, v => `${n0(v)}人（占比${n1(d.adultsRatio)}%）`);
    lines.push('');

    lines.push('【财政数据】');
    lines.push(`  月收入：${yuan(e.income)}`);
    if (f.expenseAvailable) {
      lines.push(`  月支出：${yuan(e.expense)}`);
      lines.push(`  净收入：${yuan(e.netIncome)}（利润率${e.profitMargin >= 0 ? '+' : ''}${n1(e.profitMargin)}%）`);
    } else {
      lines.push(`  财政支出：本统计期尚未记录（数据采集未覆盖支出项，无法计算净收入与收支比）`);
    }
    ap('贸易额', e.trade, v => yuan(v));
    ap('发展点数', e.devTreePoints, v => `${n0(v)}`);
    lines.push(`  人均收入：${yuan(e.perCapitaIncome)}  人均支出：${yuan(e.perCapitaExpense)}`);
    ap('人均税赋', e.perCapitaTax, v => yuan(v));
    if (f.expenseAvailable) {
      lines.push(`  收支比：${n1(f.revenueExpenseRatio)}（${Desc.budget(f.revenueExpenseRatio)}）`);
    } else {
      lines.push(`  收支比：支出数据缺失，暂不计算`);
    }
    lines.push(`  税收依赖度：${n1(f.taxToIncomeRatio)}%  贸易依赖度：${n1(f.tradeToIncomeRatio)}%`);
    lines.push('');

    lines.push('【税收结构】');
    if (isAvail(e.totalTax)) {
      lines.push(`  税收总收入：${yuan(e.totalTax)}`);
      lines.push(`  住宅税：${yuan(e.residentialTax)}（${n1(e.residentialTaxPct)}%）`);
      lines.push(`  商业税：${yuan(e.commercialTax)}（${n1(e.commercialTaxPct)}%）`);
      lines.push(`  工业税：${yuan(e.industrialTax)}（${n1(e.industrialTaxPct)}%）`);
      lines.push(`  办公税：${yuan(e.officeTax)}（${n1(e.officeTaxPct)}%）`);
    } else lines.push('  税收体系尚未建立，暂无税收数据');
    lines.push('');

    lines.push('【产业结构】');
    if (isAvail(sec.totalWealth)) {
      const sLine = (name, x) => isAvail(x.wealth) || isAvail(x.count) ?
        `  ${name}：财富${yuan(x.wealth)}，企业${n0(x.count)}家，从业${n0(x.workers)}/${n0(x.maxWorkers)}人（填充率${n1(x.fillRate)}%），占比${n1(x.pct)}%` : `  ${name}：尚未发展`;
      lines.push(sLine('服务业', sec.service));
      lines.push(sLine('加工业', sec.processing));
      lines.push(sLine('办公业', sec.office));
    } else lines.push('  产业经济尚未形成规模，企业数据暂无');
    lines.push('');

    lines.push('【就业数据】');
    lines.push(`  从业人员：${n0(emp.workerCount)}人`);
    if (isAvail(emp.unemployed) || emp.workerCount > 0)
      lines.push(`  失业人口：${n0(emp.unemployed)}人（失业率${n1(emp.unemploymentRate)}%——${Desc.unemployment(emp.unemploymentRate)}）`);
    lines.push(`  劳动参与率：${n1(emp.workforceParticipation)}%`);
    if (isAvail(emp.cityServiceWorkers))
      lines.push(`  公务人员：${n0(emp.cityServiceWorkers)}/${n0(emp.cityServiceMaxWorkers)}人（填充率${n1(emp.cityServiceFillRate)}%）`);
    else lines.push(`  公务服务体系尚未建立`);
    if (isAvail(emp.seniorWorkerDemand)) lines.push(`  高级技工需求率：${n1(emp.seniorWorkerDemand)}%`);
    lines.push('');

    lines.push('【交通数据】');
    if (isAvail(t.totalPassengers)) {
      lines.push(`  客运总量：${n0(t.totalPassengers)}人次（公共交通占比${n1(t.publicTransitShare)}%——${Desc.traffic(t.publicTransitShare)}）`);
      const tLine = (name, m) => isAvail(m.passengers) ? `  ${name}：${n0(m.passengers)}人次（占比${n1(m.share)}%）` : null;
      [['公交', t.bus], ['地铁', t.subway], ['有轨电车', t.tram], ['火车', t.train], ['出租车', t.taxi], ['航空', t.airplane], ['水运', t.ship]]
        .forEach(([nm, m]) => { const l = tLine(nm, m); if (l) lines.push(l); });
    } else lines.push(`  公共交通体系尚未建立，暂无客运数据`);
    if (isAvail(t.totalCargo)) lines.push(`  货运总量：${n0(t.totalCargo)}吨（卡车${n0(t.cargoTruck)} 铁路${n0(t.cargoTrain)} 水运${n0(t.cargoShip)} 空运${n0(t.cargoAirplane)}）`);
    lines.push('');

    lines.push('【社会民生】');
    lines.push(`  幸福指数：${n1(s.wellbeing)}%（${Desc.happiness(s.wellbeing)}）`);
    lines.push(`  健康指数：${n1(s.health)}%（${Desc.health(s.health)}）`);
    if (isAvail(s.educationCount)) lines.push(`  教育机构：${n0(s.educationCount)}所（${Desc.education(s.educationRate)}）`);
    else lines.push(`  教育体系尚未建立`);
    lines.push(`  犯罪率：${n1(s.crimeRate)}%（${Desc.crime(s.crimeRate)}）`);
    if (isAvail(s.crimeCount)) lines.push(`  犯罪事件：${n0(s.crimeCount)}起  逃犯逮捕：${n0(s.escapedArrestCount)}起`);
    if (isAvail(s.homelessCount)) lines.push(`  无家可归者：${n0(s.homelessCount)}人（千人比${n1(s.homelessPerCapita)}‰）`);
    if (s.collectedMail > 0 || s.deliveredMail > 0) lines.push(`  邮件收集：${n0(s.collectedMail)}件  投递：${n0(s.deliveredMail)}件`);
    lines.push(`  生活质量综合指数：${n1(s.qualityOfLifeIndex)}/100`);
    lines.push('');

    lines.push('【家庭数据】');
    if (isAvail(h.householdCount)) {
      lines.push(`  家庭总数：${n0(h.householdCount)}户`);
      lines.push(`  家庭总财富：${yuan(h.householdWealth)}`);
      lines.push(`  户均财富：${yuan(h.avgWealthPerHousehold)}`);
      lines.push(`  户均人口：${n1(h.avgPersonsPerHousehold)}人/户`);
    } else lines.push(`  家庭数据暂无，居民尚未定居`);
    lines.push('');

    lines.push('【趋势变化】');
    const trLine = (name, v) => isAvail(Math.abs(v)) ? `  ${name}：${v >= 0 ? '+' : ''}${n1(v)}% ${v > 0 ? '↑' : v < 0 ? '↓' : '→'}` : null;
    [['人口增长率', tr.popGrowthRate], ['收入增长率', tr.incomeGrowthRate], ['幸福度变化', tr.happinessTrend], ['健康度变化', tr.healthTrend], ['犯罪率变化', tr.crimeTrend]]
      .forEach(([nm, v]) => { const l = trLine(nm, v); if (l) lines.push(l); });
    if (isAvail(Math.abs(tr.tourismTrend))) lines.push(`  旅游趋势：${tr.tourismTrend >= 0 ? '+' : ''}${n1(tr.tourismTrend)}%`);
    lines.push('');

    if (a.alerts.length) {
      lines.push('【风险告警】');
      a.alerts.forEach(al => lines.push(`  [${al.level === 'danger' ? '严重' : '警告'}] ${al.category}：${al.message}`));
      lines.push('');
    }

    if (a.scores.length) {
      lines.push('【综合评分】');
      a.scores.forEach(sc => lines.push(`  ${sc.category}/${sc.name}：${n1(sc.score)}分（${sc.grade}）——${sc.description}`));
      const avg = a.scores.reduce((s, x) => s + x.score, 0) / a.scores.length;
      lines.push(`  综合评分：${n1(avg)}分`);
      lines.push('');
    }

    if (a.history.length >= 3) {
      lines.push('【近期趋势数据】（最近12个数据点）');
      a.history.slice(-12).forEach(snap => lines.push(`  Y${snap.gameYear}M${snap.gameMonth}: 人口${n0(snap.population)} 幸福度${n1(snap.wellbeing)}% 健康度${n1(snap.health)}% 收入${yuan(snap.income)} 支出${yuan(snap.expense)}`));
    }

    return lines.join('\n');
  }

  // ───────────────────────── 系统提示词与章节提示词 ─────────────────────────
  const SYSTEM_PROMPT = `你是一位资深的城市政府办公厅主任，负责为城市撰写《政府工作报告》。你的写作必须严格遵循以下规范：

## 角色定位
你是市长授权的报告撰写官，代表市政府向全体市民汇报工作。

## 语言风格
- 使用正式、庄重、权威的公文语言
- 采用中国政府工作报告的标准格式和语气
- 大量使用以下句式：
  - "一是……二是……三是……" 来列举要点
  - "同比增长/下降X%" 来陈述数据变化
  - "稳步推进" "持续改善" "明显提升" "显著增强" 来描述趋势
  - "扎实推进" "深入实施" "全面落实" 来表述工作
  - "面对……挑战" "在……形势下" 来设置语境
  - "必须清醒看到" "存在……不足" 来指出问题
  - "要……要……要……" 来提出要求
- 用词精准、数据说话、避免空洞

## 报告结构要求
1. 开场白："各位市民代表：现在，我代表市人民政府，向大会报告工作，请予审议，并请各位列席人员提出意见。"
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
- 章节标题用"一、""二、"等中文序号`;

  const PROMPTS = {
    opening: (ctx, city) => `根据以下${city}城市数据，撰写政府工作报告的开场白和总体回顾（600字左右）：

## 数据
${ctx}

## 要求
1. 以"各位市民代表：现在，我代表市人民政府，向大会报告工作，请予审议，并请各位列席人员提出意见。"开头
2. 用一段话概括本报告期城市运行总体态势
3. 突出2-3个最亮眼的成绩（用具体数据支撑）
4. 指出1-2个需要关注的问题
5. 对于尚未发展的服务领域，可简要提及"正积极谋划"，不编造数据
6. 使用报告体语言，严禁出现"根据数据""数据显示"等元描述
7. 直接以报告正文形式输出，不加任何前缀说明`,

    demographics: (ctx) => `撰写政府工作报告中"人口发展与民生保障"章节（500字左右）：

## 数据
${ctx}

## 要求
1. 报告人口总量及增长情况，分析迁入迁出动态
2. 汇报出生率、死亡率、自然增长率
3. 分析成年人口结构和劳动力供给
4. 评述市民幸福感和健康水平的变化趋势
5. 如某些数据为0，说明该领域尚处于起步阶段
6. 格式：以"一、人口持续增长，民生福祉不断改善"作为标题
7. 正文分2-3个自然段
8. 直接输出报告正文`,

    economy: (ctx) => `撰写政府工作报告中"经济运行稳中向好"章节（600字左右）：

## 数据
${ctx}

## 要求
1. 汇报财政收支总体情况，分析盈余或赤字
2. 分析税收结构（如税收为0说明财税体系尚未成型）
3. 汇报贸易发展情况和城市发展能力
4. 对比人均收入和人均支出，分析居民经济状况
5. 结合财政健康度描述，给出总体经济评价
6. 格式：以"二、经济运行稳中提质，财政状况持续改善"为标题
7. 直接输出报告正文`,

    industry: (ctx) => `撰写政府工作报告中"产业结构优化升级"章节（500字左右）：

## 数据
${ctx}

## 要求
1. 汇报三大产业（服务业、加工业、办公业）发展情况
2. 分析各产业财富占比、企业数量、就业填充率
3. 对于尚未发展的产业，说明"处于谋划阶段"
4. 指出产业结构特点和优化方向
5. 格式：以"三、产业结构优化升级，发展动能持续增强"为标题
6. 直接输出报告正文`,

    employment: (ctx) => `撰写政府工作报告中"就业与社会保障"章节（400字左右）：

## 数据
${ctx}

## 要求
1. 汇报就业总体形势，分析失业率水平
2. 结合失业率评估描述给出分析
3. 如公务服务体系尚未建立则简要提及
4. 分析高级技工需求和人才结构
5. 格式：以"四、就业形势总体稳定，社会保障体系不断完善"为标题
6. 直接输出报告正文`,

    transport: (ctx) => `撰写政府工作报告中"交通基础设施建设"章节（500字左右）：

## 数据
${ctx}

## 要求
1. 汇报公共交通体系整体运行情况
2. 列举已有交通方式客运量及占比，未建设的交通方式不必列出
3. 如客运数据为0，说明交通体系处于规划阶段
4. 分析货运物流能力
5. 格式：以"五、交通基础设施建设扎实推进，出行条件持续改善"为标题
6. 直接输出报告正文`,

    social: (ctx) => `撰写政府工作报告中"社会民生事业"章节（500字左右）：

## 数据
${ctx}

## 要求
1. 结合幸福度描述和健康描述分析居民生活质量
2. 如教育体系尚未建立则如实说明
3. 分析治安状况，结合犯罪率描述
4. 报告医疗卫生水平
5. 如有无家可归者需提及其影响
6. 格式：以"六、社会民生事业全面发展，市民获得感持续增强"为标题
7. 对问题坦诚面对
8. 直接输出报告正文`,

    fiscal: (ctx) => `撰写政府工作报告中"财政收支与家庭生活"章节（400字左右）：

## 数据
${ctx}

## 要求
1. 结合财政健康度描述分析财政状况
2. 汇报税收依赖度和贸易依赖度
3. 报告家庭收入与消费水平
4. 如家庭数据为0说明居民尚未定居
5. 格式：以"七、财政运行稳健，居民生活水平稳步提高"为标题
6. 直接输出报告正文`,

    challenges: (ctx) => `撰写政府工作报告中"面临的问题与挑战"章节（350字左右）：

## 数据
${ctx}

## 要求
1. 冷静客观指出城市发展中的短板
2. 必须有具体数据支撑（如某指标下降X%）
3. 语气："必须清醒看到……" "仍然存在……" "有待进一步加强"
4. 对于尚未发展的领域，表述为"尚有很大发展空间"
5. 格式：以"我们也清醒认识到，城市发展中还面临不少困难和挑战"开头
6. 列举2-3个问题
7. 直接输出报告正文`,

    outlook: (ctx) => `撰写政府工作报告中"下阶段工作部署"章节（500字左右）：

## 数据
${ctx}

## 要求
1. 提出下一阶段总体目标
2. 分3-5个方面部署重点工作
3. 每项部署包括：目标方向 + 具体措施
4. 对于目前尚未发展的领域，可提出"启动XX体系建设"
5. 语言要有力、有方向感
6. 使用"要……""必须……""着力……"等句式
7. 格式：以"八、凝心聚力，奋力开创城市发展新局面"为标题
8. 最后以鼓舞人心的结语收尾：
   "各位代表！使命重在担当，实干铸就辉煌。让我们更加紧密地团结起来，锐意进取、攻坚克难，为把我市建设成为繁荣、宜居、和谐的现代化都市而不懈奋斗！"
9. 直接输出报告正文`,
  };

  // 章节定义（与桌面端一致）
  const CHAPTER_DEFS = [
    { id: 'opening', title: '开场白与建市以来总体回顾', prompt: PROMPTS.opening },
    { id: 'demographics', title: '人口发展与城镇化建设', prompt: PROMPTS.demographics },
    { id: 'economy', title: '经济发展与财政运行', prompt: PROMPTS.economy },
    { id: 'industry', title: '产业体系构建与优化升级', prompt: PROMPTS.industry },
    { id: 'employment', title: '就业促进与社会保障', prompt: PROMPTS.employment },
    { id: 'transport', title: '基础设施建设与交通发展', prompt: PROMPTS.transport },
    { id: 'social', title: '民生福祉与社会事业', prompt: PROMPTS.social },
    { id: 'fiscal', title: '财政管理与家庭经济', prompt: PROMPTS.fiscal },
    { id: 'challenges', title: '面临的问题与挑战', prompt: PROMPTS.challenges },
    { id: 'outlook', title: '下一阶段发展目标与工作部署', prompt: PROMPTS.outlook },
  ];

  const Analysis = {
    parseFiles, analyze, buildDataContext, normalize, buildFromFullHistory,
    SYSTEM_PROMPT, PROMPTS, CHAPTER_DEFS, Desc,
    n0, n1, pct, yuan, isAvail,
  };

  if (typeof module !== 'undefined' && module.exports) module.exports = Analysis;
  if (root) root.Analysis = Analysis;
})(typeof window !== 'undefined' ? window : (typeof globalThis !== 'undefined' ? globalThis : this));
