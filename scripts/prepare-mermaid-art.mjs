import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/Mermaid/',out='src/AbigailModern/assets/Mermaid/';await fs.mkdir(out,{recursive:true});
const registryFile='src/AbigailModern/artwork.json',registry=JSON.parse(await fs.readFile(registryFile,'utf8')),entry=registry.find(a=>a.Name==='LooseSprites/temporary_sprites_1');assert.ok(entry);
const baseline=await sharp('src/AbigailModern/'+entry.File).ensureAlpha().raw().toBuffer({resolveWithObject:true}),pixels=Buffer.from(baseline.data),areas=[];
async function keyed(file){const r=await sharp(work+file).ensureAlpha().raw().toBuffer({resolveWithObject:true});for(let p=0;p<r.data.length;p+=4){const[a,g,b]=r.data.subarray(p,p+3);if(a>210&&b>210&&g<60&&Math.abs(a-b)<45)r.data.fill(0,p,p+4);}return r;}
async function resizeCell(source,box,w,h){const png=await sharp(source.data,{raw:source.info}).extract(box).png().toBuffer();const result=await sharp(png).resize(w,h,{fit:'fill'}).ensureAlpha().raw().toBuffer();for(let p=0;p<result.length;p+=4)if(result[p+3]<160)result.fill(0,p,p+4);else result[p+3]=255;return result;}
function patch(data,x,y,w,h){areas.push({X:x,Y:y,Width:w,Height:h});for(let row=0;row<h;row++)data.copy(pixels,((y+row)*512+x)*4,row*w*4,(row+1)*w*4);}
const island=await keyed('modern-island-sprites.png');
for(let i=0;i<7;i++)patch(await resizeCell(island,{left:Math.round(i%4*1254/4),top:i<4?94:657,width:313,height:470},28,36),304+i*28,592,28,36);
const night=await keyed('modern-night-sprites.png');
for(let i=0;i<9;i++)patch(await resizeCell(night,{left:i%3*418,top:[32,440,848][Math.floor(i/3)],width:418,height:392},28,36),i*28,80,28,36);
// Reuse the two distinct island dance poses so the performance alternates visibly.
for(let i=0;i<2;i++){
 const dance=Buffer.alloc(28*36*4);for(let row=0;row<36;row++)pixels.copy(dance,row*28*4,((592+row)*512+304+(5+i)*28)*4,((592+row)*512+304+(6+i)*28)*4);
 for(let row=0;row<36;row++)dance.copy(pixels,((80+row)*512+i*28)*4,row*28*4,(row+1)*28*4);
}
const large=await keyed('modern-large-singing.png');
for(let i=0;i<2;i++)patch(await resizeCell(large,{left:i*627+5,top:80,width:617,height:1080},57,70),i*57,0,57,70);
const swimmer=await keyed('modern-swimmer.png');
patch(await resizeCell(swimmer,{left:170,top:10,width:660,height:860},16,32),192,0,16,32);
const hairPixels=Buffer.alloc(16*32*4),shortHair=await resizeCell(swimmer,{left:1057,top:10,width:660,height:860},14,22);
for(let y=0;y<22;y++)shortHair.copy(hairPixels,(y*16+2)*4,y*14*4,(y+1)*14*4);
// Keep the profile face and eye visible beneath the separately tinted hair.
for(let y=4;y<11;y++)for(let x=6;x<11;x++)hairPixels.fill(0,(y*16+x)*4,(y*16+x+1)*4);
patch(hairPixels,208,0,16,32);
let unchanged=0;for(let y=0;y<640;y++)for(let x=0;x<512;x++){if(areas.some(a=>x>=a.X&&x<a.X+a.Width&&y>=a.Y&&y<a.Y+a.Height))continue;const p=(y*512+x)*4;assert.ok(pixels.subarray(p,p+4).equals(baseline.data.subarray(p,p+4)));unchanged++;}
await sharp(pixels,{raw:baseline.info}).png().toFile('src/AbigailModern/'+entry.File);
const existing=entry.PatchAreas??[entry.PatchArea];entry.PatchAreas=[...existing.filter(a=>!areas.some(b=>JSON.stringify(a)===JSON.stringify(b))),...areas];delete entry.PatchArea;
const portraits=await keyed('modern-portraits.png'),layers=[];
for(let i=0;i<6;i++){const left=i%2*627,top=Math.floor(i/2)*418;const cell=await sharp(portraits.data,{raw:portraits.info}).extract({left,top,width:627,height:418}).png().toBuffer();layers.push({input:await sharp(cell).trim({background:'#00000000',threshold:1}).resize(64,64,{fit:'contain',position:'bottom',background:'#00000000'}).png().toBuffer(),left:i%2*64,top:Math.floor(i/2)*64});}
await sharp({create:{width:128,height:192,channels:4,background:'#00000000'}}).composite(layers).png().toFile(out+'portraits.png');
const portraitEntry={Name:'Portraits/Mermaid',File:'assets/Mermaid/portraits.png',Width:128,Height:192},idx=registry.findIndex(a=>a.Name===portraitEntry.Name);if(idx<0)registry.push(portraitEntry);else registry[idx]=portraitEntry;
await fs.writeFile(registryFile,JSON.stringify(registry,null,2));
for(const [name,left,top,width,height,scale] of [['island',304,592,196,36,6],['night',0,80,252,36,5],['large',0,0,114,70,5],['swimmer-layers',192,0,32,32,12]])await sharp(pixels,{raw:baseline.info}).extract({left,top,width,height}).resize(width*scale,height*scale,{kernel:'nearest'}).png().toFile(work+'prepared-'+name+'.png');
const base=await sharp(pixels,{raw:baseline.info}).extract({left:192,top:0,width:16,height:32}).png().toBuffer(),hair=await sharp(pixels,{raw:baseline.info}).extract({left:208,top:0,width:16,height:32}).png().toBuffer();
const combined=await sharp(base).composite([{input:hair}]).png().toBuffer();await sharp(combined).resize(192,384,{kernel:'nearest'}).png().toFile(work+'prepared-swimmer-combined.png');
await fs.writeFile(work+'validation.json',JSON.stringify({islandFrames:7,nightFrames:9,largeFrames:2,swimmerLayers:2,portraits:6,unchangedPixelsVerified:unchanged,runtimeVerified:false},null,2));console.log(`Prepared mermaid artwork; ${unchanged} outside pixels preserved.`);
