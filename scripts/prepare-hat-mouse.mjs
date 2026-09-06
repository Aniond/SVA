import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/HatMouse/',out='src/AbigailModern/assets/HatMouse/';await fs.mkdir(out,{recursive:true});
const registryFile='src/AbigailModern/artwork.json',registry=JSON.parse(await fs.readFile(registryFile,'utf8'));
const entry=registry.find(a=>a.Name==='LooseSprites/Cursors');assert.ok(entry);
const atlas=await sharp('src/AbigailModern/'+entry.File).ensureAlpha().raw().toBuffer({resolveWithObject:true}),pixels=Buffer.from(atlas.data);
const area={X:632,Y:1969,Width:13,Height:10};
const head=await sharp(work+'modern-head.png').resize(13,10,{fit:'fill'}).ensureAlpha().raw().toBuffer();
for(let y=0;y<10;y++)head.copy(pixels,((area.Y+y)*atlas.info.width+area.X)*4,y*13*4,(y+1)*13*4);
let unchanged=0;
for(let y=0;y<atlas.info.height;y++)for(let x=0;x<atlas.info.width;x++){
 if(x>=area.X&&x<area.X+13&&y>=area.Y&&y<area.Y+10)continue;
 const p=(y*atlas.info.width+x)*4;assert.ok(pixels.subarray(p,p+4).equals(atlas.data.subarray(p,p+4)));unchanged++;
}
await sharp(pixels,{raw:atlas.info}).png().toFile('src/AbigailModern/'+entry.File);
entry.PatchAreas=[...(entry.PatchAreas??[entry.PatchArea]).filter(a=>JSON.stringify(a)!==JSON.stringify(area)),area];delete entry.PatchArea;
const source=await sharp(work+'modern-portraits.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
for(let p=0;p<source.data.length;p+=4){const[r,g,b]=source.data.subarray(p,p+3);if(r>210&&b>210&&g<60&&Math.abs(r-b)<45)source.data.fill(0,p,p+4);}
const layers=[];
for(let i=0;i<6;i++){
 const x=i%2,y=Math.floor(i/2),left=Math.round(x*source.info.width/2),right=Math.round((x+1)*source.info.width/2),top=Math.round(y*source.info.height/3),bottom=Math.round((y+1)*source.info.height/3);
 const cell=await sharp(source.data,{raw:source.info}).extract({left,top,width:right-left,height:bottom-top}).png().toBuffer();
 const patch=await sharp(cell).trim({background:'#00000000',threshold:1}).resize(64,64,{fit:'contain',position:'bottom',background:'#00000000'}).ensureAlpha().raw().toBuffer();
 for(let p=0;p<patch.length;p+=4)if(patch[p+3]<160)patch.fill(0,p,p+4);else patch[p+3]=255;
 layers.push({input:await sharp(patch,{raw:{width:64,height:64,channels:4}}).png().toBuffer(),left:x*64,top:y*64});
}
await sharp({create:{width:128,height:192,channels:4,background:'#00000000'}}).composite(layers).png().toFile(out+'portraits.png');
const portrait={Name:'Portraits/HatMouse',File:'assets/HatMouse/portraits.png',Width:128,Height:192},index=registry.findIndex(a=>a.Name===portrait.Name);if(index<0)registry.push(portrait);else registry[index]=portrait;
await fs.writeFile(registryFile,JSON.stringify(registry,null,2));
await sharp(pixels,{raw:atlas.info}).extract({left:600,top:1957,width:64,height:32}).resize(768,384,{kernel:'nearest'}).png().toFile(work+'prepared-shop.png');
await fs.writeFile(work+'validation.json',JSON.stringify({headArea:area,unchangedPixelsVerified:unchanged,portraits:6,shopDisplayVerified:false},null,2));
console.log(`Hat Mouse head and six portraits prepared; ${unchanged} other atlas pixels preserved.`);
