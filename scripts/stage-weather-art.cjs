const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const sharp=require('../.tools/abigail-art/node_modules/sharp');
const root='C:/Users/david/SDV',src=root+'/src/AbigailModern';
(async()=>{
 const registry=JSON.parse(await fs.readFile(src+'/artwork.json','utf8'));
 const patches=JSON.parse(await fs.readFile(root+'/artifacts/weather-modern/install-manifest.json','utf8'));
 const outputs=new Map(),proof=[];
 for(const patch of patches){
  const p=await sharp(root+'/'+patch.file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const native=patch.native?await sharp(root+'/'+patch.native).ensureAlpha().raw().toBuffer({resolveWithObject:true}):null;
  const existing=registry.find(e=>e.Name.toLowerCase()===patch.asset.toLowerCase());
  if(native){assert.equal(native.info.width,p.info.width);assert.equal(native.info.height,p.info.height);}
  let item=outputs.get(patch.asset);
  if(!item){
   const baseline=existing?await sharp(src+'/'+existing.File).ensureAlpha().raw().toBuffer():native?.data??Buffer.alloc(p.data.length);
   assert.equal(baseline.length,p.data.length);
   item={data:Buffer.from(baseline),baseline:Buffer.from(baseline),relative:existing?.File??'assets/Weather/'+patch.asset.replaceAll('/','-')+'.png',width:p.info.width,height:p.info.height,areas:existing?(existing.PatchAreas??(existing.PatchArea?[existing.PatchArea]:[{X:0,Y:0,Width:p.info.width,Height:p.info.height}])):[]};
   item.custom=!native&&(!existing||(!existing.PatchAreas&&!existing.PatchArea));outputs.set(patch.asset,item);
  }
  const areas=patch.areas??[{X:0,Y:0,Width:p.info.width,Height:p.info.height}];const mask=new Uint8Array(p.info.width*p.info.height);
  for(const a of areas){assert(a.X>=0&&a.Y>=0&&a.Width>0&&a.Height>0&&a.X+a.Width<=p.info.width&&a.Y+a.Height<=p.info.height);for(let y=a.Y;y<a.Y+a.Height;y++)for(let x=a.X;x<a.X+a.Width;x++)mask[y*p.info.width+x]=1;if(!item.areas.some(b=>JSON.stringify(a)===JSON.stringify(b)))item.areas.push(a);}
  let changed=0,alphaChanges=0;
  for(let j=0;j<mask.length;j++){
   const i=j*4,n=native?.data;
   if(!mask[j]){if(n)assert(p.data.subarray(i,i+4).equals(n.subarray(i,i+4)),patch.asset+' changed outside declared areas');continue;}
   if(n&&p.data.subarray(i,i+4).equals(n.subarray(i,i+4)))continue;
   if(n&&existing&&!item.baseline.subarray(i,i+4).equals(n.subarray(i,i+4)))assert(item.baseline.subarray(i,i+4).equals(p.data.subarray(i,i+4)),patch.asset+' conflicts with prior art at pixel '+j);
   if(!item.data.subarray(i,i+4).equals(p.data.subarray(i,i+4)))changed++;
   if(item.data[i+3]!==p.data[i+3])alphaChanges++;
   p.data.copy(item.data,i,i,i+4);
  }
  assert(changed>0,patch.asset+' unchanged');proof.push({asset:patch.asset,changedPixels:changed,alphaChanges,areas,sha256:crypto.createHash('sha256').update(await fs.readFile(root+'/'+patch.file)).digest('hex')});
 }
 for(const [name,item] of outputs){await fs.mkdir(path.dirname(src+'/'+item.relative),{recursive:true});await sharp(item.data,{raw:{width:item.width,height:item.height,channels:4}}).png().toFile(src+'/'+item.relative);const entry={Name:name,File:item.relative,Width:item.width,Height:item.height};if(!item.custom)entry.PatchAreas=item.areas;const i=registry.findIndex(e=>e.Name.toLowerCase()===name.toLowerCase());if(i<0)registry.push(entry);else registry[i]=entry;}
 await fs.writeFile(src+'/artwork.json',JSON.stringify(registry,null,2));
 const manifest=JSON.parse(await fs.readFile(src+'/manifest.json','utf8'));manifest.Version='0.8.3';manifest.Description='Polished NPCs, terrain, props and animated weather, including occasional visual storm hail.';await fs.writeFile(src+'/manifest.json',JSON.stringify(manifest,null,2));
 const project=await fs.readFile(src+'/AbigailModern.csproj','utf8');await fs.writeFile(src+'/AbigailModern.csproj',project.replace(/<Version>[^<]+<\/Version>/,'<Version>0.8.3</Version>'));
 await fs.writeFile(root+'/artifacts/weather-modern/staging.json',JSON.stringify({version:'0.8.3',registeredTextures:registry.length,proof,installed:false},null,2));
 console.log(JSON.stringify({version:'0.8.3',registeredTextures:registry.length,updatedAssets:outputs.size}));
})();
