import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const read=async file=>sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
const cache=new Map();
async function source(file,cols,rows){if(cache.has(file))return cache.get(file);const s=await read(file);for(let p=0;p<s.data.length;p+=4){const [r,g,b,a]=s.data.subarray(p,p+4);if(a<160||(r>g+100&&b>g+100&&r>b*.88))s.data.fill(0,p,p+4);else s.data[p+3]=255;}
 const boundaries=(axis,n)=>{const size=axis==='x'?s.info.width:s.info.height,other=axis==='x'?s.info.height:s.info.width,arr=[0];for(let k=1;k<n;k++){let best=-1,score=Infinity;for(let v=Math.floor((k-.23)*size/n);v<Math.ceil((k+.23)*size/n);v++){let count=0;for(let z=0;z<other;z++){const x=axis==='x'?v:z,y=axis==='x'?z:v;count+=s.data[(y*s.info.width+x)*4+3]>0?1:0;}const rank=count*size+Math.abs(v-k*size/n);if(rank<score){score=rank;best=v;}}assert.ok(score<size,`${file} ${axis} gutter crosses art`);arr.push(best);}arr.push(size);return arr;};
 s.x=boundaries('x',cols);s.y=boundaries('y',rows);s.png=await sharp(s.data,{raw:s.info}).png().toBuffer();cache.set(file,s);return s;}
async function crop(file,cols,rows,i,w,h,flip=false){const s=await source(file,cols,rows),x=i%cols,y=Math.floor(i/cols);const region=await sharp(s.png).extract({left:s.x[x],top:s.y[y],width:s.x[x+1]-s.x[x],height:s.y[y+1]-s.y[y]}).png().toBuffer();let sh=sharp(region).trim({background:'#00000000',threshold:1});if(flip)sh=sh.flop();const data=await sh.resize(w,h,{fit:'fill'}).ensureAlpha().raw().toBuffer();for(let p=0;p<data.length;p+=4){if(data[p+3]<160)data.fill(0,p,p+4);else data[p+3]=255;}return data;}
for(const variant of ['Base','Winter','Beach']){
 const work=`artifacts/npc-modern/work/Caroline${variant}/`,suffix=variant==='Base'?'':'_'+variant;
 const native=await read(`artifacts/npc-modern/originals/Characters/Caroline${suffix}.png`),out=Buffer.from(native.data),mapping=[];
 const plan=Array.from({length:16},(_,i)=>({frame:i,file:work+'walk-source.png',cell:i,cols:4,rows:4,flip:false}));
 const set=(frame,cell,flip=false,file=work+'walk-source.png',cols=4,rows=4)=>{plan[frame]={frame,file,cell,cols,rows,flip};};
 for(const [to,from] of [[2,0],[6,4],[10,8],[14,12]])set(to,from);
 if(variant==='Base'){
  const file='artifacts/npc-modern/work/CarolineBase/walk-corrections-source.png';
  set(1,0,false,file,2,2);set(3,0,true,file,2,2);set(9,1,false,file,2,2);set(11,1,true,file,2,2);
 }
 if(variant==='Winter'){set(1,3,true);set(3,3);set(5,6);set(7,7);set(11,9,true);set(13,14);set(15,15);}
 if(variant==='Beach'){set(12,4,true);set(13,7,true);set(14,4,true);set(15,5,true);set(11,9,true);}
 if(variant!=='Beach')for(let i=16;i<25;i++)plan.push({frame:i,file:work+'special-source.png',cell:i-16,cols:3,rows:3,flip:[22,23].includes(i)});
 for(const item of plan){const {frame,file,cell,cols,rows,flip}=item;let minX=16,minY=32,maxX=-1,maxY=-1;const bx=frame%4*16,by=Math.floor(frame/4)*32;
  for(let y=0;y<32;y++)for(let x=0;x<16;x++)if(native.data[((by+y)*64+bx+x)*4+3]){minX=Math.min(x,minX);minY=Math.min(y,minY);maxX=Math.max(x,maxX);maxY=Math.max(y,maxY);}
  const w=maxX-minX+1,h=maxY-minY+1,data=await crop(file,cols,rows,cell,w,h,flip);
  for(let y=0;y<32;y++)out.fill(0,((by+y)*64+bx)*4,((by+y)*64+bx+16)*4);
  for(let y=0;y<h;y++)data.copy(out,((by+minY+y)*64+bx+minX)*4,y*w*4,(y+1)*w*4);
  mapping.push({...item,targetBounds:{x:minX,y:minY,w,h}});
 }
 const placeholders=variant==='Beach'?[]:[25,26,27];for(const i of placeholders)for(let y=0;y<32;y++){const p=((Math.floor(i/4)*32+y)*64+i%4*16)*4;assert.ok(out.subarray(p,p+64).equals(native.data.subarray(p,p+64)));}
 await sharp(out,{raw:native.info}).png().toFile(work+'characters-prepared.png');await sharp(out,{raw:native.info}).resize(native.info.width*6,native.info.height*6,{kernel:'nearest'}).png().toFile(work+'characters-preview.png');
 const qa={Variant:variant,OccupiedSprites:plan.length,PreservedPlaceholders:placeholders,Installed:false,RuntimeVerified:false,FullSceneVerified:false,CharacterSha256:crypto.createHash('sha256').update(await fs.readFile(work+'characters-prepared.png')).digest('hex')};
 if(!process.argv.includes('--sprites-only')){
  if(variant==='Base')await fs.copyFile('src/AbigailModern/assets/Caroline/portraits.png',work+'portraits-prepared.png');
  else {const parts=[];for(let i=0;i<4;i++){const data=await crop(work+'portraits-source.png',2,2,i,62,62);parts.push({input:await sharp(data,{raw:{width:62,height:62,channels:4}}).png().toBuffer(),left:i%2*64+1,top:Math.floor(i/2)*64+2});}await sharp({create:{width:128,height:128,channels:4,background:'#00000000'}}).composite(parts).png().toFile(work+'portraits-prepared.png');}
  await sharp(work+'portraits-prepared.png').resize(768,768,{kernel:'nearest'}).png().toFile(work+'portraits-preview.png');qa.Portraits=4;qa.PortraitSha256=crypto.createHash('sha256').update(await fs.readFile(work+'portraits-prepared.png')).digest('hex');
 }
 await fs.writeFile(work+'export-mapping.json',JSON.stringify(mapping,null,2)+'\n');await fs.writeFile(work+'validation.json',JSON.stringify(qa,null,2)+'\n');console.log(qa);
}
