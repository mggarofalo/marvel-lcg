const {chromium}=require(process.env.PLAYWRIGHT_PATH||'C:/Users/mggar/AppData/Local/Temp/marvel-prototype-render/node_modules/playwright');
const path=require('node:path'),fs=require('node:fs');
(async()=>{
 const out=process.argv[2]||'C:/Users/mggar/AppData/Local/Temp/marvel-table-geometry';fs.mkdirSync(out,{recursive:true});
 const browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'C:/Users/mggar/AppData/Local/ms-playwright/chromium-1223/chrome-win64/chrome.exe',headless:true});
 const page=await browser.newPage({viewport:{width:1920,height:1080}});const errors=[],report=[];page.on('pageerror',e=>errors.push(e.message));
 const url='file:///'+path.join(__dirname,'index.html').replaceAll('\\','/');
 const audit=async name=>{await page.screenshot({path:path.join(out,name+'.png')});report.push({name,issues:await page.evaluate(()=>[...document.querySelectorAll('button,select')].filter(el=>!el.disabled&&!el.closest('[inert]')&&el.checkVisibility({visibilityProperty:true})&&el.getClientRects().length).flatMap(el=>{const r=el.getBoundingClientRect(),x=r.x+r.width/2,y=r.y+r.height/2,hit=document.elementFromPoint(x,y);return r.left<0||r.top<0||r.right>innerWidth||r.bottom>innerHeight||!(hit===el||el.contains(hit))?[{label:el.getAttribute('aria-label')||el.textContent,rect:{x:r.x,y:r.y,w:r.width,h:r.height},hit:hit?.outerHTML.slice(0,100)}]:[]}))});};
 for(const scale of [100,150])for(const state of ['mulligan','actions','target','defense','encounter','result']){
  await page.goto(url+`?state=${state}&scale=${scale}`);await audit(`${state}-${scale}`);
  if(state==='mulligan'){await page.locator('[data-card="h0"]').click();await page.locator('[data-card="h3"]').click();await audit(`mulligan-selected-${scale}`);}
  if(state==='actions'){await page.locator('[data-card="identity"]').click();await audit(`actions-menu-${scale}`);await page.locator('[data-close-source]').click();await page.locator('[data-inspect="identity"]').click();await audit(`inspector-${scale}`);await page.locator('[data-close]').click();await page.locator('#choices').click();await audit(`fallback-${scale}`);await page.locator('[data-close]').click();await page.locator('[data-seat="2"]').click();await audit(`player-two-${scale}`);}
  if(state==='target'){await page.locator('[data-card="rhino"]').click();await page.locator('[data-card="h1"]').click();await page.locator('[data-card="shooter"]').click();await audit(`payment-selected-${scale}`);}
  if(state==='defense'){await page.locator('[data-card="identity"]').click();await audit(`defense-selected-${scale}`);}
  if(state==='encounter'){await page.locator('[data-place]').click();await audit(`encounter-attached-${scale}`);}
 }
 fs.writeFileSync(path.join(out,'geometry-report.json'),JSON.stringify({errors,report},null,2));console.log(JSON.stringify({out,errors,issues:report.filter(x=>x.issues.length)},null,2));await browser.close();
})();
