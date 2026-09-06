const fs = require('node:fs/promises');
const sharp = require('../.tools/abigail-art/node_modules/sharp');
const root = 'C:/Users/david/SDV/artifacts/terrain-modern/work/ground-spring';
(async () => {
  const native = await sharp('C:/Users/david/SDV/artifacts/terrain-modern/originals/Maps/spring_outdoorsTileSheet.png').extract({left:0,top:112,width:112,height:160}).ensureAlpha().raw().toBuffer();
  const candidate = await sharp(root+'/candidate-v1.png').ensureAlpha().raw().toBuffer();
  let changedPixels=0, changedBoundaryPixels=0, changedAlphaPixels=0;
  const tiles=[];
  for(let ty=0;ty<10;ty++) for(let tx=0;tx<7;tx++) {
    let changed=0,boundaries=0;
    for(let y=0;y<16;y++) for(let x=0;x<16;x++) {
      const i=((ty*16+y)*112+tx*16+x)*4;
      if(native[i+3]!==candidate[i+3])changedAlphaPixels++;
      if(!native.subarray(i,i+4).equals(candidate.subarray(i,i+4))) {
        changed++; changedPixels++;
        if(x===0||x===15||y===0||y===15){boundaries++;changedBoundaryPixels++;}
      }
    }
    tiles.push({column:tx,row:ty,nativeTileIndex:(ty+7)*25+tx,changedPixels:changed,changedBoundaryPixels:boundaries});
  }
  const qa={status:'Concept only; not installable',nativeCrop:{x:0,y:112,width:112,height:160},changedPixels,changedBoundaryPixels,changedAlphaPixels,reason:'Generated texture shifts tile boundary colors and geometry; preserve native transparency and validate connected layouts before production.',tiles};
  await fs.writeFile(root+'/candidate-v1-qa.json',JSON.stringify(qa,null,2));
  console.log(JSON.stringify({changedPixels,changedBoundaryPixels,changedAlphaPixels,status:qa.status}));
})();
