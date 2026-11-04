// Stable short public address for the existing EasyMount website and metadata API.
// Cloud accounts/files never pass through this endpoint. No inbound credentials
// are forwarded; this is a fixed-origin public-site gateway, not an open proxy.
const origin = 'https://easymount-cloud-drive.trtqt1203.chatgpt.site';
export default {
  async fetch(request) {
    if (!['GET', 'HEAD'].includes(request.method)) {
      return new Response('Method not allowed', {status: 405, headers: {Allow: 'GET, HEAD'}});
    }
    const url = new URL(request.url);
    const target = new URL(origin);
    target.pathname = url.pathname;
    target.search = url.search;
    const headers = new Headers();
    for (const name of ['accept', 'accept-language', 'if-none-match', 'if-modified-since',
      'range', 'rsc', 'next-router-state-tree', 'next-router-prefetch', 'next-url']) {
      if (request.headers.has(name)) headers.set(name, request.headers.get(name));
    }
    try {
      const upstream = await fetch(target, {method: request.method, headers, redirect: 'manual'});
      const response = new Response(upstream.body, upstream);
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
