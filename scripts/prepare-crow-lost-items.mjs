import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import { createRequire } from 'node:module';
const sharp = createRequire('C:/Users/david/SDV/.tools/abigail-art/package.json')('sharp');
const work = 'artifacts/npc-modern/work/CrowLostItems/';
const nativeFile = 'artifacts/npc-modern/originals/Characters/Crow.png';
const native = await sharp(nativeFile).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
const art = await sharp(work + 'sprites-source.png').resize(128, 80, { fit: 'fill' }).ensureAlpha().raw().toBuffer();
const unique = [0, 4, 14, 15, 16, 20, 31];
const slots = [0,0,0,0,4,4,4,4,0,0,0,0,4,4,14,15,16,16,16,16,20,20,20,20,16,16,16,16,20,20,20,31];
const bagRows = {21:[10,12],22:[10,13],23:[10,13],24:[10,13],25:[9,14],26:[8,15],27:[8,15],28:[8,15],29:[8,14]};
const prepared = new Map(), masks = new Map();
let neutralFallbackPixels = 0;
for (let k = 0; k < unique.length; k++) {
  const id = unique[k], cell = Buffer.alloc(16*32*4), mask = Buffer.alloc(cell.length);
  for (let y=0;y<32;y++) for(let x=0;x<16;x++) {
    const p=(y*16+x)*4, n=(y*512+id*16+x)*4;
    native.data.copy(cell,p,n,n+4);
    const bag=bagRows[y];
    if(native.data[n+3]!==255 || (bag&&x>=bag[0]&&x<=bag[1]))continue;
    assert(y>=8);
    mask.fill(255,p,p+4);
    const q=((Math.floor(k/4)*40+8+y-8)*128+k%4*32+8+x)*4;
    const rgb=[...art.subarray(q,q+3)];
    if(Math.min(...rgb)>85 && Math.max(...rgb)-Math.min(...rgb)<25){neutralFallbackPixels++;continue;}
    art.copy(cell,p,q,q+3);
    // Retain the native bright eye pixels and their exact positions; downsampling
    // the generated glow otherwise makes these tiny identifying marks too dim.
    if(native.data[n]===196 && native.data[n+1]===6 && native.data[n+2]===111)
      native.data.copy(cell,p,n,n+3);
  }
  prepared.set(id,cell);masks.set(id,mask);
  await sharp(cell,{raw:{width:16,height:32,channels:4}}).png().toFile(work+`prepared-frame-${id}.png`);
}
const output=Buffer.from(native.data), bodyMask=Buffer.alloc(output.length);let changed=0, preserved=0;
for(let i=0;i<32;i++)for(let y=0;y<32;y++){
  const start=(y*512+i*16)*4;
  prepared.get(slots[i]).copy(output,start,y*16*4,(y+1)*16*4);
  masks.get(slots[i]).copy(bodyMask,start,y*16*4,(y+1)*16*4);
}
let satchelColorPixelsVerified=0,nativeBrightEyePixelsVerified=0;
const brownPalette=new Set(['61,37,28','84,55,44','130,85,68']);
for(let p=0;p<output.length;p+=4){
  assert.equal(output[p+3],native.data[p+3]);
  if(brownPalette.has([...native.data.subarray(p,p+3)].join(','))){assert(output.subarray(p,p+4).equals(native.data.subarray(p,p+4)));satchelColorPixelsVerified++;}
  if(native.data[p]===196&&native.data[p+1]===6&&native.data[p+2]===111){assert(output.subarray(p,p+4).equals(native.data.subarray(p,p+4)));nativeBrightEyePixelsVerified++;}
  if(!bodyMask[p+3]){assert(output.subarray(p,p+4).equals(native.data.subarray(p,p+4)));preserved++;}
  if(!output.subarray(p,p+4).equals(native.data.subarray(p,p+4)))changed++;
}
for(let i=0;i<32;i++)for(let y=0;y<32;y++){
  const a=(y*512+i*16)*4,b=(y*512+slots[i]*16)*4;
  assert(output.subarray(a,a+64).equals(output.subarray(b,b+64)));
}
await sharp(output,{raw:native.info}).png().toFile(work+'characters-prepared.png');
await sharp(bodyMask,{raw:native.info}).png().toFile(work+'characters-body-mask.png');
const contacts=[];
for(let i=0;i<32;i++)contacts.push({input:await sharp(output,{raw:native.info}).extract({left:i*16,top:0,width:16,height:32}).png().toBuffer(),left:i%8*24,top:Math.floor(i/8)*40});
const contact=await sharp({create:{width:192,height:160,channels:4,background:'#8d8d88'}}).composite(contacts).png().toBuffer();
await sharp(contact).resize(1152,960,{kernel:'nearest'}).toFile(work+'prepared-32-pose-preview.png');
const uniqueLayers=[];
for(let k=0;k<unique.length;k++)uniqueLayers.push({input:await sharp(prepared.get(unique[k]),{raw:{width:16,height:32,channels:4}}).png().toBuffer(),left:k*24,top:0});
const strip=await sharp({create:{width:168,height:32,channels:4,background:'#8d8d88'}}).composite(uniqueLayers).png().toBuffer();
await sharp(strip).resize(1008,192,{kernel:'nearest'}).toFile(work+'prepared-seven-unique-preview.png');
const qa={asset:'Characters/Crow',dimensions:[512,32],frameSize:[16,32],occupiedSlots:32,uniqueDrawings:7,slots,intervalMs:100,loopDurationMs:3200,changedPixels:changed,outsideMaskPixelsPreserved:preserved,totalExactPixels:512*32-changed,neutralFallbackPixels,allNativeAlphaExact:true,allSatchelsAndGroundShadowsExact:true,nativeRepeatedSlotsExact:true,sourceId:'aeafd580-1f52-4613-8530-7b43ccf63e56',sha256:crypto.createHash('sha256').update(await fs.readFile(work+'characters-prepared.png')).digest('hex'),runtimeVerified:false,fullSceneVerified:false,installed:false};
qa.measuredUniqueDrawings=new Set([...prepared.values()].map(b=>crypto.createHash('sha256').update(b).digest('hex'))).size;
qa.satchelColorPixelsVerified=satchelColorPixelsVerified;qa.nativeBrightEyePixelsVerified=nativeBrightEyePixelsVerified;
assert.equal(qa.measuredUniqueDrawings,7);
qa.brightEyePixelsPerUniquePose=unique.map(id=>({frame:id,count:[...Array(16*32).keys()].filter(i=>{let p=i*4,b=prepared.get(id);return b[p]>75&&b[p+1]<50&&b[p+2]>b[p]*0.3&&b[p+2]<b[p]*1.5}).length}));
assert.equal(qa.brightEyePixelsPerUniquePose.find(x=>x.frame===14).count,0);
await fs.writeFile(work+'sprite-static-qa.json',JSON.stringify(qa,null,2));
await fs.writeFile(work+'sprite-mapping.json',JSON.stringify(slots.map((u,i)=>({frame:i,rectangle:[16*i,0,16,32],sourceUniquePose:u,topTileIndex:i,bottomTileIndex:32+i,durationMs:100})),null,2));
console.log(qa);
const portraitInputs = ['portraits-batch-1-source.png','portraits-batch-2-source.png'];
try { await fs.access(work+portraitInputs[1]); } catch { process.exit(0); }
const portraitLayers=[],portraitMappings=[];
const expressions=['neutral attentive','welcoming','pleased','thoughtful','concerned','surprised'];
for(let i=0;i<6;i++){
  const batch=i<4?0:1,cell=i<4?i:i-4;
  const r=await sharp(work+portraitInputs[batch]).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  for(let p=0;p<r.data.length;p+=4)
    if(r.data[p+1]>60&&r.data[p+1]>r.data[p]+25&&r.data[p+1]>r.data[p+2]+25)r.data.fill(0,p,p+4);
  const cw=Math.floor(r.info.width/2),ch=Math.floor(r.info.height/2);
  const crop={left:cell%2*cw,top:Math.floor(cell/2)*ch,width:cw,height:ch};
  const cut=await sharp(r.data,{raw:r.info}).extract(crop).png().toBuffer();
  const resized=await sharp(cut).trim({background:'#00000000',threshold:1}).resize(62,62,{fit:'contain',position:'bottom',background:'#00000000'}).ensureAlpha().raw().toBuffer();
  for(let p=0;p<resized.length;p+=4){if(resized[p+3]<128)resized.fill(0,p,p+4);else resized[p+3]=255;}
  const png=await sharp(resized,{raw:{width:62,height:62,channels:4}}).png().toBuffer();
  portraitLayers.push({input:png,left:i%2*64+1,top:Math.floor(i/2)*64+1});
  portraitMappings.push({cell:i,expression:expressions[i],destination:[i%2*64,Math.floor(i/2)*64,64,64],source:portraitInputs[batch],sourceCrop:crop});
}
const portrait=await sharp({create:{width:128,height:192,channels:4,background:'#00000000'}}).composite(portraitLayers).png().toBuffer();
await fs.writeFile(work+'portraits-prepared.png',portrait);
for(const[name,bg]of[['dark','#383838'],['light','#e7ddc8']])await sharp(portrait).flatten({background:bg}).resize(512,768,{kernel:'nearest'}).toFile(work+`portraits-${name}-preview.png`);
const pr=await sharp(portrait).ensureAlpha().raw().toBuffer(),portraitQa={dimensions:[128,192],occupiedCells:6,expressions,greenMattePixels:0,nonBinaryAlphaPixels:0,allCellBordersClear:true,sha256:crypto.createHash('sha256').update(portrait).digest('hex'),runtimeVerified:false};
for(let y=0;y<192;y++)for(let x=0;x<128;x++){
  const p=(y*128+x)*4,a=pr[p+3];
  if(a!==0&&a!==255)portraitQa.nonBinaryAlphaPixels++;
  if(a&&pr[p+1]>60&&pr[p+1]>pr[p]+25&&pr[p+1]>pr[p+2]+25)portraitQa.greenMattePixels++;
  if((x%64===0||x%64===63||y%64===0||y%64===63)&&a)portraitQa.allCellBordersClear=false;
}
assert.equal(portraitQa.greenMattePixels,0);assert.equal(portraitQa.nonBinaryAlphaPixels,0);assert(portraitQa.allCellBordersClear);
await fs.writeFile(work+'portrait-mapping.json',JSON.stringify(portraitMappings,null,2));
await fs.writeFile(work+'portrait-static-qa.json',JSON.stringify(portraitQa,null,2));
console.log(portraitQa);
