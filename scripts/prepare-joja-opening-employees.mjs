import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {createRequire} from 'node:module';
const sharp=createRequire('C:/Users/david/SDV/.tools/abigail-art/package.json')('sharp');
const base='artifacts/npc-modern/work/JojaOpeningEmployees';
const plan=JSON.parse(fs.readFileSync(`${base}/plan.json`));
const {data:native,info}=await sharp(plan.native).ensureAlpha().raw().toBuffer({resolveWithObject:true});
const current=await sharp(plan.productionAtStart).ensureAlpha().raw().toBuffer();
const prepared=Buffer.from(current), combinedMask=Buffer.alloc(native.length);
const qa=[];
for(const a of plan.actors){
 const [left,top,width,height]=a.rect;
 const generated=await sharp(path.join(base,a.source)).resize(width,height,{fit:'fill',kernel:'nearest'}).ensureAlpha().raw().toBuffer();
 for(const layer of a.layers??[]){
  const [sx,sy,sw,sh]=layer.crop,[dx,dy,dw,dh]=layer.target;
  const cropped=await sharp(path.join(base,layer.source)).extract({left:sx,top:sy,width:sw,height:sh}).png().toBuffer();
  const pixels=await sharp(cropped).resize(dw,dh,{fit:'fill',kernel:'nearest'}).ensureAlpha().raw().toBuffer();
  for(let yy=0;yy<dh;yy++)for(let xx=0;xx<dw;xx++){
   const local=((dy+yy)*width+dx+xx)*4,global=((top+dy+yy)*info.width+left+dx+xx)*4;
   if(layer.maskColors.includes(native.subarray(global,global+3).toString('hex')))pixels.copy(generated,local,(yy*dw+xx)*4,(yy*dw+xx)*4+4);
  }
 }
 const patch=Buffer.alloc(width*height*4),mask=Buffer.alloc(patch.length),original=Buffer.alloc(patch.length),result=Buffer.alloc(patch.length);
 let selected=0,changed=0;
 for(let y=0;y<height;y++)for(let x=0;x<width;x++){
  const i=(y*width+x)*4,j=((top+y)*info.width+left+x)*4;
  native.copy(original,i,j,j+4);native.copy(result,i,j,j+4);
  const nativeColor=native.subarray(j,j+3).toString('hex');
  const include=(a.maskColors.includes(nativeColor)||((a.extraRunColors??['000000','bcbcbc']).includes(nativeColor)&&(a.extraRuns??[]).some(([ry,x1,x2])=>y===ry&&x>=x1&&x<=x2)))&&!(a.excludeRects??[]).some(([ex,ey,ew,eh])=>x>=ex&&x<ex+ew&&y>=ey&&y<ey+eh);
  if(include){selected++;const rgb=[...generated.subarray(i,i+3)];const keyRoom=(a.keyGrayRoom&&Math.max(...rgb)-Math.min(...rgb)<18&&Math.min(...rgb)>25&&Math.max(...rgb)<130)||(a.keyCoolRoom&&rgb[0]<rgb[1]-10&&rgb[2]>rgb[1]-20)||((a.warmColors??[]).includes(nativeColor)&&rgb[0]<rgb[1]+10)||((a.redColors??[]).includes(nativeColor)&&!(rgb[0]>rgb[1]*1.6&&rgb[0]>rgb[2]*1.6))||(a.preserveColors??[]).includes(nativeColor)||((a.skinOnly||(a.skinColors??[]).includes(nativeColor))&&!(rgb[0]>rgb[1]+15&&rgb[1]>105&&rgb[2]>65&&rgb[0]-rgb[1]<90))||(a.boneOnly&&!(rgb[0]>110&&rgb[1]>105&&rgb[2]<rgb[0]-8));(keyRoom?native:generated).copy(patch,i,keyRoom?j:i,(keyRoom?j:i)+3);patch[i+3]=255;patch.copy(result,i,i,i+4);patch.copy(prepared,j,i,i+4);mask.fill(255,i,i+4);combinedMask.fill(255,j,j+4);if(!patch.subarray(i,i+3).equals(native.subarray(j,j+3)))changed++;}
 }
 await fs.promises.mkdir(`${base}/patches`,{recursive:true});
 for(const[n,data]of[['patch',patch],['mask',mask],['native',original],['prepared',result]])await sharp(data,{raw:{width,height,channels:4}}).png().toFile(`${base}/patches/${a.id}-${n}.png`);
 const comparison=await sharp({create:{width:width*2,height,channels:4,background:'#777'}}).composite([{input:await sharp(original,{raw:{width,height,channels:4}}).png().toBuffer(),left:0,top:0},{input:await sharp(result,{raw:{width,height,channels:4}}).png().toBuffer(),left:width,top:0}]).png().toBuffer();
 await sharp(comparison).resize(width*40,height*20,{kernel:'nearest'}).png().toFile(`${base}/patches/${a.id}-comparison.png`);
 qa.push({actor:a.id,rect:a.rect,selected,changed,sourceId:a.sourceId});
}
let outsideMaskChanged=0;for(let i=0;i<prepared.length;i+=4)if(!combinedMask[i+3]&&!prepared.subarray(i,i+4).equals(current.subarray(i,i+4)))outsideMaskChanged++;
await sharp(prepared,{raw:info}).png().toFile(`${base}/atlas-review-only.png`);
await sharp(combinedMask,{raw:info}).png().toFile(`${base}/combined-mask.png`);
fs.writeFileSync(`${base}/qa.json`,JSON.stringify({passed:outsideMaskChanged===0,outsideMaskChanged,comparison:'current production atlas at assembly; all Grandpa patches preserved',actors:qa,preparedRawSha256:crypto.createHash('sha256').update(prepared).digest('hex')},null,2));
console.log(JSON.stringify({outsideMaskChanged,actors:qa}));
