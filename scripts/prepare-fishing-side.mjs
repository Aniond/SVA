import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/FishingContestants/';
const base=await sharp(work+'winter-front-prepared.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
const source=await sharp(work+'winter-side-five.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
for(let p=0;p<source.data.length;p+=4){const r=source.data[p],g=source.data[p+1],b=source.data[p+2];if(r>g+100&&b>g+100&&r>b*.88)source.data.fill(0,p,p+4);}
const input=await sharp(source.data,{raw:source.info}).png().toBuffer();
const output=Buffer.from(base.data),changed=new Set();
// Native bodies occupy these rectangles; the rest of each32x32cell contains rods/line/bobbers.
const regions=[[0,128,14,30],[50,128,14,30],[0,160,14,30],[32,160,14,30],[16,192,16,30]];
for(const [index,[x,y,width,height]] of regions.entries()){
 const left=Math.round(index*source.info.width/5),right=Math.round((index+1)*source.info.width/5);
 const crop=await sharp(input).extract({left,top:0,width:right-left,height:source.info.height}).png().toBuffer();
 const cell=await sharp(crop).trim({background:'#00000000',threshold:1}).resize(width,height,{fit:'fill'}).ensureAlpha().raw().toBuffer();
 for(let p=0;p<cell.length;p+=4){if(cell[p+3]<160)cell.fill(0,p,p+4);else cell[p+3]=255;}
 for(let row=0;row<height;row++)for(let col=0;col<width;col++){
  const dst=((y+row)*64+x+col)*4,src=(row*width+col)*4;
  cell.copy(output,dst,src,src+4);changed.add(dst);
 }
}
let preserved=0;for(let p=0;p<output.length;p+=4)if(!changed.has(p)){assert.ok(output.subarray(p,p+4).equals(base.data.subarray(p,p+4)));preserved++;}
await sharp(output,{raw:base.info}).png().toFile(work+'winter-prepared.png');
await sharp(output,{raw:base.info}).resize(384,1536,{kernel:'nearest'}).png().toFile(work+'winter-all-preview.png');
await fs.writeFile(work+'side-validation.json',JSON.stringify({UpdatedSideFigures:5,TotalWinterFigures:13,PreservedFromPreparedBase:preserved,Regions:regions,Installed:false,RuntimeVerified:false},null,2));
console.log(`Prepared five side figures; ${preserved} outside pixels preserved against front-prepared base.`);
