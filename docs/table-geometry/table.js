/* Presentation study: fixture transitions are authored, never engine legality. */
const $=s=>document.querySelector(s), cards=window.CARDS;
const params=new URLSearchParams(location.search);
let scene=params.get('state')||'actions', scale=+(params.get('scale')||100), seat=+(params.get('seat')||1);
let selected=new Set(), target=null, payments=new Set(), source=null, committed=false, resultText='', inspector=null, menu=false, encounterPlaced=false, draft='kick';
const names={1:'Spider-Man',2:'Captain Marvel'};
const short={'01001a':'Spider-Sense','01001b':'Scientist','01002':'1 ATK · No attack damage','01003':'Prevent attack damage','01005':'Deal 8 damage to an enemy','01006':'Heal Peter Parker','01008':'3 web counters','01010a':'Rechannel','01010b':'Commander','01011':'Confuse on entering play','01012':'Remove threat','01013':'Deal 5 damage','01014':'Three energy resources','01015':'Exchange cards','01016':'+1 DEF / +2 with Aerial','01017':'Aerial · damage prevention','01018':'Store energy','01058':'After thwarting: deal 1 damage','01083':'Stun on entering play','01088':'Two energy resources','01089':'Two mental resources','01090':'Two physical resources','01091':'Choose a player to draw','01094':'Brute. Criminal.','01097b':'Final scheme · 7 per player','01098':'Damage is placed here','01103':'Criminal.'};
const resourceLabel={Y:'E',B:'M',R:'P',G:'W'};
const res=c=>(c.attrs.RES||'').split('').map(x=>resourceLabel[x]||x).join(' ');
const esc=s=>String(s).replace(/[&<>"']/g,x=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[x]));
function reset(value){scene=value;selected.clear();target=null;payments.clear();source=null;committed=false;resultText='';inspector=null;menu=false;encounterPlaced=false;draft='kick';render();}
function face(id,key,kind,options={}){
 const c=cards[id],a=c.attrs, cost=a.Cost, mark=c.type==='Resource'?res(c):c.name.split(/[ -]/).map(s=>s[0]).slice(0,2).join('');
 let stats=c.type==='Hero'?`${a.THW} THW　${a.ATK} ATK　${a.DEF} DEF`:c.type==='AlterEgo'?`${a.REC} REC　${a.HS} HAND`:a.ATK?`${a.SCH?a.SCH+' SCH':a.THW+' THW'}　${a.ATK} ATK`:'';
 if(seat===2&&id==='01010a')stats='2 THW　2 ATK　3 DEF';
 const text=options.summary||short[id]||c.type;
 const statTokens=stats.split(/　/).map(s=>{const [value,...label]=s.split(' ');return `<span class="stat-token"><b>${value}</b><small>${label.join(' ')}</small></span>`;}).join('');
 return `<button class="card-face" data-card="${key}" aria-label="${esc(c.name)}${options.action?' — '+esc(options.action):' — inspect or choose'}" ${options.selectable?`aria-pressed="${options.selected?'true':'false'}"`:''}><span class="card-top">${cost!==undefined?`<span class="cost">${cost}</span>`:''}<span>${esc(c.name)}</span></span><span class="card-type">${c.type==='AlterEgo'?'Alter-ego':c.type==='MainScheme'?'Main scheme · 1B':esc(c.type)}${options.exhausted?' · EXHAUSTED':''}</span><span class="card-art" data-mark="${esc(mark)}"></span>${stats?`<span class="stats">${statTokens}</span>`:''}<span class="card-summary">${esc(text)}</span></button><button class="inspect" data-inspect="${key}" aria-label="Inspect ${esc(c.name)}">i</button>`;
}
let objectData={};
function object(id,key,x,y,kind='',options={}){
 objectData[key]={id,options};
 const selectable=canChoose(key), isSelected=target===key||payments.has(key)||selected.has(key);
 return `<div class="object ${kind} ${selectable?'is-target':''} ${isSelected?'is-selected':''} ${options.exhausted?'exhausted':''}" data-object="${key}" style="left:${x/19.2}%;top:${y}px;${options.height?`height:${options.height}px;`:''}">${face(id,key,kind,{...options,selectable,selected:isSelected})}${options.hp!==undefined?`<div class="dial ${kind==='scheme'?'threat':''}" aria-label="${options.hp} ${kind==='scheme'?'threat':'hit points'}">${options.hp}<small>${options.max?'/ '+options.max:kind==='scheme'?'THREAT':'HP'}</small></div>`:''}${(options.badge||selectable)?`<div class="badge">${isSelected?'✓ SELECTED':options.badge||'◇ '+(scene==='defense'?'DEFEND':scene==='target'&&key==='shooter'?'PAY':'TARGET')}</div>`:''}${options.under?`<div class="label-under">${options.under}</div>`:''}${options.attachment?`<button class="attachment" data-inspect="armor">↳ Armored Rhino Suit · 0 damage</button>`:''}</div>`;
}
function opening(){return scene==='mulligan';}
function handIds(){return seat===1?opening()?['01005','01002','01006','01088','01008','01003']:scene==='result'||(committed&&scene==='target')?['01089','01003','01008']:['01005','01088','01089','01003','01008']:opening()?['01013','01014','01012','01011','01017','01015']:['01013','01014','01012','01018','01083'];}
function offered(){
 if(seat!==1||committed)return [];
 if(scene==='mulligan')return handIds().map((id,i)=>({id:'h'+i,label:'Discard '+cards[id].name,source:'Opening hand',type:'card',key:'h'+i}));
 if(scene==='target')return [...(draft==='thwart'?[{id:'t-scheme',label:'The Break-In!',source:'Target · scheme',type:'card',key:'scheme'}]:[{id:'t-rhino',label:'Rhino',source:'Target · enemy',type:'card',key:'rhino'},{id:'t-shocker',label:'Shocker',source:'Target · enemy',type:'card',key:'shocker'}]),...(draft==='kick'?[{id:'pay-shooter',label:'Use Web-Shooter',source:'Payment · 1 wild',type:'card',key:'shooter'},...handIds().map((id,i)=>({id:'pay-h'+i,label:'Discard '+cards[id].name,source:'Payment · '+res(cards[id]),type:'card',key:'h'+i})).filter(x=>x.key!=='h0')]:[])];
 if(scene==='defense')return [{id:'def-identity',label:'Defend with Spider-Man',source:'3 DEF · exhaust',type:'card',key:'identity'},{id:'def-cat',label:'Defend with Black Cat',source:'2 HP · exhaust',type:'card',key:'cat'},{id:'def-daredevil',label:'Defend with Daredevil',source:'2 HP · exhaust',type:'card',key:'daredevil'},{id:'undefended',label:'Do not defend',source:'Spider-Man takes the attack',type:'undefended'}];
 if(scene==='encounter')return [];
 if(scene==='actions')return [{id:'attack',label:'Basic attack',source:'Spider-Man · 2 ATK',key:'identity'},{id:'thwart',label:'Basic thwart',source:'Spider-Man · 1 THW',key:'identity'},{id:'flip',label:'Change to Peter Parker',source:'Spider-Man',key:'identity'},{id:'cat-attack',label:'Basic attack',source:'Black Cat · 1 ATK',key:'cat'},{id:'cat-thwart',label:'Basic thwart',source:'Black Cat · 1 THW',key:'cat'},{id:'kick',label:'Play Swinging Web Kick',source:'Hand · cost 3',key:'h0'},{id:'play-shooter',label:'Play Web-Shooter',source:'Hand · cost 1',key:'h4'},{id:'end',label:'End turn',source:'Player turn',key:null}];
 return [];
}
function canChoose(key){return seat===1&&!committed&&((scene==='target'&&offered().some(x=>x.key===key))||(scene==='defense'&&['identity','cat','daredevil'].includes(key)));}
function render(){
 document.body.className=(scale===150?'large ':'')+scene;$('#scene').value=scene;$('#scale').textContent=`Text & controls · ${scale}%`;$('#phase').textContent=opening()?'SETUP · MULLIGAN':scene==='defense'||scene==='encounter'?'VILLAIN PHASE · ROUND 3':'PLAYER PHASE · ROUND 3';
 $('#player-label').textContent=(opening()?(seat===1?'PETER PARKER':'CAROL DANVERS'):names[seat])+"'S PLAY AREA";$('#engaged-name').textContent=names[seat].toUpperCase();$('.boundary').style.display=opening()?'none':'';
 $('#seats').innerHTML=[1,2].map(s=>`<button class="seat ${s===2?'carol':''}" data-seat="${s}" aria-pressed="${seat===s}"><span class="portrait">${s===1?'S':'C'}</span><span><b>${opening()?(s===1?'Peter Parker':'Carol Danvers'):names[s]}</b><small>${s===1?'First player · '+(opening()?'10':'9')+' HP':'12 HP · '+(opening()?'6':'5')+' cards'}${s===1?' · decision':''}</small></span></button>`).join('')+'<div class="seat-note">One player area open · shared table</div>';
 const large=scale===150, villainX=large?938:947, identityX=large?932:943;
 objectData={armor:{id:'01098'}};
 let out=object('01097b','scheme',large?572:615,85,'scheme',{hp:opening()?0:4,max:14,under:'Main scheme'})+object('01094','rhino',villainX,64,'enemy',{hp:opening()?28:scene==='result'||(scene==='target'&&committed&&target==='rhino')?12:20,max:28,attachment:scene==='encounter'&&encounterPlaced,under:'Stage I · next: Stage II'});
 if(!opening()&&seat===1)out+=object('01103','shocker',large?940:959,315,'enemy minion',{hp:3,height:126});
 out+=object(seat===1?(opening()?'01001b':'01001a'):(opening()?'01010b':'01010a'),'identity',identityX,opening()?large?531:510:454,'hero',{height:opening()?130:174,hp:opening()?(seat===1?10:12):seat===1?9:12,max:seat===1?10:12,badge:opening()?'ALTER-EGO':undefined});
 if(!opening()){
  if(seat===1){
   out+=object('01002','cat',large?1217:1190,466,'ally',{hp:2,under:'Ally · ready'});
   out+=object('01058','daredevil',large?1510:1430,466,'ally',{hp:2,exhausted:scene!=='defense',under:scene==='defense'?'Daredevil · ready':'Daredevil · exhausted',badge:scene==='defense'?undefined:'EXHAUSTED'});
   out+=object('01008','shooter',large?948:947,641,'backrow',{height:large?112:103,summary:scene==='result'||(scene==='target'&&committed)?'2 web · exhausted':'3 web · ready'});
   out+=object('01006','may',large?625:669,653,'backrow',{height:large?100:88,summary:'Alter-ego action'});
  }else{
   out+=object('01011','spiderwoman',1215,466,'ally',{hp:2,under:'Ally · ready'});
   out+=object('01017','flight',large?950:944,641,'backrow',{height:large?112:103,summary:'Aerial · prevent damage'});
   out+=object('01016','helmet',large?1220:1170,641,'backrow',{height:large?112:103,summary:'+2 DEF with Aerial'});
   out+=object('01015','station',large?625:669,653,'backrow',{height:large?100:88,summary:'Exchange cards'});
  }
 }
 if(scene==='encounter'&&!encounterPlaced)out+=object('01098','reveal',large?1270:1260,68,'enemy',{under:'Revealed for Spider-Man'});
 $('#objects').innerHTML=out;
 const pile=(key,x,y,label,count,extra='',top='')=>`<div class="pile ${extra}" style="left:${x/19.2}%;top:${y}px"><button data-pile="${key}" aria-label="${label}, ${count} cards"><span class="count">${count}</span>${top?`<small>${top}</small>`:''}</button><span class="pile-name">${label}</span></div>`;
 $('#piles').innerHTML=pile('encounter-discard',145,95,'Encounter discard',opening()?0:9,'encounter discard',opening()?'Empty':'Face up')+pile('encounter',315,95,'Encounter deck',opening()?30:18,'encounter')+pile('discard',145,493,'Player discard',opening()?0:scene==='result'?14:12,'discard',opening()?'Empty':'Face up')+pile('deck',315,493,'Player deck',opening()?34:19)+`<button class="set-aside" data-pile="set-aside">Set aside · nemesis / obligations</button>`;
 $('#hand-label').textContent=(seat===1?'YOUR HAND':"CAPTAIN MARVEL'S HAND")+' · '+handIds().length;$('#hand-help').textContent=seat===2?'Cooperative visibility':opening()?'Select cards to discard':scene==='target'?'Choose resources from hand or Web-Shooter':'Select a card to use it · i to read';
 $('#hand').innerHTML=handIds().map((id,i)=>{const key='h'+i;objectData[key]={id};const selectable=seat===1&&!committed&&(opening()||scene==='target'&&draft==='kick'&&key!=='h0'),sel=selected.has(key)||payments.has(key);return `<div class="hand-card ${selectable?'is-target':''} ${sel?'is-selected':''}" data-object="${key}">${face(id,key,'hand',{selectable,selected:sel,action:opening()?'toggle discard':scene==='target'&&key!=='h0'?'toggle resource payment':'select card'})}<span class="resource" aria-label="Resources ${esc(res(cards[id]))}">${res(cards[id])}</span>${sel?`<span class="badge">✓ ${opening()?'DISCARD':'PAY'}</span>`:scene==='target'&&key==='h0'?'<span class="badge">PLAYING</span>':''}</div>`;}).join('');
 renderMoment();renderLocal();renderOverlay();$('#receipt').innerHTML=scene==='result'?'<strong>✓ Swinging Web Kick resolved</strong>Rhino: 20 → 12 HP<br>Energy discarded<br>Web-Shooter: 3 → 2 counters<br>Web-Shooter exhausted<br>Event moved to discard':'';
 $('#choices').textContent=`All offered choices (${offered().length})`;$('#choices').disabled=seat!==1||committed||scene==='result'||scene==='encounter';
 requestAnimationFrame(drawConnections);
}
function renderMoment(){
 const el=$('#moment');el.className='';el.style.cssText='';let title='',copy='',controls='',extras='';
 if(seat!==1){title='Spider-Man is deciding';copy='You are viewing Captain Marvel’s area. The current decision belongs to Spider-Man.';controls='<button class="primary" data-return>Return to decision</button>';el.className='plain-moment';}
 else if(committed){title='✓ '+(opening()?'Mulligan submitted':scene==='defense'?'Defender declared':draft==='kick'?'Swinging Web Kick resolved':'Choice submitted');copy=resultText;controls='<button data-reset>Reset this fixture</button>';}
 else if(opening()){title='Which cards will you replace?';copy='Lift any cards out of your hand. They will go to your discard pile; then draw back to 6.';controls=`<span class="status">${selected.size} selected for discard</span><button class="primary" data-commit>${selected.size?`Discard ${selected.size} & draw ${selected.size}`:'Keep all 6 cards'}</button>`;}
 else if(scene==='actions'){title='Your turn, Spider-Man';copy='Select your identity, an ally, or a card in your hand. The available action stays beside its source.';controls='<button data-end>End turn</button>';el.className='plain-moment';}
 else if(scene==='target'){
  title=draft==='kick'?'Swinging Web Kick':draft==='thwart'?'Basic thwart':'Basic attack';copy=draft==='kick'?'Choose an enemy, then choose how to pay.':draft==='thwart'?'Choose a scheme to thwart.':'Choose an enemy to attack.';
  const paid=paymentTotal();extras=`<div class="draft-line"><span>Target <b>${target?cards[objectData[target].id].name:'choose on table'}</b></span>${draft==='kick'?`<span>Resources <b>${paid} / 3</b></span>`:''}</div>${payments.size?`<div class="moment-copy" style="margin-top:10px">${[...payments].map(k=>cards[objectData[k].id].name).join(' + ')}</div>`:''}`;
  controls=`<button data-cancel>Cancel</button><button class="primary" data-commit ${!target||(draft==='kick'&&paid<3)?'disabled':''}>${draft==='kick'?'Pay & play':'Confirm target'}</button>`;el.style.cssText='left:465px;top:325px;transform:none;width:440px';
 }
 else if(scene==='defense'){title='Rhino attacks Spider-Man';copy='2 ATK + facedown boost. Choose a ready character on your table to defend.';extras=`<div class="draft-line"><span>Defender <b>${target==='none'?'None':target?cards[objectData[target].id].name:'choose on table'}</b></span></div>`;controls=`<button data-undefended>Do not defend</button><button class="primary" data-commit ${!target?'disabled':''}>Declare defender</button>`;el.style.cssText='left:465px;top:325px;transform:none;width:440px';}
 else if(scene==='encounter'){title=encounterPlaced?'Attached to Rhino':'Armored Rhino Suit revealed';copy=encounterPlaced?'The attachment is now tucked beneath Rhino. Its damage belongs to the attachment.':'This card attaches to Rhino. The reveal remains between the encounter deck and its destination.';controls=encounterPlaced?'<button data-reset>Reset this fixture</button>':'<button class="primary" data-place>Continue reveal</button>';el.style.cssText='left:465px;top:325px;transform:none;width:440px';}
 else{title='Your turn continues';copy='The result stays by the affected part of the table. Your next decision can use the same cards and places.';controls='<button class="primary" data-reset-actions>Review player actions</button>';el.className='plain-moment';}
 el.innerHTML=`<div class="eyebrow">${opening()?'SETUP · PETER PARKER':scene==='defense'?'DEFENSE DECLARATION':scene==='encounter'?'ENCOUNTER · SPIDER-MAN':'SPIDER-MAN · YOUR DECISION'}</div><div class="moment-title">${title}</div><div class="moment-copy">${copy}</div>${extras}<div class="moment-controls">${controls}</div>`;
}
function paymentTotal(){return [...payments].reduce((n,k)=>n+(k==='shooter'?1:(cards[objectData[k].id].attrs.RES||'').length),0);}
function renderLocal(){
 $('#local-actions').innerHTML='';$('#moment').style.visibility=source?'hidden':'';if(!source||scene!=='actions'||seat!==1||menu||inspector)return;
 const ops=offered().filter(x=>x.key===source);if(!ops.length)return;
 const anchor=document.querySelector(`[data-object="${source}"]`).getBoundingClientRect(),table=$('#table').getBoundingClientRect(),w=scale===150?310:265;
 let x=scale===150?574:630,y=330;
 $('#local-actions').innerHTML=`<div class="popover" style="left:${x}px;top:${y}px"><b>${cards[objectData[source].id].name}</b>${ops.map(o=>`<button data-op="${o.id}">${o.label}</button>`).join('')}<button class="ghost" data-close-source>Close</button></div>`;drawConnections();
}
function inspectorContent(key){const c=cards[objectData[key]?.id||'01098'];return `<button class="close" data-close>Close ×</button><div class="eyebrow">${c.type.toUpperCase()} · CORE ${c.id}</div><h2>${c.name}</h2><p class="metadata">${c.traits.join(' · ')}${c.attrs.Cost?' · Cost '+c.attrs.Cost:''}</p><p class="printed">${esc(c.text||'No printed ability.').replaceAll('\\n','\n')}</p><p class="metadata">${Object.entries(c.attrs).filter(([k])=>['ATK','THW','DEF','HP','REC','HS','RES'].includes(k)).map(([k,v])=>k+' '+v).join(' · ')}</p><p class="metadata">Read-only inspection. The table keeps the action.</p>`;}
function renderOverlay(){
 const host=$('#overlay');host.innerHTML='';$('#table').inert=!!menu;$('#local-actions').hidden=!!menu;
 if(menu){host.innerHTML=`<div class="scrim"><section class="sheet" role="dialog" aria-modal="true" aria-label="All offered choices"><button class="close" data-close>Close ×</button><div class="eyebrow">CURRENT DECISION</div><h2>All offered choices</h2><div class="choice-list">${offered().map(o=>`<button data-fallback="${o.id}">${o.label}<small>${o.source}${selected.has(o.key)||payments.has(o.key)||target===o.key?' · selected':''}</small></button>`).join('')}</div><p style="font-size:.8em">Choose here or return to the table. Both use the same draft.</p></section></div>`;return;}
 if(!inspector)return;
 if(inspector==='notes'){host.innerHTML='<div class="scrim"><section class="sheet" role="dialog" aria-modal="true" aria-label="Geometry notes"><button class="close" data-close>Close ×</button><div class="eyebrow">DESIGN STUDY</div><h2>Read the table from far to near</h2><p>Encounter piles and scheme behind Rhino. Engaged minions across the boundary, facing your identity. Allies beside you, upgrades and supports behind you. Your hand at the near edge.</p><p>Click a card to act; click i to read. Blue diamonds show candidates. Lifted cards and checkmarks show a draft. Actions only commit through the named confirmation.</p><p>At 150%, text and controls grow; quiet card art contracts. An enlarged inspector opens as a readable sheet when a nearby slot cannot fit it.</p><p>This is a visual prototype with authored examples, not a complete playable engine session. The scenes are independent snapshots.</p></section></div>';$('#table').inert=true;return;}
 const el=document.querySelector(`[data-object="${inspector}"]`),r=el?.getBoundingClientRect();
 if(scale===150||!r||r.left<430){host.innerHTML=`<div class="scrim"><section class="sheet inspector-fallback" role="dialog" aria-modal="true" aria-label="Card inspector">${inspectorContent(inspector)}</section></div>`;$('#table').inert=true;}
 else{const top=330;host.innerHTML=`<section class="inspector" role="dialog" aria-label="Card inspector" style="left:${r.left-375}px;top:${top}px;--tip:210px">${inspectorContent(inspector)}</section>`;$('#moment').style.visibility='hidden';}
}
function drawConnections(){
 let pairs=[];if(seat===1&&scene==='target'&&target)pairs.push([draft==='kick'?'h0':'identity',target]);if(seat===1&&scene==='defense')pairs.push(['rhino',target&&target!=='none'?target:'identity']);if(scene==='encounter'&&!encounterPlaced)pairs.push(['reveal','rhino']);
 $('#lines').innerHTML=pairs.map(([a,b])=>{const p=document.querySelector(`[data-object="${a}"]`)?.getBoundingClientRect(),q=document.querySelector(`[data-object="${b}"]`)?.getBoundingClientRect(),t=$('#table').getBoundingClientRect();if(!p||!q)return '';const x=p.left+p.width/2,y=p.top-t.top,x2=q.left+q.width/2,y2=q.bottom-t.top;return `<path d="M ${x} ${y} C ${x-85} ${(y+y2)/2},${x2-85} ${(y+y2)/2},${x2} ${y2}"/>`;}).join('');
}
function choose(key){
 if(seat!==1||committed)return;
 if(opening()&&key.startsWith('h')){selected.has(key)?selected.delete(key):selected.add(key);render();return;}
 if(scene==='target'){if(['rhino','shocker','scheme'].includes(key)&&offered().some(x=>x.key===key))target=key;else if(offered().some(x=>x.key===key)){payments.has(key)?payments.delete(key):payments.add(key);}render();return;}
 if(scene==='defense'&&['identity','cat','daredevil'].includes(key)){target=key;render();return;}
 if(scene==='actions'){source=source===key?null:key;renderLocal();return;}
 inspector=key;renderOverlay();
}
function runOp(id){
 source=null;menu=false;if(['kick','attack','thwart','cat-attack','cat-thwart'].includes(id)){draft=id==='kick'?'kick':id.includes('thwart')?'thwart':'attack';scene='target';target=null;payments.clear();render();return;}
 inspector='notes';renderOverlay();
}
document.addEventListener('click',e=>{
 const b=e.target.closest('button');if(!b)return;
 if(b.dataset.seat){seat=+b.dataset.seat;source=null;inspector=null;menu=false;render();}
 else if(b.hasAttribute('data-return')){seat=1;render();}
 else if(b.dataset.inspect){inspector=b.dataset.inspect;source=null;renderLocal();renderOverlay();}
 else if(b.dataset.card)choose(b.dataset.card);
 else if(b.dataset.op)runOp(b.dataset.op);
 else if(b.dataset.fallback){const o=offered().find(x=>x.id===b.dataset.fallback);menu=false;if(o.type==='card'){choose(o.key);}else if(o.type==='undefended'){target='none';render();}else runOp(o.id);renderOverlay();}
 else if(b.hasAttribute('data-close')){inspector=null;menu=false;renderOverlay();renderLocal();}
 else if(b.hasAttribute('data-close-source')){source=null;renderLocal();}
 else if(b.hasAttribute('data-commit')){if(committed)return;committed=true;resultText=opening()?`${selected.size} cards selected for discard. The next engine snapshot would supply the replacement hand.`:scene==='defense'?`${target==='none'?'No defender':cards[objectData[target].id].name+' selected'}. The next engine window would reveal the boost and resolve the attack.`:draft==='kick'?target==='rhino'?'8 damage dealt to Rhino. Energy is discarded; Web-Shooter has 2 counters and is exhausted.':'8 damage defeats Shocker. The next snapshot would move Shocker to the encounter discard.':'The engine would resolve the selected basic power.';render();}
 else if(b.hasAttribute('data-reset'))reset(scene);
 else if(b.hasAttribute('data-reset-actions'))reset('actions');
 else if(b.hasAttribute('data-cancel'))reset('actions');
 else if(b.hasAttribute('data-undefended')){target='none';render();}
 else if(b.hasAttribute('data-place')){encounterPlaced=true;render();}
 else if(b.hasAttribute('data-end')){inspector='notes';renderOverlay();}
 else if(b.dataset.pile){inspector='notes';renderOverlay();}
});
$('#scene').addEventListener('change',e=>reset(e.target.value));$('#scale').onclick=()=>{scale=scale===100?150:100;render();};$('#choices').onclick=()=>{menu=true;source=null;renderLocal();renderOverlay();};$('#guide').onclick=()=>{inspector='notes';renderOverlay();};
document.addEventListener('keydown',e=>{if(e.key==='Escape'){inspector=null;menu=false;source=null;renderOverlay();renderLocal();}});
window.addEventListener('resize',drawConnections);render();
