import fs from 'node:fs/promises';
import {createRequire} from 'node:module';
import assert from 'node:assert/strict';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/Marcello/';
const file='src/AbigailModern/assets/Marcello/stall.png';
const original=await sharp('artifacts/npc-modern/originals/LooseSprites/Cursors_1_6.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
const registryFile='src/AbigailModern/artwork.json';
let registry=JSON.parse(await fs.readFile(registryFile,'utf8'));
const existing=registry.find(a=>a.Name==='LooseSprites/Cursors_1_6');
assert.ok(!existing||existing.File==='assets/Marcello/stall.png','Merge other shared patches explicitly');
const baseline=existing ? await sharp(file).ensureAlpha().raw().toBuffer() : original.data;
const pixels=Buffer.from(baseline);
const patches=[['body',54,474,17,24],['blink',110,488,17,24],['hands',127,508,68,4]];
for(const [name,x,y,w,h] of patches){
  for(let row=0;row<h;row++){const start=((y+row)*original.info.width+x)*4;original.data.copy(pixels,start,start,start+w*4);}
  let source=sharp(work+`modern-stall-${name}.png`);
  if(name==='hands')source=source.trim({background:'#00000000',threshold:1});
  const resized=await source.resize(w,h,{fit:'fill'}).ensureAlpha().raw().toBuffer();
  for(let dy=0;dy<h;dy++)for(let dx=0;dx<w;dx++){
    const a=(dy*w+dx)*4,b=((y+dy)*original.info.width+x+dx)*4;
    // Keep the original opacity stencil for this overlay, including its transparent lower rows.
    if(original.data[b+3] && resized[a+3]>=160){resized.copy(pixels,b,a,a+3);pixels[b+3]=original.data[b+3];}
  }
}
let untouched=0;
for(let y=0;y<original.info.height;y++)for(let x=0;x<original.info.width;x++){
  if(patches.some(([,px,py,w,h])=>x>=px&&x<px+w&&y>=py&&y<py+h))continue;
  const p=(y*original.info.width+x)*4;
  assert.ok(pixels.subarray(p,p+4).equals(baseline.subarray(p,p+4)));untouched++;
}
await sharp(pixels,{raw:original.info}).png().toFile(file);
const ownAreas=patches.map(([,X,Y,Width,Height])=>({X,Y,Width,Height}));
const otherAreas=(existing?.PatchAreas??(existing?.PatchArea?[existing.PatchArea]:[])).filter(a=>!ownAreas.some(b=>JSON.stringify(a)===JSON.stringify(b)));
registry=registry.filter(a=>a.Name!=='LooseSprites/Cursors_1_6');
registry.push({Name:'LooseSprites/Cursors_1_6',File:'assets/Marcello/stall.png',Width:original.info.width,Height:original.info.height,PatchAreas:[...ownAreas,...otherAreas]});
await fs.writeFile(registryFile,JSON.stringify(registry,null,2));
await sharp(file).extract({left:0,top:433,width:110,height:79}).resize(880,632,{kernel:'nearest'}).png().toFile(work+'prepared-stall.png');
const base=await sharp(file).extract({left:0,top:433,width:110,height:79}).png().toBuffer();
const blink=await sharp(file).extract({left:110,top:488,width:17,height:24}).png().toBuffer();
const frames=[];
for(let i=0;i<8;i++){
  const hands=await sharp(file).extract({left:127+(i%4)*17,top:508,width:17,height:4}).png().toBuffer();
  const overlays=i>=4?[{input:blink,left:54,top:41}]:[];
  overlays.push({input:hands,left:54,top:61});
  frames.push({input:await sharp(base).composite(overlays).png().toBuffer(),left:(i%4)*110,top:Math.floor(i/4)*79});
}
await sharp({create:{width:440,height:158,channels:4,background:'#00000000'}}).composite(frames).png().toFile(work+'stall-animation-contact.png');
await fs.writeFile(work+'stall-validation.json',JSON.stringify({patches,untouchedPixelsVerified:untouched,originalOpacityPreserved:true,sceneAnimationVerified:false},null,2));
console.log(`${untouched} unrelated shared-sheet pixels preserved; 3 regions prepared.`);
