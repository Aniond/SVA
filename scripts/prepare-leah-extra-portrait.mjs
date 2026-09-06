import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/LeahBase/';
const before=await sharp(work+'portraits-before-extra.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
assert.equal(before.info.width,128);assert.equal(before.info.height,320);
for(let y=256;y<320;y++)for(let x=64;x<128;x++)assert.equal(before.data[(y*128+x)*4+3],0,'Expected unused portrait9.');
const metadata=await sharp(work+'extra-portrait-source.png').metadata();
assert.ok(metadata.height>=1150);
// Match the existing head-and-shoulders framing by cropping excess lower torso.
const source=await sharp(work+'extra-portrait-source.png').extract({left:0,top:0,width:metadata.width,height:1150}).ensureAlpha().raw().toBuffer({resolveWithObject:true});
for(let p=0;p<source.data.length;p+=4){const r=source.data[p],g=source.data[p+1],b=source.data[p+2];if(source.data[p+3]<160||(r>g+100&&b>g+100&&r>b*.88))source.data.fill(0,p,p+4);else source.data[p+3]=255;}
const cell=await sharp(source.data,{raw:source.info}).trim({background:'#00000000',threshold:1}).resize(62,62,{fit:'fill'}).ensureAlpha().raw().toBuffer();
for(let p=0;p<cell.length;p+=4){if(cell[p+3]<160)cell.fill(0,p,p+4);else cell[p+3]=255;}
assert.ok(cell.some((v,p)=>p%4===3&&v>0),'New portrait is empty.');
const output=Buffer.from(before.data);
for(let y=0;y<62;y++)cell.copy(output,((258+y)*128+65)*4,y*62*4,(y+1)*62*4);
for(let y=0;y<320;y++)for(let x=0;x<128;x++)if(y<256||x<64){const p=(y*128+x)*4;assert.ok(output.subarray(p,p+4).equals(before.data.subarray(p,p+4)),'Existing portrait changed.');}
await sharp(output,{raw:before.info}).png().toFile(work+'portraits-prepared.png');
await sharp(output,{raw:before.info}).resize(512,1280,{kernel:'nearest'}).png().toFile(work+'portraits-preview.png');
await fs.writeFile(work+'extra-portrait-validation.json',JSON.stringify({AddedPortraitSlot:9,ExistingPortraitsPreserved:9,PortraitSlots:10,RuntimeVerified:false,Installed:false},null,2)+'\n');
console.log('Added Leah portrait9; existing nine portraits remain byte-identical.');
