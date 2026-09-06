import fs from 'node:fs/promises';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const work='artifacts/npc-modern/work/Junimo/',out='src/AbigailModern/assets/Junimo/';
await fs.mkdir(out,{recursive:true});
const source=await sharp(work+'portraits-magenta.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
for(let p=0;p<source.data.length;p+=4){const r=source.data[p],g=source.data[p+1],b=source.data[p+2];if(r>g+30&&b>g+30)source.data.fill(0,p,p+4);else{const gray=Math.round((r+g+b)/3);source.data[p]=source.data[p+1]=source.data[p+2]=gray;source.data[p+3]=255;}}
const image=await sharp(source.data,{raw:source.info}).png().toBuffer(),layers=[];
for(let i=0;i<6;i++){
 const cell=await sharp(image).extract({left:i%2*512,top:Math.floor(i/2)*512,width:512,height:512}).png().toBuffer();
 const raw=await sharp(cell).trim({background:'#00000000',threshold:1}).resize(60,60,{fit:'contain',position:'bottom',background:'#00000000'}).extend({left:2,right:2,top:2,bottom:2,background:'#00000000'}).ensureAlpha().raw().toBuffer();
 for(let p=0;p<raw.length;p+=4){if(raw[p+3]<160)raw.fill(0,p,p+4);else raw[p+3]=255;}
 layers.push({input:await sharp(raw,{raw:{width:64,height:64,channels:4}}).png().toBuffer(),left:i%2*64,top:Math.floor(i/2)*64});
}
await sharp({create:{width:128,height:192,channels:4,background:'#00000000'}}).composite(layers).png().toFile(out+'portraits.png');
await fs.copyFile(work+'prepared-characters.png',out+'characters.png');
const file='src/AbigailModern/artwork.json',registry=JSON.parse(await fs.readFile(file,'utf8'));
for(const entry of [{Name:'Characters/Junimo',File:'assets/Junimo/characters.png',Width:128,Height:128,PatchArea:{X:0,Y:0,Width:128,Height:96}},{Name:'Portraits/Junimo',File:'assets/Junimo/portraits.png',Width:128,Height:192}]){const i=registry.findIndex(a=>a.Name===entry.Name);if(i<0)registry.push(entry);else registry[i]=entry;}
await fs.writeFile(file,JSON.stringify(registry,null,2));
console.log('Prepared and registered Junimo sprite and six-expression portrait sheets; not installed.');
