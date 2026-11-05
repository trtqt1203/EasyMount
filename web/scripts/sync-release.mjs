import {writeFile} from 'node:fs/promises';
const response=await fetch('https://api.github.com/repos/trtqt1203/EasyMount/releases/latest',{headers:{Accept:'application/vnd.github+json','User-Agent':'EasyMount-Release-Sync'},signal:AbortSignal.timeout(15000)});
const checkedAt=new Date().toISOString().slice(0,10);
let manifest;
if(response.status===404) manifest={status:'unpublished',version:null,downloadUrl:null,releasesUrl:'https://github.com/trtqt1203/EasyMount/releases',checkedAt,message:'Chưa có bản phát hành công khai.'};
else {
  if(!response.ok) throw new Error(`GitHub returned ${response.status}; existing manifest preserved.`);
  const release=await response.json();
  const version=release.tag_name?.replace(/^v/,'');
  if(!/^\d+\.\d+\.\d+$/.test(version??'')) throw new Error('Not a stable version; manifest preserved.');
  const asset=release.assets?.find(a=>a.name===`EasyMount-${version}-Setup.exe`);
  if(!asset || !Number.isSafeInteger(asset.size) || asset.size<=0) throw new Error('Installer missing; manifest preserved.');
  const download=new URL(asset.browser_download_url);
  if(download.origin!=='https://github.com'||!download.pathname.startsWith('/trtqt1203/EasyMount/releases/download/'))throw new Error('Untrusted installer URL; manifest preserved.');
  manifest={status:'published',version,downloadUrl:download.href,releaseUrl:release.html_url,publishedAt:release.published_at,size:asset.size,platform:'windows-x64',signed:false,checkedAt};
}
await writeFile(new URL('../lib/release-manifest.json',import.meta.url),JSON.stringify(manifest,null,2)+'\n');
console.log(`Release manifest synchronized: ${manifest.status}${manifest.version?' '+manifest.version:''}. Rebuild and deploy the site to publish this metadata.`);
