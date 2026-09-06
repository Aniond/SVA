const fs=require('node:fs/promises'),sharp=require('../.tools/abigail-art/node_modules/sharp');
const root='C:/Users/david/SDV',work=root+'/artifacts/town-houses-modern';
// Independently selected whole material regions. A missing season/home replacement
// must fail even if the other homes or already-polished neighboring props changed.
const targets=[
 ['Emily',[0,0,128,144]],['Jodi',[128,48,128,128]],['George',[256,32,144,144]],
 ['Lewis',[368,176,128,208]],['PamTrailer',[224,320,128,64]],['PamRebuilt',[384,656,128,144]],
 ['PamNight',[352,352,16,16]]
];
(async()=>{
 const registry=JSON.parse(await fs.readFile(root+'/src/AbigailModern/artwork.json','utf8'));
 const checks=[];
 for(const season of ['spring','summer','fall','winter']){
  const entry=registry.find(e=>e.Name==='Maps/'+season+'_town');
  const original=await sharp(work+'/baseline/'+season+'_town.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const current=await sharp(root+'/src/AbigailModern/'+entry.File).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  if(current.info.width!==512||current.info.height!==1152)throw Error('Seasonal atlas size changed: '+season);
  for(const [name,[x,y,w,h]]of targets){
   let changed=0;for(let yy=y;yy<y+h;yy++)for(let xx=x;xx<x+w;xx++){
    const i=(yy*512+xx)*4;if(!original.data.subarray(i,i+4).equals(current.data.subarray(i,i+4)))changed++;
   }
   checks.push({season,home:name,changedPixels:changed,passed:changed>0});
  }
 }
 const result={Passed:checks.every(c=>c.passed),Checks:checks.length,ChecksPassed:checks.filter(c=>c.passed).length,ChecksFailed:checks.filter(c=>!c.passed),results:checks};
 await fs.writeFile(work+'/'+(result.Passed?'coverage-checks.json':'coverage-red.json'),JSON.stringify(result,null,2));
 console.log(JSON.stringify({Passed:result.Passed,Checks:result.Checks,ChecksPassed:result.ChecksPassed,Failures:result.ChecksFailed.map(c=>c.season+'/'+c.home)}));
 if(!result.Passed)process.exitCode=1;
})();
