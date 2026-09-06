// Detect omitted seasonal building art in the source cells the native map draws.
const fs=require('node:fs/promises'),assert=require('node:assert/strict');
const sharp=require('../.tools/abigail-art/node_modules/sharp');
const root='C:/Users/david/SDV',work=root+'/artifacts/town-civic-modern';
const read=async p=>JSON.parse((await fs.readFile(p,'utf8')).replace(/^\uFEFF/,''));
(async()=>{
 const map=(await read(work+'/native-contract.json')).find(m=>m.map==='Town');
 const current=await read(root+'/src/AbigailModern/artwork.json'),cases=[];
 for(const season of ['spring','summer','fall','winter']) {
  const asset='Maps/'+season+'_town',entry=current.find(e=>e.Name===asset);
  const before=await sharp(work+'/baseline/'+season+'_town.png').ensureAlpha().raw().toBuffer();
  const after=await sharp(root+'/src/AbigailModern/'+entry.File).ensureAlpha().raw().toBuffer();
  for(const building of ['CommunityCenter','Blacksmith','Museum','JojaMart']) {
   const cells=new Set(map.tiles.filter(t=>t.group===building&&t.sheet==='Town').map(t=>t.index));
   let changed=0;
   for(const id of cells)for(let y=0;y<16;y++)for(let x=0;x<16;x++) {
    const i=((Math.floor(id/32)*16+y)*512+id%32*16+x)*4;
    if(!before.subarray(i,i+4).equals(after.subarray(i,i+4)))changed++;
   }
   cases.push({season,building,changedPixels:changed,Passed:changed>=100});
  }
 }
 const result={Passed:cases.every(c=>c.Passed),cases,scope:'Four base civic exteriors selected through native Town placements, across four seasons. Variant contracts and visual review provide additional coverage.'};
 await fs.writeFile(work+'/'+(process.argv.includes('--baseline')?'coverage-red.json':'coverage-checks.json'),JSON.stringify(result,null,2));
 console.log(JSON.stringify({Passed:result.Passed,cases:cases.length,failures:cases.filter(c=>!c.Passed)}));
 assert(result.Passed,'Seasonal civic artwork is missing from native building source cells');
})();
