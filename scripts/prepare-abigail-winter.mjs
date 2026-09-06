import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const actor=process.argv.find(arg=>arg.startsWith('--actor='))?.slice(8)??'Abigail';
assert.match(actor,/^[A-Za-z]+$/);
const beach=process.argv.includes('--beach'),base=process.argv.includes('--base'),hospital=process.argv.includes('--hospital'),variant=base?'Base':beach?'Beach':hospital?'Hospital':'Winter';
assert.ok([beach,base,hospital].filter(Boolean).length<=1,'Choose one outfit variant.');
assert.ok(!hospital||actor==='Maru','Only Maru has a supported hospital sheet.');
assert.ok(!base||['Abigail','Emily','Haley','Leah','Maru','Penny','Alex','Elliott','Harvey','Sam'].includes(actor),'Unsupported base sheet.');
const work=`artifacts/npc-modern/work/${actor}${variant}/`;
async function source(file,rows){
 const s=await sharp(work+file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 for(let p=0;p<s.data.length;p+=4){const r=s.data[p],g=s.data[p+1],b=s.data[p+2];if(s.data[p+3]<160||(r>g+100&&b>g+100&&r>b*.88))s.data.fill(0,p,p+4);else s.data[p+3]=255;}
 const counts=Array(s.info.height).fill(0);
 for(let y=0;y<s.info.height;y++)for(let x=0;x<s.info.width;x++)if(s.data[(y*s.info.width+x)*4+3])counts[y]++;
 const bounds=[0];for(let row=1;row<rows;row++){const expected=row*s.info.height/rows;let best=Math.round(expected),score=Infinity;for(let y=Math.round(expected-s.info.height/rows*.22);y<expected+s.info.height/rows*.22;y++){const rank=counts[y]*s.info.height+Math.abs(y-expected);if(rank<score){best=y;score=rank;}}bounds.push(best);}bounds.push(s.info.height);
 if(actor==='Haley'&&variant==='Winter'&&file==='portraits-source.png'){
  const gutters=[0,209,414,619,830,1037,1251,s.info.height];
  for(const y of gutters.slice(1,-1))assert.equal(counts[y],0,'Winter portrait gutter must be empty.');
  bounds.splice(0,bounds.length,...gutters);
 }
 const columns=actor==='Sam'&&base&&file==='special-source.png'?[0,365,620,875,s.info.width]:actor==='Sam'&&base&&file==='walk-source.png'?[0,580,890,1190,s.info.width]:actor==='Sam'&&variant==='Winter'&&file==='work-corrections-source.png'?[0,630,1110,s.info.width]:actor==='Sam'&&variant==='Winter'&&['walk-source.png','walk-corrections-source.png'].includes(file)?[0,410,627,840,s.info.width]:null;
 if(columns)for(const x of columns.slice(1,-1))for(let y=0;y<s.info.height;y++)assert.equal(s.data[(y*s.info.width+x)*4+3],0,`Sam walking column gutter crosses artwork: ${file} x=${x}.`);
 return {...s,png:await sharp(s.data,{raw:s.info}).png().toBuffer(),bounds,columns};
}
async function cell(s,cols,index,w,h,trimRight=0){const col=index%cols,row=Math.floor(index/cols),left=s.columns?.[col]??Math.round(col*s.info.width/cols),right=s.columns?.[col+1]??Math.round((col+1)*s.info.width/cols);const crop=await sharp(s.png).extract({left,top:s.bounds[row],width:right-left-trimRight,height:s.bounds[row+1]-s.bounds[row]}).png().toBuffer();const data=await sharp(crop).trim({background:'#00000000',threshold:1}).resize(w,h,{fit:'fill'}).ensureAlpha().raw().toBuffer();for(let p=0;p<data.length;p+=4){if(data[p+3]<160)data.fill(0,p,p+4);else data[p+3]=255;}return data;}
const original=await sharp(`artifacts/npc-modern/originals/Characters/${actor}${base?'':'_'+variant}.png`).ensureAlpha().raw().toBuffer({resolveWithObject:true}),output=Buffer.from(original.data);
assert.equal(original.info.width,64,'Expected four native 16px frame columns.');
if(base){const walking=await sharp(`src/AbigailModern/assets/${actor==='Abigail'?'':actor+'/'}characters.png`).ensureAlpha().raw().toBuffer();walking.copy(output,0,0,64*128*4);}
const retainedFormal=['Emily','Haley','Leah','Maru','Penny'].includes(actor)&&base?[36,37,38,40,41,42,43,44,45,46,47]:[];
const reusePlans=actor==='Sam'&&base?[{variant:'Winter',pairs:[40,41,42,44,45,46,47,48,49,50].map(frame=>[frame,frame])}]:['Elliott','Harvey'].includes(actor)&&base?[{variant:'Winter',pairs:[44,45,46,47,48,49,50].map(frame=>[frame,frame])}]:actor==='Alex'&&base?[{variant:'Winter',pairs:[28,29,30,31,32,33,34,44,45,46,47,48,49,50,51].map(frame=>[frame,frame])}]:actor==='Penny'&&base?[{variant:'Winter',pairs:[31,32,33,...retainedFormal,48].map(frame=>[frame,frame])}]:actor==='Haley'&&base?[{variant:'Winter',pairs:[34,36,37,38,39,40,41,42,43,44,45,46,47].map(frame=>[frame,frame])},{variant:'Beach',pairs:[[48,20],[49,21],[50,22]]}]:['Leah','Maru'].includes(actor)&&base?[{variant:'Winter',pairs:[...retainedFormal,actor==='Leah'?51:33].map(frame=>[frame,frame])}]:retainedFormal.length?[{variant:'Winter',pairs:retainedFormal.map(frame=>[frame,frame])}]:[];
const retainedFrames=reusePlans.flatMap(plan=>plan.pairs.map(([frame])=>frame));
const retainedBlank=actor==='Alex'&&base?[43]:actor==='Alex'&&beach?[19]:hospital?[19,20,21,22,23,24,25,26,27,29,30,31]:actor==='Haley'&&base?[35,51]:actor==='Leah'&&base?[39,50]:actor==='Maru'&&base?[39]:[];
for(const plan of reusePlans){
 const retained=await sharp(`src/AbigailModern/assets/${actor}${plan.variant}/characters.png`).ensureAlpha().raw().toBuffer();
 const native=await sharp(`artifacts/npc-modern/originals/Characters/${actor}_${plan.variant}.png`).ensureAlpha().raw().toBuffer();
 for(const [frame,sourceFrame] of plan.pairs)for(let dy=0;dy<32;dy++){
  const p=((Math.floor(frame/4)*32+dy)*64+frame%4*16)*4,s=((Math.floor(sourceFrame/4)*32+dy)*64+sourceFrame%4*16)*4;
  for(let dx=0;dx<16;dx++)assert.equal(original.data[p+dx*4+3],native[s+dx*4+3],`Reused pose ${frame} native silhouette differs.`);
  if(['Alex','Elliott','Harvey','Sam'].includes(actor)&&base)assert.ok(original.data.subarray(p,p+64).equals(native.subarray(s,s+64)),`${actor} reused native pose ${frame} differs in color or alpha.`);
  retained.copy(output,p,s,s+64);
 }
}
const newWalking=!!(await fs.stat(work+'walk-source.png').catch(()=>null));
const groups=base&&!newWalking?[]:[['walk-source.png',beach&&!['Maru','Penny','Alex','Sam'].includes(actor)?original.info.height/32:4,0]];
if(actor==='Sam'&&beach)groups.push(['guitar-source.png',2,16,3,[16,17,18,19,20]]);
if(['Maru','Penny'].includes(actor)&&beach)groups.push(['towel-source.png',1,16]);
if(actor==='Alex'&&beach)groups.push(['towel-source.png',1,16,3]);
if(hospital)groups.push(['special-source.png',1,0,4,[16,17,18,28]]);
else if(actor==='Alex'&&base)groups.push(['football-source.png',3,16],['special-source.png',2,35]);
else if(actor==='Haley'&&base)groups.push(['special-upper-source.png',3,16],['special-tail-source.png',2,28,3]);
else if(await fs.stat(work+'special-source.png').catch(()=>null))groups.push(['special-source.png',5,16]);
if(await fs.stat(work+'final-source.png').catch(()=>null))groups.push(actor==='Sam'&&base?['final-source.png',3,0,3,[36,37,38,39,43,51,52,53,54]]:actor==='Harvey'&&base?['final-source.png',3,0,4,[36,37,38,39,40,41,42,43,51,52,53,54]]:actor==='Elliott'&&['Winter','Base'].includes(variant)?['final-source.png',3,0,4,[36,37,38,39,44,45,46,47,48,49,50,51]]:actor==='Emily'&&base?['final-source.png',3,0,3,[39,48,49,50,51,52,53,54,55]]:['final-source.png',original.info.height/32-9,36]);
if(actor==='Leah'&&base)groups.push(['painting-source.png',1,48,2]);
if(actor==='Penny'&&base)groups.push(['hug-source.png',1,39,1]);
const retainedPlaceholderFrames=['Harvey','Sam'].includes(actor)&&base?[55]:actor==='Penny'&&base?[49,50,51]:[];
for(const frame of retainedPlaceholderFrames)for(let dy=0;dy<32;dy++)for(let dx=0;dx<16;dx++){
 const p=((Math.floor(frame/4)*32+dy)*64+frame%4*16+dx)*4;
 assert.deepEqual([...original.data.subarray(p,p+4)],actor==='Sam'?[255,255,255,255]:actor==='Harvey'?[0,0,0,255]:[123,66,26,255],`${actor} placeholder ${frame} changed.`);
 assert.ok(output.subarray(p,p+4).equals(original.data.subarray(p,p+4)));
}
let count=(base&&!newWalking?16:0)+retainedPlaceholderFrames.length,occupied=base&&!newWalking?16:0,placeholders=retainedPlaceholderFrames.length;const bounds={};
const hover=actor==='Emily'&&!beach&&!base?await source('hover-source.png',1):null;
const idle=actor==='Emily'&&beach?await source('idle-source.png',1):null;
const camera=actor==='Haley'&&!base&&!beach&&await fs.stat(work+'camera-source.png').catch(()=>null)?await source('camera-source.png',1):null;
const towel=actor==='Haley'&&beach&&await fs.stat(work+'towel-source.png').catch(()=>null)?await source('towel-source.png',1):null;
const stride=actor==='Haley'&&base?await source('stride-source.png',1):null;
const leahFrontStride=actor==='Leah'&&!base&&!beach?await source('front-stride-source.png',1):null;
const maruCorrections=actor==='Maru'&&variant==='Winter'?await source('corrected-poses-source.png',2):null;
const maruStride=actor==='Maru'&&variant==='Winter'?await source('stride-source.png',1):null;
const hospitalIdle=hospital?await source('walk-source.png',4):null;
const maruBeachStride=actor==='Maru'&&beach?await source('stride-source.png',1):null;
const pennyWave=actor==='Penny'&&['Winter','Base'].includes(variant)?await source('wave-source.png',1):null;
const pennyClosedBook=actor==='Penny'&&base?await source('closed-book-source.png',1):null;
const alexSideStride=actor==='Alex'&&variant==='Winter'?await source('side-stride-source.png',1):null;
const alexFinalCorrections=actor==='Alex'&&variant==='Winter'?await source('final-corrections-source.png',1):null;
const alexMusicBox=actor==='Alex'&&variant==='Winter'?await source('music-box-bowed-source.png',1):null;
const alexSitting=actor==='Alex'&&variant==='Winter'?await source('sitting-source.png',1):null;
const alexBaseStride=actor==='Alex'&&base?await source('front-stride-source.png',1):null;
const alexBasePoses=actor==='Alex'&&base?await source('pose-corrections-source.png',1):null;
const elliottSeated=actor==='Elliott'&&['Winter','Base'].includes(variant)?await source('seated-source.png',2):null;
const elliottBeachStride=actor==='Elliott'&&beach?await source('stride-source.png',1):null;
const harveyCorrections=actor==='Harvey'&&variant==='Winter'?await source('pose-corrections-source.png',1):null;
const harveyFinalCorrections=actor==='Harvey'&&variant==='Winter'?await source('final-corrections-source.png',1):null;
const harveyWalkingArms=actor==='Harvey'&&variant==='Winter'?await source('walking-arm-correction-source.png',4):null;
const harveyBlueArms=actor==='Harvey'&&variant==='Winter'?await source('blue-arms-correction-source.png',1):null;
const harveyBeachStrides=actor==='Harvey'&&beach?await source('stride-correction-source.png',4):null;
const harveyBaseSpecial=actor==='Harvey'&&base?await source('special-corrections-source.png',5):null;
const samWalk=actor==='Sam'&&variant==='Winter'?await source('walk-corrections-source.png',4):null;
const samSpecial=actor==='Sam'&&variant==='Winter'?await source('special-corrections-source.png',5):null;
const samFinal=actor==='Sam'&&variant==='Winter'?await source('final-corrections-source.png',5):null;
const samWork=actor==='Sam'&&variant==='Winter'&&await fs.stat(work+'work-corrections-source.png').catch(()=>null)?await source('work-corrections-source.png',1):null;
const samBeachIdle=actor==='Sam'&&beach?await source('idle-source.png',1):null;
const samBeachGuitar=actor==='Sam'&&beach?await source('compact-guitar-source.png',1):null;
for(const [file,rows,start,cols=4,frames=null] of groups){const s=await source(file,rows);bounds[file]=s.bounds;for(let i=0;i<rows*cols;i++){
 const frame=frames?.[i]??start+i,x=frame%4*16,y=Math.floor(frame/4)*32;let minX=16,minY=32,maxX=-1,maxY=-1;
 assert.ok(frame>=0&&y+32<=original.info.height,`Frame ${frame} exceeds native sheet.`);
 if(retainedFrames.includes(frame))continue;
 if(actor==='Harvey'&&variant==='Winter'&&frame===55){
  let black=0,transparent=0;
  for(let dy=0;dy<32;dy++)for(let dx=0;dx<16;dx++){
   const p=((y+dy)*64+x+dx)*4,rgba=original.data.subarray(p,p+4);
   assert.equal(rgba[0]+rgba[1]+rgba[2],0,'Harvey placeholder gained color.');
   if(rgba[3]===255)black++;else{assert.equal(rgba[3],0);transparent++;}
   assert.ok(output.subarray(p,p+4).equals(rgba),'Harvey placeholder changed.');
  }
  assert.equal(black,496);assert.equal(transparent,16);count++;placeholders++;continue;
 }
 for(let dy=0;dy<32;dy++)for(let dx=0;dx<16;dx++)if(original.data[((y+dy)*64+x+dx)*4+3]){minX=Math.min(minX,dx);maxX=Math.max(maxX,dx);minY=Math.min(minY,dy);maxY=Math.max(maxY,dy);}
 if(maxX<0){count++;continue;}
 let whitePlaceholder=true;
 for(let dy=0;dy<32;dy++)for(let dx=0;dx<16;dx++){const p=((y+dy)*64+x+dx)*4;if(!original.data.subarray(p,p+4).every(value=>value===255))whitePlaceholder=false;}
 if(whitePlaceholder){count++;placeholders++;continue;}
 if(actor==='Penny'&&variant==='Winter'&&frame>=49){
  for(let dy=0;dy<32;dy++)for(let dx=0;dx<16;dx++){
   const p=((y+dy)*64+x+dx)*4;
   assert.deepEqual([...original.data.subarray(p,p+4)],[123,66,26,255],`Penny placeholder ${frame} changed.`);
  }
  count++;placeholders++;continue;
 }
 const w=maxX-minX+1,h=maxY-minY+1;let data=await cell(s,cols,i,w,h,actor==='Abigail'&&!base&&file==="final-source.png"&&i===10?42:0);
 if(hover&&[24,25,39].includes(frame))data=await cell(hover,3,[24,25,39].indexOf(frame),w,h);
 if(pennyWave&&frame===20)data=await cell(pennyWave,1,0,w,h);
 if(pennyClosedBook&&[16,17].includes(frame))data=await cell(pennyClosedBook,2,frame-16,w,h);
 if(actor==='Alex'&&variant==='Winter'&&[20,24].includes(frame))data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(alexSideStride&&[7,15].includes(frame))data=await cell(alexSideStride,2,frame===7?0:1,w,h);
 if(alexFinalCorrections&&[42,50,51].includes(frame))data=await cell(alexFinalCorrections,3,[42,50,51].indexOf(frame),w,h);
 if(alexFinalCorrections&&frame===42)data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(alexMusicBox&&frame===38)data=await cell(alexMusicBox,1,0,w,h);
 if(alexSitting&&frame===39)data=await cell(alexSitting,1,0,w,h);
 if(alexBaseStride&&frame===3)data=await cell(alexBaseStride,1,0,w,h);
 if(alexBasePoses&&[39,42].includes(frame))data=await cell(alexBasePoses,2,frame===39?0:1,w,h);
 if(actor==='Alex'&&base&&[17,19,20].includes(frame))data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(elliottSeated&&frame>=24&&frame<=31)data=await cell(elliottSeated,4,frame-24,w,h);
 if(harveyBeachStrides&&[3,7,11,15].includes(frame))data=await cell(harveyBeachStrides,4,frame,w,h);
 if(harveyBaseSpecial&&[19,25,27,28,31,34].includes(frame))data=await cell(harveyBaseSpecial,4,frame-16,w,h);
 if(samWalk&&frame===11)data=await cell(samWalk,4,frame,w,h);
 // Export the opposite front stride from the same figure to retain exact body proportions.
 if(samWalk&&frame===3)data=await sharp(await cell(samWalk,4,1,w,h),{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(samSpecial&&[16,17,18,19,21,23].includes(frame))data=await cell(samSpecial,4,frame-16,w,h);
 if(samFinal&&[36,40,41,42,46,53].includes(frame))data=await cell(samFinal,4,frame-36,w,h);
 if(samWork&&[40,41,42].includes(frame))data=await cell(samWork,3,frame-40,w,h);
 if(actor==='Sam'&&base&&file==='walk-source.png'&&[11,15].includes(frame))data=await sharp(await cell(s,4,frame===11?9:5,w,h),{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(actor==='Sam'&&base&&[29,30,33,34,35].includes(frame))data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(samBeachIdle&&frame<16){
  if(frame%2===0)data=await cell(samBeachIdle,4,Math.floor(frame/4),w,h);
  else{
   const sourceFrame={1:0,3:0,5:4,7:5,9:8,11:8,13:5,15:4}[frame];
   data=await cell(s,4,sourceFrame,w,h);
   if([3,11,13,15].includes(frame))data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
  }
 }
 if(samBeachGuitar&&frame>=17&&frame<=20)data=await cell(samBeachGuitar,4,frame-17,w,h);
 if(actor==='Sam'&&variant==='Winter'&&[29,30,33,34,35].includes(frame))data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(actor==='Harvey'&&base&&[36,37,38,51,52,53].includes(frame))data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(harveyCorrections&&[23,28,31,7,15].includes(frame))data=await cell(harveyCorrections,5,[23,28,31,7,15].indexOf(frame),w,h);
 if(harveyCorrections&&frame===15)data=await sharp(await cell(harveyCorrections,5,3,w,h),{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(harveyFinalCorrections&&[44,45,46,47,50,54].includes(frame))data=await cell(harveyFinalCorrections,6,[44,45,46,47,50,54].indexOf(frame),w,h);
 if(harveyWalkingArms&&[7,15].includes(frame))data=await cell(harveyWalkingArms,4,frame,w,h);
 if(harveyBlueArms&&frame===46)data=await cell(harveyBlueArms,1,0,w,h);
 if(actor==='Harvey'&&variant==='Winter'&&[36,37,38].includes(frame))data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(elliottBeachStride&&frame<16){
  // The generated sheet's side rows are reversed; select the correctly facing artwork.
  const sourceFrame=frame===2?0:frame===10?8:frame>=4&&frame<8?(frame===6?12:frame+8):frame>=12?(frame===14?4:frame-8):frame;
  data=await cell(s,4,sourceFrame,w,h);
  const corrected=[3,9,11,7,15].indexOf(frame);
  if(corrected>=0)data=await cell(elliottBeachStride,5,corrected,w,h);
  if(frame===3)data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
  if(frame===11)data=await sharp(await cell(elliottBeachStride,5,1,w,h),{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 }
 if(idle&&[4,6,12,14].includes(frame))data=await cell(idle,2,frame<8?0:1,w,h);
 if(camera&&frame===31)data=await cell(camera,1,0,w,h);
 if(towel&&[20,21,22].includes(frame))data=await cell(towel,3,frame-20,w,h);
 if(maruCorrections&&[30,31].includes(frame))data=await cell(maruCorrections,2,frame-28,w,h);
 if(maruStride&&frame===3)data=await cell(maruStride,2,0,w,h);
 if(maruStride&&frame===11)data=await sharp(await cell(s,4,9,w,h),{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(hospital&&file==='walk-source.png'&&[7,15].includes(frame))data=await sharp(await cell(s,4,frame===7?13:5,w,h),{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(hospitalIdle&&frame===16)data=await cell(hospitalIdle,4,0,w,h);
 if(actor==='Maru'&&base&&file==='walk-source.png'&&[7,11,15].includes(frame))data=await sharp(await cell(s,4,frame===7?13:frame===11?9:5,w,h),{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(maruBeachStride&&[1,3,7,9,11,15].includes(frame)){
  data=await cell(maruBeachStride,4,frame===1?0:frame===3?1:[7,15].includes(frame)?2:3,w,h);
  if([1,11,15].includes(frame))data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 }
 if(stride&&[7,15].includes(frame)){data=await cell(stride,1,0,w,h);if(frame===15)data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();}
 if(actor==='Haley'&&base&&frame===21)data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(actor==='Leah'&&base&&[24,25].includes(frame))data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(actor==='Leah'&&!base&&!beach){
  if(file==='walk-source.png'&&frame>=4&&frame<8)data=await cell(s,4,frame+8,w,h);
  if(file==='walk-source.png'&&frame>=12&&frame<16)data=await cell(s,4,frame-8,w,h);
  if(frame===3)data=await cell(leahFrontStride,1,0,w,h);
  if([11,24,25].includes(frame))data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 }
 if(actor==='Emily'&&base&&file==='walk-source.png'&&frame===7)data=await sharp(await cell(s,4,15,w,h),{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(actor==='Emily'&&base&&frame===52)data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(actor==='Haley'&&!base&&!beach&&file==='walk-source.png'&&[3,11].includes(frame))data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(actor==='Haley'&&!base&&!beach&&[25,27].includes(frame))data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(actor==='Haley'&&beach&&frame===19)data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(actor==='Emily'&&!base&&((file==='special-source.png'&&(i===6||i===17))||(file==='final-source.png'&&i===6)))data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 if(actor==='Emily'&&beach&&[20,21].includes(frame))data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
 assert.ok(data.some((value,index)=>index%4===3&&value>0),`Generated pose ${frame} is empty.`);
 for(let dy=0;dy<32;dy++)output.fill(0,((y+dy)*64+x)*4,((y+dy)*64+x+16)*4);
 for(let dy=0;dy<h;dy++)data.copy(output,((y+minY+dy)*64+x+minX)*4,dy*w*4,(dy+1)*w*4);count++;occupied++;
}}
count+=retainedFrames.length+retainedBlank.length;occupied+=retainedFrames.length;
if(actor==='Elliott'&&['Winter','Base'].includes(variant)){
 const fishing=await source('fishing-source.png',1);bounds.fishing=fishing.bounds;
 for(let pose=0;pose<2;pose++){
  const left=pose*32,top=320;let minX=32,minY=32,maxX=-1,maxY=-1;
  for(let y=0;y<32;y++)for(let x=0;x<32;x++)if(original.data[((top+y)*64+left+x)*4+3]){minX=Math.min(minX,x);minY=Math.min(minY,y);maxX=Math.max(maxX,x);maxY=Math.max(maxY,y);}
  assert.ok(maxX>=16,'Native ice-fishing composite must include the prop half.');
  const w=maxX-minX+1,h=maxY-minY+1,data=await cell(fishing,2,pose,w,h);
  for(let y=0;y<32;y++)output.fill(0,((top+y)*64+left)*4,((top+y)*64+left+32)*4);
  for(let y=0;y<h;y++)data.copy(output,((top+minY+y)*64+left+minX)*4,y*w*4,(y+1)*w*4);
  count+=2;occupied+=2;
 }
}
for(const frame of retainedBlank)for(let y=0;y<32;y++)for(let x=0;x<16;x++)assert.equal(output[((Math.floor(frame/4)*32+y)*64+frame%4*16+x)*4+3],0,'Retained blank is occupied.');
if(actor==='Sam'&&beach)for(const frame of [21,22,23])for(let y=0;y<32;y++){
 const start=((Math.floor(frame/4)*32+y)*64+frame%4*16)*4;
 assert.ok(output.subarray(start,start+64).equals(original.data.subarray(start,start+64)),`Native Sam beach blank ${frame} changed.`);
}
if(actor==='Sam'&&beach){assert.equal(count,22);count=24;}
// Sam beach ends partway through an atlas row. Its three untouched cells are
// checked row-by-row above; a contiguous buffer tail would cross frame20.
if(!(actor==='Sam'&&beach))assert.ok(output.subarray(count*16*32*4).equals(original.data.subarray(count*16*32*4)));
await sharp(output,{raw:original.info}).png().toFile(work+'characters-prepared.png');
await sharp(output,{raw:original.info}).resize(384,original.info.height*6,{kernel:'nearest'}).png().toFile(work+'characters-preview.png');
let portraitSlots=0;
if(base&&actor==='Sam'){
 const previous=await sharp(work+'portraits-before.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
 assert.equal(previous.info.width,128);assert.equal(previous.info.height,384);
 const p=await source('portrait-corrections-source.png',1),parts=[],canvas=Buffer.from(previous.data);
 bounds.portraits=p.bounds;portraitSlots=12;
 for(const [index,target] of [9,11].entries()){
  const x=target%2*64,y=Math.floor(target/2)*64;
  for(let dy=0;dy<64;dy++)canvas.fill(0,((y+dy)*128+x)*4,((y+dy)*128+x+64)*4);
  const data=await cell(p,2,index,62,62);
  parts.push({input:await sharp(data,{raw:{width:62,height:62,channels:4}}).png().toBuffer(),left:x+1,top:y+2});
 }
 const prepared=await sharp(canvas,{raw:previous.info}).composite(parts).png().toBuffer();
 const pixels=await sharp(prepared).ensureAlpha().raw().toBuffer();
 for(let index=0;index<12;index++)if(![9,11].includes(index))for(let y=0;y<64;y++){
  const start=((Math.floor(index/2)*64+y)*128+index%2*64)*4;
  assert.ok(pixels.subarray(start,start+256).equals(previous.data.subarray(start,start+256)),`Existing Sam portrait ${index} changed.`);
 }
 await fs.writeFile(work+'portraits-prepared.png',prepared);
}else if(base&&actor==='Harvey'){
 const previous=await sharp(work+'portraits-before.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
 assert.equal(previous.info.width,128);assert.equal(previous.info.height,384);
 const p=await source('medical-portrait-source.png',1);
 // Match the existing close bust framing; the generated reference includes extra lower chest.
 p.bounds=[0,Math.round(p.info.height*.84)];bounds.portraits=p.bounds;portraitSlots=12;
 const data=await cell(p,1,0,62,62),canvas=Buffer.from(previous.data);
 for(let y=64;y<128;y++)canvas.fill(0,(y*128+64)*4,(y*128+128)*4);
 const prepared=await sharp(canvas,{raw:previous.info}).composite([{input:await sharp(data,{raw:{width:62,height:62,channels:4}}).png().toBuffer(),left:65,top:66}]).png().toBuffer();
 const pixels=await sharp(prepared).ensureAlpha().raw().toBuffer();
 for(let index=0;index<12;index++)if(index!==3)for(let y=0;y<64;y++){
  const start=((Math.floor(index/2)*64+y)*128+index%2*64)*4;
  assert.ok(pixels.subarray(start,start+256).equals(previous.data.subarray(start,start+256)),`Existing Harvey portrait ${index} changed.`);
 }
 await fs.writeFile(work+'portraits-prepared.png',prepared);
}else if(base&&actor==='Elliott'){
 const previous=await sharp(work+'portraits-before-correction.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
 assert.equal(previous.info.width,128);assert.equal(previous.info.height,320);
 const p=await source('portrait-correction-source.png',1);bounds.portraits=p.bounds;portraitSlots=10;
 const data=await cell(p,1,0,62,62),canvas=Buffer.from(previous.data);
 for(let y=256;y<320;y++)canvas.fill(0,(y*128+64)*4,(y*128+128)*4);
 const prepared=await sharp(canvas,{raw:previous.info}).composite([{input:await sharp(data,{raw:{width:62,height:62,channels:4}}).png().toBuffer(),left:65,top:258}]).png().toBuffer();
 const pixels=await sharp(prepared).ensureAlpha().raw().toBuffer();
 for(let index=0;index<9;index++)for(let y=0;y<64;y++){
  const start=((Math.floor(index/2)*64+y)*128+index%2*64)*4;
  assert.ok(pixels.subarray(start,start+256).equals(previous.data.subarray(start,start+256)),`Existing Elliott portrait ${index} changed.`);
 }
 await fs.writeFile(work+'portraits-prepared.png',prepared);
}else if(base&&actor==='Alex'){
 const previous=await sharp(work+'portraits-before-correction.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
 assert.equal(previous.info.width,128);assert.equal(previous.info.height,384);
 const p=await source('portrait-corrections-source.png',1),parts=[];bounds.portraits=p.bounds;portraitSlots=12;
 for(let index=0;index<2;index++){
  const data=await cell(p,2,index,62,62);
  parts.push({input:await sharp(data,{raw:{width:62,height:62,channels:4}}).png().toBuffer(),left:index*64+1,top:322});
 }
 const canvas=Buffer.from(previous.data);canvas.fill(0,128*320*4);
 const prepared=await sharp(canvas,{raw:previous.info}).composite(parts).png().toBuffer();
 const pixels=await sharp(prepared).ensureAlpha().raw().toBuffer();
 assert.ok(pixels.subarray(0,128*320*4).equals(previous.data.subarray(0,128*320*4)),'First ten Alex portraits changed.');
 await fs.writeFile(work+'portraits-prepared.png',prepared);
}else if(beach&&actor==='Alex'){
 const mapping=[0,1,2,3,4,5,9,7],p=await source('portraits-source.png',4),parts=[];
 bounds.portraits=p.bounds;portraitSlots=10;
 for(let i=0;i<mapping.length;i++){
  const target=mapping[i],data=await cell(p,2,i,62,62);
  parts.push({input:await sharp(data,{raw:{width:62,height:62,channels:4}}).png().toBuffer(),left:target%2*64+1,top:Math.floor(target/2)*64+2});
 }
 for(const target of [6,8]){
  const rect={left:target%2*64,top:Math.floor(target/2)*64,width:64,height:64};
  const a=await sharp('artifacts/npc-modern/originals/Portraits/Alex_Beach.png').extract(rect).ensureAlpha().raw().toBuffer();
  const b=await sharp('artifacts/npc-modern/originals/Portraits/Alex_Winter.png').extract(rect).ensureAlpha().raw().toBuffer();
  assert.ok(a.equals(b),`Native reused Alex beach portrait ${target} differs.`);
  parts.push({input:await sharp('src/AbigailModern/assets/AlexWinter/portraits.png').extract(rect).png().toBuffer(),left:rect.left,top:rect.top});
 }
 await sharp({create:{width:128,height:320,channels:4,background:'#00000000'}}).composite(parts).png().toFile(work+'portraits-prepared.png');
}else if(base&&actor==='Penny'){
 const mapping=[0,1,2,3,4,5,7,11,12,13],p=await source('portraits-source.png',5),parts=[];
 bounds.portraits=p.bounds;portraitSlots=14;
 for(let i=0;i<mapping.length;i++){
  const target=mapping[i],data=await cell(p,2,i,62,62);
  parts.push({input:await sharp(data,{raw:{width:62,height:62,channels:4}}).png().toBuffer(),left:target%2*64+1,top:Math.floor(target/2)*64+2});
 }
 for(const target of [6,8,9,10]){
  const rect={left:target%2*64,top:Math.floor(target/2)*64,width:64,height:64};
  const a=await sharp('artifacts/npc-modern/originals/Portraits/Penny.png').extract(rect).ensureAlpha().raw().toBuffer();
  const b=await sharp('artifacts/npc-modern/originals/Portraits/Penny_Winter.png').extract(rect).ensureAlpha().raw().toBuffer();
  assert.ok(a.equals(b),`Native reused swimsuit portrait ${target} differs.`);
  parts.push({input:await sharp('src/AbigailModern/assets/PennyWinter/portraits.png').extract(rect).png().toBuffer(),left:rect.left,top:rect.top});
 }
 await sharp({create:{width:128,height:448,channels:4,background:'#00000000'}}).composite(parts).png().toFile(work+'portraits-prepared.png');
}
if(!base&&!(actor==='Alex'&&beach)){const nativePortrait=await sharp(`artifacts/npc-modern/originals/Portraits/${actor}_${variant}.png`).metadata();
assert.equal(nativePortrait.width,128,'Expected two native portrait columns.');
portraitSlots=nativePortrait.height/64*2;
if(actor==='Haley'&&beach){
 const retained='src/AbigailModern/assets/Haley/portraits.png';
 const dimensions=await sharp(retained).metadata();
 assert.equal(dimensions.width,nativePortrait.width);assert.equal(dimensions.height,nativePortrait.height);
 await fs.copyFile(retained,work+'portraits-prepared.png');
}else{
const splitMaruBeach=actor==='Maru'&&beach;
const splitPenny=actor==='Penny'&&['Winter','Beach'].includes(variant),splitAlexWinter=actor==='Alex'&&variant==='Winter',splitHarveyWinter=actor==='Harvey'&&variant==='Winter',splitSamWinter=actor==='Sam'&&['Winter','Beach'].includes(variant),splitPortraits=splitMaruBeach||splitPenny||splitAlexWinter||splitHarveyWinter||splitSamWinter,upperRows=splitPenny?5:splitSamWinter?2:splitAlexWinter||splitHarveyWinter?4:3,lowerStart=upperRows*2;
const p=await source(splitPortraits?'portraits-upper-source.png':'portraits-source.png',splitPortraits?upperRows:nativePortrait.height/64),parts=[];bounds.portraits=p.bounds;
const lowerPortraits=splitPortraits?await source('portraits-lower-source.png',2):null;
if(lowerPortraits)bounds.lowerPortraits=lowerPortraits.bounds;
const middlePortraits=splitSamWinter?await source('portraits-middle-source.png',2):null;
if(middlePortraits)bounds.middlePortraits=middlePortraits.bounds;
for(let i=0;i<portraitSlots;i++){
 const sourceIndex=actor==='Elliott'&&variant==='Winter'&&[4,5].includes(i)?9-i:i;
 let selected=p,index=sourceIndex;
 if(splitSamWinter){if(i>=8){selected=lowerPortraits;index=i-8;}else if(i>=4){selected=middlePortraits;index=i-4;}}
 else if(lowerPortraits&&i>=lowerStart){selected=lowerPortraits;index=i-lowerStart;}
 const data=await cell(selected,2,index,62,62);
 parts.push({input:await sharp(data,{raw:{width:62,height:62,channels:4}}).png().toBuffer(),left:i%2*64+1,top:Math.floor(i/2)*64+2});
}
await sharp({create:{width:128,height:nativePortrait.height,channels:4,background:'#00000000'}}).composite(parts).png().toFile(work+'portraits-prepared.png');
}
}
if(actor==='Alex'&&beach)for(const target of [6,8]){
 const rect={left:target%2*64,top:Math.floor(target/2)*64,width:64,height:64};
 const prepared=await sharp(work+'portraits-prepared.png').extract(rect).ensureAlpha().raw().toBuffer();
 const retained=await sharp('src/AbigailModern/assets/AlexWinter/portraits.png').extract(rect).ensureAlpha().raw().toBuffer();
 assert.ok(prepared.equals(retained),`Final Alex beach portrait ${target} must retain verified winter artwork.`);
}
await fs.writeFile(work+'validation.json',JSON.stringify({ProcessedFrames:count,UpdatedOccupiedFrames:occupied,RetainedModernFrames:retainedFrames,RetainedModernFormalFrames:retainedFormal,RetainedPlaceholders:placeholders,RetainedModernWalkingFrames:base&&!newWalking?16:0,Portraits:portraitSlots,UnchangedLowerPixels:64*(original.info.height-count/4*32),Bounds:bounds,Installed:false,RuntimeVerified:false},null,2));
console.log(`Prepared ${occupied} occupied ${actor} ${variant} poses; ${count-occupied-placeholders} blank frames and ${placeholders} placeholders retained; ${portraitSlots} portraits.`);

