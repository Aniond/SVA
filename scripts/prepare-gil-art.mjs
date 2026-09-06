import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/Gil/';
const out='src/AbigailModern/assets/Gil/';
await fs.mkdir(out,{recursive:true});
async function keyed(file){
 const im=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 for(let p=0;p<im.data.length;p+=4)if(im.data[p]>170&&im.data[p+2]>170&&im.data[p+1]<100&&Math.abs(im.data[p]-im.data[p+2])<60)im.data.fill(0,p,p+4);
 return im;
}
const portraits=await keyed(work+'modern-portraits.png');
const layers=[];
for(let i=0;i<2;i++){
 const left=Math.round(i*portraits.info.width/2),right=Math.round((i+1)*portraits.info.width/2);
 const crop=await sharp(portraits.data,{raw:portraits.info}).extract({left,top:0,width:right-left,height:portraits.info.height}).png().toBuffer();
 const pix=await sharp(crop).trim({background:'#00000000',threshold:1}).resize(64,64,{fit:'contain',position:'bottom',background:'#00000000'}).ensureAlpha().raw().toBuffer();
 for(let p=0;p<pix.length;p+=4){if(pix[p+3]<160)pix.fill(0,p,p+4);else pix[p+3]=255;}
 layers.push({input:await sharp(pix,{raw:{width:64,height:64,channels:4}}).png().toBuffer(),left:i*64,top:0});
}
await sharp({create:{width:128,height:64,channels:4,background:'#00000000'}}).composite(layers).png().toFile(out+'portraits.png');
const original=await sharp('artifacts/npc-modern/originals/Maps/townInterior.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
const pixels=Buffer.from(original.data);
const poses=[['modern-sprite.png',176,656],['modern-rock-middle.png',176,624],['modern-rock-back.png',208,656]];
const areas=poses.map(([,X,Y])=>({X,Y,Width:32,Height:32}));
for(const [file,left,top] of poses){
 const generated=await keyed(work+file);
 const sprite=await sharp(generated.data,{raw:generated.info}).resize(32,32,{fit:'fill'}).ensureAlpha().raw().toBuffer();
 for(let y=0;y<32;y++)for(let x=0;x<32;x++){
  const p=((top+y)*original.info.width+left+x)*4,q=(y*32+x)*4;
  pixels.fill(0,p,p+4);
  if(sprite[q+3]>=160){sprite.copy(pixels,p,q,q+3);pixels[p+3]=255;}
 }
}
let untouched=0;
for(let y=0;y<original.info.height;y++)for(let x=0;x<original.info.width;x++){
 if(areas.some(a=>x>=a.X&&x<a.X+a.Width&&y>=a.Y&&y<a.Y+a.Height))continue;
 const p=(y*original.info.width+x)*4;assert.ok(original.data.subarray(p,p+4).equals(pixels.subarray(p,p+4)));untouched++;
}
await sharp(pixels,{raw:original.info}).png().toFile(out+'townInterior.png');
await sharp(out+'townInterior.png').extract({left:176,top:656,width:32,height:32}).resize(512,512,{kernel:'nearest'}).png().toFile(work+'prepared-sprite.png');
await sharp(out+'portraits.png').resize(512,256,{kernel:'nearest'}).png().toFile(work+'prepared-portraits.png');
const registryPath='src/AbigailModern/artwork.json';
let registry=JSON.parse(await fs.readFile(registryPath,'utf8'));
const existing=registry.find(a=>a.Name==='Maps/townInterior');assert.ok(!existing||existing.File==='assets/Gil/townInterior.png','Merge shared map patches explicitly');
registry=registry.filter(a=>!['Maps/townInterior','Portraits/Gil'].includes(a.Name));
registry.push({Name:'Portraits/Gil',File:'assets/Gil/portraits.png',Width:128,Height:64},{Name:'Maps/townInterior',File:'assets/Gil/townInterior.png',Width:original.info.width,Height:original.info.height,PatchAreas:areas});
await fs.writeFile(registryPath,JSON.stringify(registry,null,2));
await fs.writeFile(work+'validation.json',JSON.stringify({updatedPortraits:[0,1],patches:areas,untouchedPixelsVerified:untouched,guildTileIndices:[1323,1324,1355,1356,1259,1260,1291,1292,1325,1326,1357,1358],otherChairPosesUpdated:true,sceneVerified:false},null,2));
const frames=[];
for(let i=0;i<poses.length;i++)frames.push({input:await sharp(out+'townInterior.png').extract({left:poses[i][1],top:poses[i][2],width:32,height:32}).png().toBuffer(),left:i*32,top:0});
await sharp({create:{width:96,height:32,channels:4,background:'#00000000'}}).composite(frames).png().toFile(work+'rocking-contact.png');
console.log(`${untouched} unrelated map pixels preserved; Gil portrait and all three rocking poses prepared.`);
