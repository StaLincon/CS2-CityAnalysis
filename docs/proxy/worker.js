/**
 * 私有 CORS 反向代理（Cloudflare Worker）
 * -------------------------------------------------------------
 * 用途：让纯前端页面（GitHub Pages / file:// / localhost）能够调用
 *       DeepSeek / OpenAI 等“不允许浏览器直连”的接口。
 *
 * 隐私说明（重要）：
 *   - 本 Worker 不存储任何 API Key。Key 仍由浏览器在请求头 Authorization 中携带，
 *     原样透传给上游接口，符合“密钥仅存在于你浏览器内存”的承诺。
 *   - 这是“你自建”的端点，不是公共代理，因此不会把 Key 泄露给第三方。
 *
 * 部署（免费）：
 *   1. 打开 https://dash.cloudflare.com/  → Workers & Pages → 创建 Worker
 *   2. 把本文件内容粘贴为 Worker 代码并部署，得到如 https://ds-proxy.<你的子域>.workers.dev
 *   3. 在本应用的「CORS 代理」中填写： https://ds-proxy.<你的子域>.workers.dev/?url=
 *      （或者干脆把「API 地址」直接改成 https://ds-proxy.<你的子域>.workers.dev/v1/chat/completions 并留空代理）
 *
 * 两种用法本 Worker 都支持：
 *   A) 代理模式： https://你的worker.workers.dev/?url=<编码后的目标URL>   -> 转发到该 URL
 *   B) 直连模式： https://你的worker.workers.dev/v1/chat/completions       -> 转发到 api.deepseek.com/v1/chat/completions
 */

export default {
  async fetch(request, env) {
    const url = new URL(request.url);

    // 预检请求（浏览器在带 Authorization 自定义头时会先发 OPTIONS）
    if (request.method === 'OPTIONS') {
      return new Response(null, {
        status: 204,
        headers: {
          'Access-Control-Allow-Origin': '*',
          'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
          'Access-Control-Allow-Headers': 'Content-Type, Authorization',
          'Access-Control-Max-Age': '86400',
        },
      });
    }

    // 解析目标上游地址
    const upstream = url.searchParams.get('url'); // 代理模式 ?url=https://...
    const target = upstream
      ? upstream
      : 'https://api.deepseek.com' + url.pathname + url.search; // 直连模式默认转发到 DeepSeek

    // 透传请求头（含 Authorization: Bearer sk-...），剔除 host / content-length 让运行时重算
    const headers = new Headers(request.headers);
    headers.delete('host');
    headers.delete('content-length');

    const init = {
      method: request.method,
      headers,
      body: request.body,
      redirect: 'follow',
    };

    let resp;
    try {
      resp = await fetch(target, init);
    } catch (e) {
      return new Response('上游请求失败：' + (e && e.message ? e.message : e), {
        status: 502,
        headers: { 'Access-Control-Allow-Origin': '*' },
      });
    }

    // 透传上游响应，并补上 CORS 头让浏览器接受
    const outHeaders = new Headers(resp.headers);
    outHeaders.set('Access-Control-Allow-Origin', '*');
    outHeaders.set('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
    outHeaders.set('Access-Control-Allow-Headers', 'Content-Type, Authorization');

    return new Response(resp.body, { status: resp.status, headers: outHeaders });
  },
};
