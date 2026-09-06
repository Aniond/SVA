import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/Junimo/';
const original=await sharp('artifacts/npc-modern/originals/Characters/Junimo.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
const pixels=Buffer.from(original.data);
const tops=[20,209,382,553,718,885],heights=[181,174,170,158,158,168];
for(let i=0;i<48;i++){
 let row=Math.floor(i/8),col=i%8;
 // The generated final row faces forward throughout. Native back-idle uses back poses.
 if(i>=40&&i<44){row=4;col=(i-40)*2;}
 // Keep the fourth side-idle cell facing sideways too.
 if(i===27){row=3;col=1;}
 const left=Math.round(col*1448/8),right=Math.round((col+1)*1448/8);
 const raw=await sharp(work+'modern-sprites.png').extract({left,top:tops[row],width:right-left,height:heights[row]}).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 const w=raw.info.width,h=raw.info.height,seen=new Uint8Array(w*h);let largest=[];
 for(let p=0;p<w*h;p++){
  if(seen[p]||raw.data[p*4+3]<160)continue;
  const component=[p];seen[p]=1;
  for(let k=0;k<component.length;k++){const q=component[k],x=q%w,y=Math.floor(q/w);for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++){const nx=x+dx,ny=y+dy,np=ny*w+nx;if(nx<0||nx>=w||ny<0||ny>=h||seen[np]||raw.data[np*4+3]<160)continue;seen[np]=1;component.push(np);}}
  if(component.length>largest.length)largest=component;
 }
 assert.ok(largest.length>500,`No connected body ${i}`);
 const keep=new Set(largest);let minX=w,minY=h,maxX=0,maxY=0;
 for(let p=0;p<w*h;p++){if(!keep.has(p))raw.data.fill(0,p*4,p*4+4);else{raw.data[p*4+3]=255;minX=Math.min(minX,p%w);maxX=Math.max(maxX,p%w);minY=Math.min(minY,Math.floor(p/w));maxY=Math.max(maxY,Math.floor(p/w));}}
 const bw=maxX-minX+1,bh=maxY-minY+1;
 const targetW=Math.round(bw*.095),targetH=Math.round(bh*.095);
 assert.ok(targetW<=16&&targetH<=16,`Body too large ${i}`);
 const data=await sharp(raw.data,{raw:raw.info}).extract({left:minX,top:minY,width:bw,height:bh}).resize(targetW,targetH).extend({left:Math.floor((16-targetW)/2),right:Math.ceil((16-targetW)/2),top:16-targetH-1,bottom:1,background:'#00000000'}).ensureAlpha().raw().toBuffer();
 for(let p=0;p<data.length;p+=4){if(data[p+3]<160)data.fill(0,p,p+4);else{const gray=Math.round((data[p]+data[p+1]+data[p+2])/3);data[p]=data[p+1]=data[p+2]=gray;data[p+3]=255;}}
 assert.ok(data.some((v,p)=>p%4===3&&v===255),`Empty cell ${i}`);
 for(let y=0;y<16;y++)data.copy(pixels,((Math.floor(i/8)*16+y)*128+i%8*16)*4,y*64,(y+1)*64);
}
assert.ok(pixels.subarray(96*128*4).equals(original.data.subarray(96*128*4)));
await sharp(pixels,{raw:original.info}).png().toFile(work+'prepared-characters.png');
await sharp(pixels,{raw:original.info}).resize(768,768,{kernel:'nearest'}).png().toFile(work+'prepared-preview.png');
await fs.writeFile(work+'body-validation.json',JSON.stringify({frames:48,grayscale:true,lowerPixelsPreserved:4096,installed:false,runtimeVerified:false},null,2));
console.log('Prepared 48 grayscale body cells; lower4096pixels preserved. Review needed before registration.');
