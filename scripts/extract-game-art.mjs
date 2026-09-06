import { unpackToFiles } from '../.tools/abigail-art/node_modules/xnb/dist/xnb.module.js';
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const root = 'C:/Users/david/SDV/artifacts/npc-modern';
const inventoryFile = path.join(root, 'source-inventory.json');
const inventory = JSON.parse(await fs.readFile(inventoryFile, 'utf8'));
for (const asset of process.argv.slice(2)) {
  assert.match(asset, /^[A-Za-z0-9_-]+\/[A-Za-z0-9_-]+$/);
  const bytes = await fs.readFile(path.join(inventory.gamePath, 'Content', asset + '.xnb'));
  const outputs = await unpackToFiles(bytes, { fileName: path.basename(asset) + '.xnb' });
  const png = outputs.find(output => output.extension === 'png');
  assert.ok(png, `No PNG in ${asset}`);
  const data = png.data instanceof Blob ? Buffer.from(await png.data.arrayBuffer()) : Buffer.from(png.data);
  const output = path.join(root, 'originals', asset + '.png');
  await fs.mkdir(path.dirname(output), { recursive: true });
  await fs.writeFile(output, data);
  if (!inventory.assets.some(entry => entry.asset === asset)) inventory.assets.push({ asset, width: data.readUInt32BE(16), height: data.readUInt32BE(20), sourceSha256: crypto.createHash('sha256').update(bytes).digest('hex'), pngSha256: crypto.createHash('sha256').update(data).digest('hex'), scope: 'Shared texture; update only confirmed NPC regions, not unrelated map or UI art.' });
  console.log(`${asset}: ${data.readUInt32BE(16)}x${data.readUInt32BE(20)}`);
}
await fs.writeFile(inventoryFile, JSON.stringify(inventory, null, 2));
