import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
import {createHash} from 'node:crypto';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const sha=b=>createHash('sha256').update(b).digest('hex');
const root='artifacts/npc-modern/';
const actor=process.argv.find(a=>a.startsWith('--actor='))?.slice(8)||'Kent';
const variant=process.argv.find(a=>a.startsWith('--variant='))?.slice(10)||'Winter';
const folder=root+'work/'+actor+variant+'/';
const plan=JSON.parse(await fs.readFile(folder+'plan.json','utf8'));
const grids={};
async function grid(file,cols,rows){
 if(grids[file])return grids[file];
 const s=await sharp(folder+file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 for(let p=0;p<s.data.length;p+=4){const [r,g,b,a]=s.data.subarray(p,p+4);if(a<160||(r>180&&b>180&&g<70))s.data.fill(0,p,p+4);else s.data[p+3]=255;}
 const axis=(length,parts,other,isY)=>{const n=Array(length).fill(0);for(let q=0;q<length;q++)for(let k=0;k<other;k++)if(s.data[((isY?q:k)*s.info.width+(isY?k:q))*4+3])n[q]++;const bounds=[0];for(let i=1;i<parts;i++){const expected=length*i/parts;let best=Math.round(expected),score=Infinity;for(let q=Math.round(expected-length/parts*.28);q<expected+length/parts*.28;q++){const v=n[q]*length+Math.abs(q-expected);if(v<score){score=v;best=q;}}assert.equal(n[best],0,`${file}: ${isY?'row':'column'} gutter ${i} crosses art`);bounds.push(best);}bounds.push(length);return bounds;};
 const xs=plan.gridBounds?.[file]?.x??axis(s.info.width,cols,s.info.height,false),ys=plan.gridBounds?.[file]?.y??axis(s.info.height,rows,s.info.width,true);
 const png=await sharp(s.data,{raw:s.info}).png().toBuffer();return grids[file]={...s,png,xs,ys,cols,rows};
}
async function cut(mapping,w,h){const s=await grid(mapping.file,mapping.cols,mapping.rows);const col=mapping.index%s.cols,row=Math.floor(mapping.index/s.cols);const cropped=await sharp(s.png).extract({left:s.xs[col],top:s.ys[row],width:s.xs[col+1]-s.xs[col],height:s.ys[row+1]-s.ys[row]}).png().toBuffer();let pipeline=sharp(cropped).trim({background:'#00000000',threshold:1});if(mapping.flip)pipeline=pipeline.flop();const data=await pipeline.resize(w,h,{fit:'fill',kernel:'lanczos3'}).ensureAlpha().raw().toBuffer();for(let p=0;p<data.length;p+=4)if(data[p+3]<160)data.fill(0,p,p+4);else data[p+3]=255;return data;}
const report={actor,variant,preparedOnly:true,installed:false,frames:[],portraits:[],gridBounds:{}};
for(const [kind,cellW,cellH,outputName] of [['Characters',16,32,'characters'],['Portraits',64,64,'portraits']]){
 if(!plan[kind])continue;
 const input=root+'originals/'+kind+'/'+actor+(variant==='Base'?'':'_'+variant)+'.png';
 const native=await sharp(input).ensureAlpha().raw().toBuffer({resolveWithObject:true}),output=Buffer.from(native.data),cols=native.info.width/cellW;
 for(let frame=0;frame<cols*native.info.height/cellH;frame++){
  const x=frame%cols*cellW,y=Math.floor(frame/cols)*cellH;let minX=cellW,minY=cellH,maxX=-1,maxY=-1;const colors=new Set(),before=[];
  for(let dy=0;dy<cellH;dy++)for(let dx=0;dx<cellW;dx++){const p=((y+dy)*native.info.width+x+dx)*4;before.push(...native.data.subarray(p,p+4));if(native.data[p+3]){minX=Math.min(minX,dx);maxX=Math.max(maxX,dx);minY=Math.min(minY,dy);maxY=Math.max(maxY,dy);colors.add(native.data.subarray(p,p+3).toString('hex'));}}
  const mapping=plan[kind][frame],placeholder=kind==='Characters'&&colors.size===1;
  if(mapping){if(mapping.reuseAsset){minX=0;minY=0;maxX=cellW-1;maxY=cellH-1;}assert.ok(!placeholder,'Cannot modify native placeholder '+frame);if(maxX<0){minX=0;minY=0;maxX=cellW-1;maxY=cellH-1;}const w=maxX-minX+1,h=maxY-minY+1;let data;
   if(mapping.reuseAsset){const file=root+"work/"+mapping.reuseAsset;const im=await sharp(file).metadata();const f=mapping.frame??frame;data=await sharp(file).extract({left:f%(im.width/cellW)*cellW,top:Math.floor(f/(im.width/cellW))*cellH,width:cellW,height:cellH}).ensureAlpha().raw().toBuffer();}else if(mapping.reuse){const other=await sharp(root+'work/'+mapping.reuse+'/characters-prepared.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});const f=mapping.frame??frame;data=await sharp(other.data,{raw:other.info}).extract({left:f%4*16+minX,top:Math.floor(f/4)*32+minY,width:w,height:h}).raw().toBuffer();}
   else { try { data=await cut(mapping,w,h); } catch(e) { throw new Error(JSON.stringify({frame,mapping,w,h})+e.message); } }
   for(let dy=0;dy<cellH;dy++)output.fill(0,((y+dy)*native.info.width+x)*4,((y+dy)*native.info.width+x+cellW)*4);
   for(let dy=0;dy<h;dy++)data.copy(output,((y+minY+dy)*native.info.width+x+minX)*4,dy*w*4,(dy+1)*w*4);
  }else if(kind==='Characters'&&maxX>=0&&!placeholder)throw new Error('Unmapped real sprite '+frame);
  const after=[];let visible=0;for(let dy=0;dy<cellH;dy++)for(let dx=0;dx<cellW;dx++){const p=((y+dy)*native.info.width+x+dx)*4;after.push(...output.subarray(p,p+4));if(output[p+3])visible++;}
  const same=Buffer.from(before).equals(Buffer.from(after));if(mapping){assert.ok(visible>0);assert.ok(!same,'Mapped frame unchanged '+frame);}else assert.ok(same);
  (kind==='Characters'?report.frames:report.portraits).push({frame,visible,nativeColors:colors.size,nativeBounds:maxX<0?null:[minX,minY,maxX,maxY],placeholder,changed:!same,mapping:mapping??null});
 }
 const png=await sharp(output,{raw:native.info}).png().toBuffer();await fs.writeFile(folder+outputName+'-prepared.png',png);await sharp(png).resize({width:native.info.width*(kind==='Characters'?8:4),kernel:'nearest'}).png().toFile(folder+outputName+'-preview.png');report[outputName+'Sheet']={width:native.info.width,height:native.info.height,sha256:sha(png)};
}
for(const [file,s]of Object.entries(grids))report.gridBounds[file]={x:s.xs,y:s.ys};
await fs.writeFile(folder+'qa.json',JSON.stringify(report,null,2));console.log(JSON.stringify({actor,variant,characters:report.charactersSheet,portraits:report.portraits.filter(p=>p.changed).length,realSprites:report.frames.filter(p=>p.changed).length,preserved:report.frames.filter(p=>!p.changed).map(p=>p.frame)},null,2));






