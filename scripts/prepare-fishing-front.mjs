import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/FishingContestants/';
const original=await sharp('artifacts/npc-modern/originals/Characters/Assorted_Fishermen_Winter.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
const output=Buffer.from(original.data);
for(const [filename,top,rodCount] of [['winter-front-four.png',0,4],['winter-second-four.png',64,2]]){
const source=await sharp(work+filename).ensureAlpha().raw().toBuffer({resolveWithObject:true});
for(let p=0;p<source.data.length;p+=4){const r=source.data[p],g=source.data[p+1],b=source.data[p+2];if(r>g+100&&b>g+100&&r>b*.88)source.data.fill(0,p,p+4);}
const input=await sharp(source.data,{raw:source.info}).png().toBuffer();
for(let col=0;col<4;col++){
 const left=Math.round(col*source.info.width/4),right=Math.round((col+1)*source.info.width/4);
 const crop=await sharp(input).extract({left,top:0,width:right-left,height:source.info.height}).png().toBuffer();
 const cell=await sharp(crop).trim({background:'#00000000',threshold:1}).resize(15,30,{fit:'fill'}).extend({left:0,right:1,top:0,bottom:0,background:'#00000000'}).ensureAlpha().raw().toBuffer();
 for(let p=0;p<cell.length;p+=4){if(cell[p+3]<160)cell.fill(0,p,p+4);else cell[p+3]=255;}
 for(let y=0;y<30;y++)cell.copy(output,((top+y)*64+col*16)*4,y*64,(y+1)*64);
 // Native rod shaft runs down column8; preserve it through the lower body.
 if(col<rodCount)for(let y=22;y<30;y++){const p=((top+y)*64+col*16+8)*4;original.data.copy(output,p,p,p+4);}
}
}
let preserved=0;
for(let y=0;y<256;y++)if(!(y<30||(y>=64&&y<94))){assert.ok(output.subarray(y*256,(y+1)*256).equals(original.data.subarray(y*256,(y+1)*256)));preserved+=64;}
await sharp(output,{raw:original.info}).png().toFile(work+'winter-front-prepared.png');
await sharp(output,{raw:original.info}).extract({left:0,top:0,width:64,height:128}).resize(512,1024,{kernel:'nearest'}).png().toFile(work+'winter-front-preview.png');
await fs.writeFile(work+'front-validation.json',JSON.stringify({UpdatedFigures:8,OutsidePixelsPreserved:preserved,NativeRodShaftsPreserved:true,Installed:false,RuntimeVerified:false},null,2));
console.log(`Prepared eight winter figures, retained native rod shafts and ${preserved} outside pixels.`);
