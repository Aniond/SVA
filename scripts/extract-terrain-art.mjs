import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import { unpackToFiles } from '../.tools/abigail-art/node_modules/xnb/dist/xnb.module.js';
const root = 'C:/Users/david/SDV/artifacts/terrain-modern';
const game = 'C:/Program Files (x86)/Steam/steamapps/common/Stardew Valley';
const assets = process.argv.slice(2);
const records = await fs.readFile(path.join(root,'source-inventory.json'),'utf8').then(JSON.parse).catch(error=>{if(error.code==='ENOENT')return [];throw error;});
for (const asset of assets) {
  if (!/^[A-Za-z0-9_/-]+$/.test(asset) || asset.includes('..')) throw Error('Invalid asset path');
  const bytes = await fs.readFile(path.join(game,'Content',asset+'.xnb'));
  const outputs = await unpackToFiles(bytes,{fileName:path.basename(asset)+'.xnb'});
  const png = outputs.find(file=>file.extension==='png');
  if (!png) throw Error('No PNG: '+asset);
  const data = Buffer.from(png.data instanceof Blob ? await png.data.arrayBuffer() : png.data);
  const destination = path.join(root,'originals',asset+'.png');
  await fs.mkdir(path.dirname(destination),{recursive:true});
  await fs.writeFile(destination,data);
  const record={asset,width:data.readUInt32BE(16),height:data.readUInt32BE(20),sourceSha256:crypto.createHash('sha256').update(bytes).digest('hex')};
  const existing=records.findIndex(entry=>entry.asset===asset);
  if(existing>=0)records[existing]=record;else records.push(record);
}
await fs.writeFile(path.join(root,'source-inventory.json'),JSON.stringify(records,null,2));
console.log(JSON.stringify(records,null,2));
