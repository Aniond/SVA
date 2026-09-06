const fs=require('node:fs/promises'),assert=require('node:assert/strict'),crypto=require('node:crypto'),sharp=require('../.tools/abigail-art/node_modules/sharp');
const root='C:/Users/david/SDV',src=root+'/src/AbigailModern',work=root+'/artifacts/town-houses-modern';
(async()=>{
 const before=JSON.parse(await fs.readFile(work+'/baseline-artwork.json','utf8')),hashes=JSON.parse(await fs.readFile(work+'/baseline-hashes.json','utf8'));
 const now=JSON.parse(await fs.readFile(src+'/artwork.json','utf8')),manifest=JSON.parse(await fs.readFile(work+'/install-manifest.json','utf8'));
 let unchanged=0,oldPixels=0,newPixels=0,outsidePixels=0;const modified=[];
 assert.equal(now.length,before.length,'Unexpected asset added or removed');
 for(const old of before){const current=now.find(a=>a.Name===old.Name);assert(current,'Lost asset '+old.Name);assert.equal(current.File,old.File);const hash=crypto.createHash('sha256').update(await fs.readFile(src+'/'+current.File)).digest('hex');if(hash===hashes.find(a=>a.name===old.Name).sha256){unchanged++;continue;}
  modified.push(old.Name);assert(/^Maps\/(spring|summer|fall|winter)_town$/.test(old.Name),'Unexpected old texture modified');
  const baseline=await sharp(work+'/baseline/'+old.Name.split('/')[1]+'.png').ensureAlpha().raw().toBuffer(),final=await sharp(src+'/'+current.File).ensureAlpha().raw().toBuffer();
  const mask=new Uint8Array(old.Width*old.Height);
  for(const a of manifest.filter(p=>p.asset===old.Name).flatMap(p=>p.areas))for(let y=a.Y;y<a.Y+a.Height;y++)for(let x=a.X;x<a.X+a.Width;x++)mask[y*old.Width+x]=1;
  for(let j=0;j<mask.length;j++){const i=j*4;assert.equal(final[i+3],baseline[i+3],'Silhouette alpha changed');if(!mask[j]){assert(baseline.subarray(i,i+4).equals(final.subarray(i,i+4)),'Outside building changed');outsidePixels++;}}
  for(const area of old.PatchAreas??(old.PatchArea?[old.PatchArea]:[{X:0,Y:0,Width:old.Width,Height:old.Height}]))for(let y=area.Y;y<area.Y+area.Height;y++)for(let x=area.X;x<area.X+area.Width;x++){const i=(y*old.Width+x)*4;assert(baseline.subarray(i,i+4).equals(final.subarray(i,i+4)),'Prior patch lost at '+x+','+y);oldPixels++;}
 }
 for(const patch of manifest){const current=now.find(a=>a.Name===patch.asset),p=await sharp(root+'/'+patch.file).ensureAlpha().raw().toBuffer({resolveWithObject:true}),final=await sharp(src+'/'+current.File).ensureAlpha().raw().toBuffer();for(const area of patch.areas)for(let y=area.Y;y<area.Y+area.Height;y++)for(let x=area.X;x<area.X+area.Width;x++){const i=(y*p.info.width+x)*4;assert(p.data.subarray(i,i+4).equals(final.subarray(i,i+4)),'Prepared building differs '+patch.asset);newPixels++;}}
 assert.equal(modified.length,4,'Expected all four seasonal Town sheets');
 const proof={Passed:true,registeredTextures:now.length,unchangedPriorTextures:unchanged,modifiedPriorTextures:modified,allSeasonalAlphaExact:true,priorPatchPixelVisitsExact:oldPixels,outsideBuildingPixelsExact:outsidePixels,preparedBuildingPixelVisitsExact:newPixels};
 await fs.writeFile(work+'/preservation-checks.json',JSON.stringify(proof,null,2));console.log(JSON.stringify(proof));
})();

