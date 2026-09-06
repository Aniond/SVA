import fs from 'node:fs/promises';
import {unpackToFiles} from '../.tools/abigail-art/node_modules/xnb/dist/xnb.module.js';
const root='C:/Program Files (x86)/Steam/steamapps/common/Stardew Valley/Content/Data/Events/',matches=[];
for(const file of await fs.readdir(root)){
 if(!file.endsWith('.xnb'))continue;
 const outputs=await unpackToFiles(await fs.readFile(root+file),{fileName:file});
 for(const out of outputs){if(out.extension!=='json')continue;const data=JSON.parse(Buffer.from(out.data instanceof Blob?await out.data.arrayBuffer():out.data).toString());
 function visit(value,key=''){if(typeof value==='string'&&value.includes('IslandParrot'))matches.push({file,key,script:value});else if(value&&typeof value==='object')for(const[k,v]of Object.entries(value))visit(v,k);}
 visit(data);}
}
await fs.writeFile('artifacts/npc-modern/work/IslandParrot/event-source.json',JSON.stringify(matches,null,2));
console.log(JSON.stringify(matches.map(m=>({file:m.file,key:m.key,commands:m.script.split('/').filter(s=>/IslandParrot|speak |animate /i.test(s))})),null,2));
