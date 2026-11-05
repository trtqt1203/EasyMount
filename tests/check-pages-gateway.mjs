import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
const source = await readFile(new URL('../tools/cloudflare-pages/_worker.js', import.meta.url));
const {default: worker} = await import('data:text/javascript;base64,' + source.toString('base64'));
const realFetch = globalThis.fetch;
try {
  let called;
  globalThis.fetch = async (url, options) => {
    called = {url, options};
    return new Response('fixture', {status: 302, headers: {
      location: 'https://easymount-cloud-drive.trtqt1203.chatgpt.site/guide',
      'set-cookie': 'fixture=private',
    }});
  };
  const request = new Request('https://easymount.pages.dev//evil.example/api/releases?q=1', {
    headers: {cookie: 'private=fixture', authorization: 'Bearer fixture', rsc: '1'},
  });
  const response = await worker.fetch(request);
  assert.equal(called.url.origin, 'https://easymount-cloud-drive.trtqt1203.chatgpt.site');
  assert.equal(called.url.pathname, '//evil.example/api/releases');
  assert.equal(called.options.headers.has('cookie'), false);
  assert.equal(called.options.headers.has('authorization'), false);
  assert.equal(called.options.headers.get('rsc'), '1');
  assert.equal(called.options.redirect, 'manual');
  assert.equal(response.headers.has('set-cookie'), false);
  assert.equal(response.headers.get('location'), 'https://easymount.pages.dev/guide');
  assert.equal(await response.text(), 'fixture');
  called = null;
  assert.equal((await worker.fetch(new Request('https://easymount.pages.dev/', {method:'POST'}))).status, 405);
  assert.equal(called, null);
  await worker.fetch(new Request('https://easymount.pages.dev/api/pool-preview', {method:'POST',headers:{'content-type':'application/json',authorization:'Bearer fixture'},body:'{"sources":[]}'}));
  assert.equal(new TextDecoder().decode(called.options.body), '{"sources":[]}');
  assert.equal(called.options.headers.get('content-type'), 'application/json');
  assert.equal(called.options.headers.has('authorization'), false);
  called = null;
  assert.equal((await worker.fetch(new Request('https://easymount.pages.dev/api/pool-preview', {method:'POST',body:'x'.repeat(32769)}))).status, 413);
  assert.equal(called, null);
  globalThis.fetch = async () => {throw new Error('fixture network failure');};
  assert.equal((await worker.fetch(request)).status, 502);
  console.log('PASS fixed-origin routing, credential isolation, redirects, streamed response and error handling');
} finally {globalThis.fetch = realFetch;}
