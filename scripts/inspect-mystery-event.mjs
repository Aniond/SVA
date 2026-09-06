import fs from 'node:fs/promises';
import {unpackToFiles} from '../.tools/abigail-art/node_modules/xnb/dist/xnb.module.js';
const evidence=[];
for(const name of ['BusStop','Town']) {
 const data=await fs.readFile(`C:/Program Files (x86)/Steam/steamapps/common/Stardew Valley/Content/Data/Events/${name}.xnb`);
 for(const out of await unpackToFiles(data,{fileName:name+'.xnb'})) {
  if(out.extension!=='json')continue;
  const bytes=out.data instanceof Blob?Buffer.from(await out.data.arrayBuffer()):Buffer.from(out.data);
  const parsed=JSON.parse(bytes.toString());
  function visit(value,key='') {
   if(typeof value==='string' && /Shadow_Brute|Shadow Brute|Krobus|\?\?\?/.test(value)) evidence.push({location:name,key,script:value});
   else if(value && typeof value==='object') for(const [k,v]of Object.entries(value))visit(v,k);
  }
  visit(parsed);
 }
}
await fs.writeFile('artifacts/npc-modern/evidence/mystery-event-source.json',JSON.stringify(evidence,null,2));
for(const e of evidence)console.log(`${e.location} ${e.key}: ${e.script.split('/').filter(s=>/Shadow|Krobus|\?\?\?/.test(s)).join(' / ')}`);
