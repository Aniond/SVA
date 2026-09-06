import fs from 'node:fs/promises';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const dir='artifacts/npc-modern/work/AlexPortraitCleanup/';
const before=await sharp(dir+'portraits-before.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
assert.equal(before.info.width,128);assert.equal(before.info.height,384);
const generated=await sharp(dir+'imagegen-cleaned-pair.png').resize(128,64,{kernel:'nearest'}).ensureAlpha().raw().toBuffer();
// Only the generated clean background in these reviewed fragments is accepted.
// All faces, hands, football, hair, attached sleeves and other ten cells retain original RGBA.
const patches=[{Cell:1,Source:[0,54,3,10],Target:[64,54,3,10],ExpectedChanged:26},{Cell:3,Source:[64,57,2,7],Target:[64,121,2,7],ExpectedChanged:12}];
const after=Buffer.from(before.data),mask=Buffer.alloc(after.length);
for(const patch of patches){
 const[sx,sy,w,h]=patch.Source,[tx,ty]=patch.Target;let changed=0;
 for(let y=0;y<h;y++)for(let x=0;x<w;x++){
  const s=((sy+y)*128+sx+x)*4,t=((ty+y)*128+tx+x)*4;
  assert.ok(generated[s]>220&&generated[s+2]>220&&generated[s+1]<60,'Edited fragment is not clean keyed background');
  generated.fill(0,s,s+4); // Routine keying of imagegen background.
  if(!after.subarray(t,t+4).equals(generated.subarray(s,s+4)))changed++;
  generated.copy(after,t,s,s+4);mask.fill(255,t,t+4);
 }
 assert.equal(changed,patch.ExpectedChanged);
}
await sharp(after,{raw:before.info}).png().toFile(dir+'portraits-prepared.png');
await sharp(mask,{raw:before.info}).png().toFile(dir+'cleanup-mask.png');
let changed=0,outside=0;for(let i=0;i<after.length;i+=4){assert.ok(after[i+3]===0||after[i+3]===255);if(!after.subarray(i,i+4).equals(before.data.subarray(i,i+4))){changed++;if(!mask[i+3])outside++;}}
assert.equal(changed,38);assert.equal(outside,0);
await fs.mkdir(dir+'clean-cells',{recursive:true});
const roles=['neutral confident','eyes-closed laugh','sad','cheerful football','blush','angry','shirtless subdued blush','surprised','shirtless slight smile','serious','happy telephone','additional thoughtful'];
const perCell=[],hashes=new Set();
for(let i=0;i<12;i++){
 const r={left:i%2*64,top:Math.floor(i/2)*64,width:64,height:64};
 const cell=await sharp(after,{raw:before.info}).extract(r).png().toBuffer();await fs.writeFile(dir+`clean-cells/cell-${i}.png`,cell);
 const raw=await sharp(cell).raw().toBuffer(),old=await sharp(before.data,{raw:before.info}).extract(r).raw().toBuffer();let n=0,diff=0;
 for(let p=0;p<raw.length;p+=4){if(raw[p+3])n++;if(!raw.subarray(p,p+4).equals(old.subarray(p,p+4)))diff++;}
 assert.equal(diff,i===1?26:i===3?12:0);assert.ok(n>1000);
 const hash=crypto.createHash('sha256').update(raw).digest('hex');hashes.add(hash);perCell.push({Cell:i,Role:roles[i],OccupiedPixels:n,ChangedPixels:diff,Sha256:hash});
}
assert.equal(hashes.size,12);
for(const[name,bg]of[['light','#e3e9e7'],['dark','#24333c']]){
 for(let page=0;page<2;page++){
  const layers=[];for(let j=0;j<6;j++)layers.push({input:await sharp(dir+`clean-cells/cell-${page*6+j}.png`).resize(384,384,{kernel:'nearest'}).png().toBuffer(),left:j%2*400+8,top:Math.floor(j/2)*400+8});
  await sharp({create:{width:800,height:1200,channels:4,background:bg}}).composite(layers).png().toFile(dir+`preview-${name}-${page}.png`);
 }
}
await sharp(dir+'portraits-prepared.png').resize(512,1536,{kernel:'nearest'}).png().toFile(dir+'portraits-preview.png');
const compare=await sharp({create:{width:256,height:128,channels:4,background:'#dbe4e4'}}).composite([{input:await sharp(dir+'portraits-before.png').extract({left:0,top:0,width:128,height:128}).png().toBuffer(),left:0,top:0},{input:await sharp(dir+'portraits-prepared.png').extract({left:0,top:0,width:128,height:128}).png().toBuffer(),left:128,top:0}]).png().toBuffer();
await sharp(compare).resize(1536,768,{kernel:'nearest'}).png().toFile(dir+'affected-before-left-after-right.png');
const hash=async p=>crypto.createHash('sha256').update(await fs.readFile(dir+p)).digest('hex');
await fs.writeFile(dir+'validation.json',JSON.stringify({Passed:true,Dimensions:[128,384],ExpressionCells:12,DistinctExpressions:12,ChangedPixels:changed,OutsideConfirmedFragmentsChanged:outside,AllAcceptedFigurePixelsExact:true,OtherTenCellsExact:true,BinaryAlpha:true,Patches:patches,PerCell:perCell,BeforeSha256:await hash('portraits-before.png'),PreparedSha256:await hash('portraits-prepared.png'),ImagegenSource:{Id:'d1ad6568-2592-412b-a854-e657a037f83b',File:'imagegen-cleaned-pair.png',Sha256:await hash('imagegen-cleaned-pair.png'),Prompt:'prompt.json',AcceptedRegion:'Only clean magenta background in the two fragment masks; key to transparency. All generated body pixels excluded to preserve original identity and expressions.'},VisualReview:false,Installed:false,RuntimeVerified:false},null,2));
console.log(JSON.stringify({Passed:true,ChangedPixels:changed,OutsideConfirmedFragmentsChanged:outside,DistinctExpressions:12}));
