import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/EvelynBase/';
const bounds={};
async function sheet(file,cols,rows){
 const s=await sharp(work+file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 for(let p=0;p<s.data.length;p+=4){const [r,g,b,a]=s.data.subarray(p,p+4);if(a<160||(r>g+100&&b>g+100&&r>b*.88))s.data.fill(0,p,p+4);else s.data[p+3]=255;}
 const xc=Array(s.info.width).fill(0),yc=Array(s.info.height).fill(0);
 for(let y=0;y<s.info.height;y++)for(let x=0;x<s.info.width;x++)if(s.data[(y*s.info.width+x)*4+3]){xc[x]++;yc[y]++;}
 function cuts(counts,n){const out=[0];for(let i=1;i<n;i++){const middle=i*counts.length/n;let selected=-1,distance=Infinity;for(let x=Math.round(middle-counts.length/n*.3);x<Math.min(counts.length,middle+counts.length/n*.3);x++)if(counts[x]===0&&Math.abs(x-middle)<distance){selected=x;distance=Math.abs(x-middle);}assert.ok(selected>out.at(-1),`No clean gutter for ${file}`);out.push(selected);}out.push(counts.length);return out;}
 const xs=cuts(xc,cols),ys=cuts(yc,rows);bounds[file]={xs,ys};
 return {png:await sharp(s.data,{raw:s.info}).png().toBuffer(),xs,ys,cols};
}
async function cell(s,index,width,height){
 const x=index%s.cols,y=Math.floor(index/s.cols);
 assert.ok(s.xs[x+1]>s.xs[x]&&s.ys[y+1]>s.ys[y],JSON.stringify({index,x,y,xs:s.xs,ys:s.ys}));
 const crop=await sharp(s.png).extract({left:s.xs[x],top:s.ys[y],width:s.xs[x+1]-s.xs[x],height:s.ys[y+1]-s.ys[y]}).png().toBuffer();
 const resized=await sharp(crop).trim({background:'#00000000',threshold:1}).resize(width,height,{fit:'fill'}).ensureAlpha().raw().toBuffer();
 for(let p=0;p<resized.length;p+=4)if(resized[p+3]<160)resized.fill(0,p,p+4);else resized[p+3]=255;
 return resized;
}
const native=await sharp('artifacts/npc-modern/originals/Characters/Evelyn.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
assert.equal(native.info.width,64);assert.equal(native.info.height,192);
const output=Buffer.from(native.data),walk=await sheet('walk-source.png',4,3),special=await sheet('special-source.png',3,2),stride=await sheet('stride-source.png',2,1);
for(let frame=0;frame<21;frame++){
 const left=frame%4*16,top=Math.floor(frame/4)*32;let x0=16,y0=32,x1=-1,y1=-1;
 for(let y=0;y<32;y++)for(let x=0;x<16;x++){const p=((top+y)*64+left+x)*4;if(native.data[p+3]){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}output.fill(0,p,p+4);}
 assert.ok(x1>=x0);const width=x1-x0+1,height=y1-y0+1;
 let data=await cell(frame<16?walk:special,frame<16?(frame>=12?frame-8:frame):frame-16,width,height);
 if(frame>=4&&frame<8)data=await sharp(data,{raw:{width,height,channels:4}}).flop().raw().toBuffer();
 if(frame===11)data=await sharp(await cell(walk,9,width,height),{raw:{width,height,channels:4}}).flop().raw().toBuffer();
 if([5,7,13,15].includes(frame)){data=await cell(stride,[5,15].includes(frame)?0:1,width,height);if(frame<12)data=await sharp(data,{raw:{width,height,channels:4}}).flop().raw().toBuffer();}
 for(let y=0;y<height;y++)data.copy(output,((top+y0+y)*64+left+x0)*4,y*width*4,(y+1)*width*4);
 const colors=new Set();for(let p=0;p<data.length;p+=4)if(data[p+3])colors.add(data.subarray(p,p+4).toString('hex'));
 assert.ok(colors.size>=8,`Empty or solid pose ${frame}`);
}
for(let frame=21;frame<24;frame++)for(let y=0;y<32;y++)for(let x=0;x<16;x++){
 const p=((Math.floor(frame/4)*32+y)*64+frame%4*16+x)*4;
 assert.deepEqual([...native.data.subarray(p,p+4)],[247,255,252,255]);assert.ok(output.subarray(p,p+4).equals(native.data.subarray(p,p+4)));
}
await sharp(output,{raw:native.info}).png().toFile(work+'characters-prepared.png');
await sharp(output,{raw:native.info}).resize(384,1152,{kernel:'nearest'}).png().toFile(work+'characters-preview.png');
let portraits=0;
if(!process.argv.includes('--sprites-only')){
 await fs.copyFile(work+'portraits-before.png',work+'portraits-prepared.png');
 assert.ok((await fs.readFile(work+'portraits-before.png')).equals(await fs.readFile(work+'portraits-prepared.png')),'Accepted everyday portraits changed');
 await sharp(work+'portraits-prepared.png').resize(512,512,{kernel:'nearest'}).png().toFile(work+'portraits-preview.png');portraits=4;
}
await fs.writeFile(work+'validation.json',JSON.stringify({ProcessedFrames:24,UpdatedOccupiedFrames:21,RetainedPlaceholders:3,PlaceholderColor:[247,255,252,255],Portraits:portraits,Bounds:bounds,Installed:false,RuntimeVerified:false},null,2));
console.log(`Prepared 21 Evelyn everyday poses, three exact native placeholders, ${portraits} portraits.`);
