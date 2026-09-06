import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/SeaMonsterKrobus/';
const source=await sharp(work+'modern-characters.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
assert.equal(source.info.width,1024);assert.equal(source.info.height,1536);
for(let p=0;p<source.data.length;p+=4){const[r,g,b]=source.data.subarray(p,p+3);if(r>210&&b>210&&g<60&&Math.abs(r-b)<45)source.data.fill(0,p,p+4);}
const rows=[[0,260],[280,260],[560,240],[820,240],[1080,224],[1340,188]],layers=[],evidence=[];
for(let row=0;row<6;row++)for(let col=0;col<4;col++){
 const[top,height]=rows[row];
 // Use identical280px logical height across rows; never enlarge surfacing fragments.
 const padded=await sharp(source.data,{raw:source.info}).extract({left:col*256,top,width:256,height}).extend({top:280-height,bottom:0,left:0,right:0,background:'#00000000'}).png().toBuffer();
 const patch=await sharp(padded).resize(32,32,{fit:'fill'}).ensureAlpha().raw().toBuffer();
 let occupied=0;for(let p=0;p<patch.length;p+=4){if(patch[p+3]<160)patch.fill(0,p,p+4);else{patch[p+3]=255;occupied++;}}
 assert.ok(occupied>12&&occupied<900);evidence.push({frame:row*4+col,occupied});
 layers.push({input:await sharp(patch,{raw:{width:32,height:32,channels:4}}).png().toBuffer(),left:col*32,top:row*32});
}
const out='src/AbigailModern/assets/SeaMonsterKrobus';await fs.mkdir(out,{recursive:true});
await sharp({create:{width:128,height:192,channels:4,background:'#00000000'}}).composite(layers).png().toFile(out+'/characters.png');
await sharp(out+'/characters.png').resize(768,1152,{kernel:'nearest'}).png().toFile(work+'prepared-characters.png');
const registryFile='src/AbigailModern/artwork.json';const registry=JSON.parse(await fs.readFile(registryFile,'utf8'));
const entry={Name:'Characters/SeaMonsterKrobus',File:'assets/SeaMonsterKrobus/characters.png',Width:128,Height:192};
const index=registry.findIndex(a=>a.Name===entry.Name);if(index<0)registry.push(entry);else registry[index]=entry;
await fs.writeFile(registryFile,JSON.stringify(registry,null,2));
await fs.writeFile(work+'validation.json',JSON.stringify({frames:evidence,baseSpriteComplete:true,uniformLogicalHeight:280,fullBeachEventVerified:false},null,2));
console.log(`Prepared ${evidence.length} sea-monster frames.`);
