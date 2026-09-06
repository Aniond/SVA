import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const sharp=createRequire(new URL('../.tools/abigail-art/package.json',import.meta.url))('sharp');
const root='src/AbigailModern/',work='artifacts/npc-modern/work/FishingContestants/';
const summer=process.argv.includes('--summer'),season=summer?'summer':'winter';
const entries=JSON.parse(await fs.readFile(root+'artwork.json','utf8'));
const additions=[{Name:'Characters/Assorted_Fishermen'+(summer?'':'_Winter'),File:`assets/FishingContestants/${season}.png`,Width:64,Height:256,source:`${season}-prepared.png`}];
for(let i=0;i<(summer?10:12);i++)additions.push({Name:'Portraits/FishingContestant'+(summer?'Summer':'Winter')+i,File:`assets/FishingContestants/${season}-portrait-${i}.png`,Width:128,Height:192,source:`contestant${i}-${season}-portraits-prepared.png`});
for(const item of additions){const m=await sharp(work+item.source).metadata();assert.equal(m.width,item.Width);assert.equal(m.height,item.Height);assert.ok(m.hasAlpha);}
await fs.mkdir(root+'assets/FishingContestants',{recursive:true});
for(const {source,...entry} of additions){await fs.copyFile(work+source,root+entry.File);const index=entries.findIndex(e=>e.Name===entry.Name);if(index>=0)entries[index]=entry;else entries.push(entry);}
await fs.writeFile(root+'artwork.json',JSON.stringify(entries,null,2)+'\n');
console.log(`Registered${additions.length} ${season} textures; ${entries.length} total. Installation and runtime verification pending.`);
