import fs from 'node:fs/promises';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
// Native movie dialogue uses Krobus expression8; keep the standalone disguise portrait identical.
await sharp('src/AbigailModern/assets/Krobus/portraits.png').extract({left:0,top:256,width:64,height:64}).png().toFile('src/AbigailModern/assets/Krobus_Trenchcoat/portraits.png');
const file='artifacts/npc-modern/work/Krobus_Trenchcoat/validation.json';
const data=JSON.parse(await fs.readFile(file,'utf8'));
data.portraitReusedFrom='Portraits/Krobus expression8';
await fs.writeFile(file,JSON.stringify(data,null,2));
console.log('Disguise portrait synchronized with the modern Krobus expression8.');
