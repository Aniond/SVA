const fs=require('node:fs/promises'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const sharp=require('../.tools/abigail-art/node_modules/sharp');
const root='C:/Users/david/SDV',src=root+'/src/AbigailModern',work=root+'/artifacts/town-civic-modern';
const read=async p=>JSON.parse((await fs.readFile(p,'utf8')).replace(/^\uFEFF/,''));
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
(async()=>{
 const before=await read(work+'/baseline-artwork.json'),hashes=await read(work+'/baseline-hashes.json');
 const now=await read(src+'/artwork.json'),manifest=await read(work+'/install-manifest.json');
 const allowed=new Set(manifest.map(p=>p.asset));
 assert.equal(now.length,before.length,'Unexpected registered asset addition/removal');
 let unchanged=0,oldPixels=0,outsidePixels=0,alphaPixels=0,changedPixels=0,preparedPixels=0;
 const modified=[],changedTownCells=new Set();
 for(const old of before){
  const current=now.find(a=>a.Name===old.Name);assert(current,'Lost asset '+old.Name);assert.equal(current.File,old.File);
  const bytes=await fs.readFile(src+'/'+current.File);
  if(hash(bytes)===hashes[old.File]){assert.deepEqual(current,old,'Untouched texture registry metadata changed');unchanged++;continue;}
  assert(allowed.has(old.Name),'Unexpected modified texture '+old.Name);modified.push(old.Name);
  const b=await sharp(work+'/baseline-assets/'+old.File).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const f=await sharp(bytes).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  assert.equal(b.info.width,f.info.width);assert.equal(b.info.height,f.info.height);
  const width=b.info.width,mask=new Uint8Array(width*b.info.height),expected=Buffer.from(b.data);
  for(const patch of manifest.filter(p=>p.asset===old.Name)){
   const p=await sharp(root+'/'+patch.file).ensureAlpha().raw().toBuffer();
   const baseline=await sharp(root+'/'+patch.native).ensureAlpha().raw().toBuffer();
   assert(b.data.equals(baseline),'Prepared carrier uses stale or wrong baseline '+patch.file);
   for(const a of patch.areas)for(let y=a.Y;y<a.Y+a.Height;y++)for(let x=a.X;x<a.X+a.Width;x++)mask[y*width+x]=1;
   for(let i=0;i<p.length;i+=4)if(!p.subarray(i,i+4).equals(baseline.subarray(i,i+4))){
    assert(expected.subarray(i,i+4).equals(b.data.subarray(i,i+4))||expected.subarray(i,i+4).equals(p.subarray(i,i+4)),'Conflicting prepared pixels');
    p.copy(expected,i,i,i+4);preparedPixels++;
   }
  }
  assert(expected.equals(f.data),'Final texture differs from exact merge of prepared changes '+old.Name);
  for(let j=0;j<mask.length;j++){
   const i=j*4;assert.equal(f.data[i+3],b.data[i+3],'Alpha changed');alphaPixels++;
   const changed=!b.data.subarray(i,i+4).equals(f.data.subarray(i,i+4));
   if(!mask[j]){assert(!changed,'Outside civic area changed');outsidePixels++;}
   if(changed){changedPixels++;if(old.Name.endsWith('_town'))changedTownCells.add(Math.floor(j/width/16)*32+Math.floor((j%width)/16));}
  }
  for(const a of old.PatchAreas??(old.PatchArea?[old.PatchArea]:[{X:0,Y:0,Width:width,Height:b.info.height}])) {
   assert((current.PatchAreas??[]).some(n=>n.X===a.X&&n.Y===a.Y&&n.Width===a.Width&&n.Height===a.Height),'Prior patch registration lost');
   for(let y=a.Y;y<a.Y+a.Height;y++)for(let x=a.X;x<a.X+a.Width;x++){const i=(y*width+x)*4;assert(b.data.subarray(i,i+4).equals(f.data.subarray(i,i+4)),'Prior artwork changed');oldPixels++;}
  }
 }
 assert.equal(modified.length,allowed.size,'A prepared asset was not installed');
 const maps=await read(work+'/source-reuse-maps.json'),outside=[];let placements=0;
 const inside=(t,r)=>t.x>=r[0]&&t.x<r[0]+r[2]&&t.y>=r[1]&&t.y<r[1]+r[3];
 for(const map of maps){
  const windows=[[46,10,15,13],[90,73,9,12],[98,82,12,12],[89,41,14,13]];
  if(map.map.startsWith('Town-TheaterCC'))windows.push([46,11,15,17]);
  if(map.map==='Town-Theater')windows.push([84,41,27,15]);
  const bytes=await fs.readFile('C:/Program Files (x86)/Steam/steamapps/common/Stardew Valley/Content/Maps/'+map.map+'.xnb');
  assert.equal(hash(bytes),map.sha256,'Native reuse inventory is stale');
  for(const t of map.tiles)if(changedTownCells.has(t.index)){placements++;if(!map.map.startsWith('Town')||!windows.some(w=>inside(t,w)))outside.push({map:map.map,...t});}
 }
 const proof={Passed:outside.length===0,registeredTextures:now.length,unchangedPriorTextures:unchanged,modifiedPriorTextures:modified,changedPixels,alphaPixelsExact:alphaPixels,priorPatchPixelVisitsExact:oldPixels,outsideAreaPixelsExact:outsidePixels,preparedChangedPixelsExact:preparedPixels,sourceCells:changedTownCells.size,completeMapsReviewed:maps.length,affectedPlacements:placements,outsideSelectedBuildings:outside};
 await fs.writeFile(work+'/preservation-checks.json',JSON.stringify(proof,null,2));console.log(JSON.stringify(proof));
 assert.equal(outside.length,0,'Civic material is reused outside selected buildings');
})();
