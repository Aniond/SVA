const fs=require('node:fs/promises'),assert=require('node:assert/strict'),crypto=require('node:crypto'),sharp=require('../.tools/abigail-art/node_modules/sharp');
const root='C:/Users/david/SDV',src=root+'/src/AbigailModern',work=root+'/artifacts/weather-modern';
(async()=>{
 const before=JSON.parse(await fs.readFile(work+'/baseline-artwork.json','utf8')),hashes=JSON.parse(await fs.readFile(work+'/baseline-hashes.json','utf8'));
 const now=JSON.parse(await fs.readFile(src+'/artwork.json','utf8')),manifest=JSON.parse(await fs.readFile(work+'/install-manifest.json','utf8'));
 let unchanged=0,oldPixels=0,newPixels=0;const modified=[];
 for(const old of before){const current=now.find(a=>a.Name===old.Name);assert(current,'Lost asset '+old.Name);assert.equal(current.File,old.File);const hash=crypto.createHash('sha256').update(await fs.readFile(src+'/'+current.File)).digest('hex');if(hash===hashes.find(a=>a.name===old.Name).sha256){unchanged++;continue;}modified.push(old.Name);assert.equal(old.Name,'LooseSprites/Cursors','Unexpected old texture modified');
  const baseline=await sharp(work+'/snow-wind/baseline.png').ensureAlpha().raw().toBuffer(),final=await sharp(src+'/'+current.File).ensureAlpha().raw().toBuffer();
  for(const area of old.PatchAreas??(old.PatchArea?[old.PatchArea]:[{X:0,Y:0,Width:old.Width,Height:old.Height}]))for(let y=area.Y;y<area.Y+area.Height;y++)for(let x=area.X;x<area.X+area.Width;x++){const i=(y*old.Width+x)*4;assert(baseline.subarray(i,i+4).equals(final.subarray(i,i+4)),'Prior patch lost at '+x+','+y);oldPixels++;}
 }
 for(const patch of manifest){const current=now.find(a=>a.Name===patch.asset),p=await sharp(root+'/'+patch.file).ensureAlpha().raw().toBuffer({resolveWithObject:true}),final=await sharp(src+'/'+current.File).ensureAlpha().raw().toBuffer();for(const area of patch.areas??[{X:0,Y:0,Width:p.info.width,Height:p.info.height}])for(let y=area.Y;y<area.Y+area.Height;y++)for(let x=area.X;x<area.X+area.Width;x++){const i=(y*p.info.width+x)*4;assert(p.data.subarray(i,i+4).equals(final.subarray(i,i+4)),'Prepared weather differs '+patch.asset);newPixels++;}}
 const hail=now.find(a=>a.Name==='Mods/David.AbigailModern/Weather/Hail');assert(hail&&!hail.PatchAreas&&!hail.PatchArea,'Hail needs full-asset load');
 const proof={Passed:true,registeredTextures:now.length,unchangedPriorTextures:unchanged,modifiedPriorTextures:modified,priorPatchPixelVisitsExact:oldPixels,preparedWeatherPixelVisitsExact:newPixels,newAssets:now.filter(a=>!before.some(b=>a.Name===b.Name)).map(a=>a.Name)};
 await fs.writeFile(work+'/preservation-checks.json',JSON.stringify(proof,null,2));console.log(JSON.stringify(proof));
})();
