import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/IslandTrader/',out='src/AbigailModern/assets/IslandTrader/';await fs.mkdir(out,{recursive:true});
async function keyed(file){const r=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});for(let p=0;p<r.data.length;p+=4){const[a,g,b]=r.data.subarray(p,p+3);if(a>210&&b>210&&g<60&&Math.abs(a-b)<45)r.data.fill(0,p,p+4);}return r;}
const ids=JSON.parse(await fs.readFile(work+'frame-indices.json','utf8')),source=await keyed(work+'modern-sprites.png');
const original=await sharp('artifacts/npc-modern/originals/Maps/island_tilesheet_1.png').ensureAlpha().raw().toBuffer({resolveWithObject:true}),pixels=Buffer.from(original.data),areas=[],previews=[];
for(let i=0;i<ids.length;i++){
 const cell=await sharp(source.data,{raw:source.info}).extract({left:[22,252,484,712,940,1164][i%6],top:[110,442,772][Math.floor(i/6)],width:230,height:260}).png().toBuffer();
 const patch=await sharp(cell).resize(16,16,{fit:'fill'}).ensureAlpha().raw().toBuffer();for(let p=0;p<patch.length;p+=4)if(patch[p+3]<160)patch.fill(0,p,p+4);else patch[p+3]=255;
 const X=ids[i]%32*16,Y=Math.floor(ids[i]/32)*16;areas.push({X,Y,Width:16,Height:16});
 for(let y=0;y<16;y++)patch.copy(pixels,((Y+y)*512+X)*4,y*64,(y+1)*64);
 previews.push({input:await sharp(patch,{raw:{width:16,height:16,channels:4}}).png().toBuffer(),left:i%6*16,top:Math.floor(i/6)*16});
}
let unchanged=0;for(let y=0;y<1040;y++)for(let x=0;x<512;x++){if(areas.some(a=>x>=a.X&&x<a.X+16&&y>=a.Y&&y<a.Y+16))continue;const p=(y*512+x)*4;assert.ok(pixels.subarray(p,p+4).equals(original.data.subarray(p,p+4)));unchanged++;}
await sharp(pixels,{raw:original.info}).png().toFile(out+'island_tilesheet_1.png');
const preview=await sharp({create:{width:96,height:48,channels:4,background:'#00000000'}}).composite(previews).png().toBuffer();await sharp(preview).resize(1152,576,{kernel:'nearest'}).png().toFile(work+'prepared-sprites.png');
const portrait=await keyed(work+'modern-portraits.png'),layers=[];
for(let i=0;i<6;i++){const x=i%2,y=Math.floor(i/2),left=Math.round(x*portrait.info.width/2),right=Math.round((x+1)*portrait.info.width/2),top=Math.round(y*portrait.info.height/3),bottom=Math.round((y+1)*portrait.info.height/3);const cell=await sharp(portrait.data,{raw:portrait.info}).extract({left,top,width:right-left,height:bottom-top}).png().toBuffer();layers.push({input:await sharp(cell).trim({background:'#00000000',threshold:1}).resize(64,64,{fit:'contain',position:'bottom',background:'#00000000'}).png().toBuffer(),left:x*64,top:y*64});}
await sharp({create:{width:128,height:192,channels:4,background:'#00000000'}}).composite(layers).png().toFile(out+'portraits.png');
const file='src/AbigailModern/artwork.json',registry=JSON.parse(await fs.readFile(file,'utf8'));for(const entry of [{Name:'Maps/island_tilesheet_1',File:'assets/IslandTrader/island_tilesheet_1.png',Width:512,Height:1040,PatchAreas:areas},{Name:'Portraits/IslandTrader',File:'assets/IslandTrader/portraits.png',Width:128,Height:192}]){assert.ok(!registry.some(a=>a.Name===entry.Name),'Already registered; review shared regions before regeneration');registry.push(entry);}await fs.writeFile(file,JSON.stringify(registry,null,2));await fs.writeFile(work+'validation.json',JSON.stringify({frames:18,portraits:6,unchangedPixelsVerified:unchanged,indices:ids,fullSceneVerified:false},null,2));console.log(`Prepared 18 frames; ${unchanged} outside pixels preserved.`);
