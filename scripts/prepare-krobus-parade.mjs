import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/KrobusRaven/';
const original=await sharp('artifacts/npc-modern/originals/Characters/KrobusRaven.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
const source=await sharp(work+'modern-parade.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
assert.equal(source.info.width,1254);assert.equal(source.info.height,1254);
for(let p=0;p<source.data.length;p+=4){const[r,g,b]=source.data.subarray(p,p+3);if(r>210&&b>210&&g<60&&Math.abs(r-b)<45)source.data.fill(0,p,p+4);}
const result=Buffer.from(original.data), evidence=[];
for(let row=0;row<3;row++)for(let col=0;col<(row===2?4:5);col++) {
 const xs=row===2?[0,272,526,776,1026]:[0,250,500,750,1000,1254];
 const tops=[0,420,800],heights=[340,300,350];
 const targetHeight=row===2?39:32;
 const patch=await sharp(source.data,{raw:source.info}).extract({left:xs[col],top:tops[row],width:xs[col+1]-xs[col],height:heights[row]}).resize(32,targetHeight,{fit:'fill'}).ensureAlpha().raw().toBuffer();
 let occupied=0;
 for(let p=0;p<patch.length;p+=4){if(patch[p+3]<160)patch.fill(0,p,p+4);else{patch[p+3]=255;occupied++;}}
 assert.ok(occupied>150&&occupied<32*targetHeight*.95);
 for(let y=0;y<targetHeight;y++)patch.copy(result,((row*32+y)*original.info.width+col*32)*4,y*32*4,(y+1)*32*4);
 evidence.push({row,col,occupied});
}
const areas=[{X:0,Y:0,Width:160,Height:64},{X:0,Y:64,Width:128,Height:39}];
let unchanged=0;
for(let y=0;y<original.info.height;y++)for(let x=0;x<original.info.width;x++){
 if(areas.some(a=>x>=a.X&&x<a.X+a.Width&&y>=a.Y&&y<a.Y+a.Height))continue;
 const p=(y*original.info.width+x)*4;assert.ok(result.subarray(p,p+4).equals(original.data.subarray(p,p+4)));unchanged++;
}
await fs.mkdir('src/AbigailModern/assets/KrobusRaven',{recursive:true});
await sharp(result,{raw:original.info}).png().toFile('src/AbigailModern/assets/KrobusRaven/characters.png');
await sharp(result,{raw:original.info}).extract({left:0,top:0,width:160,height:103}).resize(960,618,{kernel:'nearest'}).png().toFile(work+'prepared-parade.png');
const registryFile='src/AbigailModern/artwork.json';const registry=JSON.parse(await fs.readFile(registryFile,'utf8'));
const entry={Name:'Characters/KrobusRaven',File:'assets/KrobusRaven/characters.png',Width:original.info.width,Height:original.info.height,PatchAreas:areas};
const index=registry.findIndex(a=>a.Name===entry.Name);if(index<0)registry.push(entry);else registry[index]=entry;
await fs.writeFile(registryFile,JSON.stringify(registry,null,2));
await fs.writeFile(work+'validation.json',JSON.stringify({frames:evidence,unchangedPixelsVerified:unchanged,patchAreas:areas,lowerParadeArtUpdated:false,fullEventVerified:false},null,2));
console.log(`Prepared ${evidence.length} parade character frames; ${unchanged} other pixels unchanged.`);
