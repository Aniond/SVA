import { unpackToFiles } from '../.tools/abigail-art/node_modules/xnb/dist/xnb.module.js';
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
const repo = fileURLToPath(new URL('../', import.meta.url));
const game = process.argv[2] ?? 'C:/Program Files (x86)/Steam/steamapps/common/Stardew Valley';
const inventory = [];
for (const folder of ['Characters', 'Portraits']) {
  const sourceFolder = path.join(game, 'Content', folder);
  const outputFolder = path.join(repo, 'artifacts/npc-modern/originals', folder);
  await fs.mkdir(outputFolder, { recursive: true });
  for (const file of (await fs.readdir(sourceFolder)).filter(name => name.endsWith('.xnb')).sort()) {
    const source = await fs.readFile(path.join(sourceFolder, file));
    const name = file.slice(0, -4);
    const outputs = await unpackToFiles(source, { fileName: file });
    const png = outputs.find(output => output.extension === 'png');
    if (!png) throw new Error(`No image exported for ${folder}/${name}`);
    const bytes = png.data instanceof Blob ? Buffer.from(await png.data.arrayBuffer()) : Buffer.from(png.data);
    await fs.writeFile(path.join(outputFolder, name + '.png'), bytes);
    inventory.push({ asset: `${folder}/${name}`, width: bytes.readUInt32BE(16), height: bytes.readUInt32BE(20), sourceSha256: crypto.createHash('sha256').update(source).digest('hex'), pngSha256: crypto.createHash('sha256').update(bytes).digest('hex') });
  }
}
await fs.writeFile(path.join(repo, 'artifacts/npc-modern/source-inventory.json'), JSON.stringify({ gamePath: game, generatedAt: new Date().toISOString(), assets: inventory }, null, 2));
console.log(`Inventoried and extracted ${inventory.length} installed character/portrait textures.`);
