import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/TravelingMerchant/',out='src/AbigailModern/assets/TravelingMerchant/';await fs.mkdir(out,{recursive:true});
const registryFile='src/AbigailModern/artwork.json',registry=JSON.parse(await fs.readFile(registryFile,'utf8'));
const entry=registry.find(a=>a.Name==='LooseSprites/Cursors');assert.ok(entry);
const atlas=await sharp('src/AbigailModern/'+entry.File).ensureAlpha().raw().toBuffer({resolveWithObject:true}),pixels=Buffer.from(atlas.data);
const frames=[];
for(const left of [30,495,960,1425])frames.push(await sharp(work+'modern-blink.png').extract({left,top:120,width:470,height:570}).resize(16,14,{fit:'fill'}).ensureAlpha().raw().toBuffer());
const areas=[{X:194,Y:1414,Width:16,Height:14},{X:89,Y:1445,Width:18,Height:3}];
for(let y=0;y<14;y++)frames[0].copy(pixels,((1414+y)*atlas.info.width+194)*4,y*16*4,(y+1)*16*4);
for(let i=0;i<3;i++)for(let y=0;y<3;y++)frames[i+1].copy(pixels,((1445+y)*atlas.info.width+89+i*6)*4,((6+y)*16+5)*4,((6+y)*16+11)*4);
let unchanged=0;
for(let y=0;y<atlas.info.height;y++)for(let x=0;x<atlas.info.width;x++){
 if(areas.some(a=>x>=a.X&&x<a.X+a.Width&&y>=a.Y&&y<a.Y+a.Height))continue;
 const p=(y*atlas.info.width+x)*4;assert.ok(pixels.subarray(p,p+4).equals(atlas.data.subarray(p,p+4)));unchanged++;
}
await sharp(pixels,{raw:atlas.info}).png().toFile('src/AbigailModern/'+entry.File);
entry.PatchAreas=[...(entry.PatchAreas??[entry.PatchArea]).filter(a=>!areas.some(b=>JSON.stringify(a)===JSON.stringify(b))),...areas];delete entry.PatchArea;
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
const portrait={Name:'Portraits/TravelingMerchant',File:'assets/TravelingMerchant/portraits.png',Width:128,Height:192},index=registry.findIndex(a=>a.Name===portrait.Name);if(index<0)registry.push(portrait);else registry[index]=portrait;
await fs.writeFile(registryFile,JSON.stringify(registry,null,2));
await sharp(pixels,{raw:atlas.info}).extract({left:142,top:1382,width:109,height:70}).resize(872,560,{kernel:'nearest'}).png().toFile(work+'prepared-cart.png');
const previews=[];
for(let i=0;i<4;i++){
 const face=Buffer.from(frames[0]);if(i>0)for(let y=0;y<3;y++)frames[i].copy(face,((6+y)*16+5)*4,((6+y)*16+5)*4,((6+y)*16+11)*4);
 previews.push({input:await sharp(face,{raw:{width:16,height:14,channels:4}}).png().toBuffer(),left:i*16,top:0});
}
const contact=await sharp({create:{width:64,height:14,channels:4,background:'#00000000'}}).composite(previews).png().toBuffer();await sharp(contact).resize(1024,224,{kernel:'nearest'}).png().toFile(work+'prepared-blink.png');
await fs.writeFile(work+'validation.json',JSON.stringify({areas,unchangedPixelsVerified:unchanged,portraits:6,blinkFrames:3,otherFestivalArtVerified:false},null,2));
console.log(`Traveling Merchant prepared; ${unchanged} other atlas pixels preserved.`);
