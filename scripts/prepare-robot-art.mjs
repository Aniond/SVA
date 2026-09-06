import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const w='artifacts/npc-modern/work/Robot/';
const provenance=JSON.parse(await fs.readFile(w+'generation-provenance.json','utf8'));
provenance['robot-portraits']=JSON.parse(await fs.readFile(w+'portrait-provenance.json','utf8'));
const read=file=>sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
const cuts={};
async function grid(key,cols,rows){
 const file=provenance[key].source;await fs.copyFile(file,w+key+'-source.png');const s=await read(file);
 for(let p=0;p<s.data.length;p+=4){const[r,g,b,a]=s.data.subarray(p,p+4);if(a<160||(r>80&&b>70&&g<r*.55&&g<b*.65))s.data.fill(0,p,p+4);else s.data[p+3]=255;}
 const xc=Array(s.info.width).fill(0),yc=Array(s.info.height).fill(0);for(let y=0;y<s.info.height;y++)for(let x=0;x<s.info.width;x++)if(s.data[(y*s.info.width+x)*4+3]){xc[x]++;yc[y]++;}
 const edges=(counts,n)=>{const a=[0];for(let i=1;i<n;i++){const mid=i*counts.length/n;let best=-1,dist=Infinity;for(let p=Math.round(mid-counts.length/n*.3);p<mid+counts.length/n*.3;p++)if(counts[p]===0&&Math.abs(p-mid)<dist){best=p;dist=Math.abs(p-mid);}assert.ok(best>a.at(-1),'No empty source gutter');a.push(best);}return [...a,counts.length];};
 const xs=edges(xc,cols),ys=edges(yc,rows);cuts[key]={xs,ys};const png=await sharp(s.data,{raw:s.info}).png().toBuffer();const cells=[];
 for(let i=0;i<cols*rows;i++){const left=xs[i%cols],top=ys[Math.floor(i/cols)];const b=await sharp(png).extract({left,top,width:xs[i%cols+1]-left,height:ys[Math.floor(i/cols)+1]-top}).png().toBuffer();cells.push(await sharp(b).trim().png().toBuffer());}return cells;
}
const main=await grid('robot-main',4,3),native=await read('artifacts/npc-modern/originals/Characters/robot.png');
assert.equal(native.info.width,140);assert.equal(native.info.height,126);
const bounds=[];for(let i=0;i<12;i++){let minX=35,minY=42,maxX=-1,maxY=-1;for(let y=0;y<42;y++)for(let x=0;x<35;x++)if(native.data[((Math.floor(i/4)*42+y)*140+i%4*35+x)*4+3]){minX=Math.min(minX,x);minY=Math.min(minY,y);maxX=Math.max(maxX,x);maxY=Math.max(maxY,y);}assert.ok(maxX>=minX);bounds.push({left:minX,top:minY,width:maxX-minX+1,height:maxY-minY+1});}
const layers=[];for(let i=0;i<12;i++){const b=bounds[i];const buffer=await sharp(main[i]).resize(b.width,b.height,{fit:'inside',kernel:'nearest'}).png().toBuffer();const m=await sharp(buffer).metadata();layers.push({input:buffer,left:i%4*35+b.left+Math.floor((b.width-m.width)/2),top:Math.floor(i/4)*42+b.top+b.height-m.height});}
const result=await sharp({create:{width:140,height:126,channels:4,background:'#00000000'}}).composite(layers).png().toBuffer();await fs.writeFile(w+'characters-prepared.png',result);await sharp(result).resize(840,756,{kernel:'nearest'}).toFile(w+'characters-preview.png');
const flight=await grid('robot-flight',4,1);const sizes=await Promise.all(flight.map(f=>sharp(f).metadata()));const scale=Math.min(15/Math.max(...sizes.map(s=>s.width)),27/Math.max(...sizes.map(s=>s.height)));
const fl=[];for(let i=0;i<4;i++){const m=sizes[i],ww=Math.max(1,Math.round(m.width*scale)),hh=Math.min(27,Math.round(m.height*scale));const b=await sharp(flight[i]).resize(ww,hh,{kernel:'nearest'}).png().toBuffer();fl.push({input:b,left:i*15+Math.floor((15-ww)/2),top:0});}
const flightRaw=await sharp({create:{width:60,height:27,channels:4,background:'#00000000'}}).composite(fl).raw().toBuffer();
// Keep the robot itself fixed while the generated exhaust changes beneath it.
for(let i=1;i<4;i++)for(let y=0;y<19;y++)flightRaw.copy(flightRaw,(y*60+i*15)*4,y*60*4,(y*60+15)*4);
const flightPng=await sharp(flightRaw,{raw:{width:60,height:27,channels:4}}).png().toBuffer();await fs.writeFile(w+'flight-prepared.png',flightPng);await sharp(flightPng).resize(960,432,{kernel:'nearest'}).toFile(w+'flight-preview.png');
const portraits=await grid('robot-portraits',2,3),pl=[];
for(let i=0;i<6;i++){const b=await sharp(portraits[i]).resize(64,62,{fit:'inside',kernel:'nearest'}).png().toBuffer();const m=await sharp(b).metadata();pl.push({input:b,left:i%2*64+Math.floor((64-m.width)/2),top:Math.floor(i/2)*64+64-m.height});}
const portraitPng=await sharp({create:{width:128,height:192,channels:4,background:'#00000000'}}).composite(pl).png().toBuffer();await fs.writeFile(w+'portraits-prepared.png',portraitPng);await sharp(portraitPng).resize(512,768,{kernel:'nearest'}).toFile(w+'portraits-preview.png');
await fs.writeFile(w+'mapping.json',JSON.stringify({MainCell:{Width:35,Height:42},Bounds:bounds,Cuts:cuts,FlightScale:scale,FlightPatch:{X:206,Y:1827,Width:60,Height:27}},null,2)+'\n');console.log({MainPoses:12,FlightPoses:4,FlightScale:scale});

const checks=[];for(const [file,ww,hh,count,cw,ch]of [['characters-prepared.png',140,126,12,35,42],['flight-prepared.png',60,27,4,15,27],['portraits-prepared.png',128,192,6,64,64]]){
 const s=await read(w+file);assert.equal(s.info.width,ww);assert.equal(s.info.height,hh);let residue=0,partial=0;const visible=[];
 for(let i=0;i<count;i++){let n=0;for(let y=0;y<ch;y++)for(let x=0;x<cw;x++){const p=((Math.floor(i/(ww/cw))*ch+y)*ww+(i%(ww/cw))*cw+x)*4,[r,g,b,a]=s.data.subarray(p,p+4);if(a){n++;if(a!==255)partial++;if(r>80&&b>70&&g<r*.55&&g<b*.65)residue++;}}assert.ok(n>10);visible.push(n);}
 assert.equal(partial,0);assert.equal(residue,0);checks.push({File:file,VisiblePixels:visible,PartialAlpha:partial,MagentaResidue:residue});
}
await fs.writeFile(w+'static-qa.json',JSON.stringify({Passed:true,MainPoses:12,FlightPoses:4,Portraits:6,StableFlightBodyRows:19,Checks:checks,Installed:false},null,2)+'\n');
console.log('Robot sprite and portrait static checks passed');
