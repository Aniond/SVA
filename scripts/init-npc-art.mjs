import fs from 'node:fs/promises';
import path from 'node:path';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
import assert from 'node:assert/strict';
const sharp = createRequire(new URL('../.tools/abigail-art/package.json', import.meta.url))('sharp');
const repo = fileURLToPath(new URL('../', import.meta.url));
for (const name of process.argv.slice(2)) {
  assert.match(name, /^[A-Za-z0-9_-]+$/);
  const dir = path.join(repo, 'artifacts/npc-modern/work', name);
  await fs.mkdir(dir, { recursive: true });
  const layoutPath = path.join(dir, 'layout.json');
  if (await fs.access(layoutPath).then(() => true, () => false)) { console.log(`${name}: already initialized`); continue; }
  for (const [category, output] of [['Characters', 'characters'], ['Portraits', 'portraits']])
    await fs.copyFile(path.join(repo, 'artifacts/npc-modern/originals', category, name + '.png'), path.join(dir, 'original-' + output + '.png'));
  const portrait = path.join(dir, 'original-portraits.png');
  const meta = await sharp(portrait).metadata();
  assert.ok([64, 128].includes(meta.width)); assert.equal(meta.height % 64, 0);
  const chunks = [];
  for (let top = 0, index = 0; top < meta.height; top += 192, index++) {
    const height = Math.min(192, meta.height - top);
    await sharp(portrait).extract({ left: 0, top, width: meta.width, height }).png().toFile(path.join(dir, `portrait-ref-${index}.png`));
    chunks.push({ file: `modern-portraits-${index}.png`, start: top / 64 * (meta.width / 64), rows: height / 64 });
  }
  await fs.writeFile(layoutPath, JSON.stringify({ flipSide: false, portraitChunks: chunks }, null, 2));
  console.log(`${name}: ${chunks.length} portrait chunks`);
}
