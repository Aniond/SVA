const fs=require('node:fs/promises');
const path=require('node:path');
const assert=require('node:assert/strict');
const sharp=require('../.tools/abigail-art/node_modules/sharp');
const root='C:/Users/david/SDV',src=root+'/src/AbigailModern',work=root+'/artifacts/terrain-modern/work';
(async()=>{
 const registry=JSON.parse(await fs.readFile(src+'/artwork.json','utf8'));
 const patches=[
  ['Maps/spring_outdoorsTileSheet','ground-spring/atlas-prepared.png','spring-outdoors.png',400,1264,{X:0,Y:112,Width:112,Height:160}],
  ['TerrainFeatures/grass','grass-spring/grass-prepared.png','grass.png',66,240,{X:0,Y:0,Width:45,Height:20}],
  ['TerrainFeatures/Flooring','stone-floor/Flooring-prepared.png','flooring.png',256,256,{X:64,Y:0,Width:64,Height:64}],
  ['TerrainFeatures/Flooring_winter','stone-floor/Flooring_winter-prepared.png','flooring-winter.png',256,256,{X:64,Y:0,Width:64,Height:64}]
 ];
 const evidence=[];
 for(const [name,prepared,file,width,height,area] of patches){
  const actual=await sharp(work+'/'+prepared).ensureAlpha().raw().toBuffer();
  const native=await sharp(root+'/artifacts/terrain-modern/originals/'+name+'.png').ensureAlpha().raw().toBuffer();
  assert.equal(actual.length,width*height*4);let changed=0;
  for(let y=0;y<height;y++)for(let x=0;x<width;x++){
   const i=(y*width+x)*4,inside=x>=area.X&&x<area.X+area.Width&&y>=area.Y&&y<area.Y+area.Height;
   assert.equal(actual[i+3],native[i+3],name+' alpha');
   if(!actual.subarray(i,i+4).equals(native.subarray(i,i+4))){assert(inside,name+' outside patch');changed++;}
  }
  assert(changed>0);const relative='assets/Terrain/'+file;
  await fs.mkdir(path.dirname(src+'/'+relative),{recursive:true});await fs.copyFile(work+'/'+prepared,src+'/'+relative);
  const entry={Name:name,File:relative,Width:width,Height:height,PatchArea:area};
  const index=registry.findIndex(e=>e.Name===name);if(index>=0)registry[index]=entry;else registry.push(entry);
  evidence.push({name,changedPixels:changed,alphaPreserved:true,outsidePatchExact:true});
 }
 const cursors=registry.find(e=>e.Name==='LooseSprites/Cursors');
 const native=await sharp(src+'/'+cursors.File).ensureAlpha().raw().toBuffer();
 const output=await sharp(work+'/water-animated/cursors-prepared.png').ensureAlpha().raw().toBuffer();
 let waterChanged=0;
 for(let y=0;y<2256;y++)for(let x=0;x<704;x++){
  const i=(y*704+x)*4,inside=x<640&&((y>=2064&&y<2128)||(y>=2192&&y<2256));
  if(!inside)assert.equal(native[i+3],output[i+3],'Outside water alpha'); else assert(output[i+3]>=215&&output[i+3]<=252,'Water remains translucent');
  if(!native.subarray(i,i+4).equals(output.subarray(i,i+4))){assert(inside,'Existing NPC artwork changed');waterChanged++;}
 }
 await fs.copyFile(work+'/water-animated/cursors-prepared.png',src+'/'+cursors.File);
 for(const Y of [2064,2192])if(!cursors.PatchAreas.some(a=>a.Y===Y&&a.X===0))cursors.PatchAreas.push({X:0,Y,Width:640,Height:64});
 await fs.writeFile(src+'/artwork.json',JSON.stringify(registry,null,2));
 const manifest=JSON.parse(await fs.readFile(src+'/manifest.json','utf8'));manifest.Version='0.8.0';manifest.Description='Modern NPC artwork plus a Polished Stardew terrain starter: spring ground and grass, stone flooring, and reanimated water.';
 await fs.writeFile(src+'/manifest.json',JSON.stringify(manifest,null,2));
 const project=await fs.readFile(src+'/AbigailModern.csproj','utf8');await fs.writeFile(src+'/AbigailModern.csproj',project.replace(/<Version>[^<]+<\/Version>/,'<Version>0.8.0</Version>'));
 const readme=await fs.readFile(src+'/README.md','utf8');await fs.writeFile(src+'/README.md',readme.replace('# NPC Modern — 0.7.97','# NPC Modern — 0.8.0')+'\n\nTerrain starter: spring map ground region, ordinary spring grass, Stone Floor in normal/winter versions, and a new ten-frame water loop. Other terrain regions and seasonal ground remain pending. NPC artwork is retained.\n');
 await fs.writeFile(root+'/artifacts/terrain-modern/staging-validation.json',JSON.stringify({version:'0.8.0',registeredTextures:registry.length,patches:evidence,waterChangedPixels:waterChanged,existingNpcPixelsPreserved:true,installed:false},null,2));
 console.log(JSON.stringify({version:'0.8.0',registeredTextures:registry.length,waterChangedPixels:waterChanged}));
})();
