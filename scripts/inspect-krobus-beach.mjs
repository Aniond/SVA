import fs from 'node:fs/promises';
import {unpackToFiles} from '../.tools/abigail-art/node_modules/xnb/dist/xnb.module.js';
const bytes=await fs.readFile('C:/Program Files (x86)/Steam/steamapps/common/Stardew Valley/Content/Data/Events/Beach.xnb');
for(const out of await unpackToFiles(bytes,{fileName:'Beach.xnb'})){
 if(out.extension!=='json')continue;
 const data=JSON.parse(Buffer.from(out.data instanceof Blob?await out.data.arrayBuffer():out.data).toString());
 const matches=[];
 function visit(value,key=''){if(typeof value==='string'&&value.includes('SeaMonsterKrobus'))matches.push({key,script:value});else if(value&&typeof value==='object')for(const[k,v]of Object.entries(value))visit(v,k);}
 visit(data);await fs.writeFile('artifacts/npc-modern/work/SeaMonsterKrobus/event-source.json',JSON.stringify(matches,null,2));
 for(const match of matches)console.log(match.script.split('/').filter(s=>/SeaMonster|Krobus|sprite|animate/i.test(s)).join('\n'));
}
