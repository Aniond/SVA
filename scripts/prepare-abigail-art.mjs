import { createRequire } from 'node:module';
const require = createRequire(new URL('../.tools/abigail-art/package.json', import.meta.url));
const sharp = require('sharp');
import fs from 'node:fs/promises';
import assert from 'node:assert/strict';

const input = new URL('../artifacts/abigail-modern/', import.meta.url);
const output = new URL('../src/AbigailModern/assets/', import.meta.url);
await fs.mkdir(output, { recursive: true });

// Convert the generated RGB previews into transparent game textures.
// Their only near-white neutral pixels are the baked checkerboard matte.
async function unmatte(name) {
  const { data, info } = await sharp(new URL(name, input).pathname.replace(/^\/(\w:)/, '$1')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  for(let p=0;p<data.length;p+=4) {
    const lo=Math.min(data[p],data[p+1],data[p+2]);
    const hi=Math.max(data[p],data[p+1],data[p+2]);
    if(lo>=190 && hi-lo<=24) data.fill(0,p,p+4);
  }
  return {data,info};
}
async function cell(source, cols, rows, index, width, height) {
  const col=index%cols, row=Math.floor(index/cols);
  const left=Math.round(col*source.info.width/cols), top=Math.round(row*source.info.height/rows);
  const right=Math.round((col+1)*source.info.width/cols), bottom=Math.round((row+1)*source.info.height/rows);
  const extracted=await sharp(source.data,{raw:source.info}).extract({left,top,width:right-left,height:bottom-top}).png().toBuffer();
  const pixels=await sharp(extracted)
    .trim({background:'#00000000',threshold:1}).resize(width,height,{fit:'contain',position:'bottom',background:'#00000000',kernel:'lanczos3'}).ensureAlpha().raw().toBuffer();
  for(let p=0;p<pixels.length;p+=4) {
    if(pixels[p+3]<160) pixels.fill(0,p,p+4);
    else pixels[p+3]=255;
  }
  return sharp(pixels,{raw:{width,height,channels:4}}).png().toBuffer();
}
const portraits=await unmatte('abigail-portraits-modern-v1.png');
const portraitLayers=[];
for(let i=0;i<10;i++) portraitLayers.push({input:await cell(portraits,2,5,i,64,64),left:(i%2)*64,top:Math.floor(i/2)*64});
await sharp({create:{width:128,height:320,channels:4,background:'#00000000'}}).composite(portraitLayers).png().toFile(new URL('portraits.png',output).pathname.replace(/^\/(\w:)/,'$1'));

const sprites=await unmatte('abigail-modern-v1.png');
const originalPath=new URL('original-characters.png',input).pathname.replace(/^\/(\w:)/,'$1');
const original=await sharp(originalPath).ensureAlpha().raw().toBuffer({resolveWithObject:true});
// Leave all special animation frames (row 3 onward) byte-for-byte intact.
const sheet=Buffer.from(original.data);
for(let i=0;i<12;i++) {
  const frame=await sharp(await cell(sprites,4,3,i,14,28)).ensureAlpha().raw().toBuffer();
  const x=(i%4)*16, y=Math.floor(i/4)*32;
  for(let dy=0;dy<32;dy++) for(let dx=0;dx<16;dx++) {
    const p=((y+dy)*64+x+dx)*4;
    sheet.fill(0,p,p+4);
    if(dx>=1&&dx<15&&dy>=3&&dy<31) frame.copy(sheet,p,((dy-3)*14+dx-1)*4,((dy-3)*14+dx-1)*4+4);
  }
}
assert.deepEqual(sheet.subarray(64*96*4),original.data.subarray(64*96*4));
await sharp(sheet,{raw:original.info}).png().toFile(new URL('characters.png',output).pathname.replace(/^\/(\w:)/,'$1'));
for(const [name,w,h] of [['characters.png',64,448],['portraits.png',128,320]]) {
  const path=new URL(name,output).pathname.replace(/^\/(\w:)/,'$1');
  const {data,info}=await sharp(path).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  assert.equal(info.width,w); assert.equal(info.height,h);
  assert.ok(data.some((v,i)=>i%4===3&&v===0),'Transparent background required');
  console.log(`${name}: ${w}x${h}, transparent RGBA; special sprite frames preserved.`);
  await sharp(path).resize(w*4,h*4,{kernel:'nearest'}).png().toFile(new URL('prepared-'+name,input).pathname.replace(/^\/(\w:)/,'$1'));
}
await import('./complete-left-facing-art.mjs');
