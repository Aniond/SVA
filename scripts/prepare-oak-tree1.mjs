import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {createRequire} from 'node:module';
const sharp=createRequire('C:/Users/david/SDV/.tools/abigail-art/package.json')('sharp');
const work='artifacts/terrain-modern/work/tree1/';
const cuts=JSON.parse(await fs.readFile(work+'source-crops.json','utf8'));
const cells={main:[0,0,48,96],moss:[96,0,48,96],sapling:[0,96,16,32],stump:[32,96,16,32],mossStump:[128,96,16,32],sprout:[0,128,16,16],small:[16,128,16,16],seed:[32,128,16,16],damage0:[0,144,16,16],damage1:[16,144,16,16],damage2:[32,144,16,16],leaf0:[16,112,8,8],leaf1:[24,112,8,8],leaf2:[16,120,8,8],leaf3:[24,120,8,8]};
const snow=([r,g,b])=>(b>=r&&b>=g&&b>160)||(r>220&&g>220&&b>220);
const caps=new Set(['224,170,76','255,217,150']);
const reports=[];
const previous=[];
for(const season of ['spring','summer','fall','winter']){
  const native=await sharp(work+`native/tree1_${season}.png`).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const source=await sharp(work+season+'-source.png').ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const keyed=Buffer.from(source.data);
  for(let p=0;p<keyed.length;p+=4){const[r,g,b]=keyed.subarray(p,p+3);if(r>=g&&g>=b&&r-b<55&&b>50&&r<220)keyed.fill(0,p,p+4);}
  const output=Buffer.from(native.data),mask=Buffer.alloc(output.length),records=[];let sourceMatteFallback=0,nativeSnowPreserved=0,nativeMushroomPixelsPreserved=0;
  for(const[name,cut]of Object.entries(cuts[season])){
    const[x,y,w,h]=cells[name];assert(x+w<=native.info.width&&y+h<=native.info.height);
    const points=[];
    for(let yy=0;yy<h;yy++)for(let xx=0;xx<w;xx++)if(native.data[((y+yy)*native.info.width+x+xx)*4+3]>=240)points.push([xx,yy]);
    const bx=Math.min(...points.map(p=>p[0])),by=Math.min(...points.map(p=>p[1])),bw=Math.max(...points.map(p=>p[0]))-bx+1,bh=Math.max(...points.map(p=>p[1]))-by+1;
    const[left,top,width,height]=cut;
    const cropped=await sharp(keyed,{raw:source.info}).extract({left,top,width,height}).png().toBuffer();
    const trimmed=await sharp(cropped).trim({background:'#00000000',threshold:1}).png().toBuffer({resolveWithObject:true});
    const resized=await sharp(trimmed.data).resize(bw,bh,{fit:'fill'}).ensureAlpha().raw().toBuffer();
    let changed=0;
    for(let yy=0;yy<bh;yy++)for(let xx=0;xx<bw;xx++){
      const p=((y+by+yy)*native.info.width+x+bx+xx)*4,q=(yy*bw+xx)*4;
      if(native.data[p+3]<240)continue;
      mask.fill(255,p,p+4);
      const nr=[...native.data.subarray(p,p+3)],sr=[...resized.subarray(q,q+3)];
      if(season==='winter'&&snow(nr)){nativeSnowPreserved++;continue;}
      if(season==='fall'&&name.startsWith('moss')&&caps.has(nr.join(','))){nativeMushroomPixelsPreserved++;continue;}
      if(resized[q+3]<192||(season==='winter'&&snow(sr))||(season==='fall'&&name.startsWith('moss')&&sr[0]>160&&sr[1]>130&&sr[2]>80&&sr[0]/sr[1]<1.25)){sourceMatteFallback++;continue;}
      resized.copy(output,p,q,q+3);
      if(!output.subarray(p,p+4).equals(native.data.subarray(p,p+4)))changed++;
    }
    records.push({name,nativeRectangle:cells[name],nativeOpaqueBounds:[x+bx,y+by,bw,bh],sourceCrop:cut,sourceTrimmedSize:[trimmed.info.width,trimmed.info.height],initialChangedPixels:changed});
  }
  let exactBasePixelsReusedForMoss=0;
  if(season!=='winter')for(const[base,moss]of[['main','moss'],['stump','mossStump']]){
    const[a,b,w,h]=cells[base],[c,d]=cells[moss];
    for(let y=0;y<h;y++)for(let x=0;x<w;x++){
      const p=((b+y)*native.info.width+a+x)*4,q=((d+y)*native.info.width+c+x)*4;
      if(native.data[q+3]>=240&&native.data[p+3]>=240&&native.data.subarray(p,p+3).equals(native.data.subarray(q,q+3))){output.copy(output,q,p,p+3);exactBasePixelsReusedForMoss++;}
    }
  }
  // These three native health-overlay slots are byte-identical in every season.
  // Preserve that repetition instead of introducing a new damage animation.
  for(let i=1;i<3;i++)for(let y=0;y<16;y++){
    const a=((144+y)*native.info.width)*4,b=a+i*16*4;
    assert(native.data.subarray(a,a+64).equals(native.data.subarray(b,b+64)));
    output.copy(output,b,a,a+64);
  }
  const exactSeasonalComponentReuses=[];
  for(const[name,[x,y,w,h]]of Object.entries(cells)){
    if(x+w>native.info.width)continue;
    for(const old of previous){
      if(x+w>old.native.info.width)continue;
      let same=true;
      for(let yy=0;yy<h&&same;yy++){const a=((y+yy)*native.info.width+x)*4,b=((y+yy)*old.native.info.width+x)*4;same=native.data.subarray(a,a+w*4).equals(old.native.data.subarray(b,b+w*4));}
      if(!same)continue;
      for(let yy=0;yy<h;yy++){const a=((y+yy)*native.info.width+x)*4,b=((y+yy)*old.native.info.width+x)*4;old.output.copy(output,a,b,b+w*4);}
      exactSeasonalComponentReuses.push({name,fromSeason:old.season});break;
    }
  }
  if(season==='fall')for(let y=0;y<128;y++)for(let x=96;x<144;x++){
    const p=(y*144+x)*4;if(native.data[p+3]>=240&&caps.has([...native.data.subarray(p,p+3)].join(',')))native.data.copy(output,p,p,p+4);
  }
  let changed=0,outsideMaskExact=0,shadowPixelsExact=0;
  for(let p=0;p<output.length;p+=4){
    assert.equal(output[p+3],native.data[p+3]);
    if(!mask[p+3]){assert(output.subarray(p,p+4).equals(native.data.subarray(p,p+4)));outsideMaskExact++;}
    if(native.data[p+3]>0&&native.data[p+3]<240){assert(output.subarray(p,p+4).equals(native.data.subarray(p,p+4)));shadowPixelsExact++;}
    if(!output.subarray(p,p+4).equals(native.data.subarray(p,p+4)))changed++;
  }
  await sharp(output,{raw:native.info}).png().toFile(work+`tree1_${season}-prepared.png`);
  await sharp(output,{raw:native.info}).png().toFile(work+season+'-prepared.png');
  await sharp(mask,{raw:native.info}).png().toFile(work+`tree1_${season}-body-mask.png`);
  await sharp(output,{raw:native.info}).resize(native.info.width*5,native.info.height*5,{kernel:'nearest'}).flatten({background:'#a59f8f'}).toFile(work+season+'-prepared-preview.png');
  const before=await sharp(native.data,{raw:native.info}).png().toBuffer(),after=await sharp(output,{raw:native.info}).png().toBuffer();
  const comparison=await sharp({create:{width:native.info.width*2+8,height:160,channels:4,background:'#a59f8f'}}).composite([{input:before,left:0,top:0},{input:after,left:native.info.width+8,top:0}]).png().toBuffer();
  await sharp(comparison).resize((native.info.width*2+8)*4,640,{kernel:'nearest'}).png().toFile(work+season+'-before-after.png');
  const report={season,asset:`TerrainFeatures/tree1_${season}`,dimensions:[native.info.width,native.info.height],components:records,changedPixels:changed,outsideMaskPixelsPreserved:outsideMaskExact,allNativeAlphaExact:true,shadowPixelsExact,sourceMatteFallback,nativeSnowPreserved,nativeMushroomPixelsPreserved,exactBasePixelsReusedForMoss,nativeRepeatedDamageCellsVerified:true,exactSeasonalComponentReuses,sha256:crypto.createHash('sha256').update(await fs.readFile(work+`tree1_${season}-prepared.png`)).digest('hex'),runtimeVerified:false,installed:false};
  previous.push({season,native,output});
  reports.push(report);console.log({season,changed,outsideMaskExact,sourceMatteFallback,nativeSnowPreserved,nativeMushroomPixelsPreserved});
}
await fs.writeFile(work+'preparation-qa.json',JSON.stringify(reports,null,2));
