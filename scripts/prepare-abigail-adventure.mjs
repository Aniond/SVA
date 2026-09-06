import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/AbigailAdventure/';

async function source(file,rows){
 const s=await sharp(work+file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 for(let p=0;p<s.data.length;p+=4){const r=s.data[p],g=s.data[p+1],b=s.data[p+2];if(s.data[p+3]<160||(r>g+100&&b>g+100&&r>b*.88))s.data.fill(0,p,p+4);else s.data[p+3]=255;}
 const counts=Array(s.info.height).fill(0);
 for(let y=0;y<s.info.height;y++)for(let x=0;x<s.info.width;x++)if(s.data[(y*s.info.width+x)*4+3])counts[y]++;
 const bounds=[0];
 for(let row=1;row<rows;row++){
  const expected=row*s.info.height/rows;let best=Math.round(expected),score=Infinity;
  for(let y=Math.round(expected-s.info.height/rows*.22);y<expected+s.info.height/rows*.22;y++){const rank=counts[y]*s.info.height+Math.abs(y-expected);if(rank<score){best=y;score=rank;}}
  bounds.push(best);
 }
 bounds.push(s.info.height);
 // This sheet has taller standing rows and a shorter seated row.
 // Use its measured empty gutters so the next row never inherits boots.
 if(file==='special-source.png'){
  bounds.splice(0,bounds.length,0,384,725,975,1221,s.info.height);
  for(const y of bounds.slice(1,-1))assert.equal(counts[y],0,'Expected clear special-pose gutter.');
 }
 return {...s,png:await sharp(s.data,{raw:s.info}).png().toBuffer(),bounds};
}
async function cell(s,cols,index,width,height){
 const col=index%cols,row=Math.floor(index/cols),left=Math.round(col*s.info.width/cols),right=Math.round((col+1)*s.info.width/cols);
 const crop=await sharp(s.png).extract({left,top:s.bounds[row],width:right-left,height:s.bounds[row+1]-s.bounds[row]}).png().toBuffer();
 const data=await sharp(crop).trim({background:'#00000000',threshold:1}).resize(width,height,{fit:'fill'}).ensureAlpha().raw().toBuffer();
 for(let p=0;p<data.length;p+=4){if(data[p+3]<160)data.fill(0,p,p+4);else data[p+3]=255;}
 assert.ok(data.some((v,i)=>i%4===3&&v>0),'Empty generated cell '+index);
 return data;
}
const original=await sharp('artifacts/npc-modern/originals/Characters/Abigail.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
const baseline=await sharp('src/AbigailModern/assets/characters.png').ensureAlpha().raw().toBuffer();
const output=Buffer.from(baseline),updated=new Set(),bounds={};
const standing=await source('standing-source.png',1);
const groups=[['walk-source.png',4,4,Array.from({length:16},(_,i)=>i)],['special-source.png',4,5,Array.from({length:20},(_,i)=>i+16)],['final-source.png',3,2,[39,48,49,50,51,53]]];
for(const [file,cols,rows,frames] of groups){
 const s=await source(file,rows);bounds[file]=s.bounds;
 for(let i=0;i<frames.length;i++){
  const frame=frames[i],x=frame%4*16,y=Math.floor(frame/4)*32;let minX=16,minY=32,maxX=-1,maxY=-1;
  for(let dy=0;dy<32;dy++)for(let dx=0;dx<16;dx++)if(original.data[((y+dy)*64+x+dx)*4+3]){minX=Math.min(minX,dx);maxX=Math.max(maxX,dx);minY=Math.min(minY,dy);maxY=Math.max(maxY,dy);}
  assert.ok(maxX>=0,'Unexpected empty native pose '+frame);
  const w=maxX-minX+1,h=maxY-minY+1;let data=frame>=28&&frame<=32?await cell(standing,5,frame-28,w,h):await cell(s,cols,i,w,h);
  if(frame===28||frame===29)data=await sharp(data,{raw:{width:w,height:h,channels:4}}).flop().raw().toBuffer();
  for(let dy=0;dy<32;dy++)output.fill(0,((y+dy)*64+x)*4,((y+dy)*64+x+16)*4);
  for(let dy=0;dy<h;dy++)data.copy(output,((y+minY+dy)*64+x+minX)*4,dy*w*4,(dy+1)*w*4);
  assert.ok(!updated.has(frame));updated.add(frame);
 }
}
const formal=[36,37,38,40,41,42,43,44,45,46,47,52];
for(const frame of [...formal,54,55])for(let dy=0;dy<32;dy++){
 const p=((Math.floor(frame/4)*32+dy)*64+frame%4*16)*4;
 assert.ok(output.subarray(p,p+64).equals(baseline.subarray(p,p+64)),'Retained costume/blank changed '+frame);
}
assert.equal(updated.size,42);
await sharp(output,{raw:original.info}).png().toFile(work+'characters-prepared.png');
await sharp(output,{raw:original.info}).resize(384,2688,{kernel:'nearest'}).png().toFile(work+'characters-preview.png');
const portraits=await source('portraits-source.png',5),parts=[];bounds.portraits=portraits.bounds;
for(let i=0;i<10;i++)parts.push({input:await sharp(await cell(portraits,2,i,62,62),{raw:{width:62,height:62,channels:4}}).png().toBuffer(),left:i%2*64+1,top:Math.floor(i/2)*64+2});
await sharp({create:{width:128,height:320,channels:4,background:'#00000000'}}).composite(parts).png().toFile(work+'portraits-prepared.png');
await fs.writeFile(work+'validation.json',JSON.stringify({AdventureFrames:42,RetainedModernFormalFrames:formal,BlankFrames:[54,55],TotalOccupiedFrames:54,Portraits:10,Bounds:bounds,Installed:false,RuntimeVerified:false},null,2));
console.log('Prepared 42 adventure poses, retained 12 modern formal poses and 2 blank cells, plus 10 portraits.');
