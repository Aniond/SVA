const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const sharp=require('../.tools/abigail-art/node_modules/sharp');
const root='C:/Users/david/SDV',src=root+'/src/AbigailModern',work=root+'/artifacts/terrain-modern/work';
(async()=>{
 const registry=JSON.parse(await fs.readFile(src+'/artwork.json','utf8'));
 const manifestPath=path.resolve(root,process.argv[2]??'artifacts/terrain-modern/work/outdoor-install-manifest.json');
 assert(manifestPath.toLowerCase().startsWith(path.resolve(work).toLowerCase()+path.sep),'Manifest must be inside artwork work folder');
 const patches=JSON.parse(await fs.readFile(manifestPath,'utf8'));
 const outputs=new Map(),proof=[];
 for(const patch of patches){
  const n=await sharp(root+'/'+patch.native).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const p=await sharp(root+'/'+patch.file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  assert.equal(n.info.width,p.info.width,patch.asset);assert.equal(n.info.height,p.info.height,patch.asset);
  for(let i=0;i<n.data.length;i+=4){assert.equal(p.data[i+3],n.data[i+3],patch.asset+' alpha');if(!n.data[i+3])assert(p.data.subarray(i,i+4).equals(n.data.subarray(i,i+4)),patch.asset+' transparent RGB');}
  const existing=registry.find(e=>e.Name.toLowerCase()===patch.asset.toLowerCase());
  let item=outputs.get(patch.asset);
  if(!item){
   const baseline=existing?await sharp(src+'/'+existing.File).ensureAlpha().raw().toBuffer():n.data;
   const prior=existing?(existing.PatchAreas??(existing.PatchArea?[existing.PatchArea]:[{X:0,Y:0,Width:n.info.width,Height:n.info.height}])):[];
   item={data:Buffer.from(baseline),baseline:Buffer.from(baseline),relative:existing?.File??'assets/OutdoorProps/'+patch.asset.replaceAll('/','-')+'.png',areas:[...prior],width:n.info.width,height:n.info.height};outputs.set(patch.asset,item);
  }
  const areas=patch.areas??[{X:0,Y:0,Width:n.info.width,Height:n.info.height}];let changed=0;
  for(const area of areas){
   assert(area.X>=0&&area.Y>=0&&area.Width>0&&area.Height>0&&area.X+area.Width<=n.info.width&&area.Y+area.Height<=n.info.height);
   for(let y=area.Y;y<area.Y+area.Height;y++)for(let x=area.X;x<area.X+area.Width;x++){
    const i=(y*n.info.width+x)*4;
    // Carriers contain untouched native pixels around the requested artwork.
    // Those pixels must never revert an earlier patch on the shared atlas.
    if(p.data.subarray(i,i+4).equals(n.data.subarray(i,i+4)))continue;
    if(existing&&!item.baseline.subarray(i,i+4).equals(n.data.subarray(i,i+4)))
     assert(item.baseline.subarray(i,i+4).equals(p.data.subarray(i,i+4)),patch.asset+' would replace previously polished pixels at '+x+','+y);
    if(!item.data.subarray(i,i+4).equals(p.data.subarray(i,i+4)))changed++;
    p.data.copy(item.data,i,i,i+4);
   }
   if(!item.areas.some(a=>JSON.stringify(a)===JSON.stringify(area)))item.areas.push(area);
  }
  assert(changed>0,patch.asset+' has no changes');proof.push({asset:patch.asset,changedPixels:changed,alphaExact:true,areas,preparedSha256:crypto.createHash('sha256').update(await fs.readFile(root+'/'+patch.file)).digest('hex')});
 }
 // Complete every preflight before writing production files.
 for(const [name,item] of outputs){
  await fs.mkdir(path.dirname(src+'/'+item.relative),{recursive:true});await sharp(item.data,{raw:{width:item.width,height:item.height,channels:4}}).png().toFile(src+'/'+item.relative);
  const entry={Name:name,File:item.relative,Width:item.width,Height:item.height,PatchAreas:item.areas};
  const index=registry.findIndex(e=>e.Name.toLowerCase()===name.toLowerCase());if(index>=0)registry[index]=entry;else registry.push(entry);
 }
 await fs.writeFile(src+'/artwork.json',JSON.stringify(registry,null,2));
 const manifest=JSON.parse(await fs.readFile(src+'/manifest.json','utf8'));manifest.Version='0.8.2';manifest.Description='Modern NPC art, polished seasonal vegetation, terrain, roads, bridges, fences, paths and regular props.';await fs.writeFile(src+'/manifest.json',JSON.stringify(manifest,null,2));
 const project=await fs.readFile(src+'/AbigailModern.csproj','utf8');await fs.writeFile(src+'/AbigailModern.csproj',project.replace(/<Version>[^<]+<\/Version>/,'<Version>0.8.2</Version>'));
 const readme=await fs.readFile(src+'/README.md','utf8');const note='Outdoor props batch: seasonal paths, roads and bridges, four fence materials and gates, craftable surfaces and regular furnishings. Shapes and source-frame layouts are preserved. Exact coverage and gameplay verification limits: docs/terrain-art/roads-props-progress.md in the project.';
 await fs.writeFile(src+'/README.md',readme.replace(/# NPC Modern — [^\r\n]+/,'# NPC Modern — 0.8.2').replace(/Version 0\.8\.2 contains \d+ registered textures/,'Version 0.8.2 contains '+registry.length+' registered textures').replace(/(?:this terrain starter|the current package) contains \d+ total/,'the current package contains '+registry.length+' total')+(readme.includes('Outdoor props batch:')?'':'\n\n'+note+'\n'));
 const previous=await fs.readFile(root+'/artifacts/terrain-modern/outdoor-staging.json','utf8').then(JSON.parse).catch(e=>{if(e.code==='ENOENT')return null;throw e;});
 await fs.writeFile(root+'/artifacts/terrain-modern/outdoor-staging.json',JSON.stringify({version:'0.8.2',registeredTextures:registry.length,proof:[...(previous?.version==='0.8.2'?previous.proof:[]),...proof],installed:false},null,2));console.log(JSON.stringify({version:'0.8.2',registeredTextures:registry.length,updatedAssets:outputs.size}));
})();
