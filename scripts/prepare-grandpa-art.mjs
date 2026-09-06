import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
const sharp = createRequire(new URL('../.tools/abigail-art/package.json', import.meta.url))('sharp');
const root = new URL('../', import.meta.url);
const work = new URL('artifacts/npc-modern/work/Grandpa/', root);
const out = new URL('src/AbigailModern/assets/Grandpa/', root);
await fs.mkdir(out, { recursive: true });
async function keyed(file, removeKey = true) {
  const image = await sharp(Buffer.from(await fs.readFile(file))).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  for(let p=0;p<image.data.length;p+=4) {
    const [r,g,b]=image.data.subarray(p,p+3);
    if(removeKey && r>170 && b>170 && g<100 && Math.abs(r-b)<60) image.data.fill(0,p,p+4);
  }
  return image;
}
async function fit(image, rect, width, height) {
  const extracted = await sharp(image.data,{raw:image.info}).extract(rect).png().toBuffer();
  const pixels = await sharp(extracted).trim({background:'#00000000',threshold:1}).resize(width,height,{fit:'contain',position:'bottom',background:'#00000000'}).ensureAlpha().raw().toBuffer();
  let occupied=0;
  for(let p=0;p<pixels.length;p+=4) {
    if(pixels[p+3]<160) pixels.fill(0,p,p+4);
    else {pixels[p+3]=255;occupied++;}
  }
  assert.ok(occupied>width*height*.12 && occupied<width*height*.95,'Unexpected foreground occupancy');
  return pixels;
}
const registryFile = new URL('src/AbigailModern/artwork.json', root);
let registry = JSON.parse(await fs.readFile(registryFile,'utf8'));
const portrait = await keyed(new URL('modern-portraits-0.png',work));
const layers=[];
for(let i=0;i<2;i++) {
  const left=Math.round(i*portrait.info.width/2), right=Math.round((i+1)*portrait.info.width/2);
  const pixels=await fit(portrait,{left,top:0,width:right-left,height:portrait.info.height},64,64);
  layers.push({input:await sharp(pixels,{raw:{width:64,height:64,channels:4}}).png().toBuffer(),left:i*64,top:0});
}
await sharp({create:{width:128,height:64,channels:4,background:'#00000000'}}).composite(layers).png().toFile(new URL('portraits.png',out).pathname.replace(/^\//,''));
const entries=[{Name:'Portraits/Grandpa',File:'assets/Grandpa/portraits.png',Width:128,Height:64}];
const evidence=[];
for(const [name,asset,area] of [
  ['spirit','Cursors',{X:555,Y:1956,Width:18,Height:35}],
  ['thumbsup','Cursors2',{X:186,Y:265,Width:22,Height:34}]
]) {
  const source = await keyed(new URL(`modern-${name}.png`,work));
  const existingAtlas = registry.find(a=>a.Name===`LooseSprites/${asset}`);
  const otherAreas = (existingAtlas?.PatchAreas ?? (existingAtlas?.PatchArea ? [existingAtlas.PatchArea] : []))
    .filter(a=>JSON.stringify(a)!==JSON.stringify(area));
  const original = await keyed(new URL(otherAreas.length ? `src/AbigailModern/${existingAtlas.File}` : `artifacts/npc-modern/originals/LooseSprites/${asset}.png`,root), false);
  const pixels=Buffer.from(original.data);
  const patch=await fit(source,{left:0,top:0,width:source.info.width,height:source.info.height},area.Width,area.Height);
  for(let y=0;y<area.Height;y++) patch.copy(pixels,((area.Y+y)*original.info.width+area.X)*4,y*area.Width*4,(y+1)*area.Width*4);
  let untouched=0;
  for(let y=0;y<original.info.height;y++) for(let x=0;x<original.info.width;x++) {
    if(x>=area.X && x<area.X+area.Width && y>=area.Y && y<area.Y+area.Height) continue;
    const p=(y*original.info.width+x)*4;
    assert.ok(pixels.subarray(p,p+4).equals(original.data.subarray(p,p+4)),'Unrelated shared pixels changed');untouched++;
  }
  const atlasFile = otherAreas.length ? existingAtlas.File : `assets/Grandpa/${asset}.png`;
  await sharp(pixels,{raw:original.info}).png().toFile(new URL(`src/AbigailModern/${atlasFile}`,root).pathname.replace(/^\//,''));
  await sharp(patch,{raw:{width:area.Width,height:area.Height,channels:4}}).resize(area.Width*8,area.Height*8,{kernel:'nearest'}).png().toFile(new URL(`prepared-${name}.png`,work).pathname.replace(/^\//,''));
  entries.push({Name:`LooseSprites/${asset}`,File:atlasFile,Width:original.info.width,Height:original.info.height,...(otherAreas.length ? {PatchAreas:[area,...otherAreas]} : {PatchArea:area})});
  evidence.push({asset,area,untouchedPixelsVerified:untouched});
}
for(const entry of entries) {
  const existing=registry.find(a=>a.Name===entry.Name);
  assert.ok(!existing || existing.File===entry.File,'Existing shared patch must be merged explicitly');
  registry=registry.filter(a=>a.Name!==entry.Name);registry.push(entry);
}
await fs.writeFile(registryFile,JSON.stringify(registry,null,2));
await fs.writeFile(new URL('validation.json',work),JSON.stringify({portraitFrames:[0,1],sharedPatches:evidence,placeholderUnmodified:true,openingStoryUpdated:false,sceneReviewVerified:false},null,2));
console.log(JSON.stringify(evidence));
