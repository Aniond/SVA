import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/Children/';
const gender=process.argv[2]??'boy',skin=process.argv[3]??'light';
assert.ok(['boy','girl'].includes(gender)&&['light','dark'].includes(skin));
const prefix=`toddler-${gender}-${skin}-`,native=`Toddler${gender==='girl'?'_girl':''}${skin==='dark'?'_dark':''}`;
async function key(file){const raw=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});for(let p=0;p<raw.data.length;p+=4){const r=raw.data[p],g=raw.data[p+1],b=raw.data[p+2];if(r>g+100&&b>g+100&&r>b*0.88)raw.data.fill(0,p,p+4);}return sharp(raw.data,{raw:raw.info}).png().toBuffer();}
async function crop(image,col,row,cols,bounds){const m=await sharp(image).metadata(),left=Math.round(col*m.width/cols),right=Math.round((col+1)*m.width/cols);return sharp(image).extract({left,top:bounds[row],width:right-left,height:bounds[row+1]-bounds[row]}).png().toBuffer();}
async function threshold(image){const raw=await sharp(image).ensureAlpha().raw().toBuffer({resolveWithObject:true});for(let p=0;p<raw.data.length;p+=4){if(raw.data[p+3]<160)raw.data.fill(0,p,p+4);else raw.data[p+3]=255;}return sharp(raw.data,{raw:raw.info}).png().toBuffer();}
const original=await sharp(`artifacts/npc-modern/originals/Characters/${native}.png`).ensureAlpha().raw().toBuffer();
const source=await key(work+prefix+'sprites.png'),layers=[];
const bounds=gender==='boy'?[0,286,525,770,1005,1236,1536]:[0,275,518,758,1007,1250,1536];
for(let i=0;i<24;i++){
 let minX=16,maxX=0,minY=32,maxY=0;
 for(let y=0;y<32;y++)for(let x=0;x<16;x++)if(original[((Math.floor(i/4)*32+y)*64+i%4*16+x)*4+3]>0){minX=Math.min(minX,x);maxX=Math.max(maxX,x);minY=Math.min(minY,y);maxY=Math.max(maxY,y);}
 let cell=await crop(source,i%4,Math.floor(i/4),4,bounds);
 // Generated special poses face left; native special poses face right.
 if(i>=20)cell=await sharp(cell).flop().png().toBuffer();
 cell=await sharp(cell).trim({background:'#00000000',threshold:1}).resize(maxX-minX+1,maxY-minY+1,{fit:'fill'}).png().toBuffer();
 layers.push({input:await threshold(cell),left:i%4*16+minX,top:Math.floor(i/4)*32+minY});
}
await sharp({create:{width:64,height:192,channels:4,background:'#00000000'}}).composite(layers).png().toFile(work+prefix+'prepared.png');
const portrait=await key(work+prefix+'portraits.png'),portraits=[];
const portraitBounds=gender==='boy'?[0,440,826,1254]:[0,540,1018,1536];
for(let i=0;i<6;i++){
 const index=gender==='boy'?i:[0,2,3,4,1,5][i];
 let cell=await crop(portrait,index%2,Math.floor(index/2),2,portraitBounds);
 // The boy sleeping portrait includes detached Z marks beyond the head.
 if(gender==='boy'&&i===5)cell=await sharp(cell).extract({left:0,top:0,width:390,height:428}).png().toBuffer();
 cell=await sharp(cell).trim({background:'#00000000',threshold:1}).resize(60,60,{fit:'contain',position:'bottom',background:'#00000000'}).png().toBuffer();
 portraits.push({input:await threshold(cell),left:i%2*64+2,top:Math.floor(i/2)*64+2});
}
await sharp({create:{width:128,height:192,channels:4,background:'#00000000'}}).composite(portraits).png().toFile(work+prefix+'prepared-portraits.png');
await sharp(work+prefix+'prepared.png').resize(384,1152,{kernel:'nearest'}).toFile(work+prefix+'preview.png');
console.log(`Prepared ${prefix}24frames and6portraits; native frame bounds retained. Not installed.`);
