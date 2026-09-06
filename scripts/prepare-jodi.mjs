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
 const work=`artifacts/npc-modern/work/Jodi${variant}/`,suffix=variant==='Base'?'':'_'+variant,native=await read(`artifacts/npc-modern/originals/Characters/Jodi${suffix}.png`),out=Buffer.from(native.data),mapping=[];
 const plan=variant==='Base'?[]:Array.from({length:16},(_,i)=>({frame:i,file:work+'walk-source.png',cell:i,cols:4,rows:4,flip:false}));
 if(variant==='Base'){for(let i=0;i<16;i++){const cell=i<12?({2:0,6:4,10:8,11:9}[i]??i):({12:4,13:7,14:4,15:5}[i]);plan.push({frame:i,file:'artifacts/npc-modern/work/Jodi/modern-characters.png',cell,cols:4,rows:3,flip:i>=12||i===11});}}
 else {
  const set=(frame,cell,flip=false,file=work+'walk-source.png',cols=4,rows=4)=>{plan[frame]={frame,file,cell,cols,rows,flip};};
  for(const [to,from] of [[2,0],[6,4],[10,8],[14,12]])set(to,from);
  set(11,9,true);
  const stride='artifacts/npc-modern/work/JodiWinter/stride-source.png',cell=variant==='Winter'?0:1;
  set(7,cell,false,stride,2,1);set(13,cell,true,stride,2,1);set(15,5,true);
 }
 const actual=variant==='Base'?26:variant==='Winter'?25:16;
 if(variant!=='Beach')for(let i=16;i<actual;i++)plan.push({frame:i,file:work+'special-source.png',cell:i-16,cols:3,rows:variant==='Base'?4:3,flip:false});
 for(const item of plan){const{frame,file,cell,cols,rows,flip}=item,bx=frame%4*16,by=Math.floor(frame/4)*32;let minX=16,minY=32,maxX=-1,maxY=-1;
  for(let y=0;y<32;y++)for(let x=0;x<16;x++)if(native.data[((by+y)*64+bx+x)*4+3]){minX=Math.min(x,minX);minY=Math.min(y,minY);maxX=Math.max(x,maxX);maxY=Math.max(y,maxY);}
  const w=maxX-minX+1,h=maxY-minY+1,data=await crop(file,cols,rows,cell,w,h,flip);
  for(let y=0;y<32;y++)out.fill(0,((by+y)*64+bx)*4,((by+y)*64+bx+16)*4);
  for(let y=0;y<h;y++)data.copy(out,((by+minY+y)*64+bx+minX)*4,y*w*4,(y+1)*w*4);
  mapping.push({...item,targetBounds:{x:minX,y:minY,w,h}});
 }
 const placeholders=variant==='Base'?[26,27]:variant==='Winter'?[25,26,27]:[];
 for(const i of placeholders)for(let y=0;y<32;y++){const p=((Math.floor(i/4)*32+y)*64+i%4*16)*4;assert.ok(out.subarray(p,p+64).equals(native.data.subarray(p,p+64)));}
 await sharp(out,{raw:native.info}).png().toFile(work+'characters-prepared.png');await sharp(out,{raw:native.info}).resize(native.info.width*6,native.info.height*6,{kernel:'nearest'}).png().toFile(work+'characters-preview.png');
 const qa={Variant:variant,OccupiedSprites:actual,PreparedSprites:plan.length,RetainedModernBaseWalkingSourceRecropped:variant==='Base'?16:0,NativePlaceholders:placeholders,Installed:false,RuntimeVerified:false,FullSceneVerified:false,CharacterSha256:crypto.createHash('sha256').update(await fs.readFile(work+'characters-prepared.png')).digest('hex')};
 if(!process.argv.includes('--sprites-only')){
  if(variant==='Base')await fs.copyFile('src/AbigailModern/assets/Jodi/portraits.png',work+'portraits-prepared.png');
  else {const np=await read(`artifacts/npc-modern/originals/Portraits/Jodi${suffix}.png`),parts=[];for(let i=0;i<(variant==='Winter'?5:4);i++){const file=work+(variant==='Winter'?(i<4?'portraits-a-source.png':'portraits-b-source.png'):'portraits-source.png'),data=await crop(file,2,2,i<4?i:0,62,62);parts.push({input:await sharp(data,{raw:{width:62,height:62,channels:4}}).png().toBuffer(),left:i%2*64+1,top:Math.floor(i/2)*64+2});}
   if(variant==='Winter')parts.push({input:await sharp(`artifacts/npc-modern/originals/Portraits/Jodi_Winter.png`).extract({left:64,top:128,width:64,height:64}).png().toBuffer(),left:64,top:128});
   await sharp({create:{width:np.info.width,height:np.info.height,channels:4,background:'#00000000'}}).composite(parts).png().toFile(work+'portraits-prepared.png');
  }
  await sharp(work+'portraits-prepared.png').resize({width:768,kernel:'nearest'}).png().toFile(work+'portraits-preview.png');qa.Portraits=variant==='Beach'?4:5;qa.NativePortraitPlaceholders=variant==='Beach'?[]:[5];qa.PortraitSha256=crypto.createHash('sha256').update(await fs.readFile(work+'portraits-prepared.png')).digest('hex');
 }
 mapping.sort((a,b)=>a.frame-b.frame);await fs.writeFile(work+'export-mapping.json',JSON.stringify(mapping,null,2)+'\n');await fs.writeFile(work+'validation.json',JSON.stringify(qa,null,2)+'\n');console.log(qa);
}
