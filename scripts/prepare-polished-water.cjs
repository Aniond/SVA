const fs=require('node:fs/promises');
const sharp=require('../.tools/abigail-art/node_modules/sharp');
const root='C:/Users/david/SDV';
const work=root+'/artifacts/terrain-modern/work/water-polished';
(async()=>{
 const registry=JSON.parse(await fs.readFile(root+'/src/AbigailModern/artwork.json','utf8'));
 const entry=registry.find(e=>e.Name==='LooseSprites/Cursors');
 const input=root+'/src/AbigailModern/'+entry.File;
 const {data:native,info}=await sharp(input).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 const {data:generated}=await sharp(work+'/generated-source.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
 const samples=[];
 const luminance=(r,g,b)=>0.2126*r+0.7152*g+0.0722*b;
 for(let i=0;i<generated.length;i+=4)if(generated[i+2]>60&&generated[i+2]>generated[i]&&generated[i+1]>45)
  samples.push([generated[i],generated[i+1],generated[i+2],luminance(...generated.subarray(i,i+3))]);
 samples.sort((a,b)=>a[3]-b[3]);
 const bands=[{X:0,Y:2064,Width:640,Height:64},{X:0,Y:2192,Width:640,Height:64}];
 const palette=new Map();
 for(const b of bands)for(let y=b.Y;y<b.Y+b.Height;y++)for(let x=0;x<640;x++){
  const i=(y*info.width+x)*4,key=native.subarray(i,i+3).join(',');
  if(!palette.has(key))palette.set(key,{rgb:[...native.subarray(i,i+3)],count:0});
  palette.get(key).count++;
 }
 const colors=[...palette.entries()].sort((a,b)=>luminance(...a[1].rgb)-luminance(...b[1].rgb));
 let cumulative=0;
 const total=81920;
 for(const [key,p] of colors){
  const q=(cumulative+p.count/2)/total; cumulative+=p.count;
  const center=Math.floor(q*(samples.length-1));
  const radius=Math.max(1,Math.floor(samples.length*0.005));
  const selected=samples.slice(Math.max(0,center-radius),Math.min(samples.length,center+radius+1));
  p.output=[0,1,2].map(c=>Math.round(selected.reduce((sum,v)=>sum+v[c],0)/selected.length));
 }
 const output=Buffer.from(native);let changed=0;
 for(const b of bands)for(let y=b.Y;y<b.Y+b.Height;y++)for(let x=0;x<640;x++){
  const i=(y*info.width+x)*4,p=palette.get(native.subarray(i,i+3).join(','));
  for(let c=0;c<3;c++)output[i+c]=p.output[c];
  if(!output.subarray(i,i+4).equals(native.subarray(i,i+4)))changed++;
 }
 await sharp(output,{raw:{width:info.width,height:info.height,channels:4}}).png().toFile(work+'/cursors-prepared.png');
 const layers=[];
 for(let b=0;b<2;b++)layers.push({input:await sharp(output,{raw:{width:info.width,height:info.height,channels:4}}).extract({left:0,top:bands[b].Y,width:640,height:64}).png().toBuffer(),left:0,top:b*64});
 await sharp({create:{width:640,height:128,channels:4,background:'#202830'}}).composite(layers).png().toFile(work+'/prepared-bands.png');
 await fs.writeFile(work+'/validation.json',JSON.stringify({status:'Prepared; runtime and visual review pending',method:'Generated finish palette transferred to native wave color classes. Generated geometry rejected; native phase geometry and alpha retained.',bands,changedPixels:changed,outsidePixelsPreserved:info.width*info.height-total,palette:colors.map(([native,p])=>({native,output:p.output,count:p.count})),generation:'exec-92d02c99-1cee-472a-b1de-888067e9fc3d'},null,2));
 console.log(JSON.stringify({changedPixels:changed,paletteColors:colors.length}));
})();
