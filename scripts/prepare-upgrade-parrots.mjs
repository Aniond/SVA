import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/UpgradeParrots/',out='src/AbigailModern/assets/UpgradeParrots/';await fs.mkdir(out,{recursive:true});
async function grid(file,col,row,cols,rows){const m=await sharp(file).metadata(),left=Math.round(col*m.width/cols),top=Math.round(row*m.height/rows),right=Math.round((col+1)*m.width/cols),bottom=Math.round((row+1)*m.height/rows);return sharp(file).extract({left,top,width:right-left,height:bottom-top}).png().toBuffer();}
const atlas=await sharp('artifacts/npc-modern/originals/LooseSprites/parrots.png').ensureAlpha().raw().toBuffer({resolveWithObject:true}),pixels=Buffer.from(atlas.data);
for(let row=0;row<5;row++)for(let col=0;col<11;col++){
 let cell;
 if(row<2){if(col<2)cell=await grid(work+'green-perched.png',col,0,2,1);else{const index=[0,0,1,2,4,5,6,7,8,9,10][col];cell=await grid(work+'green-draft.png',index%4,Math.floor(index/4),4,3);}}
 else if(row<4){const index=[12,16,13,14,15,1,2,3,9,10,11][col];cell=await grid('src/AbigailModern/assets/IslandParrot/characters.png',index%4,Math.floor(index/4),4,5);if(col===1)cell=await sharp(cell).flop().png().toBuffer();}
 else cell=await grid(work+'golden-sprites.png',col%4,Math.floor(col/4),4,3);
 const size=row===1||row===3?20:24,offset=Math.floor((24-size)/2);
 const scaled=await sharp(cell).resize(size,size,{fit:'fill'}).ensureAlpha().raw().toBuffer();
 const frame=Buffer.alloc(24*24*4);for(let y=0;y<size;y++)scaled.copy(frame,((y+offset)*24+offset)*4,y*size*4,(y+1)*size*4);
 for(let p=0;p<frame.length;p+=4)if(frame[p+3]<160)frame.fill(0,p,p+4);else frame[p+3]=255;
 for(let y=0;y<24;y++)frame.copy(pixels,((row*24+y)*264+col*24)*4,y*96,(y+1)*96);
}
assert.ok(pixels.subarray(120*264*4).equals(atlas.data.subarray(120*264*4)));
await sharp(pixels,{raw:atlas.info}).png().toFile(out+'parrots.png');
await sharp(pixels,{raw:atlas.info}).extract({left:0,top:0,width:264,height:120}).resize(1584,720,{kernel:'nearest'}).png().toFile(work+'prepared-sprites.png');
for(const name of ['green','golden']){
 const layers=[];
 for(let i=0;i<6;i++){
  const row=Math.floor(i/2),left=row===2?(i%2?570:0):i%2*512,width=row===2?(i%2?454:560):512;
  const top=(name==='green'?[12,530,1024]:[16,530,1032])[row],height=(name==='green'?[482,466,480]:[488,478,504])[row];
  const cell=await sharp(work+name+'-portraits.png').extract({left,top,width,height}).png().toBuffer();
  const data=await sharp(cell).trim({background:'#00000000',threshold:1}).resize(64,64,{fit:'contain',position:'bottom',background:'#00000000'}).ensureAlpha().raw().toBuffer();for(let p=0;p<data.length;p+=4)if(data[p+3]<160)data.fill(0,p,p+4);else data[p+3]=255;
  layers.push({input:await sharp(data,{raw:{width:64,height:64,channels:4}}).png().toBuffer(),left:i%2*64,top:row*64});
 }
 await sharp({create:{width:128,height:192,channels:4,background:'#00000000'}}).composite(layers).png().toFile(out+name+'-portraits.png');
}
const file='src/AbigailModern/artwork.json',registry=JSON.parse(await fs.readFile(file,'utf8'));
for(const entry of [{Name:'LooseSprites/parrots',File:'assets/UpgradeParrots/parrots.png',Width:264,Height:288,PatchArea:{X:0,Y:0,Width:264,Height:120}},{Name:'Portraits/UpgradeParrot',File:'assets/UpgradeParrots/green-portraits.png',Width:128,Height:192},{Name:'Portraits/GoldenParrot',File:'assets/UpgradeParrots/golden-portraits.png',Width:128,Height:192}]){const idx=registry.findIndex(a=>a.Name===entry.Name);if(idx<0)registry.push(entry);else registry[idx]=entry;}
await fs.writeFile(file,JSON.stringify(registry,null,2));await fs.writeFile(work+'validation.json',JSON.stringify({frames:55,portraitExpressions:12,lowerPixelsUnchanged:264*168,runtimeVerified:false},null,2));console.log('Prepared55frames and12portraits; lower44,352pixels preserved.');
