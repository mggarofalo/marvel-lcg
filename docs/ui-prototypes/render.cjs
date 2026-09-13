const { chromium } = require('C:/Users/mggar/AppData/Local/Temp/marvel-prototype-render/node_modules/playwright');
const path=require('node:path');
const fs=require('node:fs');
(async()=>{
 const out=process.argv[2]||'C:/Users/mggar/AppData/Local/Temp/marvel-ui-prototypes-20260913';fs.mkdirSync(out,{recursive:true});
 const browser=await chromium.launch({executablePath:'C:/Users/mggar/AppData/Local/ms-playwright/chromium-1223/chrome-win64/chrome.exe',headless:true});
 const page=await browser.newPage({viewport:{width:1920,height:1080},deviceScaleFactor:1});
 const report=[];
 for(const concept of ['spatial','atlas','guided'])for(const scale of [100,150])for(const state of ['mulligan','actions','target','result']){
  await page.goto('file:///'+path.join(__dirname,'index.html').replaceAll('\\','/')+`?concept=${concept}&scale=${scale}&state=${state}`);
  await page.screenshot({path:path.join(out,`${concept}-${state}-${scale}.png`)});
  report.push({concept,scale,state,issues:await page.evaluate(()=>[...document.querySelectorAll('main button,main h1,main h2,main h3,main p,main .stat,main .selection-summary,main .inventory-row,main .hand-item')].flatMap(e=>{const r=e.getBoundingClientRect();const p=e.closest('.panel')?.getBoundingClientRect();return r.bottom>1048||r.right>1920||r.top<120||(p&&r.bottom>p.bottom+1)?[{text:e.textContent.slice(0,60),rect:{x:r.x,y:r.y,w:r.width,h:r.height},panelBottom:p?.bottom}]:[]}))});
 }
 await page.goto('file:///'+path.join(__dirname,'index.html').replaceAll('\\','/')+'?concept=spatial&state=mulligan&scale=100');
 await page.locator('[data-inspect="4"]').click();await page.screenshot({path:path.join(out,'spatial-inspector-attached-100.png')});
 await page.goto('file:///'+path.join(__dirname,'index.html').replaceAll('\\','/')+'?concept=atlas&state=mulligan&scale=150');
 await page.locator('[data-inspect="4"]').click();await page.screenshot({path:path.join(out,'atlas-inspector-fallback-150.png')});
 fs.writeFileSync(path.join(out,'geometry-report.json'),JSON.stringify(report,null,2));
 console.log(JSON.stringify({out,issues:report.filter(x=>x.issues.length)}));await browser.close();
})();
