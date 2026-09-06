import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {createRequire} from 'node:module';
const sharp=createRequire('C:/Users/david/SDV/.tools/abigail-art/package.json')('sharp');
const root='C:/Users/david/SDV';
const work=path.join(root,'artifacts/npc-modern/work/MrsRaccoonPortraitCleanup');
const baseline=await fs.readFile(path.join(work,'portraits-before.png'));
const original=await sharp(baseline).ensureAlpha().raw().toBuffer({resolveWithObject:true});
assert.equal(original.info.width,128);assert.equal(original.info.height,192);
const generated=await sharp(path.join(work,'edit-source.png')).resize(1024,1024,{kernel:'nearest'}).ensureAlpha().raw().toBuffer({resolveWithObject:true});
// Routine magenta keying of the imagegen edit, not a procedural redraw.
for(let p=0;p<generated.data.length;p+=4){let[r,g,b]=generated.data.subarray(p,p+3);if(r>190&&b>190&&g<75&&Math.abs(r-b)<60)generated.data.fill(0,p,p+4);}
const prepared=Buffer.from(original.data),mask=Buffer.alloc(prepared.length),mapping=[];
for(let k=0;k<4;k++){
 const cell=k+2,left=(k%2)*512+64,top=Math.floor(k/2)*512+64;
 // Reference panels are native cells enlarged sixfold. Imagegen was asked to
 // remove only the detached top strips. Import ONLY the edited three-pixel
 // strip into the native sheet; all accepted artwork below remains original.
 const strip=await sharp(generated.data,{raw:generated.info}).extract({left,top,width:384,height:18}).resize(64,3,{kernel:'nearest'}).ensureAlpha().raw().toBuffer();
 for(let p=0;p<strip.length;p+=4)assert.equal(strip[p+3],0,`Edit still has foreign pixels in target strip ${cell}`);
 await sharp(strip,{raw:{width:64,height:3,channels:4}}).png().toFile(path.join(work,`edited-strip-cell-${cell}.png`));
 let changed=0;const X=cell%2*64,Y=Math.floor(cell/2)*64;
 for(let y=0;y<3;y++)for(let x=0;x<64;x++){
  const p=((Y+y)*128+X+x)*4,s=(y*64+x)*4;
  if(!prepared.subarray(p,p+4).equals(strip.subarray(s,s+4))){changed++;mask.set([255,255,255,255],p);}
  strip.copy(prepared,p,s,s+4);
 }
 mapping.push({cell,nativeRectangle:[X,Y,64,3],imagegenReferenceRectangle:[left,top,384,18],changedPixels:changed});
}
let changed=0,unchanged=0,unchangedBody=0;
for(let y=0;y<192;y++)for(let x=0;x<128;x++){
 const p=(y*128+x)*4,same=prepared.subarray(p,p+4).equals(original.data.subarray(p,p+4));
 if(same)unchanged++;else{changed++;assert.ok(y>=64&&y%64<3,'Edit escaped approved strips');assert.equal(prepared[p+3],0);}
 if(y<64||y%64>=3){assert.ok(same,'Accepted expression art changed');unchangedBody++;}
}
assert.equal(changed,633);assert.equal(unchanged,23943);
await sharp(prepared,{raw:original.info}).png().toFile(path.join(work,'portraits-prepared.png'));
await sharp(mask,{raw:original.info}).png().toFile(path.join(work,'edit-mask.png'));
await sharp(prepared,{raw:original.info}).resize(512,768,{kernel:'nearest'}).png().toFile(path.join(work,'portraits-preview.png'));
const layers=[];
for(let i=0;i<6;i++)for(let side=0;side<2;side++){
 const x=side*420,y=i*310,label=`Cell ${i} ${side?'after':'before'}`;
 layers.push({input:Buffer.from(`<svg width="420" height="30"><text x="8" y="22" fill="white" font-size="20">${label}</text></svg>`),left:x,top:y});
 const pixels=side?prepared:original.data;
 layers.push({input:await sharp(pixels,{raw:original.info}).extract({left:i%2*64,top:Math.floor(i/2)*64,width:64,height:64}).resize(256,256,{kernel:'nearest'}).flatten({background:'#dce5e5'}).png().toBuffer(),left:x+70,top:y+32});
}
await sharp({create:{width:840,height:1860,channels:4,background:'#263941'}}).composite(layers).png().toFile(path.join(work,'before-after-cells.png'));
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const roles=['neutral attentive','happy grateful smile','disappointed sad','curious eager head tilt','surprised wide-eyed open mouth','contented eyes-closed smile'];
const qa={passed:true,dimensions:[128,192],cells:6,roles,changedPixels:changed,unchangedPixels:unchanged,unchangedBodyRegionPixels:unchangedBody,acceptedTopCellsByteIdentical:true,allFaceTailEarBodyPixelsByteIdentical:true,editedStripsContainOnlyTransparency:true,mapping,baselineSha256:hash(baseline),preparedSha256:hash(await fs.readFile(path.join(work,'portraits-prepared.png'))),imagegenSourceSha256:hash(await fs.readFile(path.join(work,'edit-source.png'))),productionEdited:false,runtimeVerified:false};
await fs.writeFile(path.join(work,'qa.json'),JSON.stringify(qa,null,2));
console.log(JSON.stringify(qa));
