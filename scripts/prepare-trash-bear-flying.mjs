import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
const sharp = createRequire(new URL('../.tools/abigail-art/package.json', import.meta.url))('sharp');
const root = new URL('../', import.meta.url);
const work = new URL('artifacts/npc-modern/work/TrashBear/', root);
const registryFile = new URL('src/AbigailModern/artwork.json', root);
const registry = JSON.parse(await fs.readFile(registryFile, 'utf8'));
const entry = registry.find(a => a.Name === 'LooseSprites/Cursors2');
assert.ok(entry, 'Grandpa shared atlas must exist');
const prior = await sharp(Buffer.from(await fs.readFile(new URL(`src/AbigailModern/${entry.File}`, root)))).ensureAlpha().raw().toBuffer({resolveWithObject:true});
const source = await sharp(Buffer.from(await fs.readFile(new URL('modern-flying-keyed.png', work)))).ensureAlpha().raw().toBuffer({resolveWithObject:true});
assert.equal(source.info.width, 1607);
assert.equal(source.info.height, 979);
for (let p=0;p<source.data.length;p+=4) {
  const [r,g,b] = source.data.subarray(p,p+3);
  if (r>50 && b>50 && g<Math.min(r,b)*.5 && Math.abs(r-b)<70) source.data.fill(0,p,p+4);
}
const pixels = Buffer.from(prior.data);
const patches=[];
for(let i=0;i<2;i++) {
  // The generated frames are offset by 750px; equal crops preserve body alignment.
  let cell = sharp(source.data,{raw:source.info}).extract({left:i*750,top:0,width:i?857:750,height:884});
  if(!i) cell=cell.extend({right:107,left:0,top:0,bottom:0,background:'#00000000'});
  const padded=await cell.png().toBuffer();
  const patch=await sharp(padded).resize(46,56,{fit:'fill',kernel:'lanczos3'}).ensureAlpha().raw().toBuffer();
  assert.equal(patch.length,46*56*4);
  let occupied=0;
  for(let p=0;p<patch.length;p+=4) {
    if(patch[p+3]<160) patch.fill(0,p,p+4);
    else {patch[p+3]=255;occupied++;}
  }
  assert.ok(occupied>500 && occupied<2400);
  for(let y=0;y<56;y++) patch.copy(pixels,((80+y)*prior.info.width+i*46)*4,y*46*4,(y+1)*46*4);
  patches.push({frame:i,occupiedPixels:occupied});
}
let unchanged=0;
for(let y=0;y<prior.info.height;y++) for(let x=0;x<prior.info.width;x++) {
  if(x<92 && y>=80 && y<136) continue;
  const p=(y*prior.info.width+x)*4;
  assert.ok(pixels.subarray(p,p+4).equals(prior.data.subarray(p,p+4)), 'Existing atlas art changed'); unchanged++;
}
entry.File='assets/TrashBear/Cursors2.png';
const areas=entry.PatchAreas ?? [entry.PatchArea];
const area={X:0,Y:80,Width:92,Height:56};
entry.PatchAreas=[...areas.filter(a=>JSON.stringify(a)!==JSON.stringify(area)),area];
delete entry.PatchArea;
await fs.writeFile(new URL(`src/AbigailModern/${entry.File}`,root),await sharp(pixels,{raw:prior.info}).png().toBuffer());
await fs.writeFile(new URL('prepared-flying.png',work),await sharp(pixels,{raw:prior.info}).extract({left:0,top:80,width:92,height:56}).resize(920,560,{kernel:'nearest'}).png().toBuffer());
await fs.writeFile(registryFile,JSON.stringify(registry,null,2));
const evidence={asset:entry.Name,patchAreas:entry.PatchAreas,patches,unchangedPixelsVerified:unchanged,nativeSource:'Event.cs trashBearUmbrella1 and trashBearTown',fullCleanupSceneVerified:false};
await fs.writeFile(new URL('flying-validation.json',work),JSON.stringify(evidence,null,2));
console.log(JSON.stringify(evidence));
