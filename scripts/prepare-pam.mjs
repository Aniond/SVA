import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const variant=process.argv[2]??'Base';assert.ok(['Base','Winter','Beach'].includes(variant));
const work=`artifacts/npc-modern/work/Pam${variant}/`,suffix=variant==='Base'?'':'_'+variant;
const read=file=>sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
const cuts={},cache=new Map();
async function source(file,cols,rows){
 if(cache.has(file))return cache.get(file);const s=await read(file);
 for(let p=0;p<s.data.length;p+=4){const[r,g,b,a]=s.data.subarray(p,p+4);if(a<160||(r>215&&b>215&&g<65&&Math.abs(r-b)<30))s.data.fill(0,p,p+4);else s.data[p+3]=255;}
 const xc=Array(s.info.width).fill(0),yc=Array(s.info.height).fill(0);for(let y=0;y<s.info.height;y++)for(let x=0;x<s.info.width;x++)if(s.data[(y*s.info.width+x)*4+3]){xc[x]++;yc[y]++;}
 const edges=(counts,n)=>{const a=[0];for(let i=1;i<n;i++){const mid=i*counts.length/n;let best=-1,dist=Infinity;for(let p=Math.round(mid-counts.length/n*.28);p<mid+counts.length/n*.28;p++)if(counts[p]===0&&Math.abs(p-mid)<dist){best=p;dist=Math.abs(p-mid);}assert.ok(best>a.at(-1),`No empty gutter: ${file}`);a.push(best);}return [...a,counts.length];};
 const result={png:await sharp(s.data,{raw:s.info}).png().toBuffer(),cols,xs:edges(xc,cols),ys:edges(yc,rows)};cuts[file]={xs:result.xs,ys:result.ys};cache.set(file,result);return result;
}
async function cell(item,w,h){const s=await source(item.file,item.crop?1:item.cols,item.crop?1:item.rows),x=item.index%s.cols,y=Math.floor(item.index/s.cols);if(!item.crop)assert.ok(s.xs[x+1]>s.xs[x]&&s.ys[y+1]>s.ys[y]);const crop=await sharp(s.png).extract(item.crop??{left:s.xs[x],top:s.ys[y],width:s.xs[x+1]-s.xs[x],height:s.ys[y+1]-s.ys[y]}).png().toBuffer();let operation=sharp(crop).trim({background:'#00000000',threshold:1});if(item.flip)operation=operation.flop();const data=await operation.resize(w,h,{fit:'fill'}).ensureAlpha().raw().toBuffer();for(let p=0;p<data.length;p+=4)if(data[p+3]<160)data.fill(0,p,p+4);else data[p+3]=255;return data;}


const native=await read('artifacts/npc-modern/originals/Characters/Pam'+suffix+'.png');assert.equal(native.info.width,64);assert.equal(native.info.height,variant==='Beach'?128:320);
const output=Buffer.from(native.data),mapping=[],plan=[];
if(variant==='Base'){
 const indexes=[0,1,0,1,4,5,4,7,8,9,8,9,4,7,4,5];
 indexes.forEach((index,frame)=>plan.push({frame,file:'artifacts/npc-modern/work/Pam/modern-characters.png',cols:4,rows:3,index,flip:[3,11,12,13,14,15].includes(frame)}));
}else for(let frame=0;frame<16;frame++)plan.push({frame,file:work+'walk-source.png',cols:4,rows:4,index:[2,6,10,14].includes(frame)?frame-2:[3,11].includes(frame)?frame-2:frame,flip:[3,11].includes(frame)});
if(variant!=='Beach'){
 [24,25,26,27,28,29,32,33,34].forEach((frame,index)=>plan.push({frame,file:work+'special-source.png',cols:3,rows:3,index}));
 if(variant==='Base'){
  [30,31,35,36].forEach((frame,index)=>plan.push({frame,file:work+'final-source.png',cols:2,rows:2,index}));
  [16,17,18,19].forEach((frame,index)=>plan.push({frame,file:work+'tall-source.png',cols:2,rows:2,index,crop:{left:index%2*627,top:Math.floor(index/2)*627,width:627,height:Math.floor(index/2)===0?450:440}}));
 }else{
  [16,17,18,19].forEach((frame,index)=>plan.push({frame,file:work+'final-source.png',cols:4,rows:2,index,crop:{left:index*384,top:0,width:384,height:448}}));plan.push({frame:35,file:work+'final-source.png',cols:3,rows:2,index:0,crop:{left:0,top:512,width:560,height:512}});
 }
}

// Use dedicated opposing side phases; front/rear opposites are mirrored from the first stride.
for(const frame of [5,7,13,15]){const forward=[5,15].includes(frame),row=['Base','Winter','Beach'].indexOf(variant);const item={frame,file:'artifacts/npc-modern/work/PamBase/'+(forward?'forward':'strides')+'-source.png',cols:forward?3:2,rows:forward?1:3,index:forward?row:row*2+1,flip:frame>=12};const i=plan.findIndex(p=>p.frame===frame);plan[i]=item;}
if(variant!=='Beach'){const i=plan.findIndex(p=>p.frame===36);if(i>=0)plan.splice(i,1);plan.push({frame:36,file:'artifacts/npc-modern/work/PamBase/omelet-source.png',cols:2,rows:1,index:variant==='Base'?0:1});}
// Everyday drink source faced right; mirror to native left bottle placement.
if(variant==='Base')for(const item of plan)if([33,34].includes(item.frame))item.flip=true;

for(const item of plan){const{frame}=item,left=frame%4*16,top=Math.floor(frame/4)*32,fh=item.height??32;let x0=16,y0=fh,x1=-1,y1=-1;for(let y=0;y<fh;y++)for(let x=0;x<16;x++){const p=((top+y)*64+left+x)*4;if(native.data[p+3]){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}output.fill(0,p,p+4);}assert.ok(x1>=x0);const w=x1-x0+1,h=y1-y0+1,data=await cell(item,w,h);const colors=new Set();let visible=0;for(let p=0;p<data.length;p+=4)if(data[p+3]){visible++;colors.add(data.subarray(p,p+4).toString('hex'));}assert.ok(visible>0&&colors.size>7,'Empty generated pose '+frame);for(let y=0;y<h;y++)data.copy(output,((top+y0+y)*64+left+x0)*4,y*w*4,(y+1)*w*4);mapping.push({...item,nativeBounds:{x:x0,y:y0,width:w,height:h},visible});}
if(variant==='Winter'){
 const baseNative=await read('artifacts/npc-modern/originals/Characters/Pam.png'),basePrepared=await read('artifacts/npc-modern/work/PamBase/characters-prepared.png');
 for(const frame of [30,31])for(let y=0;y<32;y++)for(let x=0;x<16;x++){const p=((Math.floor(frame/4)*32+y)*64+frame%4*16+x)*4;assert.ok(baseNative.data.subarray(p,p+4).equals(native.data.subarray(p,p+4)),'Native sleep differs');basePrepared.data.copy(output,p,p,p+4);}
}
if(variant!=='Beach')for(let frame=37;frame<40;frame++)for(let y=0;y<32;y++)for(let x=0;x<16;x++){const p=((Math.floor(frame/4)*32+y)*64+frame%4*16+x)*4;assert.ok(output.subarray(p,p+4).equals(native.data.subarray(p,p+4)),'Native placeholder changed');}
await sharp(output,{raw:native.info}).png().toFile(work+'characters-prepared.png');await sharp(output,{raw:native.info}).resize({width:384,kernel:'nearest'}).png().toFile(work+'characters-preview.png');
let portraits=0;if(!process.argv.includes('--sprites-only')){
 if(variant==='Base')await fs.copyFile('src/AbigailModern/assets/Pam/portraits.png',work+'portraits-prepared.png');else{
  const pn=await read('artifacts/npc-modern/originals/Portraits/Pam'+suffix+'.png'),parts=[];
  for(let i=0;i<5;i++){const data=await cell({file:work+'portraits-source.png',cols:2,rows:3,index:i,...(variant==='Winter'?{crop:{left:i%2*512,top:[0,534,1048][Math.floor(i/2)],width:512,height:[534,514,488][Math.floor(i/2)]}}:{})},62,62);parts.push({input:await sharp(data,{raw:{width:62,height:62,channels:4}}).png().toBuffer(),left:i%2*64+1,top:Math.floor(i/2)*64+2});}
  const blank=await sharp(pn.data,{raw:pn.info}).extract({left:64,top:128,width:64,height:64}).png().toBuffer();parts.push({input:blank,left:64,top:128});await sharp({create:{width:128,height:192,channels:4,background:'#00000000'}}).composite(parts).png().toFile(work+'portraits-prepared.png');
 }
 await sharp(work+'portraits-prepared.png').resize(768,1152,{kernel:'nearest'}).png().toFile(work+'portraits-preview.png');portraits=5;
}
await fs.writeFile(work+'export-mapping.json',JSON.stringify({Frames:mapping,TallPoses:variant==='Beach'?[]:[16,17,18,19],TallGeometry:'Updated upper body at y128; original long lower prop continuation cells20-23 retained exactly.',NativeSleepReuse:variant==='Winter'?[30,31]:[],PreservedPlaceholders:variant==='Beach'?[]:[37,38,39],PreservedPortraitPlaceholder:5,Cuts:cuts},null,2));await fs.writeFile(work+'validation.json',JSON.stringify({Variant:variant,OccupiedPoses:variant==='Beach'?16:33,OccupiedNativeCells:variant==='Beach'?16:37,PreservedPlaceholders:variant==='Beach'?[]:[37,38,39],Portraits:portraits,Installed:false,RuntimeVerified:false},null,2));console.log('Prepared Pam'+variant+': '+(variant==='Beach'?16:33)+' poses and '+portraits+' portraits');
