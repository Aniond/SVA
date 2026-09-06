import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const w='artifacts/npc-modern/work/QueenOfSauce/';
const tv=await sharp(w+'modern-tv-revised.png').resize(84,28,{fit:'fill',kernel:'nearest'}).png().toBuffer();
await fs.writeFile(w+'tv-patch.png',tv);
await sharp(tv).resize(1176,392,{kernel:'nearest'}).png().toFile(w+'tv-prepared-preview.png');
const source=await sharp(w+'portraits-source.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
const output=Buffer.alloc(128*192*4);const mappings=[];
for(let frame=0;frame<6;frame++){
  const left=frame%3*512,top=Math.floor(frame/3)*512;
  const cell=Buffer.alloc(512*512*4);
  for(let y=0;y<512;y++)source.data.copy(cell,y*512*4,((top+y)*1536+left)*4,((top+y)*1536+left+512)*4);
  const seen=new Uint8Array(512*512),queue=[];
  function visit(x,y){if(x<0||y<0||x>=512||y>=512)return;const n=y*512+x;if(seen[n])return;const[r,g,b]=cell.subarray(n*4,n*4+3);if(!(r>=140&&r<=195&&g<24&&b>=72&&b<=115))return;seen[n]=1;queue.push(n);}
  for(let i=0;i<512;i++){visit(i,0);visit(i,511);visit(0,i);visit(511,i);}
  for(let i=0;i<queue.length;i++){const n=queue[i],x=n%512,y=Math.floor(n/512);visit(x-1,y);visit(x+1,y);visit(x,y-1);visit(x,y+1);}
  for(const n of queue)cell.fill(0,n*4,n*4+4);
  const png=await sharp(cell,{raw:{width:512,height:512,channels:4}}).png().toBuffer();
  const cropped=await sharp(png).trim().png().toBuffer();
  const portrait=await sharp(cropped).resize(60,60,{fit:'contain',background:{r:0,g:0,b:0,alpha:0},kernel:'nearest'}).ensureAlpha().raw().toBuffer();
  for(let y=0;y<60;y++)portrait.copy(output,((Math.floor(frame/2)*64+2+y)*128+frame%2*64+2)*4,y*60*4,(y+1)*60*4);
  mappings.push({frame,source:{left,top,width:512,height:512},backgroundPixelsRemoved:queue.length});
}
const hashes=new Set();for(let frame=0;frame<6;frame++){const cell=Buffer.alloc(64*64*4);for(let y=0;y<64;y++)output.copy(cell,y*64*4,((Math.floor(frame/2)*64+y)*128+frame%2*64)*4,((Math.floor(frame/2)*64+y)*128+frame%2*64+64)*4);hashes.add(cell.toString('base64'));assert.ok(cell.some(v=>v!==0));}assert.equal(hashes.size,6);
await sharp(output,{raw:{width:128,height:192,channels:4}}).png().toFile(w+'portraits-prepared.png');
await sharp(output,{raw:{width:128,height:192,channels:4}}).resize(512,768,{kernel:'nearest'}).png().toFile(w+'portraits-preview.png');
await fs.writeFile(w+'qa.json',JSON.stringify({tvPatch:{X:602,Y:361,Width:84,Height:28},portraits:{Width:128,Height:192,uniqueExpressions:6},mappings,installed:false,runtimeVerified:false},null,2));
console.log('Prepared two TV frames and six unique portraits. Visual review required.');
