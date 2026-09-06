import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const root='src/AbigailModern/',work='artifacts/npc-modern/work/Children/';
const mappings=[['Baby','baby-light',88],['Baby_dark','baby-dark',88],['Toddler','toddler-boy-light',64],['Toddler_dark','toddler-boy-dark',64],['Toddler_girl','toddler-girl-light',64],['Toddler_girl_dark','toddler-girl-dark',64]];
const registry=JSON.parse(await fs.readFile(root+'artwork.json','utf8'));
for(const [name,prefix,width] of mappings){
 for(const [kind,suffix,file,w] of [['Characters','prepared.png','characters.png',width],['Portraits','prepared-portraits.png','portraits.png',128]]){
  const source=work+prefix+'-'+suffix,meta=await sharp(source).metadata();
  assert.equal(meta.width,w);assert.equal(meta.height,192);
  const folder='assets/Children/'+name+'/';await fs.mkdir(root+folder,{recursive:true});await fs.copyFile(source,root+folder+file);
  const entry={Name:kind+'/'+name,File:folder+file,Width:w,Height:192};
  const i=registry.findIndex(e=>e.Name===entry.Name);if(i<0)registry.push(entry);else registry[i]=entry;
 }
}
await fs.writeFile(root+'artwork.json',JSON.stringify(registry,null,2));
console.log('Registered 6child sprite sheets and6portrait sheets; installation andruntimechecks still required.');
