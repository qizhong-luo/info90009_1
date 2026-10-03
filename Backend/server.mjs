import http from 'node:http';
import { timingSafeEqual } from 'node:crypto';
import { pathToFileURL } from 'node:url';
import { generateReply, RequestError } from './companion.mjs';

function sameToken(a, b) {
  const first = Buffer.from(a ?? ''), second = Buffer.from(b ?? '');
  return first.length === second.length && timingSafeEqual(first, second);
}
export function createServer({ apiKey = process.env.OPENAI_API_KEY, model = process.env.OPENAI_MODEL,
  token = process.env.SLEEPET_BACKEND_TOKEN, fetchImpl = fetch } = {}) {
  let active = 0;
  const rate = new Map();
  return http.createServer(async (req, res) => {
    const send = (status, body) => {
      if (res.destroyed) return;
      res.writeHead(status, { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' });
      res.end(JSON.stringify(body));
    };
    // No CORS: this local development service is used by native Unity, not arbitrary web pages.
    if (req.headers.origin) return send(403, { error: 'Browser origins are not enabled.' });
    if (req.method === 'GET' && req.url === '/health') return send(200, { ready: Boolean(apiKey && model), provider: 'OpenAI' });
    if (req.method !== 'POST' || req.url !== '/api/chat') return send(404, { error: 'Not found.' });
    const loopback = ['127.0.0.1', '::1', '::ffff:127.0.0.1'].includes(req.socket.remoteAddress);
    if ((token && !sameToken(req.headers.authorization, `Bearer ${token}`)) || (!token && !loopback))
      return send(401, { error: 'Backend session token required.' });
    if (!req.headers['content-type']?.startsWith('application/json')) return send(415, { error: 'JSON required.' });
    const now = Date.now();
    for (const [key, entry] of rate) if (now >= entry.until) rate.delete(key);
    const key = req.socket.remoteAddress;
    const entry = rate.get(key) ?? { count: 0, until: now + 60000 };
    rate.set(key, entry);
    if (++entry.count > 20 || active >= 4) return send(429, { error: 'Please wait before sending another message.' });
    active++;
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 35000);
    const onClose = () => { if (!res.writableEnded) controller.abort(); };
    res.on('close', onClose);
    try {
      let bytes = 0; const chunks = [];
      for await (const chunk of req) {
        bytes += chunk.length;
        if (bytes > 65536) throw new RequestError('Request too large.', 413);
        chunks.push(chunk);
      }
      let body;
      try { body = JSON.parse(Buffer.concat(chunks).toString('utf8')); }
      catch { throw new RequestError('Invalid JSON.'); }
      const result = await generateReply(body, { apiKey, model, fetchImpl, signal: controller.signal });
      send(200, result);
    } catch (error) {
      send(controller.signal.aborted ? 504 : error instanceof RequestError ? error.status : 502,
        { error: controller.signal.aborted ? 'AI request timed out.' : error instanceof RequestError ? error.message : 'AI request failed.' });
    } finally { clearTimeout(timeout); res.off('close', onClose); active--; }
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const host = process.env.SLEEPET_HOST || '127.0.0.1';
  if (!['127.0.0.1', '::1'].includes(host) && !process.env.SLEEPET_BACKEND_TOKEN)
    throw new Error('Non-loopback binding requires SLEEPET_BACKEND_TOKEN and a TLS reverse proxy.');
  const port = Number(process.env.PORT || 8787);
  const server = createServer();
  server.requestTimeout = 15000;
  server.listen(port, host, () => console.log(`Sleepet AI backend listening on ${host}:${port}. API configured: ${Boolean(process.env.OPENAI_API_KEY && process.env.OPENAI_MODEL)}`));
}
