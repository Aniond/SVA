const fs=require('node:fs/promises');
const sharp=require('../.tools/abigail-art/node_modules/sharp');
const root='C:/Users/david/SDV/artifacts/terrain-modern';
(async()=>{
 const work=root+'/work/ground-spring';
 const {data:native,info}=await sharp(root+'/originals/Maps/spring_outdoorsTileSheet.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
 const candidate=await sharp(work+'/candidate-v2.png').ensureAlpha().raw().toBuffer();
 const classes=new Map();
 for(let y=0;y<160;y++)for(let x=0;x<112;x++){
  const i=((y+112)*400+x)*4,j=(y*112+x)*4;if(!native[i+3])continue;
  const key=native.subarray(i,i+3).join(',');
  if(!classes.has(key))classes.set(key,{original:[...native.subarray(i,i+3)],samples:[]});
  // Keep sampled material identity: green pixels transfer from green pixels;
  // tan earth transfers from tan earth. Reject shifted grass/dirt edges.
  const grass=native[i+1]>native[i]*1.05;
  const candidateGrass=candidate[j+1]>candidate[j]*1.05;
  if(grass===candidateGrass)classes.get(key).samples.push([...candidate.subarray(j,j+3)]);
 }
 for(const c of classes.values()){
  c.output=c.original.map((v,k)=>{
   const values=c.samples.map(s=>s[k]).sort((a,b)=>a-b);
   // Conservative finish keeps the native seasonal palette recognizable.
   return values.length?Math.round(v*0.55+values[Math.floor(values.length/2)]*0.45):v;
  });
 }
 const output=Buffer.from(native);let changed=0;
 for(let y=112;y<272;y++)for(let x=0;x<112;x++){
  const i=(y*400+x)*4;if(!native[i+3])continue;
  const c=classes.get(native.subarray(i,i+3).join(','));
  for(let k=0;k<3;k++)output[i+k]=c.output[k];
  if(!output.subarray(i,i+4).equals(native.subarray(i,i+4)))changed++;
 }
 await sharp(output,{raw:{width:400,height:1264,channels:4}}).png().toFile(work+'/atlas-prepared.png');
 await sharp(output,{raw:{width:400,height:1264,channels:4}}).extract({left:0,top:112,width:112,height:160}).resize(448,640,{kernel:'nearest'}).png().toFile(work+'/prepared-preview.png');
 await fs.writeFile(work+'/prepared-qa.json',JSON.stringify({status:'Prepared; map review pending',method:'Generated finish transferred consistently by native material color class. Preserve original tile geometry, texture placements and transparency.',region:{X:0,Y:112,Width:112,Height:160},changedPixels:changed,outsidePixelsPreserved:400*1264-112*160,alphaPreserved:true,palette:[...classes.values()].map(({original,output,samples})=>({original,output,samples:samples.length}))},null,2));
 console.log(JSON.stringify({changedPixels:changed,palette:classes.size}));
})();
