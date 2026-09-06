import fs from 'node:fs/promises';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const variants=['Base','Winter','Beach'];
for(const variant of variants){const work=`artifacts/npc-modern/work/Caroline${variant}/`;await fs.mkdir(work,{recursive:true});const suffix=variant==='Base'?'':'_'+variant;const inv={};
 for(const category of ['Characters','Portraits']){const file=`artifacts/npc-modern/originals/${category}/Caroline${suffix}.png`,{data,info}=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});const w=category==='Characters'?16:64,h=category==='Characters'?32:64,cols=info.width/w,cells=[];
 for(let i=0;i<cols*info.height/h;i++){let visible=0;const colors=new Set();for(let y=0;y<h;y++)for(let x=0;x<w;x++){const p=((Math.floor(i/cols)*h+y)*info.width+i%cols*w+x)*4;if(data[p+3]){visible++;colors.add(data.subarray(p,p+4).toString('hex'));}}cells.push({Index:i,Visible:visible,Colors:colors.size});}
 inv[category]={Width:info.width,Height:info.height,Cells:cells};await sharp(file).resize(info.width*6,info.height*6,{kernel:'nearest'}).png().toFile(work+`native-${category.toLowerCase()}-reference.png`);
 }await fs.writeFile(work+'native-inventory.json',JSON.stringify(inv,null,2)+'\n');console.log(variant,inv.Characters.Width,inv.Characters.Height,inv.Portraits.Width,inv.Portraits.Height);}
for(const category of ['characters','portraits'])await sharp(`src/AbigailModern/assets/Caroline/${category}.png`).resize({width:category==='characters'?384:768,kernel:'nearest'}).png().toFile(`artifacts/npc-modern/work/CarolineBase/modern-${category}-reference.png`);
