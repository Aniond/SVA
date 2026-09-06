import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const variant=process.argv[2]??'Winter',dir=`artifacts/npc-modern/work/Robin${variant}/`,suffix=variant==='Base'?'':'_'+variant;
const raw=async p=>sharp(p).ensureAlpha().raw().toBuffer({resolveWithObject:true});
const original=await raw(`artifacts/npc-modern/originals/Characters/Robin${suffix}.png`),output=Buffer.from(original.data),mapping=[];
const cache=new Map();
async function source(file,cols,rows){
 const p=dir+file;if(cache.has(p))return cache.get(p);
 const s=await raw(p);for(let i=0;i<s.data.length;i+=4){const [r,g,b,a]=s.data.subarray(i,i+4);if(a<160||((file==='special-source.png'&&i/4/s.info.width>s.info.height*.9)?(r>220&&b>220&&g<90):(r>g+90&&b>g+90&&r>b*.8)))s.data.fill(0,i,i+4);else s.data[i+3]=255;}
 const split=(n,length,axis)=>{const b=[0];for(let k=1;k<n;k++){const ideal=k*length/n;let best=-1,rank=Infinity;for(let q=Math.floor(ideal-length/n*.27);q<ideal+length/n*.27;q++){let count=0;for(let t=0;t<(axis==='x'?s.info.height:s.info.width);t++)if(s.data[((axis==='x'?t:q)*s.info.width+(axis==='x'?q:t))*4+3])count++;const score=count*length+Math.abs(q-ideal);if(score<rank){best=q;rank=score;}}b.push(best);}b.push(length);return b;};
 s.x=split(cols,s.info.width,'x');s.y=split(rows,s.info.height,'y');s.png=await sharp(s.data,{raw:s.info}).png().toBuffer();cache.set(p,s);return s;
}
async function cell(file,cols,rows,index,w,h,flip=false){const s=await source(file,cols,rows),x=index%cols,y=Math.floor(index/cols);const cropped=await sharp(s.png).extract({left:s.x[x],top:s.y[y],width:s.x[x+1]-s.x[x],height:s.y[y+1]-s.y[y]}).png().toBuffer();let a=sharp(cropped).trim({background:'#00000000',threshold:1});if(flip)a=a.flop();const b=await a.resize(w,h,{fit:'fill'}).ensureAlpha().raw().toBuffer();for(let p=0;p<b.length;p+=4)if(b[p+3]<160)b.fill(0,p,p+4);else b[p+3]=255;return b;}
let plan=[];
if(variant==='Winter')plan=[['walk-source.png',4,4,0,16],['special-source.png',4,4,16,16],['tail-source.png',3,2,32,6]];
if(variant==='Base')plan=[['walk-source.png',4,4,0,16],['special-source.png',4,4,16,16],['tail-source.png',3,2,32,6]];
if(variant==='Beach')plan=[['walk-source.png',4,4,0,16]];
for(const [file,cols,rows,start,count]of plan)for(let i=0;i<count;i++){
 const f=start+i,fx=f%4*16,fy=Math.floor(f/4)*32;let minX=16,minY=32,maxX=-1,maxY=-1;for(let y=0;y<32;y++)for(let x=0;x<16;x++)if(original.data[((fy+y)*64+fx+x)*4+3]){minX=Math.min(x,minX);minY=Math.min(y,minY);maxX=Math.max(x,maxX);maxY=Math.max(y,maxY);}
 if(maxX<0)continue;const w=maxX-minX+1,h=maxY-minY+1;let sourceIndex=i,flip=false,sourceFile=file,sourceCols=cols,sourceRows=rows;
 if(f===11){sourceIndex=9;flip=true;}
 const overrides=JSON.parse(await fs.readFile(dir+'overrides.json','utf8').catch(()=>'{}'));
 if(overrides[f]){const o=overrides[f];sourceFile=o.file;sourceCols=o.cols;sourceRows=o.rows;sourceIndex=o.index;flip=o.flip??false;}
 const data=await cell(sourceFile,sourceCols,sourceRows,sourceIndex,w,h,flip);
 for(let y=0;y<32;y++)output.fill(0,((fy+y)*64+fx)*4,((fy+y)*64+fx+16)*4);
 for(let y=0;y<h;y++)data.copy(output,((fy+minY+y)*64+fx+minX)*4,y*w*4,(y+1)*w*4);
 mapping.push({frame:f,file:sourceFile,index:sourceIndex,flip,nativeBounds:{x:minX,y:minY,width:w,height:h}});
}
const limit=original.info.height/32*4,placeholders=[];for(let f=38;f<limit;f++){for(let y=0;y<32;y++){const p=((Math.floor(f/4)*32+y)*64+f%4*16)*4;assert.ok(output.subarray(p,p+64).equals(original.data.subarray(p,p+64)));}placeholders.push(f);}
await sharp(output,{raw:original.info}).png().toFile(dir+'characters-prepared.png');
await sharp(output,{raw:original.info}).resize({width:512,kernel:'nearest'}).png().toFile(dir+'characters-preview.png');
for(let start=0;start<limit;start+=16)await sharp(output,{raw:original.info}).extract({left:0,top:Math.floor(start/4)*32,width:64,height:Math.min(128,original.info.height-Math.floor(start/4)*32)}).resize({width:512,kernel:'nearest'}).png().toFile(dir+`characters-preview-${start}.png`);
const qa={variant,width:original.info.width,height:original.info.height,modernizedFrames:mapping.length,placeholders,preservedNativeBounds:true,sha256:crypto.createHash('sha256').update(await fs.readFile(dir+'characters-prepared.png')).digest('hex'),sourceGutters:Object.fromEntries([...cache].map(([p,s])=>[p,{x:s.x,y:s.y}])),fullScenePlayback:false,runtimeVerified:false};
await fs.writeFile(dir+'mapping.json',JSON.stringify(mapping,null,2));await fs.writeFile(dir+'qa.json',JSON.stringify(qa,null,2));console.log(JSON.stringify(qa,null,2));
if(process.argv.includes('--portraits')){
 const native=await raw(`artifacts/npc-modern/originals/Portraits/Robin${suffix}.png`),pout=Buffer.alloc(native.data.length),pm=[];
 for(let index=0;index<native.info.height/64*2;index++){
  const file=index<4?'portraits-0-3-source.png':index<8?'portraits-4-7-source.png':'portraits-8-9-source.png',data=await cell(file,2,2,index%4,64,64);
  for(let y=0;y<64;y++)data.copy(pout,((Math.floor(index/2)*64+y)*128+index%2*64)*4,y*256,(y+1)*256);pm.push({index,file,cell:index%4});
 }
 await sharp(pout,{raw:native.info}).png().toFile(dir+'portraits-prepared.png');await sharp(pout,{raw:native.info}).resize({width:768,kernel:'nearest'}).png().toFile(dir+'portraits-preview.png');
 await fs.writeFile(dir+'portrait-mapping.json',JSON.stringify(pm,null,2));
 qa.portraitWidth=native.info.width;qa.portraitHeight=native.info.height;qa.portraitCount=native.info.height/64*2;qa.portraitSha256=crypto.createHash('sha256').update(await fs.readFile(dir+'portraits-prepared.png')).digest('hex');await fs.writeFile(dir+'qa.json',JSON.stringify(qa,null,2));
}

