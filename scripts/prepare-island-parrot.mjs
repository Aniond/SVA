import fs from 'node:fs/promises';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/IslandParrot/',out='src/AbigailModern/assets/IslandParrot/';await fs.mkdir(out,{recursive:true});
const layers=[];
for(let i=0;i<18;i++){
 const left=Math.round(i%4*1122/4),right=Math.round((i%4+1)*1122/4),top=Math.round(Math.floor(i/4)*1402/5),bottom=Math.round((Math.floor(i/4)+1)*1402/5);
 const patch=await sharp(work+'modern-sprites.png').extract({left,top,width:right-left,height:bottom-top}).resize(32,32,{fit:'fill'}).ensureAlpha().raw().toBuffer();
 for(let p=0;p<patch.length;p+=4)if(patch[p+3]<160)patch.fill(0,p,p+4);else patch[p+3]=255;
 layers.push({input:await sharp(patch,{raw:{width:32,height:32,channels:4}}).png().toBuffer(),left:i%4*32,top:Math.floor(i/4)*32});
}
await sharp({create:{width:128,height:160,channels:4,background:'#00000000'}}).composite(layers).png().toFile(out+'characters.png');
await sharp(out+'characters.png').resize(768,960,{kernel:'nearest'}).png().toFile(work+'prepared-sprites.png');
const portraits=[];
for(let i=0;i<6;i++){
 const row=Math.floor(i/2),left=row===2?(i%2?580:0):i%2*512,width=row===2?(i%2?444:570):512;
 const cell=await sharp(work+'modern-portraits.png').extract({left,top:[24,522,998][row],width,height:[470,454,490][row]}).png().toBuffer();
 portraits.push({input:await sharp(cell).trim({background:'#00000000',threshold:1}).resize(64,64,{fit:'contain',position:'bottom',background:'#00000000'}).png().toBuffer(),left:i%2*64,top:Math.floor(i/2)*64});
}
await sharp({create:{width:128,height:192,channels:4,background:'#00000000'}}).composite(portraits).png().toFile(out+'portraits.png');
const file='src/AbigailModern/artwork.json',registry=JSON.parse(await fs.readFile(file,'utf8'));
for(const entry of [{Name:'Characters/IslandParrot',File:'assets/IslandParrot/characters.png',Width:128,Height:160},{Name:'Portraits/IslandParrot',File:'assets/IslandParrot/portraits.png',Width:128,Height:192}]){const idx=registry.findIndex(a=>a.Name===entry.Name);if(idx<0)registry.push(entry);else registry[idx]=entry;}await fs.writeFile(file,JSON.stringify(registry,null,2));
console.log('Prepared 18 parrot frames, two blank cells and six portraits.');
