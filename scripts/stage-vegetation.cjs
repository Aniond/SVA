const fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict');
const sharp=require('../.tools/abigail-art/node_modules/sharp');
const root='C:/Users/david/SDV',src=root+'/src/AbigailModern',work=root+'/artifacts/terrain-modern/work';
(async()=>{
 const registry=JSON.parse(await fs.readFile(src+'/artwork.json','utf8'));
 const pending=[],proof=[];
 for(const id of [1,2,3])for(const season of id===3?['spring','fall','winter']:['spring','summer','fall','winter']){
  const file=work+'/tree'+id+'/'+season+'-prepared.png';
  const nativeFile=id===3?root+'/artifacts/terrain-modern/originals/TerrainFeatures/tree3_'+season+'.png':work+'/tree'+id+'/native/tree'+id+'_'+season+'.png';
  const n=await sharp(nativeFile).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const p=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  assert.equal(p.info.width,n.info.width);assert.equal(p.info.height,n.info.height);let changed=0;
  for(let i=0;i<n.data.length;i+=4){assert.equal(n.data[i+3],p.data[i+3]);if(!n.data[i+3])assert(n.data.subarray(i,i+4).equals(p.data.subarray(i,i+4)));if(!n.data.subarray(i,i+4).equals(p.data.subarray(i,i+4)))changed++;}
  assert(changed>0);
  const name='TerrainFeatures/tree'+id+'_'+season,relative='assets/Vegetation/tree'+id+'_'+season+'.png';
  pending.push({file,relative,entry:{Name:name,File:relative,Width:p.info.width,Height:p.info.height}});proof.push({name,changedPixels:changed,alphaExact:true,transparentPixelsExact:true});
 }
 const bushFile=work+'/bushes/bushes-prepared.png';
 const bushAreas=[{X:0,Y:0,Width:128,Height:128},{X:0,Y:128,Width:96,Height:96},{X:0,Y:224,Width:128,Height:32}];
 const n=await sharp(work+'/bushes/native.png').ensureAlpha().raw().toBuffer();const p=await sharp(bushFile).ensureAlpha().raw().toBuffer();
 assert.equal(p.length,128*352*4);let changed=0;
 for(let y=0;y<352;y++)for(let x=0;x<128;x++){const i=(y*128+x)*4;assert.equal(n[i+3],p[i+3]);if(!n.subarray(i,i+4).equals(p.subarray(i,i+4))){assert(bushAreas.some(a=>x>=a.X&&x<a.X+a.Width&&y>=a.Y&&y<a.Y+a.Height));changed++;}}
 pending.push({file:bushFile,relative:'assets/Vegetation/bushes.png',entry:{Name:'TileSheets/bushes',File:'assets/Vegetation/bushes.png',Width:128,Height:352,PatchAreas:bushAreas}});proof.push({name:'TileSheets/bushes',changedPixels:changed,outsideExact:true,alphaExact:true});
 // Flowers use their own patch manifest; no stale full atlas is copied.
 const flowerPatches=JSON.parse(await fs.readFile(work+'/flowers/install-patches.json','utf8'));
 const atlasOutputs=[];
 for(const season of ['spring','summer','fall','winter']){
  const name='Maps/'+season+'_outdoorsTileSheet',existing=registry.find(e=>e.Name===name);
  const baseline=existing?src+'/'+existing.File:root+'/artifacts/terrain-modern/originals/'+name+'.png';
  const data=await sharp(baseline).ensureAlpha().raw().toBuffer();const output=Buffer.from(data);
  const patches=flowerPatches.filter(p=>p.season===season);assert.equal(patches.length,2);
  const areas=[];
  for(const patch of patches){
   const pixels=await sharp(work+'/flowers/'+patch.file).ensureAlpha().raw().toBuffer();assert.equal(pixels.length,16*16*4);
   for(let y=0;y<16;y++)for(let x=0;x<16;x++){const i=((patch.y+y)*400+patch.x+x)*4,j=(y*16+x)*4;assert.equal(data[i+3],pixels[j+3]);pixels.copy(output,i,j,j+4);}
   areas.push({X:patch.x,Y:patch.y,Width:16,Height:16});
  }
  const relative=existing?.File??'assets/Vegetation/'+season+'-outdoors.png';
  const previous=existing?.PatchAreas??(existing?.PatchArea?[existing.PatchArea]:[]);
  const combined=[...previous];for(const a of areas)if(!combined.some(b=>JSON.stringify(a)===JSON.stringify(b)))combined.push(a);
  atlasOutputs.push({output,relative,entry:{Name:name,File:relative,Width:400,Height:1264,PatchAreas:combined}});
 }
 for(const item of pending){await fs.mkdir(path.dirname(src+'/'+item.relative),{recursive:true});await fs.copyFile(item.file,src+'/'+item.relative);const i=registry.findIndex(e=>e.Name===item.entry.Name);if(i>=0)registry[i]=item.entry;else registry.push(item.entry);}
 for(const item of atlasOutputs){await fs.mkdir(path.dirname(src+'/'+item.relative),{recursive:true});await sharp(item.output,{raw:{width:400,height:1264,channels:4}}).png().toFile(src+'/'+item.relative);const i=registry.findIndex(e=>e.Name===item.entry.Name);if(i>=0)registry[i]=item.entry;else registry.push(item.entry);}
 await fs.writeFile(src+'/artwork.json',JSON.stringify(registry,null,2));
 const manifest=JSON.parse(await fs.readFile(src+'/manifest.json','utf8'));manifest.Version='0.8.1';manifest.Description='Modern NPC artwork, polished terrain and water, common seasonal wild trees, ordinary bushes and decorative seasonal plants.';await fs.writeFile(src+'/manifest.json',JSON.stringify(manifest,null,2));
 const project=await fs.readFile(src+'/AbigailModern.csproj','utf8');await fs.writeFile(src+'/AbigailModern.csproj',project.replace(/<Version>[^<]+<\/Version>/,'<Version>0.8.1</Version>'));
 const readme=await fs.readFile(src+'/README.md','utf8');await fs.writeFile(src+'/README.md',readme.replace('# NPC Modern — 0.8.0','# NPC Modern — 0.8.1')+'\n\nVegetation batch: regular seasonal oak, maple and pine growth/stump sheets; ordinary seasonal bushes; two decorative plant positions across four seasons. Special trees, fruit trees, tea/walnut bushes and crop flowers remain separate work.\n');
 await fs.writeFile(root+'/artifacts/terrain-modern/vegetation-staging.json',JSON.stringify({version:'0.8.1',textures:registry.length,treesAndBushes:proof,flowerPatches,installed:false},null,2));console.log(JSON.stringify({version:'0.8.1',textures:registry.length}));
})();
