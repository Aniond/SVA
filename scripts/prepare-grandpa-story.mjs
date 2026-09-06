import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const w='artifacts/npc-modern/work/GrandpaStory/';
const provenance=JSON.parse(await fs.readFile(w+'generation-provenance.json','utf8'));
const native=await sharp('artifacts/npc-modern/originals/Minigames/jojacorps.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
const out=Buffer.from(native.data), width=native.info.width, height=native.info.height;
assert.equal(width,1200);assert.equal(height,800);
const allowed=new Uint8Array(width*height), patches=[];
const bodyMask=(x,y,r,g,b)=>
 (x>=24&&x<64&&y>=16&&y<79&&r>g+12&&g>b+13)||
 (x>=32&&x<71&&y>=10&&y<72&&g>r*1.23&&g>b*1.2)||
 (x>=28&&x<62&&y>=22&&y<43&&Math.abs(r-g)<23&&Math.abs(g-b)<23)||
 (x>=38&&x<63&&y>=12&&y<26&&Math.abs(r-g)<20&&Math.abs(g-b)<20);
const small=[];
for(const key of ['grandpa-body-generation','grandpa-talking']){
 const src=provenance[key].source;await fs.copyFile(src,w+(key==='grandpa-talking'?'talking-source.png':'body-source.png'));
 small.push(await sharp(src).resize(85,85,{kernel:'nearest'}).ensureAlpha().raw().toBuffer());
}
for(const top of [50,290]){
 let changed=0;
 for(let y=0;y<85;y++)for(let x=0;x<85;x++){
  const p=(top+y)*width+580+x,i=p*4,[r,g,b]=native.data.subarray(i,i+3);
  if(bodyMask(x,y,r,g,b)){small[0].copy(out,i,(y*85+x)*4,(y*85+x)*4+4);allowed[p]=1;changed++;}
 }
 patches.push({Role:'body',X:580,Y:top,Width:85,Height:85,MaskedPixels:changed});
}
// Face cells use the same registration as the updated body, so the quiet frame
// joins its beard and pillow exactly. Only masked facial pixels animate.
for(let frame=0;frame<2;frame++){
 const left=497+frame*18;
 for(let y=0;y<18;y++)for(let x=0;x<18;x++){
  const bodyPos=(69+y)*width+616+x,target=(523+y)*width+left+x;
  out.copy(out,target*4,bodyPos*4,bodyPos*4+4);
  const [r,g,b]=native.data.subarray(bodyPos*4,bodyPos*4+3);
  if(frame===0&&bodyMask(x+36,y+19,r,g,b))small[1].copy(out,target*4,((y+19)*85+x+36)*4,((y+19)*85+x+36)*4+4);
  allowed[target]=1;
 }
 patches.push({Role:frame===0?'talking-face':'quiet-face',X:left,Y:523,Width:18,Height:18});
}
const handSource=provenance['grandpa-hands'].source;await fs.copyFile(handSource,w+'hands-source.png');
const hand=await sharp(handSource).ensureAlpha().raw().toBuffer({resolveWithObject:true});
const rows=[];for(let y=0;y<hand.info.height;y++){let count=0;for(let x=0;x<hand.info.width;x++){const i=(y*hand.info.width+x)*4;if(Math.max(...hand.data.subarray(i,i+3))>40)count++;}if(count>hand.info.width*.9)rows.push(y);}
assert.ok(rows.length>50);const crop={left:0,top:rows[0],width:hand.info.width,height:rows.at(-1)-rows[0]+1};
const band=await sharp(handSource).extract(crop).png().toBuffer();
const handSmall=await sharp(band).resize(74,17,{fit:'fill',kernel:'nearest'}).ensureAlpha().raw().toBuffer();
for(let frame=0;frame<2;frame++){
 let masked=0;const left=463+frame*37;
 for(let y=0;y<17;y++)for(let x=0;x<37;x++){
  const p=(556+y)*width+left+x,i=p*4,[r,g,b]=native.data.subarray(i,i+3);
  if((r>g&&g>b)||(g>r*1.2&&g>b*1.2)){
   const from=(y*74+frame*37+x)*4;handSmall.copy(out,i,from,from+4);allowed[p]=1;masked++;
  }
 }
 patches.push({Role:'empty-hand-'+frame,X:left,Y:556,Width:37,Height:17,MaskedPixels:masked});
}
let changed=0,untouched=0;for(let p=0;p<width*height;p++){
 const same=out.subarray(p*4,p*4+4).equals(native.data.subarray(p*4,p*4+4));
 if(!allowed[p]){assert.ok(same,'Changed an unrelated scene pixel');untouched++;}
 if(!same)changed++;
}
const png=await sharp(out,{raw:{width,height,channels:4}}).png().toBuffer();await fs.writeFile(w+'jojacorps-prepared.png',png);
for(const [name,rect]of [['body',{left:580,top:50,width:85,height:85}],['faces',{left:497,top:523,width:36,height:18}],['hands',{left:463,top:556,width:74,height:17}],['scene',{left:427,top:0,width:427,height:240}]]){
 const part=await sharp(png).extract(rect).png().toBuffer();await sharp(part).resize(rect.width*(name==='scene'?2:8),rect.height*(name==='scene'?2:8),{kernel:'nearest'}).toFile(w+name+'-prepared-preview.png');
}
const scenes=[];for(let face=0;face<2;face++)for(let handFrame=0;handFrame<2;handFrame++){
 const base=await sharp(png).extract({left:580,top:50,width:85,height:85}).png().toBuffer();
 const facePng=await sharp(png).extract({left:497+face*18,top:523,width:18,height:18}).png().toBuffer();
 const handPng=await sharp(png).extract({left:463+handFrame*37,top:556,width:37,height:17}).png().toBuffer();
 const combined=await sharp(base).composite([{input:facePng,left:36,top:19},{input:handPng,left:4,top:63}]).png().toBuffer();
 const name=`scene-face${face}-hand${handFrame}.png`;await sharp(combined).resize(680,680,{kernel:'nearest'}).toFile(w+name);scenes.push(name);
}
await fs.writeFile(w+'validation.json',JSON.stringify({Prepared:true,Installed:false,RuntimeVerified:false,FullSceneVerified:false,ChangedPixels:changed,UntouchedPixelsVerified:untouched,HandSourceCrop:crop,Patches:patches,ScenePreviews:scenes},null,2)+'\n');
console.log({changed,untouched,patches:patches.length,handSourceCrop:crop});
