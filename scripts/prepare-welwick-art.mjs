import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const root=new URL('../',import.meta.url), work=new URL('artifacts/npc-modern/work/Welwick/',root);
const registryFile=new URL('src/AbigailModern/artwork.json',root);
const registry=JSON.parse(await fs.readFile(registryFile,'utf8'));
const out=new URL('src/AbigailModern/assets/Welwick/',root);await fs.mkdir(out,{recursive:true});
const tv=await sharp(Buffer.from(await fs.readFile(new URL('modern-tv.png',work)))).metadata();
assert.equal(tv.width,1881);assert.equal(tv.height,836);
const frames=[];
// Explicit bounds exclude generated white separators; keep native TV composition.
for(const [left,top,width,height] of [[0,0,621,414],[628,0,624,414],[1259,0,622,414],[0,422,621,414],[628,422,624,414]])
  frames.push(await sharp(Buffer.from(await fs.readFile(new URL('modern-tv.png',work)))).extract({left,top,width,height}).resize(42,28,{fit:'fill'}).ensureAlpha().raw().toBuffer());
const evidence=[];
for(const [asset,targets] of [['Cursors',[[540,305,0],[582,305,1],[624,305,2]]],['Cursors_1_6',[[424,447,3],[424,476,4]]]]) {
  const entry=registry.find(e=>e.Name===`LooseSprites/${asset}`);assert.ok(entry);
  const base=await sharp(Buffer.from(await fs.readFile(new URL(`src/AbigailModern/${entry.File}`,root)))).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const result=Buffer.from(base.data);
  for(const [x,y,i] of targets) for(let row=0;row<28;row++) frames[i].copy(result,((y+row)*base.info.width+x)*4,row*42*4,(row+1)*42*4);
  let unchanged=0;
  for(let y=0;y<base.info.height;y++) for(let x=0;x<base.info.width;x++) {
    if(targets.some(([tx,ty])=>x>=tx && x<tx+42 && y>=ty && y<ty+28)) continue;
    const p=(y*base.info.width+x)*4;assert.ok(result.subarray(p,p+4).equals(base.data.subarray(p,p+4)));unchanged++;
  }
  const areas=targets.map(([X,Y])=>({X,Y,Width:42,Height:28}));
  entry.PatchAreas=[...(entry.PatchAreas??[entry.PatchArea]).filter(a=>!areas.some(b=>JSON.stringify(a)===JSON.stringify(b))),...areas];delete entry.PatchArea;
  await fs.writeFile(new URL(`src/AbigailModern/${entry.File}`,root),await sharp(result,{raw:base.info}).png().toBuffer());
  evidence.push({asset,areas,unchangedPixelsVerified:unchanged});
}
const portraits=await sharp(Buffer.from(await fs.readFile(new URL('modern-portraits.png',work)))).ensureAlpha().raw().toBuffer({resolveWithObject:true});
for(let p=0;p<portraits.data.length;p+=4) { const [r,g,b]=portraits.data.subarray(p,p+3);if(r>50 && b>50 && g<Math.min(r,b)*.5 && Math.abs(r-b)<70) portraits.data.fill(0,p,p+4); }
const layers=[];
for(let i=0;i<6;i++) {
  const x=i%2,y=Math.floor(i/2),left=Math.round(x*portraits.info.width/2),right=Math.round((x+1)*portraits.info.width/2),top=Math.round(y*portraits.info.height/3),bottom=Math.round((y+1)*portraits.info.height/3);
  const cell=await sharp(portraits.data,{raw:portraits.info}).extract({left,top,width:right-left,height:bottom-top}).png().toBuffer();
  const patch=await sharp(cell).trim({background:'#00000000',threshold:1}).resize(64,64,{fit:'contain',position:'bottom',background:'#00000000'}).ensureAlpha().raw().toBuffer();
  for(let p=0;p<patch.length;p+=4) if(patch[p+3]<160)patch.fill(0,p,p+4);else patch[p+3]=255;
  layers.push({input:await sharp(patch,{raw:{width:64,height:64,channels:4}}).png().toBuffer(),left:x*64,top:y*64});
}
await fs.writeFile(new URL('portraits.png',out),await sharp({create:{width:128,height:192,channels:4,background:'#00000000'}}).composite(layers).png().toBuffer());
const portraitEntry={Name:'Portraits/Welwick',File:'assets/Welwick/portraits.png',Width:128,Height:192};
const index=registry.findIndex(a=>a.Name===portraitEntry.Name);if(index<0)registry.push(portraitEntry);else registry[index]=portraitEntry;
await fs.writeFile(registryFile,JSON.stringify(registry,null,2));
await fs.writeFile(new URL('prepared-tv.png',work),await sharp({create:{width:210,height:28,channels:4,background:'#00000000'}}).composite(await Promise.all(frames.map(async(input,i)=>({input:await sharp(input,{raw:{width:42,height:28,channels:4}}).png().toBuffer(),left:i*42,top:0})))).png().toBuffer());
await fs.writeFile(new URL('validation.json',work),JSON.stringify({tvFrames:5,portraitExpressions:6,sharedPatches:evidence,fullTVSceneVerified:false},null,2));
console.log(JSON.stringify(evidence));
