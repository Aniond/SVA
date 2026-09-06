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
for(const category of ['characters','portraits'])await fs.copyFile(`src/AbigailModern/assets/George/${category}.png`,`artifacts/npc-modern/work/GeorgeBase/${category}-prepared.png`);
const work='artifacts/npc-modern/work/GeorgeWinter/';
const native=await read('artifacts/npc-modern/originals/Characters/George_Winter.png'),out=Buffer.from(native.data),mapping=[];
for(let frame=0;frame<22;frame++){
 if([17,18,19].includes(frame))continue;
 const bx=frame%4*16,by=Math.floor(frame/4)*32;let minX=16,minY=32,maxX=-1,maxY=-1;
 for(let y=0;y<32;y++)for(let x=0;x<16;x++)if(native.data[((by+y)*64+bx+x)*4+3]){minX=Math.min(x,minX);minY=Math.min(y,minY);maxX=Math.max(x,maxX);maxY=Math.max(y,maxY);}
 const file=work+(frame<16?'walk-source.png':'special-source.png'),cols=frame<16?4:3,rows=frame<16?4:2,cell=frame<16?frame:frame-16,w=maxX-minX+1,h=maxY-minY+1;
 const data=await crop(file,cols,rows,cell,w,h);
 for(let y=0;y<32;y++)out.fill(0,((by+y)*64+bx)*4,((by+y)*64+bx+16)*4);
 for(let y=0;y<h;y++)data.copy(out,((by+minY+y)*64+bx+minX)*4,y*w*4,(y+1)*w*4);
 mapping.push({frame,file,cell,targetBounds:{x:minX,y:minY,w,h}});
}
// Native states17-19 differ from16 ONLY by the small moving/fading Z glyph.
// Preserve the native glyph and reuse exactly the same modern sleeping figure.
for(const frame of [17,18,19])for(let y=0;y<32;y++)for(let x=0;x<16;x++){
 const a=((128+y)*64+x)*4,b=((128+y)*64+frame%4*16+x)*4;
 const same=native.data.subarray(a,a+4).equals(native.data.subarray(b,b+4));
 if(!same)assert.ok(y<=7,'Native sleeping state includes non-glyph differences');
 (same?out:native.data).copy(out,b,same?a:b,(same?a:b)+4);
}
for(const frame of [17,18,19])mapping.push({frame,sourceFrame:16,overlay:'native exact Z glyph differences only'});
for(const frame of [22,23])for(let y=0;y<32;y++){const p=((Math.floor(frame/4)*32+y)*64+frame%4*16)*4;assert.ok(out.subarray(p,p+64).equals(native.data.subarray(p,p+64)));}
await sharp(out,{raw:native.info}).png().toFile(work+'characters-prepared.png');
if(!process.argv.includes('--sprites-only')){
 const parts=[];for(let i=0;i<4;i++){const data=await crop(work+'portraits-source.png',2,2,i,62,62);parts.push({input:await sharp(data,{raw:{width:62,height:62,channels:4}}).png().toBuffer(),left:i%2*64+1,top:Math.floor(i/2)*64+2});}
 await sharp({create:{width:128,height:128,channels:4,background:'#00000000'}}).composite(parts).png().toFile(work+'portraits-prepared.png');
}
mapping.sort((a,b)=>a.frame-b.frame);await fs.writeFile(work+'export-mapping.json',JSON.stringify(mapping,null,2)+'\n');
for(const variant of ['Base','Winter']){
 const folder=`artifacts/npc-modern/work/George${variant}/`,qa={Variant:variant,OccupiedSprites:22,NativePlaceholders:[22,23],RetainedBase:variant==='Base',NativeSleepGlyphsPreserved:variant==='Winter',Installed:false,RuntimeVerified:false,FullSceneVerified:false};
 for(const category of ['characters',...(!process.argv.includes('--sprites-only')?['portraits']:[])]){const file=folder+category+'-prepared.png';await sharp(file).resize({width:category==='characters'?384:768,kernel:'nearest'}).png().toFile(folder+category+'-preview.png');qa[category+'Sha256']=crypto.createHash('sha256').update(await fs.readFile(file)).digest('hex');}
 if(!process.argv.includes('--sprites-only'))qa.Portraits=4;
 await fs.writeFile(folder+'validation.json',JSON.stringify(qa,null,2)+'\n');console.log(qa);
}
