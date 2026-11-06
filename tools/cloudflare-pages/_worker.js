// Stable short public address for the existing EasyMount website and metadata API.
// Cloud accounts/files never pass through this endpoint. No inbound credentials
// are forwarded; this is a fixed-origin public-site gateway, not an open proxy.
const origin = 'https://easymount-cloud-drive.trtqt1203.chatgpt.site';
export default {
  async fetch(request) {
    const url = new URL(request.url);
    const previewPost = request.method === 'POST' && url.pathname === '/api/pool-preview';
    if (!['GET', 'HEAD'].includes(request.method) && !previewPost) {
      return new Response('Method not allowed', {status: 405, headers: {Allow: 'GET, HEAD'}});
    }
    const target = new URL(origin);
    target.pathname = url.pathname;
    target.search = url.search;
    const headers = new Headers();
    for (const name of ['accept', 'accept-language', 'if-none-match', 'if-modified-since',
      'range', 'rsc', 'next-router-state-tree', 'next-router-prefetch', 'next-url']) {
      if (request.headers.has(name)) headers.set(name, request.headers.get(name));
    }
    let body;
    if (previewPost) {
      const reader = request.body?.getReader(), chunks = [];
      let size = 0;
      if (reader) {
        while (true) {
          const {done, value} = await reader.read();
          if (done) break;
          size += value.byteLength;
          if (size > 32768) {
            await reader.cancel();
            return new Response('Request too large', {status: 413});
          }
          chunks.push(value);
        }
      }
      body = new Uint8Array(size);
      let offset = 0;
      for (const chunk of chunks) {body.set(chunk, offset); offset += chunk.byteLength;}
      if (request.headers.has('content-type')) headers.set('content-type', request.headers.get('content-type'));
    }
    try {
      const upstream = await fetch(target, {method: request.method, headers, body, redirect: 'manual'});
      const response = new Response(upstream.body, upstream);
      response.headers.set('Strict-Transport-Security', 'max-age=86400');
      response.headers.set('X-Content-Type-Options', 'nosniff');
      response.headers.set('X-Frame-Options', 'DENY');
      response.headers.set('Referrer-Policy', 'strict-origin-when-cross-origin');
      response.headers.set('Permissions-Policy', 'camera=(), microphone=(), geolocation=()');
      response.headers.set('Content-Security-Policy', "base-uri 'self'; object-src 'none'; frame-ancestors 'none'");
      response.headers.delete('set-cookie');
      const location = response.headers.get('location');
      if (location?.startsWith(origin + '/')) {
        response.headers.set('location', url.origin + location.slice(origin.length));
      }
      return response;
    } catch {
      return new Response('EasyMount website temporarily unavailable', {
        status: 502, headers: {'Content-Type': 'text/plain; charset=utf-8', 'Cache-Control': 'no-store'}
      });
    }
  }
};
