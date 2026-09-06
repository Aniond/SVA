import fs from 'node:fs/promises';
import path from 'node:path';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
import assert from 'node:assert/strict';
const sharp = createRequire(new URL('../.tools/abigail-art/package.json', import.meta.url))('sharp');
const repo = fileURLToPath(new URL('../', import.meta.url));
const registryPath = path.join(repo, 'src/AbigailModern/artwork.json');
const registry = JSON.parse(await fs.readFile(registryPath, 'utf8'));
async function readKeyed(file) {
  const image = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  for (let p = 0; p < image.data.length; p += 4) {
    const [r, g, b] = image.data.subarray(p, p + 3);
    if (r > 170 && b > 170 && g < 100 && Math.abs(r - b) < 60) image.data.fill(0, p, p + 4);
  }
  return image;
}
const requested = process.argv.slice(2);
const names = requested.includes('--available') ? await fs.readdir(path.join(repo, 'artifacts/npc-modern/work')) : requested;
for (const name of names) {
  if (requested.includes('--available') && !await fs.access(path.join(repo, 'artifacts/npc-modern/work', name, 'modern-special.png')).then(() => true, () => false)) continue;
  assert.match(name, /^[A-Za-z0-9_-]+$/);
  const dir = path.join(repo, 'artifacts/npc-modern/work', name);
  const layout = JSON.parse(await fs.readFile(path.join(dir, 'layout.json'), 'utf8'));
  const spriteHeight = layout.spriteHeight ?? 28;
  assert.ok(Number.isInteger(spriteHeight) && spriteHeight >= 16 && spriteHeight <= 28, 'Invalid sprite height.');
  const alphaThreshold = layout.specialAlphaThreshold ?? 160;
  assert.ok(Number.isInteger(alphaThreshold) && alphaThreshold >= 32 && alphaThreshold <= 224, 'Invalid special alpha threshold.');
  const entry = registry.find(e => e.Name === `Characters/${name}`);
  assert.ok(entry && entry.Width === 64 && entry.Height >= 160 && entry.Height <= 224 && entry.Height % 32 === 0, 'This special-frame layout supports 1-3 rows after the 16 movement frames.');
  const rows = (entry.Height - 128) / 32;
  const original = await sharp(path.join(dir, 'original-characters.png')).ensureAlpha().raw().toBuffer();
  const file = path.join(repo, 'src/AbigailModern', entry.File);
  const pixels = await sharp(file).ensureAlpha().raw().toBuffer();
  const before = Buffer.from(pixels);
  const image = await readKeyed(path.join(dir, 'modern-special.png'));
  const overrides = await fs.readFile(path.join(dir, 'special-overrides.json'), 'utf8').then(JSON.parse).catch(() => ({}));
  const updated = [], blanks = [];
  for (let index = 0; index < rows * 4; index++) {
    const col = index % 4, row = Math.floor(index / 4);
    const wide = layout.specialWideRows?.includes(row) ?? false;
    if (wide && col % 2) continue;
    const frameWidth = wide ? 32 : 16;
    const colors = new Set();
    for (let y = 0; y < 32; y++) for (let x = 0; x < frameWidth; x++) {
      const p = ((row * 32 + 128 + y) * 64 + col * 16 + x) * 4;
      colors.add(original.readUInt32LE(p));
    }
    if (colors.size <= 2) { blanks.push(index + 16); if (wide) blanks.push(index + 17); continue; }
    const override = overrides[index + 16];
    const source = override ? await readKeyed(path.join(dir, override)) : image;
    const left = override ? 0 : Math.round(col * image.info.width / 4), right = override ? source.info.width : Math.round((col + (wide ? 2 : 1)) * image.info.width / 4);
    const top = override ? 0 : layout.specialEdges?.[row] ?? Math.round(row * image.info.height / rows), bottom = override ? source.info.height : layout.specialEdges?.[row + 1] ?? Math.round((row + 1) * image.info.height / rows);
    assert.ok(Number.isInteger(top) && Number.isInteger(bottom) && top >= 0 && bottom > top && bottom <= source.info.height, 'Invalid special-frame row boundaries.');
    const extracted = await sharp(source.data, { raw: source.info }).extract({ left, top, width: right - left, height: bottom - top }).png().toBuffer();
    const innerWidth = frameWidth - 2;
    const inner = await sharp(extracted).trim({ background: '#00000000', threshold: 1 }).resize(innerWidth, spriteHeight, { fit: 'contain', position: 'bottom', background: '#00000000', kernel: 'lanczos3' }).ensureAlpha().raw().toBuffer();
    const frame = Buffer.alloc(frameWidth * 32 * 4);
    for (let y = 0; y < spriteHeight; y++) inner.copy(frame, ((y + 31 - spriteHeight) * frameWidth + 1) * 4, y * innerWidth * 4, (y + 1) * innerWidth * 4);
    let occupied = 0;
    for (let p = 0; p < frame.length; p += 4) {
      if (frame[p + 3] < alphaThreshold) frame.fill(0, p, p + 4);
      else { frame[p + 3] = 255; occupied++; }
    }
    assert.ok(occupied > 50 && occupied < (wide ? 900 : 500), `Bad special-frame occupancy: ${name}/${index + 16}`);
    for (let y = 0; y < 32; y++) for (let x = 0; x < frameWidth; x++) {
      const src = (y * frameWidth + x) * 4, dst = ((row * 32 + 128 + y) * 64 + col * 16 + x) * 4;
      frame.copy(pixels, dst, src, src + 4);
    }
    updated.push(index + 16);
    if (wide) updated.push(index + 17);
  }
  assert.deepEqual(pixels.subarray(0, 128 * 64 * 4), before.subarray(0, 128 * 64 * 4), 'Walking frames changed during special preparation.');
  for (const index of blanks) for (let y = 0; y < 32; y++) {
    const start = ((Math.floor(index / 4) * 32 + y) * 64 + index % 4 * 16) * 4;
    assert.deepEqual(pixels.subarray(start, start + 64), original.subarray(start, start + 64), 'Unused source cell changed.');
  }
  await sharp(pixels, { raw: { width: 64, height: entry.Height, channels: 4 } }).png().toFile(file);
  await sharp(file).resize(256, entry.Height * 4, { kernel: 'nearest' }).png().toFile(path.join(dir, 'prepared-characters.png'));
  delete entry.PatchArea;
  const evidence = JSON.parse(await fs.readFile(path.join(dir, 'validation.json'), 'utf8'));
  Object.assign(evidence, { spriteUpdatedFrames: [...Array.from({ length: 16 }, (_, i) => i), ...updated], unusedSpriteFrames: blanks, remainingSpriteRows: [], baseSpriteComplete: true, specialFramesPreserved: false, installationVerified: false });
  await fs.writeFile(path.join(dir, 'validation.json'), JSON.stringify(evidence, null, 2));
  console.log(`${name}: all ${16 + updated.length} occupied base sprite cells modernized; ${blanks.length} unused cells preserved.`);
}
await fs.writeFile(registryPath, JSON.stringify(registry, null, 2));
