import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/SamJojaMart/';
const originals='artifacts/npc-modern/originals/';
const read=async path=>sharp(path).ensureAlpha().raw().toBuffer({resolveWithObject:true});
const native=await read(originals+'Characters/Sam_JojaMart.png');
const baseNative=await read(originals+'Characters/Sam.png');
const base=await read('artifacts/npc-modern/work/SamBase/characters-prepared.png');
assert.equal(base.info.width,64);assert.equal(base.info.height,448);
const output=Buffer.from(native.data),mapping=[];
const reusable=JSON.parse(await fs.readFile(work+'native-base-matches.json','utf8')).filter(i=>i!==55);
function cellBytes(s,i,w=16,h=32,cols=4){const b=Buffer.alloc(w*h*4);for(let y=0;y<h;y++)s.data.copy(b,y*w*4,((Math.floor(i/cols)*h+y)*s.info.width+i%cols*w)*4,((Math.floor(i/cols)*h+y)*s.info.width+i%cols*w+w)*4);return b;}
function paste(s,i,data,w=16,h=32,cols=4){for(let y=0;y<h;y++)data.copy(s.data,((Math.floor(i/cols)*h+y)*s.info.width+i%cols*w)*4,y*w*4,(y+1)*w*4);}
for(const i of reusable){assert.ok(cellBytes(native,i).equals(cellBytes(baseNative,i)),`Native reuse ${i}`);paste({data:output,info:native.info},i,cellBytes(base,i));mapping.push({frame:i,source:'SamBase/characters-prepared.png',sourceFrame:i});}
const cache=new Map();
async function source(file,cols,rows){if(cache.has(file))return cache.get(file);const s=await read(work+file);for(let p=0;p<s.data.length;p+=4){const [r,g,b,a]=s.data.subarray(p,p+4);if(a<160||(r>g+100&&b>g+100&&r>b*.88))s.data.fill(0,p,p+4);else s.data[p+3]=255;}
 const boundaries=(axis,n)=>{const size=axis==='x'?s.info.width:s.info.height,other=axis==='x'?s.info.height:s.info.width,arr=[0];for(let k=1;k<n;k++){let best=-1,score=Infinity;for(let v=Math.floor((k-.18)*size/n);v<Math.ceil((k+.18)*size/n);v++){let count=0;for(let z=0;z<other;z++){const x=axis==='x'?v:z,y=axis==='x'?z:v;count+=s.data[(y*s.info.width+x)*4+3]>0?1:0;}const rank=count*size+Math.abs(v-k*size/n);if(rank<score){score=rank;best=v;}}assert.ok(score<size,`${file} ${axis} gutter crosses art`);arr.push(best);}arr.push(size);return arr;};
 s.x=boundaries('x',cols);s.y=boundaries('y',rows);s.png=await sharp(s.data,{raw:s.info}).png().toBuffer();cache.set(file,s);return s;}
async function crop(file,cols,rows,i,w,h,flip=false){const s=await source(file,cols,rows),x=i%cols,y=Math.floor(i/cols);const region=await sharp(s.png).extract({left:s.x[x],top:s.y[y],width:s.x[x+1]-s.x[x],height:s.y[y+1]-s.y[y]}).png().toBuffer();let sh=sharp(region).trim({background:'#00000000',threshold:1});if(flip)sh=sh.flop();const data=await sh.resize(w,h,{fit:'fill'}).ensureAlpha().raw().toBuffer();for(let p=0;p<data.length;p+=4){if(data[p+3]<160)data.fill(0,p,p+4);else data[p+3]=255;}return data;}
const plan=[
 [0,'walk',0],[1,'walk',1],[2,'walk',0],[3,'walk',3],
 [4,'special',3,true],[5,'walk',5,true],[6,'special',3,true],[7,'walk',6,true],
 [8,'walk',8],[9,'walk',9],[10,'walk',8],[11,'walk',11],
 [12,'special',7],[13,'walk',6],[14,'special',7],[15,'walk',5],
 [36,'special',0,true],[37,'special',1],[38,'special',2,true],
 [40,'special',4],[41,'special',5],[42,'special',6]
];
await fs.mkdir(work+'unique-sprite-cells',{recursive:true});
for(const [frame,group,sourceCell,flip=false] of plan){const n=cellBytes(native,frame);let minX=16,minY=32,maxX=-1,maxY=-1;for(let y=0;y<32;y++)for(let x=0;x<16;x++)if(n[(y*16+x)*4+3]){minX=Math.min(x,minX);minY=Math.min(y,minY);maxX=Math.max(x,maxX);maxY=Math.max(y,maxY);}
 const w=maxX-minX+1,h=maxY-minY+1,data=await crop(group+'-source.png',4,group==='walk'?4:2,sourceCell,w,h,flip),cell=Buffer.alloc(16*32*4);
 for(let y=0;y<h;y++)data.copy(cell,((y+minY)*16+minX)*4,y*w*4,(y+1)*w*4);
 paste({data:output,info:native.info},frame,cell);
 await sharp(cell,{raw:{width:16,height:32,channels:4}}).png().toFile(work+`unique-sprite-cells/${frame}.png`);
 mapping.push({frame,source:group+'-source.png',sourceCell,flip,targetBounds:{x:minX,y:minY,w,h}});
}
assert.ok(cellBytes({data:output,info:native.info},55).equals(cellBytes(native,55)),'white placeholder');
for(const i of reusable)assert.ok(cellBytes({data:output,info:native.info},i).equals(cellBytes(base,i)));
await sharp(output,{raw:native.info}).png().toFile(work+'characters-prepared.png');
await sharp(output,{raw:native.info}).resize(384,2688,{kernel:'nearest'}).png().toFile(work+'characters-preview.png');
mapping.sort((a,b)=>a.frame-b.frame);await fs.writeFile(work+'sprite-export-mapping.json',JSON.stringify(mapping,null,2)+'\n');
const qa={PreparedSprites:55,UniqueGeneratedSprites:22,ReusedBaseSprites:reusable,WhitePlaceholder:55,BaseSha256:crypto.createHash('sha256').update(base.data).digest('hex'),Installed:false,RuntimeVerified:false,FullSceneVerified:false};
if(!process.argv.includes('--sprites-only')){
 const np=await read(originals+'Portraits/Sam_JojaMart.png'),nb=await read(originals+'Portraits/Sam.png'),bp=await read('artifacts/npc-modern/work/SamBase/portraits-prepared.png'),p={data:Buffer.alloc(np.data.length),info:np.info};
 const reuse=[6,10,11];for(const i of reuse){assert.ok(cellBytes(np,i,64,64,2).equals(cellBytes(nb,i,64,64,2)));paste(p,i,cellBytes(bp,i,64,64,2),64,64,2);}
 const portraitPlan=[[0,'portraits-a-source.png',0],[1,'portraits-a-source.png',1],[2,'portraits-a-source.png',2],[3,'portraits-a-source.png',3],[4,'portraits-b-source.png',0],[5,'portraits-b-source.png',1],[7,'portraits-b-source.png',2],[8,'portraits-b-source.png',3],[9,'portraits-c-source.png',0]];
 for(const [i,file,sourceCell] of portraitPlan){const data=await crop(file,2,2,sourceCell,62,62),cell=Buffer.alloc(64*64*4);for(let y=0;y<62;y++)data.copy(cell,((y+2)*64+1)*4,y*62*4,(y+1)*62*4);paste(p,i,cell,64,64,2);}
 await sharp(p.data,{raw:p.info}).png().toFile(work+'portraits-prepared.png');await sharp(p.data,{raw:p.info}).resize(512,1536,{kernel:'nearest'}).png().toFile(work+'portraits-preview.png');
 await fs.writeFile(work+'portrait-export-mapping.json',JSON.stringify({reuseBase:reuse,generated:portraitPlan},null,2)+'\n');qa.PreparedPortraits=12;qa.UniqueGeneratedPortraits=9;qa.ReusedBasePortraits=reuse;
}
await fs.writeFile(work+'validation.json',JSON.stringify(qa,null,2)+'\n');console.log(qa);

