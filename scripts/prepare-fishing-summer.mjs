import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/FishingContestants/';
const original=await sharp('artifacts/npc-modern/originals/Characters/Assorted_Fishermen.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
const output=Buffer.from(original.data),touched=new Set();
const groups=[
 ['summer-front-four.png',[[0,0,16,30],[16,0,16,30],[32,0,16,30],[48,0,16,30]],true],
 ['summer-pair.png',[[0,64,16,30],[16,64,16,30]],true],
 ['summer-side-five.png',[[0,128,14,30],[50,128,14,30],[0,160,14,30],[32,160,14,30],[16,192,16,30]],false]
];
for(const [file,regions,front] of groups){
 const source=await sharp(work+file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 for(let p=0;p<source.data.length;p+=4){const r=source.data[p],g=source.data[p+1],b=source.data[p+2];if(r>g+100&&b>g+100&&r>b*.88)source.data.fill(0,p,p+4);}
 const input=await sharp(source.data,{raw:source.info}).png().toBuffer();
 for(const [index,[x,y,width,height]] of regions.entries()){
  const left=Math.round(index*source.info.width/regions.length),right=Math.round((index+1)*source.info.width/regions.length);
  const crop=await sharp(input).extract({left,top:0,width:right-left,height:source.info.height}).png().toBuffer();
  let builder=sharp(crop).trim({background:'#00000000',threshold:1}).resize(front?width-1:width,height,{fit:'fill'});
  // Generated fifth side figure faces right; native actor9 faces left.
  if(!front&&index===4)builder=builder.flop();
  if(front)builder=builder.extend({left:0,right:1,top:0,bottom:0,background:'#00000000'});
  const cell=await builder.ensureAlpha().raw().toBuffer();
  for(let p=0;p<cell.length;p+=4){if(cell[p+3]<160)cell.fill(0,p,p+4);else cell[p+3]=255;}
  for(let row=0;row<height;row++)for(let col=0;col<width;col++){
   const dst=((y+row)*64+x+col)*4,src=(row*width+col)*4;cell.copy(output,dst,src,src+4);touched.add(dst);
  }
  if(front)for(let row=22;row<30;row++){const p=((y+row)*64+x+8)*4;original.data.copy(output,p,p,p+4);}
 }
}
let preserved=0;
for(let p=0;p<output.length;p+=4)if(!touched.has(p)){assert.ok(output.subarray(p,p+4).equals(original.data.subarray(p,p+4)));preserved++;}
await sharp(output,{raw:original.info}).png().toFile(work+'summer-prepared.png');
await sharp(output,{raw:original.info}).resize(384,1536,{kernel:'nearest'}).png().toFile(work+'summer-all-preview.png');
await fs.writeFile(work+'summer-validation.json',JSON.stringify({UpdatedFigures:11,PreservedOutsidePixels:preserved,WinterOnlySlotsPreserved:true,Installed:false,RuntimeVerified:false},null,2));
console.log(`Prepared11summer figures; ${preserved} outside pixels preserved.`);
