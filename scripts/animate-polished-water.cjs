const fs=require('node:fs/promises');
const crypto=require('node:crypto');
const sharp=require('../.tools/abigail-art/node_modules/sharp');
const root='C:/Users/david/SDV',work=root+'/artifacts/terrain-modern/work/water-animated';
(async()=>{
 const small=await sharp(work+'/source.png').resize(32,32,{kernel:'nearest'}).png().toBuffer();
 const source=await sharp(small).resize(64,64,{kernel:'nearest'}).ensureAlpha().raw().toBuffer();
 // Prepare a periodic tile from the generated material. The narrow boundary
 // blend joins opposite edges; all motion samples wrap within this tile.
 const base=Buffer.from(source),w=64;
 for(let y=0;y<w;y++)for(let x=0;x<4;x++)for(let c=0;c<3;c++){
  const a=(y*w+x)*4+c,b=(y*w+w-1-x)*4+c,t=(4-x)/8;
  base[a]=Math.round(source[a]*(1-t)+source[b]*t);base[b]=Math.round(source[b]*(1-t)+source[a]*t);
 }
 const horizontal=Buffer.from(base);
 for(let x=0;x<w;x++)for(let y=0;y<4;y++)for(let c=0;c<3;c++){
  const a=(y*w+x)*4+c,b=((w-1-y)*w+x)*4+c,t=(4-y)/8;
  base[a]=Math.round(horizontal[a]*(1-t)+horizontal[b]*t);base[b]=Math.round(horizontal[b]*(1-t)+horizontal[a]*t);
 }
 const levels=[]; for(let i=0;i<base.length;i+=4)levels.push(base[i]*0.2126+base[i+1]*0.7152+base[i+2]*0.0722); levels.sort((a,b)=>a-b);
 const mid=levels[Math.floor(levels.length*0.824)],high=levels[Math.floor(levels.length*0.957)];
 for(let i=0;i<base.length;i+=4){const l=base[i]*0.2126+base[i+1]*0.7152+base[i+2]*0.0722;base[i+3]=l>=high?252:l>=mid?216:215;}
 const frames=[];
 for(let f=0;f<10;f++){
  const phase=2*Math.PI*f/10,frame=Buffer.alloc(64*64*4);
  for(let y=0;y<64;y++)for(let x=0;x<64;x++){
   const sx=(x+Math.round(2*Math.sin(2*Math.PI*y/64+phase))+64)%64;
   const sy=(y+Math.round(Math.sin(2*Math.PI*x/64-phase))+64)%64;
   base.copy(frame,(y*64+x)*4,(sy*64+sx)*4,(sy*64+sx)*4+4);
  }
  frames.push(frame);
 }
 const registry=JSON.parse(await fs.readFile(root+'/src/AbigailModern/artwork.json','utf8'));
 const entry=registry.find(e=>e.Name==='LooseSprites/Cursors');
 const {data:native,info}=await sharp(root+'/src/AbigailModern/'+entry.File).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 const output=Buffer.from(native),bands=[2064,2192];
 for(const top of bands)for(let f=0;f<10;f++)for(let y=0;y<64;y++)
  frames[f].copy(output,((top+y)*info.width+f*64)*4,y*64*4,(y+1)*64*4);
 await sharp(output,{raw:{width:info.width,height:info.height,channels:4}}).png().toFile(work+'/cursors-prepared.png');
 const previewFrames=[];
 for(const frame of frames){
  const canvas=Buffer.alloc(192*192*4);
  for(let y=0;y<192;y++)for(let x=0;x<192;x++)frame.copy(canvas,(y*192+x)*4,((y%64)*64+x%64)*4,((y%64)*64+x%64)*4+4);
  previewFrames.push(canvas);
 }
 await sharp(Buffer.concat(previewFrames),{raw:{width:192,height:1920,channels:4,pageHeight:192}}).gif({delay:Array(10).fill(200),loop:0}).toFile(work+'/water-loop.gif');
 await sharp(previewFrames[0],{raw:{width:192,height:192,channels:4}}).resize(576,576,{kernel:'nearest'}).png().toFile(work+'/water-tiled-preview.png');
 const hashes=frames.map(f=>crypto.createHash('sha256').update(f).digest('hex'));
 await fs.writeFile(work+'/validation.json',JSON.stringify({generation:'exec-3065ea07-91c9-4d19-8df8-3c91bf32202a',frames:10,uniqueFrames:new Set(hashes).size,frameDurationMs:200,loopMs:2000,bands:bands.map(Y=>({X:0,Y,Width:640,Height:64})),parity:'Both native bands use the same continuous tile animation to avoid alternating seams.',method:'Generated texture; periodic boundary preparation and looping sinusoidal displacement. Original native frame positions retained; wave artwork and motion replaced with user authorization.',frameHashes:hashes,runtimeVerified:false},null,2));
 console.log(JSON.stringify({frames:10,uniqueFrames:new Set(hashes).size}));
})();
