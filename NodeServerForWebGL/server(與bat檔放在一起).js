// Unity WebGL 專用簡易靜態伺服器
// 功能：自動辨識 .br / .gz 壓縮檔，並補上正確的 Content-Encoding 標頭
// 用法：node server.js [port]

const http = require('http');
const fs = require('fs');
const path = require('path');
const url = require('url');

const PORT = process.argv[2] || 8000;
const ROOT = process.cwd(); // 跟 index.html 同目錄執行

// 依「真正內容」的副檔名判斷 MIME type
// 例如 CityFlyThrough.data.br -> 拿掉 .br 後看 .data -> octet-stream
const MIME_TYPES = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'application/javascript',
  '.mjs': 'application/javascript',
  '.wasm': 'application/wasm',
  '.data': 'application/octet-stream',
  '.json': 'application/json',
  '.css': 'text/css',
  '.symbols.json': 'application/octet-stream',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.svg': 'image/svg+xml',
  '.ico': 'image/x-icon',
};

function getContentType(filePath) {
  // 先把壓縮副檔名 (.br / .gz) 拿掉，再判斷真正的檔案類型
  let base = filePath;
  if (base.endsWith('.br') || base.endsWith('.gz')) {
    base = base.slice(0, base.lastIndexOf('.'));
  }
  const ext = path.extname(base);
  return MIME_TYPES[ext] || 'application/octet-stream';
}

const server = http.createServer((req, res) => {
  let reqPath = decodeURIComponent(url.parse(req.url).pathname);
  if (reqPath === '/') reqPath = '/index.html';

  const filePath = path.join(ROOT, reqPath);

  // 避免路徑跳出根目錄
  if (!filePath.startsWith(ROOT)) {
    res.writeHead(403);
    res.end('Forbidden');
    return;
  }

  fs.stat(filePath, (err, stats) => {
    if (err || !stats.isFile()) {
      res.writeHead(404);
      res.end('Not Found: ' + reqPath);
      return;
    }

    const headers = {
      'Content-Type': getContentType(filePath),
      'Content-Length': stats.size,
      'Access-Control-Allow-Origin': '*',
      'Cross-Origin-Embedder-Policy': 'require-corp',
      'Cross-Origin-Opener-Policy': 'same-origin',
    };

    // 關鍵：依副檔名補上正確的壓縮編碼標頭
    if (filePath.endsWith('.br')) {
      headers['Content-Encoding'] = 'br';
    } else if (filePath.endsWith('.gz')) {
      headers['Content-Encoding'] = 'gzip';
    }

    res.writeHead(200, headers);
    fs.createReadStream(filePath).pipe(res);
  });
});

server.listen(PORT, () => {
  console.log(`Unity WebGL 伺服器已啟動: http://localhost:${PORT}/index.html`);
});
