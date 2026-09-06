import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
import {createHash} from 'node:crypto';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const sha=b=>createHash('sha256').update(b).digest('hex');
const root='artifacts/npc-modern/';
const actor=process.argv.find(a=>a.startsWith('--actor='))?.slice(8)||'Clint';
const variant=process.argv.find(a=>a.startsWith('--variant='))?.slice(10)||'Winter';
const folder=root+'work/'+actor+variant+'/';
const plan=JSON.parse(await fs.readFile(folder+'plan.json','utf8'));
const grids={};
async function grid(file,cols,rows){
 if(grids[file])return grids[file];
 const s=await sharp(folder+file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 for(let p=0;p<s.data.length;p+=4){const [r,g,b,a]=s.data.subarray(p,p+4);if(a<160||(r>g+45&&b>g+45&&r>b*.85))s.data.fill(0,p,p+4);else s.data[p+3]=255;}
 const axis=(length,parts,other,isY)=>{const n=Array(length).fill(0);for(let q=0;q<length;q++)for(let k=0;k<other;k++)if(s.data[((isY?q:k)*s.info.width+(isY?k:q))*4+3])n[q]++;const bounds=[0];for(let i=1;i<parts;i++){const expected=length*i/parts;let best=Math.round(expected),score=Infinity;for(let q=Math.round(expected-length/parts*.28);q<expected+length/parts*.28;q++){const v=n[q]*length+Math.abs(q-expected);if(v<score){score=v;best=q;}}assert.equal(n[best],0,`${file}: ${isY?'row':'column'} gutter ${i} crosses art`);bounds.push(best);}bounds.push(length);return bounds;};
 const xs=plan.gridBounds?.[file]?.x??axis(s.info.width,cols,s.info.height,false),ys=plan.gridBounds?.[file]?.y??axis(s.info.height,rows,s.info.width,true);
 const png=await sharp(s.data,{raw:s.info}).png().toBuffer();return grids[file]={...s,png,xs,ys,cols,rows};
}
async function cut(mapping,w,h){const s=await grid(mapping.file,mapping.cols,mapping.rows);const col=mapping.index%s.cols,row=Math.floor(mapping.index/s.cols);const cropped=await sharp(s.png).extract({left:s.xs[col],top:s.ys[row],width:s.xs[col+1]-s.xs[col],height:s.ys[row+1]-s.ys[row]}).png().toBuffer();let pipeline=sharp(cropped).trim({background:'#00000000',threshold:1});if(mapping.flip)pipeline=pipeline.flop();const data=await pipeline.resize(w,h,{fit:'fill',kernel:'lanczos3'}).ensureAlpha().raw().toBuffer();for(let p=0;p<data.length;p+=4)if(data[p+3]<160)data.fill(0,p,p+4);else data[p+3]=255;return data;}

const report={actor,variant,preparedOnly:true,installed:false,regions:[],frames:[],portraits:[],gridBounds:{}};
for(const [kind,cw,ch,name]of [['Characters',16,32,'characters'],['Portraits',64,64,'portraits']]){
 if(!plan[kind])continue;
 const native=await sharp(root+'originals/'+kind+'/'+actor+(variant==='Base'?'':'_'+variant)+'.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});const output=Buffer.from(native.data);const used=new Uint8Array(native.info.width*native.info.height);
 const mappings=Object.entries(plan[kind]).map(([f,m])=>({...m,rect:[+f%(native.info.width/cw)*cw,Math.floor(+f/(native.info.width/cw))*ch,cw,ch],label:kind+':'+f}));
 if(kind==='Characters')mappings.push(...(plan.Regions??[]));
 for(const m of mappings){const [x,y,w,h]=m.rect;let x0=w,y0=h,x1=-1,y1=-1;for(let dy=0;dy<h;dy++)for(let dx=0;dx<w;dx++){const p=(y+dy)*native.info.width+x+dx;assert.equal(used[p],0,'Overlapping region '+m.label);used[p]=1;if(native.data[p*4+3]){x0=Math.min(x0,dx);y0=Math.min(y0,dy);x1=Math.max(x1,dx);y1=Math.max(y1,dy);}}if(x1<0){x0=0;y0=0;x1=w-1;y1=h-1;}let data;
  if(m.reuseAsset){const file=root+'work/'+m.reuseAsset;data=await sharp(file).extract({left:x,top:y,width:w,height:h}).ensureAlpha().raw().toBuffer();x0=0;y0=0;x1=w-1;y1=h-1;}else data=await cut(m,x1-x0+1,y1-y0+1);
  assert.ok(data.some((v,i)=>i%4===3&&v>0),'Empty semantic region '+m.label);
  for(let dy=0;dy<h;dy++)output.fill(0,((y+dy)*native.info.width+x)*4,((y+dy)*native.info.width+x+w)*4);
  for(let dy=0;dy<=y1-y0;dy++)data.copy(output,((y+y0+dy)*native.info.width+x+x0)*4,dy*(x1-x0+1)*4,(dy+1)*(x1-x0+1)*4);
  report.regions.push({kind,label:m.label,rect:m.rect,nativeBounds:[x0,y0,x1,y1],mapping:m});
 }
 for(let f=0;f<native.info.width/cw*native.info.height/ch;f++){const x=f%(native.info.width/cw)*cw,y=Math.floor(f/(native.info.width/cw))*ch;let before=0,after=0,changed=false;for(let dy=0;dy<ch;dy++)for(let dx=0;dx<cw;dx++){const p=((y+dy)*native.info.width+x+dx)*4;if(native.data[p+3])before++;if(output[p+3])after++;if(!output.subarray(p,p+4).equals(native.data.subarray(p,p+4)))changed=true;assert.ok(output[p+3]===0||output[p+3]===255);if(kind==='Characters'&&!used[p/4])assert.ok(output.subarray(p,p+4).equals(native.data.subarray(p,p+4)));}
  if(before>0){if(kind!=='Characters'||plan.Characters[f])assert.ok(after>0,'Empty original occupied frame '+f);assert.ok(changed,'Original occupied frame unchanged '+f);}else if(!changed)assert.equal(after,0);
  (kind==='Characters'?report.frames:report.portraits).push({frame:f,nativePixels:before,visible:after,changed});
 }
 const png=await sharp(output,{raw:native.info}).png().toBuffer();await fs.writeFile(folder+name+'-prepared.png',png);await sharp(png).resize({width:native.info.width*(kind==='Characters'?8:4),kernel:'nearest'}).png().toFile(folder+name+'-preview.png');report[name+'Sheet']={width:native.info.width,height:native.info.height,sha256:sha(png)};
}
for(const[file,s]of Object.entries(grids))report.gridBounds[file]={x:s.xs,y:s.ys};await fs.writeFile(folder+'qa.json',JSON.stringify(report,null,2));console.log(JSON.stringify({actor,variant,sprites:report.charactersSheet,portraits:report.portraitsSheet,changedSpriteCells:report.frames.filter(f=>f.changed).length,preservedBlankCells:report.frames.filter(f=>!f.changed).map(f=>f.frame),semanticSpriteRegions:report.regions.filter(r=>r.kind==='Characters').length},null,2));

