#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
本地 CORS 反向代理（仅用于本机测试，不要部署到公网）

作用：让浏览器页面（例如 http://localhost:8765）能够调用 DeepSeek 等“不允许浏览器直连”的接口。
隐私：本脚本不存储任何 API Key，密钥仍只存在于你的浏览器，脚本仅做转发并补上 CORS 响应头。

用法：
  1) 启动：  python local_proxy.py
  2) 在本应用中：把「API 地址」改为  http://localhost:8787/v1/chat/completions ，「CORS 代理」留空
  3) 点击「测试连接」即可验证。

注意：仅监听 127.0.0.1，外部无法访问。验证无误后，生产环境请用 analysis/docs/proxy/worker.js 部署到 Cloudflare Worker。
"""
import http.server
import ssl
import json
import urllib.request
import urllib.error

UPSTREAM = "https://api.deepseek.com"


class Handler(http.server.BaseHTTPRequestHandler):
    def _cors(self):
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "Content-Type, Authorization")

    def do_OPTIONS(self):
        self.send_response(204)
        self._cors()
        self.end_headers()

    def do_POST(self):
        length = int(self.headers.get("Content-Length", 0) or 0)
        body = self.rfile.read(length) if length else b""
        target = UPSTREAM + self.path
        req = urllib.request.Request(target, data=body, method="POST")
        for h in ("Content-Type", "Authorization"):
            if h in self.headers:
                req.add_header(h, self.headers[h])
        ctx = ssl.create_default_context()
        try:
            with urllib.request.urlopen(req, context=ctx, timeout=180) as r:
                data = r.read()
                self.send_response(r.status)
                self._cors()
                self.send_header("Content-Type", r.headers.get("Content-Type", "application/json"))
                self.send_header("Content-Length", str(len(data)))
                self.end_headers()
                self.wfile.write(data)
        except urllib.error.HTTPError as e:
            data = e.read()
            self.send_response(e.code)
            self._cors()
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(data)))
            self.end_headers()
            self.wfile.write(data)
        except Exception as e:  # noqa: BLE001
            msg = json.dumps({"error": {"message": str(e)}}).encode("utf-8")
            self.send_response(502)
            self._cors()
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(msg)))
            self.end_headers()
            self.wfile.write(msg)

    def log_message(self, *args):
        pass


if __name__ == "__main__":
    print("本地 CORS 代理已启动: http://localhost:8787  (Ctrl+C 停止)")
    http.server.HTTPServer(("127.0.0.1", 8787), Handler).serve_forever()
