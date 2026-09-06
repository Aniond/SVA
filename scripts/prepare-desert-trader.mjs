import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/DesertTrader/',out='src/AbigailModern/assets/DesertTrader/';
await fs.mkdir(out,{recursive:true});
async function keyed(file){const r=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});for(let p=0;p<r.data.length;p+=4){const [a,g,b]=r.data.subarray(p,p+3);if(a>210&&b>210&&g<60&&Math.abs(a-b)<45)r.data.fill(0,p,p+4);}return r;}
const source=await keyed(work+'modern-sprites.png');
const registryFile='src/AbigailModern/artwork.json',registry=JSON.parse(await fs.readFile(registryFile,'utf8'));
const registeredAtlas=registry.find(a=>a.Name==='LooseSprites/temporary_sprites_1');
const atlas=await sharp(registeredAtlas?'src/AbigailModern/'+registeredAtlas.File:'artifacts/npc-modern/originals/LooseSprites/temporary_sprites_1.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
const pixels=Buffer.from(atlas.data);
for(let i=0;i<8;i++){
 const left=60+(i%4)*420,top=i<4?60:458;
 const crop=await sharp(source.data,{raw:source.info}).extract({left,top,width:380,height:365}).png().toBuffer();
 const cell=await sharp(crop).resize(20,26,{fit:'fill'}).ensureAlpha().raw().toBuffer();
 for(let y=0;y<26;y++)for(let x=0;x<20;x++){
  const p=(y*20+x)*4,q=((614+y)*atlas.info.width+i*20+x)*4;
  const bg=y<20?[93,44,32,255]:[237,183,132,255];
  pixels.set(cell[p+3]>=160?[cell[p],cell[p+1],cell[p+2],255]:bg,q);
 }
}
let unchanged=0;for(let y=0;y<640;y++)for(let x=0;x<512;x++){if(y>=614&&x<160)continue;const p=(y*512+x)*4;assert.ok(pixels.subarray(p,p+4).equals(atlas.data.subarray(p,p+4)));unchanged++;}
await sharp(pixels,{raw:atlas.info}).png().toFile(registeredAtlas?'src/AbigailModern/'+registeredAtlas.File:out+'temporary_sprites_1.png');
await sharp(pixels,{raw:atlas.info}).extract({left:0,top:614,width:160,height:26}).resize(1600,260,{kernel:'nearest'}).png().toFile(work+'prepared-sprites.png');
const portraits=await keyed(work+'modern-portraits.png'),layers=[];
for(let i=0;i<6;i++){
 const x=i%2,y=Math.floor(i/2),left=Math.round(x*portraits.info.width/2),right=Math.round((x+1)*portraits.info.width/2),top=Math.round(y*portraits.info.height/3),bottom=Math.round((y+1)*portraits.info.height/3);
 const cell=await sharp(portraits.data,{raw:portraits.info}).extract({left,top,width:right-left,height:bottom-top}).png().toBuffer();
 const patch=await sharp(cell).trim({background:'#00000000',threshold:1}).resize(64,64,{fit:'contain',position:'bottom',background:'#00000000'}).png().toBuffer();layers.push({input:patch,left:x*64,top:y*64});
}
await sharp({create:{width:128,height:192,channels:4,background:'#00000000'}}).composite(layers).png().toFile(out+'portraits.png');
const ownArea={X:0,Y:614,Width:160,Height:26};
const otherAreas=(registeredAtlas?.PatchAreas??(registeredAtlas?.PatchArea?[registeredAtlas.PatchArea]:[])).filter(a=>JSON.stringify(a)!==JSON.stringify(ownArea));
for(const entry of [{Name:'LooseSprites/temporary_sprites_1',File:registeredAtlas?.File??'assets/DesertTrader/temporary_sprites_1.png',Width:512,Height:640,PatchAreas:[...otherAreas,ownArea]},{Name:'Portraits/DesertTrader',File:'assets/DesertTrader/portraits.png',Width:128,Height:192}]){const index=registry.findIndex(a=>a.Name===entry.Name);if(index<0)registry.push(entry);else registry[index]=entry;}
await fs.writeFile(registryFile,JSON.stringify(registry,null,2));
await fs.writeFile(work+'validation.json',JSON.stringify({unchangedPixelsVerified:unchanged,frames:8,portraits:6,backgroundReconstructed:true,fullSceneVerified:false},null,2));
console.log(`Prepared Desert Trader; ${unchanged} outside pixels preserved.`);
