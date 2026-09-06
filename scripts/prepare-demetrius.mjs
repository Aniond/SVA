import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const variant=process.argv[2]??'Base';assert.ok(['Base','Winter','Beach'].includes(variant));
const work=`artifacts/npc-modern/work/Demetrius${variant}/`,suffix=variant==='Base'?'':'_'+variant;
const read=file=>sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
const cuts={},cache=new Map();
async function source(file,cols,rows){
 if(cache.has(file))return cache.get(file);const s=await read(file);
 for(let p=0;p<s.data.length;p+=4){const[r,g,b,a]=s.data.subarray(p,p+4);if(a<160||(r>g+100&&b>g+100&&r>b*.88))s.data.fill(0,p,p+4);else s.data[p+3]=255;}
 const xc=Array(s.info.width).fill(0),yc=Array(s.info.height).fill(0);for(let y=0;y<s.info.height;y++)for(let x=0;x<s.info.width;x++)if(s.data[(y*s.info.width+x)*4+3]){xc[x]++;yc[y]++;}
 const edges=(counts,n)=>{const a=[0];for(let i=1;i<n;i++){const mid=i*counts.length/n;let best=-1,dist=Infinity;for(let p=Math.round(mid-counts.length/n*.28);p<mid+counts.length/n*.28;p++)if(counts[p]===0&&Math.abs(p-mid)<dist){best=p;dist=Math.abs(p-mid);}assert.ok(best>a.at(-1),`No empty gutter: ${file}`);a.push(best);}return [...a,counts.length];};
 const result={png:await sharp(s.data,{raw:s.info}).png().toBuffer(),cols,xs:edges(xc,cols),ys:edges(yc,rows)};cuts[file]={xs:result.xs,ys:result.ys};cache.set(file,result);return result;
}
async function cell(item,w,h){const s=await source(item.file,item.cols,item.rows),x=item.index%s.cols,y=Math.floor(item.index/s.cols);assert.ok(s.xs[x+1]>s.xs[x]&&s.ys[y+1]>s.ys[y]);const crop=await sharp(s.png).extract({left:s.xs[x],top:s.ys[y],width:s.xs[x+1]-s.xs[x],height:s.ys[y+1]-s.ys[y]}).png().toBuffer();let operation=sharp(crop).trim({background:'#00000000',threshold:1});if(item.flip)operation=operation.flop();const data=await operation.resize(w,h,{fit:'fill'}).ensureAlpha().raw().toBuffer();for(let p=0;p<data.length;p+=4)if(data[p+3]<160)data.fill(0,p,p+4);else data[p+3]=255;return data;}
const native=await read(`artifacts/npc-modern/originals/Characters/Demetrius${suffix}.png`);assert.equal(native.info.width,64);assert.equal(native.info.height,256);const output=Buffer.from(native.data),mapping=[];
const plan=Array.from({length:16},(_,frame)=>({frame,file:work+'walk-source.png',cols:4,rows:4,index:frame,flip:false}));
for(const frame of [3,11]){plan[frame].index=frame-2;plan[frame].flip=true;}
const strideRow=['Base','Winter','Beach'].indexOf(variant);
for(const frame of [5,7,13,15])plan[frame]={frame,file:'artifacts/npc-modern/work/DemetriusBase/all-strides-source.png',cols:2,rows:3,index:strideRow*2+([5,15].includes(frame)?0:1),flip:frame>=12};
const append=(file,cols,rows,frames)=>frames.forEach((frame,index)=>plan.push({frame,file:work+file,cols,rows,index,flip:false}));
if(variant==='Base'){append('special-source.png',4,2,[16,17,18,19,21,22,24,25]);append('final-source.png',3,2,[26,27,28,29,30,31]);}
if(variant==='Winter'){append('special-source.png',4,2,[16,17,18,19,21,24,25,26]);append('final-source.png',3,2,[27,28,29,30,31]);}
if(variant==='Beach')append('notes-source.png',3,1,[27,30,31]);
// The reviewed generated source rendered these two profiles to the right.
if(variant==='Base')for(const frame of [26,28])plan.find(item=>item.frame===frame).flip=true;
for(const item of plan){const{frame}=item,left=frame%4*16,top=Math.floor(frame/4)*32;let x0=16,y0=32,x1=-1,y1=-1;for(let y=0;y<32;y++)for(let x=0;x<16;x++){const p=((top+y)*64+left+x)*4;if(native.data[p+3]){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}output.fill(0,p,p+4);}assert.ok(x1>=x0);const w=x1-x0+1,h=y1-y0+1,data=await cell(item,w,h);let visible=0;const colors=new Set();for(let p=0;p<data.length;p+=4)if(data[p+3]){visible++;colors.add(data.subarray(p,p+4).toString('hex'));}assert.ok(visible>0&&colors.size>7,`Empty or solid frame${frame}`);for(let y=0;y<h;y++)data.copy(output,((top+y0+y)*64+left+x0)*4,y*w*4,(y+1)*w*4);mapping.push({...item,nativeBounds:{x:x0,y:y0,width:w,height:h},visible,colors:colors.size});}
const reused=variant==='Beach'?[16,17,18,19,21,24,25,26,28,29]:[];
if(reused.length){const base=await read('artifacts/npc-modern/originals/Characters/Demetrius.png'),modern=await read('artifacts/npc-modern/work/DemetriusBase/characters-prepared.png');for(const frame of reused)for(let y=0;y<32;y++){const p=((Math.floor(frame/4)*32+y)*64+frame%4*16)*4;assert.ok(native.data.subarray(p,p+64).equals(base.data.subarray(p,p+64)),`Native beach/Base reuse differs${frame}`);modern.data.copy(output,p,p,p+64);}}
const placeholders=variant==='Base'?[20,23]:[20,22,23];for(const frame of placeholders){const colors=new Set();for(let y=0;y<32;y++)for(let x=0;x<16;x++){const p=((Math.floor(frame/4)*32+y)*64+frame%4*16+x)*4;assert.ok(output.subarray(p,p+4).equals(native.data.subarray(p,p+4)),`Placeholder${frame} changed`);if(native.data[p+3])colors.add(native.data.subarray(p,p+4).toString('hex'));}assert.ok(colors.size<=2);}
assert.equal(plan.length+reused.length+placeholders.length,32,'All cells must be accounted for');
await sharp(output,{raw:native.info}).png().toFile(work+'characters-prepared.png');await sharp(output,{raw:native.info}).resize(384,1536,{kernel:'nearest'}).png().toFile(work+'characters-preview.png');
let portraits=0;if(!process.argv.includes('--sprites-only')){
 if(variant==='Base'||variant==='Beach'){const portraitFile='src/AbigailModern/assets/Demetrius/portraits.png';await fs.copyFile(portraitFile,work+'portraits-prepared.png');assert.ok((await fs.readFile(portraitFile)).equals(await fs.readFile(work+'portraits-prepared.png')));}
 else{const parts=[];for(let i=0;i<8;i++){const item={file:work+(i<4?'portraits-upper-source.png':'portraits-lower-source.png'),cols:2,rows:2,index:i%4,flip:false},data=await cell(item,62,62);parts.push({input:await sharp(data,{raw:{width:62,height:62,channels:4}}).png().toBuffer(),left:i%2*64+1,top:Math.floor(i/2)*64+2});}await sharp({create:{width:128,height:256,channels:4,background:'#00000000'}}).composite(parts).png().toFile(work+'portraits-prepared.png');}
 await sharp(work+'portraits-prepared.png').resize(512,1024,{kernel:'nearest'}).png().toFile(work+'portraits-preview.png');portraits=8;
}
await fs.writeFile(work+'export-mapping.json',JSON.stringify({Frames:mapping,ReusedBaseFrames:reused,Placeholders:placeholders,Cuts:cuts},null,2));await fs.writeFile(work+'validation.json',JSON.stringify({Variant:variant,UpdatedOccupiedFrames:plan.length+reused.length,NewlyGeneratedFrames:plan.length,ReusedBaseFrames:reused,PreservedPlaceholders:placeholders,Portraits:portraits,NativeBeachPortraitFallback:variant==='Beach',Installed:false,RuntimeVerified:false},null,2));console.log(`Prepared Demetrius${variant}: ${plan.length+reused.length} poses, ${placeholders.length} exact native placeholders, ${portraits} portraits.`);
