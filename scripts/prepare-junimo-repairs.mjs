import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const w='artifacts/npc-modern/work/JunimoRepairs/';
const source=await sharp(w+'modern-source.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
assert.equal(source.data[3],0,'Expected transparent generated background');
const native=await sharp('artifacts/npc-modern/originals/LooseSprites/Cursors.png').extract({left:294,top:1432,width:64,height:16}).ensureAlpha().raw().toBuffer();
const atlas=Buffer.alloc(64*16*4),mappings=[];
for(let frame=0;frame<4;frame++){
  let minX=16,minY=16,maxX=0,maxY=0;
  for(let y=0;y<16;y++)for(let x=0;x<16;x++)if(native[(y*64+frame*16+x)*4+3]){minX=Math.min(minX,x);maxX=Math.max(maxX,x);minY=Math.min(minY,y);maxY=Math.max(maxY,y);}
  const left=Math.floor(source.info.width*frame/4),right=Math.floor(source.info.width*(frame+1)/4);
  const crop=await sharp(source.data,{raw:source.info}).extract({left,top:0,width:right-left,height:source.info.height}).png().toBuffer();
  const trimmed=await sharp(crop).trim().png().toBuffer();
  const width=maxX-minX+1,height=maxY-minY+1;
  const cell=await sharp(trimmed).resize(width,height,{fit:'fill',kernel:'nearest'}).ensureAlpha().raw().toBuffer();
  for(let p=0;p<cell.length;p+=4){if(cell[p+3]<128)cell.fill(0,p,p+4);else cell[p+3]=255;}
  for(let y=0;y<height;y++)cell.copy(atlas,((y+minY)*64+frame*16+minX)*4,y*width*4,(y+1)*width*4);
  mappings.push({frame,sourceCrop:{left,top:0,width:right-left,height:source.info.height},nativeBounds:{minX,minY,maxX,maxY}});
}
await sharp(atlas,{raw:{width:64,height:16,channels:4}}).png().toFile(w+'patch.png');
await sharp(atlas,{raw:{width:64,height:16,channels:4}}).resize(1024,256,{kernel:'nearest'}).png().toFile(w+'prepared-preview.png');
await fs.writeFile(w+'qa.json',JSON.stringify({rectangle:{X:294,Y:1432,Width:64,Height:16},frames:4,mappings,binaryAlpha:true,installed:false,runtimeVerified:false},null,2));
console.log('Prepared four native-sized Junimo repair frames.');
