import fs from 'node:fs/promises';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const dir='artifacts/npc-modern/work/MarcelloPortraitCleanup/';
const before=await sharp(dir+'portraits-before.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
assert.equal(before.info.width,128);assert.equal(before.info.height,192);
const rectangles=[[27,63,6,1],[90,63,10,1],[25,127,10,1],[92,127,10,1]];
const roles=['neutral welcoming','happy broad smile','curious thoughtful','surprised','concerned','grateful'];
await fs.mkdir(dir+'clean-cells',{recursive:true});
const layers=[];
for(let i=0;i<6;i++){
 const x=i%2*64,y=Math.floor(i/2)*64;
 // Routine crop correction: omit only the detached final row in the first four source cells.
 // A clear row62 separates each removed strip from the accepted bust ending at row61.
 const cell=await sharp(before.data,{raw:before.info}).extract({left:x,top:y,width:64,height:i<4?63:64})
  .extend({top:0,left:0,right:0,bottom:i<4?1:0,background:{r:0,g:0,b:0,alpha:0}}).png().toBuffer();
 await fs.writeFile(dir+`clean-cells/cell-${i}.png`,cell);
 layers.push({input:cell,left:x,top:y});
}
await sharp({create:{width:128,height:192,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite(layers).png().toFile(dir+'portraits-prepared.png');
const after=await sharp(dir+'portraits-prepared.png').ensureAlpha().raw().toBuffer();
let changed=0,outside=0;const perCell=[];
for(let i=0;i<128*192;i++)if(!before.data.subarray(i*4,i*4+4).equals(after.subarray(i*4,i*4+4))){changed++;const x=i%128,y=Math.floor(i/128);if(!rectangles.some(([l,t,w,h])=>x>=l&&x<l+w&&y>=t&&y<t+h))outside++;assert.equal(after[i*4+3],0);}
assert.equal(changed,36);assert.equal(outside,0);
const distinct=new Set();
for(let i=0;i<6;i++){
 const x=i%2*64,y=Math.floor(i/2)*64;const pixels=await sharp(after,{raw:before.info}).extract({left:x,top:y,width:64,height:64}).raw().toBuffer();
 const old=await sharp(before.data,{raw:before.info}).extract({left:x,top:y,width:64,height:64}).raw().toBuffer();
 let n=0,diff=0,partial=0,last=0;for(let p=0;p<pixels.length;p+=4){if(pixels[p+3])n++;if(pixels[p+3]!==0&&pixels[p+3]!==255)partial++;if(!pixels.subarray(p,p+4).equals(old.subarray(p,p+4)))diff++;if(p>=63*64*4&&pixels[p+3])last++;}
 assert.equal(partial,0);assert.ok(n>1000);if(i<4){assert.equal(last,0);assert.equal(diff,[6,10,10,10][i]);}else assert.equal(diff,0);
 const hash=crypto.createHash('sha256').update(pixels).digest('hex');distinct.add(hash);perCell.push({Cell:i,Role:roles[i],OccupiedPixels:n,ChangedPixels:diff,BottomRowOccupiedPixels:last,Sha256:hash});
}
assert.equal(distinct.size,6);
for(const [name,bg]of [['light','#e3e9e7'],['dark','#24333c']]){
 const panels=[];for(let i=0;i<6;i++)panels.push({input:await sharp(dir+`clean-cells/cell-${i}.png`).resize(384,384,{kernel:'nearest'}).png().toBuffer(),left:i%2*400+8,top:Math.floor(i/2)*400+8});
 await sharp({create:{width:800,height:1200,channels:4,background:bg}}).composite(panels).png().toFile(dir+`preview-${name}.png`);
}
await sharp(dir+'portraits-prepared.png').resize(768,1152,{kernel:'nearest'}).png().toFile(dir+'portraits-preview.png');
const compare=await sharp({create:{width:256,height:192,channels:4,background:'#dbe4e4'}}).composite([{input:dir+'portraits-before.png',left:0,top:0},{input:dir+'portraits-prepared.png',left:128,top:0}]).png().toBuffer();
await sharp(compare).resize(1024,768,{kernel:'nearest'}).png().toFile(dir+'comparison-before-left-after-right.png');
const hash=async f=>crypto.createHash('sha256').update(await fs.readFile(f)).digest('hex');
await fs.writeFile(dir+'validation.json',JSON.stringify({Passed:true,Dimensions:[128,192],ExpressionCells:6,DistinctExpressions:6,ChangedPixels:changed,OutsideConfirmedFragmentsChanged:outside,AllAcceptedFigurePixelsExact:true,Cells4And5Exact:true,BinaryAlpha:true,PerCell:perCell,BeforeSha256:await hash(dir+'portraits-before.png'),PreparedSha256:await hash(dir+'portraits-prepared.png'),Method:'Routine crop of first four accepted native-size source cells from64 to63 pixels high; transparent bottom padding; no resizing or recoloring of final art.',ImagegenAttempt:{Id:'77b72f4f-5078-4ca0-8c43-7b800df552cf',File:'imagegen-cleaned-source.png',Sha256:await hash(dir+'imagegen-cleaned-source.png'),Accepted:false,Reason:'Tool altered accepted faces and returned opaque purple background instead of transparent cleanup. No generated pixels from this attempt enter the final sheet.'},OriginalGeneratedMaster:{File:'original-generated-source.png',Sha256:await hash(dir+'original-generated-source.png'),OriginalPromptFile:'../Marcello/prompts.json'},Installed:false,RuntimeVerified:false},null,2));
console.log(JSON.stringify({Passed:true,ChangedPixels:changed,OutsideConfirmedFragmentsChanged:outside,Cells:perCell}));
