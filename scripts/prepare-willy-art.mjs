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
const specialFrames=[16,17,18,19,24,25,26,27,28,29,30,31,32,33,34,40,41,42];
for(const variant of ['Base','Winter']){
 const work=`artifacts/npc-modern/work/Willy${variant}/`,suffix=variant==='Base'?'':'_Winter',native=await read(`artifacts/npc-modern/originals/Characters/Willy${suffix}.png`),out=Buffer.from(native.data),mapping=[],plan=[];
 for(let frame=0;frame<16;frame++){let cell=({2:0,6:4,10:8,11:9,12:4,13:5,14:4,15:7}[frame]??frame),flip=frame===11;
  if(frame>=4&&frame<8)flip=variant==='Base';if(frame>=12)flip=variant==='Winter';
  if(frame===3){cell=variant==='Base'?2:1;flip=variant==='Winter';}
  plan.push({frame,file:variant==='Base'?'artifacts/npc-modern/work/Willy/modern-characters.png':work+'walk-source.png',cell,cols:4,rows:3,flip});
 }
 for(const[cell,frame]of specialFrames.entries())plan.push({frame,file:work+'special-source.png',cell,cols:4,rows:5,flip:false});
 for(const item of plan){if([33,34].includes(item.frame)){item.file='artifacts/npc-modern/work/WillyBase/raised-character-source.png';item.cols=2;item.rows=2;item.cell=item.frame-33+(variant==='Base'?0:2);}if(variant==='Base'&&item.frame>=28&&item.frame<=31){item.file=work+'smoke-source.png';item.cols=2;item.rows=2;item.cell=item.frame-28;}}
 for(const item of plan){const{frame,file,cell,cols,rows,flip}=item,bx=frame%4*16,by=Math.floor(frame/4)*32;let minX=16,minY=32,maxX=-1,maxY=-1;
  for(let y=0;y<32;y++)for(let x=0;x<16;x++)if(native.data[((by+y)*64+bx+x)*4+3]){minX=Math.min(x,minX);minY=Math.min(y,minY);maxX=Math.max(x,maxX);maxY=Math.max(y,maxY);}
  if([33,34].includes(frame))minY=3;
  const w=maxX-minX+1,h=maxY-minY+1,data=await crop(file,cols,rows,cell,w,h,flip);
  for(let y=0;y<32;y++)out.fill(0,((by+y)*64+bx)*4,((by+y)*64+bx+16)*4);
  for(let y=0;y<h;y++)data.copy(out,((by+minY+y)*64+bx+minX)*4,y*w*4,(y+1)*w*4);
  if(frame>=16&&frame<=19)for(let y=22;y<32;y++)for(let x=7;x<=8;x++){const p=((by+y)*64+bx+x)*4;native.data.copy(out,p,p,p+4);}
  if([33,34].includes(frame))for(let y=0;y<32;y++){const xs=y<3?[0,1]:y<6?[1,2]:y<12?[2]:y<20?[3]:y<27?[4]:[5];for(const x of xs){const p=((by+y)*64+bx+x)*4;native.data.copy(out,p,p,p+4);}}
  mapping.push({...item,targetBounds:{x:minX,y:minY,w,h},nativeRodJoin:frame>=16&&frame<=19?'x7-8 y22-31':[33,34].includes(frame)?'Exact native upper rod path x0-5, y0-31, joining untouched lower overlays37/38':null});
 }
 for(const frame of [20,21,22,23,35,36,37,38,39,43])mapping.push({frame,nativeExactPreserved:true,kind:[35,39,43].includes(frame)?'transparent placeholder':frame===36?'white placeholder':'fishing line/bobber lower overlay'});
 await sharp(out,{raw:native.info}).png().toFile(work+'characters-prepared.png');await sharp(out,{raw:native.info}).resize(384,2112,{kernel:'nearest'}).png().toFile(work+'characters-preview.png');
 const qa={Variant:variant,OccupiedBodySprites:34,PreservedFishingOverlayCells:[20,21,22,23,37,38],WhitePlaceholder:36,TransparentPlaceholders:[35,39,43],Installed:false,RuntimeVerified:false,FullSceneVerified:false,CharacterSha256:crypto.createHash('sha256').update(await fs.readFile(work+'characters-prepared.png')).digest('hex')};
 if(!process.argv.includes('--sprites-only')){if(variant==='Base')await fs.copyFile('src/AbigailModern/assets/Willy/portraits.png',work+'portraits-prepared.png');else{const parts=[];for(let i=0;i<4;i++){const data=await crop(work+'portraits-source.png',2,2,i,62,62);parts.push({input:await sharp(data,{raw:{width:62,height:62,channels:4}}).png().toBuffer(),left:i%2*64+1,top:Math.floor(i/2)*64+2});}await sharp({create:{width:128,height:128,channels:4,background:'#00000000'}}).composite(parts).png().toFile(work+'portraits-prepared.png');}await sharp(work+'portraits-prepared.png').resize(768,768,{kernel:'nearest'}).png().toFile(work+'portraits-preview.png');qa.Portraits=4;qa.PortraitSha256=crypto.createHash('sha256').update(await fs.readFile(work+'portraits-prepared.png')).digest('hex');}
 mapping.sort((a,b)=>a.frame-b.frame);await fs.writeFile(work+'export-mapping.json',JSON.stringify(mapping,null,2)+'\n');await fs.writeFile(work+'validation.json',JSON.stringify(qa,null,2)+'\n');console.log(qa);
}
