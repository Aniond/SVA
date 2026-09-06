import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/Children/';
const variant=process.argv[2]??'light';
assert.ok(['light','dark'].includes(variant),'Use light or dark');
const prefix='baby-'+variant+'-';
async function keyed(name){const raw=await sharp(work+name+'.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});for(let p=0;p<raw.data.length;p+=4){const r=raw.data[p],g=raw.data[p+1],b=raw.data[p+2];if(r>g+40&&b>g+40)raw.data.fill(0,p,p+4);}return sharp(raw.data,{raw:raw.info}).png().toBuffer();}
async function frame(image,col,row,cols,rows,w,h,bodyW,bodyH,bottom=1,explicit){const m=await sharp(image).metadata();const left=Math.round(col*m.width/cols),right=Math.round((col+1)*m.width/cols),top=explicit?.[row]??Math.round(row*m.height/rows),end=explicit?.[row+1]??Math.round((row+1)*m.height/rows);const crop=await sharp(image).extract({left,top,width:right-left,height:end-top}).png().toBuffer();const input=await sharp(crop).trim({background:'#00000000',threshold:1}).resize(bodyW,bodyH,{fit:'contain',position:'bottom',background:'#00000000'}).extend({left:Math.floor((w-bodyW)/2),right:Math.ceil((w-bodyW)/2),top:h-bodyH-bottom,bottom,background:'#00000000'}).ensureAlpha().raw().toBuffer();for(let p=0;p<input.length;p+=4){if(input[p+3]<160)input.fill(0,p,p+4);else input[p+3]=255;}return sharp(input,{raw:{width:w,height:h,channels:4}}).png().toBuffer();}
const layers=[];
for(const [name,rows,h,count,top,bw,bh] of [['newborn',2,16,8,0,21,12],['upright',2,32,8,32,20,25],['crawler',6,16,23,96,21,15]]){
 const input=await keyed(prefix+name);
 for(let i=0;i<count;i++)layers.push({input:await frame(input,i%4,Math.floor(i/4),4,rows,22,h,bw,bh),left:i%4*22,top:top+Math.floor(i/4)*h});
}
await sharp({create:{width:88,height:192,channels:4,background:'#00000000'}}).composite(layers).png().toFile(work+prefix+'prepared.png');
const portrait=await keyed(prefix+'portraits'),portraits=[];
for(let i=0;i<6;i++)portraits.push({input:await frame(portrait,i%2,Math.floor(i/2),2,3,64,64,60,60,2,[0,535,1008,1536]),left:i%2*64,top:Math.floor(i/2)*64});
await sharp({create:{width:128,height:192,channels:4,background:'#00000000'}}).composite(portraits).png().toFile(work+prefix+'prepared-portraits.png');
await sharp(work+prefix+'prepared.png').resize(528,1152,{kernel:'nearest'}).toFile(work+prefix+'preview.png');
const last=await sharp(work+prefix+'prepared.png').extract({left:66,top:176,width:22,height:16}).raw().toBuffer();assert.ok(last.every(x=>x===0),'Unused cell must remain blank');
await fs.writeFile(work+prefix+'validation.json',JSON.stringify({NewbornFrames:8,UprightFrames:8,CrawlerFrames:23,PortraitExpressions:6,UnusedCellBlank:true,Installed:false,RuntimeVerified:false},null,2));
console.log('Prepared39babyposes and6portraits; mixednativeframe layout retained.');
