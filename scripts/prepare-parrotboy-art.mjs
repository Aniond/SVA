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
for(const variant of ['Base','Winter']){
 const work=`artifacts/npc-modern/work/ParrotBoy${variant}/`,suffix=variant==='Base'?'':'_Winter',native=await read(`artifacts/npc-modern/originals/Characters/ParrotBoy${suffix}.png`),out=Buffer.from(native.data),mapping=[],plan=[];
 if(variant==='Base'){
  const cells=[0,4,0,8,1,5,1,7,2,6,2,6,1,5,1,7];for(let frame=0;frame<16;frame++)plan.push({frame,file:'artifacts/npc-modern/work/ParrotBoy/modern-characters.png',cell:cells[frame],cols:4,rows:3,flip:[7,11,12,13,14].includes(frame)});
  for(const item of plan)if([7,15].includes(item.frame)){item.file=work+'stride-source.png';item.cols=1;item.rows=1;item.cell=0;item.flip=item.frame===15;}
  const old='artifacts/npc-modern/work/ParrotBoy/modern-special.png';const s=await source(old,4,3);s.y=[0,512,848,s.info.height];
  for(const frame of[16,18,19,24])plan.push({frame,file:old,cell:frame-16,cols:4,rows:3,flip:false,retainedAcceptedSource:true});
  for(const[frame,cell,flip]of[[17,0,false],[20,2,false],[21,2,true],[22,0,false],[23,3,false]])plan.push({frame,file:work+'corrections-source.png',cell,cols:3,rows:2,flip});
 }else{
  const cells=[0,1,0,1,4,5,4,7,8,9,8,9,4,5,4,7];for(let frame=0;frame<16;frame++)plan.push({frame,file:work+'walk-source.png',cell:cells[frame],cols:4,rows:3,flip:[3,11,12,13,14,15].includes(frame)});
  for(const[cell,frame]of[16,17,18,19,20,21,22,24].entries())plan.push({frame,file:work+'special-source.png',cell,cols:4,rows:2,flip:false});
 }
 for(const item of plan){const{frame,file,cell,cols,rows,flip}=item,bx=frame%4*16,by=Math.floor(frame/4)*32;let minX=16,minY=32,maxX=-1,maxY=-1;for(let y=0;y<32;y++)for(let x=0;x<16;x++)if(native.data[((by+y)*64+bx+x)*4+3]){minX=Math.min(x,minX);minY=Math.min(y,minY);maxX=Math.max(x,maxX);maxY=Math.max(y,maxY);}const w=maxX-minX+1,h=maxY-minY+1,data=await crop(file,cols,rows,cell,w,h,flip);for(let y=0;y<32;y++)out.fill(0,((by+y)*64+bx)*4,((by+y)*64+bx+16)*4);for(let y=0;y<h;y++)data.copy(out,((by+minY+y)*64+bx+minX)*4,y*w*4,(y+1)*w*4);mapping.push({...item,targetBounds:{x:minX,y:minY,w,h}});}
 if(variant==='Winter'){const b=await read('artifacts/npc-modern/work/ParrotBoyBase/characters-prepared.png');for(let y=0;y<32;y++){const p=((5*32+y)*64+48)*4;b.data.copy(out,p,p,p+64);}mapping.push({frame:23,exactNativeBaseMatchReused:true});}
 for(const frame of[25,26,27])mapping.push({frame,nativeWhitePlaceholder:true});
 await sharp(out,{raw:native.info}).png().toFile(work+'characters-prepared.png');await sharp(out,{raw:native.info}).resize(384,1344,{kernel:'nearest'}).png().toFile(work+'characters-preview.png');
 const qa={Variant:variant,OccupiedBodySprites:25,NativeWhitePlaceholders:[25,26,27],BaseSleepReused:variant==='Winter',Installed:false,RuntimeVerified:false,FullSceneVerified:false,CharacterSha256:crypto.createHash('sha256').update(await fs.readFile(work+'characters-prepared.png')).digest('hex')};
 if(!process.argv.includes('--sprites-only')){if(variant==='Base')await fs.copyFile('src/AbigailModern/assets/ParrotBoy/portraits.png',work+'portraits-prepared.png');else{const parts=[];for(let i=0;i<4;i++){const data=await crop(work+'portraits-source.png',2,2,i,62,62);parts.push({input:await sharp(data,{raw:{width:62,height:62,channels:4}}).png().toBuffer(),left:i%2*64+1,top:Math.floor(i/2)*64+2});}await sharp({create:{width:128,height:128,channels:4,background:'#00000000'}}).composite(parts).png().toFile(work+'portraits-prepared.png');}await sharp(work+'portraits-prepared.png').resize(768,768,{kernel:'nearest'}).png().toFile(work+'portraits-preview.png');qa.Portraits=4;qa.PortraitSha256=crypto.createHash('sha256').update(await fs.readFile(work+'portraits-prepared.png')).digest('hex');}
 mapping.sort((a,b)=>a.frame-b.frame);await fs.writeFile(work+'export-mapping.json',JSON.stringify(mapping,null,2)+'\n');await fs.writeFile(work+'validation.json',JSON.stringify(qa,null,2)+'\n');console.log(qa);
}
