import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/FishingContestants/';
const previews=[],checks=[];
const summer=process.argv.includes('--summer'),season=summer?'summer':'winter';
const args=process.argv.slice(2).filter(a=>a!=='--summer');
const actors=args.length?args.map(Number):[0,1,2,3];
for(const [index,actor] of actors.entries()){
 const source=await sharp(work+`contestant${actor}-${season}-portraits.png`).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 for(let p=0;p<source.data.length;p+=4){
  const r=source.data[p],g=source.data[p+1],b=source.data[p+2];
  if(source.data[p+3]<160||(r>g+100&&b>g+100&&r>b*.88))source.data.fill(0,p,p+4);
  else source.data[p+3]=255;
 }
 const input=await sharp(source.data,{raw:source.info}).png().toBuffer();
 const cells=[];
 for(let pose=0;pose<6;pose++){
  const col=pose%2,row=Math.floor(pose/2);
  const left=Math.round(col*source.info.width/2),right=Math.round((col+1)*source.info.width/2);
  const rows=summer?(actor===0?[0,418,810,1254]:[3,9].includes(actor)?[0,425,820,1254]:actor===8?[0,420,807,1254]:null):actor===4?[0,510,980,1536]:actor===10?[0,510,990,1536]:[6,8].includes(actor)?[0,420,820,1254]:actor===9?[0,430,820,1254]:null;
  const top=rows?.[row]??Math.round(row*source.info.height/3),bottom=rows?.[row+1]??Math.round((row+1)*source.info.height/3);
  const crop=await sharp(input).extract({left,top,width:right-left,height:bottom-top}).png().toBuffer();
  const cell=await sharp(crop).trim({background:'#00000000',threshold:1}).resize(62,62,{fit:'contain',position:'bottom',background:'#00000000'}).extend({top:2,bottom:0,left:1,right:1,background:'#00000000'}).png().toBuffer();
  const stats=await sharp(cell).stats();assert.ok(stats.channels[3].max>200);
  cells.push({input:cell,left:col*64,top:row*64});
 }
 const sheet=await sharp({create:{width:128,height:192,channels:4,background:'#00000000'}}).composite(cells).png().toBuffer();
 await fs.writeFile(work+`contestant${actor}-${season}-portraits-prepared.png`,sheet);
 previews.push({input:sheet,left:index*128,top:0});
 checks.push({Actor:actor,Width:128,Height:192,Expressions:6,Installed:false,RuntimeVerified:false});
}
const preview=await sharp({create:{width:actors.length*128,height:192,channels:4,background:'#26313a'}}).composite(previews).png().toBuffer();
await sharp(preview).resize(actors.length*256,384,{kernel:'nearest'}).png().toFile(work+(summer?'summer-':'front-')+'portraits-preview.png');
await fs.writeFile(work+(summer?'summer-':'')+'portrait-validation.json',JSON.stringify(checks,null,2));
console.log(`Prepared ${actors.length*6} expressions for ${actors.length} ${season} contestants. Not installed.`);
