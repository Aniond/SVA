import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/Raccoons/';
const out='src/AbigailModern/assets/Raccoons/';await fs.mkdir(out,{recursive:true});
async function keyed(file){
 const image=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 for(let p=0;p<image.data.length;p+=4)if(image.data[p]>50&&image.data[p+2]>50&&image.data[p+1]<Math.min(image.data[p],image.data[p+2])*.5&&Math.abs(image.data[p]-image.data[p+2])<60)image.data.fill(0,p,p+4);
 return image;
}
async function cell(image,cols,rows,i,w,h){
 const x=i%cols,y=Math.floor(i/cols),left=Math.round(x*image.info.width/cols),right=Math.round((x+1)*image.info.width/cols),top=Math.round(y*image.info.height/rows),bottom=Math.round((y+1)*image.info.height/rows);
 const crop=await sharp(image.data,{raw:image.info}).extract({left,top,width:right-left,height:bottom-top}).png().toBuffer();
 const pixels=await sharp(crop).trim({background:'#00000000',threshold:1}).resize(w,h,{fit:'contain',position:'bottom',background:'#00000000'}).ensureAlpha().raw().toBuffer();
 let occupied=0;
 for(let p=0;p<pixels.length;p+=4)if(pixels[p+3]<160)pixels.fill(0,p,p+4);else{pixels[p+3]=255;occupied++;}
 assert.ok(occupied>w*h*.12&&occupied<w*h*.97,`Invalid occupancy ${i}: ${occupied}`);
 return sharp(pixels,{raw:{width:w,height:h,channels:4}}).png().toBuffer();
}
const sprites=[];
for(let chunk=0;chunk<4;chunk++){
 const image=await keyed(work+`modern-sprites-${chunk}.png`);
 for(let i=0;i<16;i++){const frame=chunk*16+i;sprites.push({input:await cell(image,4,4,i,30,28),left:(frame%8)*32+1,top:Math.floor(frame/8)*32+3});}
}
await sharp({create:{width:256,height:256,channels:4,background:'#00000000'}}).composite(sprites).png().toFile(out+'characters.png');
const newEntries=[{Name:'Characters/raccoon',File:'assets/Raccoons/characters.png',Width:256,Height:256}];
const mapping=JSON.parse(await fs.readFile(work+'alternate-mapping.json','utf8'));
const couple=await keyed(work+'modern-alternate-couple.png');
const extra=await keyed(work+'modern-alternate-extra.png');
const alternate=[];
for(let i=0;i<64;i++){
 const source=mapping[i];
 if(source>=0){
  const originalArea={left:(source%8)*32,top:Math.floor(source/8)*32,width:32,height:32};
  const destinationArea={left:(i%8)*32,top:Math.floor(i/8)*32,width:32,height:32};
  const old=await sharp('artifacts/npc-modern/originals/Characters/raccoon.png').extract(originalArea).raw().toBuffer();
  const variant=await sharp('artifacts/npc-modern/originals/Characters/mrs_raccoon.png').extract(destinationArea).raw().toBuffer();
  assert.ok(old.equals(variant),'Alternate frame mapping must match original pixels.');
  alternate.push({input:await sharp(out+'characters.png').extract(originalArea).png().toBuffer(),left:destinationArea.left,top:destinationArea.top});
 }else{
  assert.ok(i>=8&&i<24||i>=56&&i<64,'Unmapped alternate pose.');
  alternate.push({input:await cell(i>=56?extra:couple,4,i>=56?2:4,i>=56?i-56:i-8,30,28),left:(i%8)*32+1,top:Math.floor(i/8)*32+3});
 }
}
await sharp({create:{width:256,height:256,channels:4,background:'#00000000'}}).composite(alternate).png().toFile(out+'alternate-characters.png');
await sharp(out+'alternate-characters.png').resize(768,768,{kernel:'nearest'}).png().toFile(work+'prepared-alternate-characters.png');
newEntries.push({Name:'Characters/mrs_raccoon',File:'assets/Raccoons/alternate-characters.png',Width:256,Height:256});
for(const name of ['MrRaccoon','MrsRaccoon']){
 const image=await keyed(work+`modern-${name}-portraits.png`),layers=[];
 for(let i=0;i<6;i++)layers.push({input:await cell(image,2,3,i,64,64),left:(i%2)*64,top:Math.floor(i/2)*64});
 await sharp({create:{width:128,height:192,channels:4,background:'#00000000'}}).composite(layers).png().toFile(out+name+'-portraits.png');
 await sharp(out+name+'-portraits.png').resize(512,768,{kernel:'nearest'}).png().toFile(work+`prepared-${name}-portraits.png`);
 newEntries.push({Name:'Portraits/'+name,File:'assets/Raccoons/'+name+'-portraits.png',Width:128,Height:192});
}
await sharp(out+'characters.png').resize(768,768,{kernel:'nearest'}).png().toFile(work+'prepared-characters.png');
let registry=JSON.parse(await fs.readFile('src/AbigailModern/artwork.json','utf8'));
registry=registry.filter(a=>!newEntries.some(n=>n.Name===a.Name));registry.push(...newEntries);
await fs.writeFile('src/AbigailModern/artwork.json',JSON.stringify(registry,null,2));
await fs.writeFile(work+'validation.json',JSON.stringify({spriteFramesUpdated:64,newPortraits:['MrRaccoon','MrsRaccoon'],expressionsPerPortrait:6,alternateMrsSheetUpdated:true,alternateReusedFrames:mapping.filter(i=>i>=0).length,alternateUniqueFrames:mapping.filter(i=>i<0).length,bundleMenuContainsNpcArt:false,liveSceneVerified:false},null,2));
console.log('64 shared sprite frames and two six-expression portraits prepared.');
