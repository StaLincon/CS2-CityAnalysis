/*
 * sample-data.js — 生成一份逼真的演示数据（无需上传文件即可体验）
 * 返回 [{name, text}]，可直接喂给 Analysis.parseFiles
 */
(function (root) {
  'use strict';

  function mulberry32(a) {
    return function () {
      a |= 0; a = (a + 0x6D2B79F5) | 0;
      let t = Math.imul(a ^ (a >>> 15), 1 | a);
      t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
  }

  function getDemoFiles() {
    const rnd = mulberry32(20260519);
    const N = 48;
    const history = [];
    let pop = 1800;
    for (let i = 0; i < N; i++) {
      const year = Math.floor(i / 12) + 2026;
      const month = (i % 12) + 1;
      const growth = 1 + (0.04 + rnd() * 0.05);
      pop = Math.round(pop * growth);
      const wellbeingPct = Math.min(82, 52 + i * 0.5 + rnd() * 4);
      const healthPct = Math.min(85, 55 + i * 0.4 + rnd() * 4);
      const income = Math.round(pop * (120 + rnd() * 30));
      const expense = Math.round(income * (0.62 + rnd() * 0.18));
      const bus = Math.round(pop * (0.8 + rnd() * 0.3));
      const subway = Math.round(pop * (0.5 + rnd() * 0.2) * (i > 8 ? 1 : 0));
      const tram = Math.round(pop * (0.3 + rnd() * 0.1) * (i > 14 ? 1 : 0));
      const train = Math.round(pop * (0.4 + rnd() * 0.2) * (i > 10 ? 1 : 0));
      const taxi = Math.round(pop * (0.25 + rnd() * 0.1));
      const airplane = Math.round(pop * 0.05 * (i > 20 ? 1 : 0));
      const ship = Math.round(pop * 0.08 * (i > 18 ? 1 : 0));
      const sWealth = Math.round(pop * (200 + rnd() * 80) * (i > 6 ? 1 : 0));
      const pWealth = Math.round(pop * (150 + rnd() * 60) * (i > 12 ? 1 : 0));
      const oWealth = Math.round(pop * (180 + rnd() * 70) * (i > 10 ? 1 : 0));
      history.push({
        realTime: new Date(Date.UTC(2026, month - 1, 15, 12, 0, 0) + i * 86400000).toISOString(),
        gameTick: 639000000000000000 + i * 1000000000,
        gameYear: year, gameMonth: month, gameDay: 15,
        population: pop,
        populationWithMoveIn: Math.round(pop * 1.18),
        citizensMovedIn: Math.round(pop * 0.04),
        citizensMovedAway: Math.round(pop * 0.012),
        birthRate: Math.round(pop * 0.0025),
        deathRate: Math.round(pop * 0.0008),
        money: 2000000000 + i * 15000000,
        income, expense, trade: Math.round(income * 0.25),
        averageHappiness: Math.round(wellbeingPct * pop),
        averageHealth: Math.round(healthPct * pop),
        homelessCount: Math.round(pop * (0.005 + rnd() * 0.01)),
        workerCount: Math.round(pop * 0.52),
        unemployed: Math.round(pop * 0.03),
        crimeRate: Math.max(1, Math.round(12 - i * 0.12 + rnd() * 3)),
        crimeCount: Math.round(pop * 0.002),
        passengerCountBus: bus, passengerCountSubway: subway, passengerCountTram: tram,
        passengerCountTrain: train, passengerCountTaxi: taxi, passengerCountAirplane: airplane, passengerCountShip: ship,
        cargoCountTruck: Math.round(pop * 0.4), cargoCountTrain: Math.round(pop * 0.2 * (i > 10 ? 1 : 0)),
        cargoCountShip: Math.round(pop * 0.1 * (i > 18 ? 1 : 0)), cargoCountAirplane: Math.round(pop * 0.02 * (i > 20 ? 1 : 0)),
        residentialTaxableIncome: Math.round(income * 0.3), commercialTaxableIncome: Math.round(income * 0.3),
        industrialTaxableIncome: Math.round(income * 0.2), officeTaxableIncome: Math.round(income * 0.2),
        educationCount: Math.round(pop * 0.12),
        adultsCount: Math.round(pop * 0.7),
        serviceWealth: sWealth, serviceCount: Math.round(pop * 0.04), serviceWorkers: Math.round(pop * 0.02), serviceMaxWorkers: Math.round(pop * 0.025),
        processingWealth: pWealth, processingCount: Math.round(pop * 0.02), processingWorkers: Math.round(pop * 0.012), processingMaxWorkers: Math.round(pop * 0.015),
        officeWealth: oWealth, officeCount: Math.round(pop * 0.025), officeWorkers: Math.round(pop * 0.015), officeMaxWorkers: Math.round(pop * 0.02),
        cityServiceWorkers: Math.round(pop * 0.01), cityServiceMaxWorkers: Math.round(pop * 0.013),
        householdWealth: Math.round(pop * 5000), householdCount: Math.round(pop * 0.4),
        seniorWorkerInDemandPercentage: Math.round(40 + rnd() * 30),
        escapedArrestCount: Math.round(pop * 0.0005),
        currentTourists: Math.round(pop * 0.1), attractiveness: Math.round(50 + i),
        devTreePoints: i * 3,
      });
    }

    // 当前快照（完整字段，最后一个月）
    const cur = JSON.parse(JSON.stringify(history[history.length - 1]));
    cur.name = '当前快照';

    return [
      { name: 'history.json', text: JSON.stringify(history, null, 0) },
      { name: 'current_snapshot.json', text: JSON.stringify(cur, null, 0) },
    ];
  }

  const Sample = { getDemoFiles };
  if (typeof module !== 'undefined' && module.exports) module.exports = Sample;
  if (root) root.Sample = Sample;
})(typeof window !== 'undefined' ? window : (typeof globalThis !== 'undefined' ? globalThis : this));
