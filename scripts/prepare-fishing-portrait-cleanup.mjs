import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/FishingPortraitCleanup/';
await fs.mkdir(work,{recursive:true});
const results=[];
for(const season of ['winter','summer'])for(const index of [1,2]){
 const name=`${season}-portrait-${index}.png`,source='src/AbigailModern/assets/FishingContestants/'+name;
 const original=await fs.readFile(source);await fs.writeFile(work+'before-'+name,original);
 const cells=index===1?[2,3]:[2],a=await sharp(original).ensureAlpha().raw().toBuffer(),b=Buffer.alloc(a.length);
 for(let cell=0;cell<6;cell++){
  const input=await sharp(original).extract({left:cell%2*64,top:Math.floor(cell/2)*64,width:64,height:cells.includes(cell)?63:64}).png().toBuffer();
  const crop=await sharp(input).ensureAlpha().raw().toBuffer();
  for(let row=0;row<(cells.includes(cell)?63:64);row++)crop.copy(b,((Math.floor(cell/2)*64+row)*128+cell%2*64)*4,row*64*4,(row+1)*64*4);
 }
 const output=await sharp(b,{raw:{width:128,height:192,channels:4}}).png().toBuffer();
 let removed=0;
 for(let y=0;y<192;y++)for(let x=0;x<128;x++){
  const p=(y*128+x)*4,cell=Math.floor(y/64)*2+Math.floor(x/64);
  if(cells.includes(cell)&&y%64===63){if(a[p+3])removed++;assert.equal(b[p+3],0);}
  else assert.ok(a.subarray(p,p+4).equals(b.subarray(p,p+4)),'Accepted pixels changed');
 }
 assert.ok(removed>0 && removed<=cells.length*64);
 await fs.writeFile(work+name,output);
 for(const [label,color]of [['light','#eeeeee'],['dark','#202830']]){
  const flat=await sharp(output).flatten({background:color}).png().toBuffer();
  await sharp(flat).resize(512,768,{kernel:'nearest'}).png().toFile(work+`${season}-${index}-${label}.png`);
 }
 results.push({File:name,CroppedCells:cells,RemovedDetachedPixels:removed,AllOtherPixelsExact:true});
}
await fs.writeFile(work+'verification.json',JSON.stringify({Passed:true,Results:results,Installed:false},null,2));
console.log(results);
