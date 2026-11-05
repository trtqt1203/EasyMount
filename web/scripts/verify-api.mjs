import assert from 'node:assert/strict';
const base=process.argv[2]??'http://127.0.0.1:4173';
const sources=[{id:'a',name:'Nguồn một',freeGiB:2},{id:'b',name:'Nguồn hai',freeGiB:20},{id:'c',name:'Nguồn ba',freeGiB:8}];
let passed=0;
async function post(data,expected=200){const response=await fetch(`${base}/api/pool-preview`,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(data)});assert.equal(response.status,expected);passed++;return response.json();}
assert.equal((await fetch(`${base}/api/health`).then(r=>r.json())).status,'ok');passed++;
assert.equal((await fetch(`${base}/api/providers`).then(r=>r.json())).accountLimit,null);passed++;
assert.equal((await post({sources,fileGiB:5})).selectedId,'b');
assert.equal((await post({sources:[sources[2],...sources.slice(0,2)],fileGiB:5})).selectedId,'c');
assert.equal((await post({sources,fileGiB:25})).selectedId,null);
assert.equal((await post({sources,fileGiB:2})).selectedId,'a');
await post({sources,fileGiB:0},400);
await post({sources:[sources[0],sources[0]],fileGiB:1},400);
await post({sources:[sources[0]],fileGiB:1},400);
await post({sources:[{...sources[0],freeGiB:-1},sources[1]],fileGiB:1},400);
assert.equal((await fetch(`${base}/api/pool-preview`,{method:'POST',body:'not json'})).status,415);passed++;
assert.equal((await fetch(`${base}/api/pool-preview`,{method:'POST',headers:{'Content-Type':'application/json'},body:'{broken'})).status,400);passed++;
const release=await fetch(`${base}/api/releases`);assert.equal(release.status,200);const info=await release.json();if(release.ok)assert.ok(info.status==='unpublished'||info.downloadUrl?.startsWith('https://github.com/trtqt1203/EasyMount/releases/download/'));passed++;
for (const path of ['/', '/guide','/privacy']) {assert.equal((await fetch(base+path)).status,200);passed++;}
console.log(`PASS ${passed} live HTTP checks; priority order, whole-file placement, invalid inputs, metadata, and pages.`);
