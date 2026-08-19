/*
 * llm.js — 浏览器内调用 LLM（OpenAI Chat Completions 兼容协议）
 * 支持 DeepSeek / OpenAI / SiliconFlow / Ollama / Azure / 自定义 endpoint
 * 纯前端直连；如遇 CORS 拦截，可填入 CORS 代理。
 */
(function (root) {
  'use strict';

  // 将用户配置的代理地址规范化为最终请求 URL
  function resolveUrl(apiUrl, proxy) {
    if (!proxy) return apiUrl;
    if (proxy.includes('{url}')) return proxy.replace('{url}', encodeURIComponent(apiUrl));
    if (proxy.trim().endsWith('=') || proxy.trim().endsWith('?')) return proxy + encodeURIComponent(apiUrl);
    return proxy.replace(/\/+$/, '') + '/' + apiUrl.replace(/^https?:\/\//, '');
  }

  async function callLLM(config, userPrompt, opts) {
    opts = opts || {};
    const { apiUrl, apiKey, model, proxy } = config;
    if (!apiUrl || !apiKey || !model) throw new Error('请完整填写 API 地址、API Key 与模型名');

    const url = resolveUrl(apiUrl, proxy);
    const body = {
      model,
      messages: [
        { role: 'system', content: config.systemPrompt || (root.Analysis && root.Analysis.SYSTEM_PROMPT) || '' },
        { role: 'user', content: userPrompt },
      ],
      temperature: 0.7,
      max_tokens: 4096,
    };

    const controller = new AbortController();
    if (opts.signal) opts.signal.addEventListener('abort', () => controller.abort());
    const timer = setTimeout(() => controller.abort(), 180000);

    try {
      let resp;
      try {
        resp = await fetch(url, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${apiKey}` },
          body: JSON.stringify(body),
          signal: controller.signal,
        });
      } catch (netErr) {
        // fetch 失败（TypeError: Failed to fetch）几乎总是 CORS 拦截 / 网络不可达 / 混合内容
        const msg = (netErr && netErr.message) || String(netErr);
        if (/Failed to fetch|NetworkError|load failed|TypeError|blocked by CORS|CORS/i.test(msg)) {
          throw new Error(
            '网络/CORS 被浏览器拦截：DeepSeek、OpenAI 等接口默认不允许浏览器直连（同源策略）。' +
            '请在「CORS 代理」填入你自己部署的代理地址（推荐 Cloudflare Worker）后再试。' +
            '公共代理会泄露你的 API Key，不建议使用。'
          );
        }
        throw new Error('网络请求失败：' + msg);
      }
      if (!resp.ok) {
        const txt = await resp.text().catch(() => '');
        let detail = txt.slice(0, 300);
        try { const j = JSON.parse(txt); if (j && j.error && j.error.message) detail = j.error.message; } catch (e) {}
        throw new Error(`HTTP ${resp.status} — ${detail}`);
      }
      const data = await resp.json();
      const content = data && data.choices && data.choices[0] && data.choices[0].message && data.choices[0].message.content;
      if (!content) throw new Error('AI 未返回有效内容');
      return content;
    } finally {
      clearTimeout(timer);
    }
  }

  // 测试连接（轻量请求）
  async function testConnection(config) {
    const t0 = performance.now();
    try {
      const r = await callLLM(config, '你好，请只回复“OK”两个字。', { max_tokens: 5 });
      return { success: true, latency: Math.round(performance.now() - t0), model: config.model };
    } catch (e) {
      return { success: false, latency: Math.round(performance.now() - t0), error: e.message };
    }
  }

  const LLM = { callLLM, testConnection, resolveUrl };
  if (typeof module !== 'undefined' && module.exports) module.exports = LLM;
  if (root) root.LLM = LLM;
})(typeof window !== 'undefined' ? window : (typeof globalThis !== 'undefined' ? globalThis : this));
