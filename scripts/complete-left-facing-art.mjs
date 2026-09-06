import fs from 'node:fs/promises';
import path from 'node:path';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
import assert from 'node:assert/strict';
const sharp = createRequire(new URL('../.tools/abigail-art/package.json', import.meta.url))('sharp');
const repo = fileURLToPath(new URL('../', import.meta.url));
const registryPath = path.join(repo, 'src/AbigailModern/artwork.json');
const registry = JSON.parse(await fs.readFile(registryPath, 'utf8'));
const updated = [];
for (const entry of registry) {
  const area = entry.PatchArea;
  if (!entry.Name.startsWith('Characters/') || !area || area.X !== 0 || area.Y !== 0 || area.Width !== 64 || ![96, 128].includes(area.Height)) continue;
  const file = path.join(repo, 'src/AbigailModern', entry.File);
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  assert.equal(info.width, 64); assert.ok(info.height >= 128);
  const output = Buffer.from(data);
  // The game's AnimateLeft uses frames 12..15. Mirror each authored right-facing frame separately.
  for (let frame = 0; frame < 4; frame++) for (let y = 0; y < 32; y++) for (let x = 0; x < 16; x++) {
    const src = ((y + 32) * 64 + frame * 16 + (15 - x)) * 4;
    const dst = ((y + 96) * 64 + frame * 16 + x) * 4;
    data.copy(output, dst, src, src + 4);
  }
  assert.deepEqual(output.subarray(0, 64 * 96 * 4), data.subarray(0, 64 * 96 * 4));
  assert.deepEqual(output.subarray(64 * 128 * 4), data.subarray(64 * 128 * 4));
  await sharp(output, { raw: info }).png().toFile(file);
  entry.PatchArea.Height = 128;
  const name = entry.Name.split('/')[1];
  const dirs = [path.join(repo, 'artifacts/npc-modern/work', name), path.join(repo, 'artifacts/bachelorettes-modern', name), ...(name === 'Abigail' ? [path.join(repo, 'artifacts/abigail-modern')] : [])];
  for (const dir of dirs) {
    if (!await fs.access(dir).then(() => true, () => false)) continue;
    await sharp(output, { raw: info }).resize(info.width * 4, info.height * 4, { kernel: 'nearest' }).png().toFile(path.join(dir, 'prepared-characters.png'));
    const checkFile = path.join(dir, 'validation.json');
    const check = await fs.readFile(checkFile, 'utf8').then(JSON.parse).catch(() => ({}));
    Object.assign(check, { spriteUpdatedFrames: Array.from({ length: 16 }, (_, i) => i), leftFacingFrames: [12, 13, 14, 15], remainingSpriteRows: [128, info.height], specialFramesPreservedFromRow: 128, installationVerified: false });
    await fs.writeFile(checkFile, JSON.stringify(check, null, 2));
  }
  updated.push(entry.Name);
}
await fs.writeFile(registryPath, JSON.stringify(registry, null, 2));
await fs.writeFile(path.join(repo, 'artifacts/npc-modern/left-facing-validation.json'), JSON.stringify({ updated, frameRange: [12, 15], method: 'Each corresponding right-facing frame mirrored horizontally', originalOtherPixelsPreserved: true, gameEvidence: 'StardewValley/AnimatedSprite.cs AnimateLeft selects framesPerAnimation * 3 through * 4' }, null, 2));
console.log(`Completed left-facing movement for ${updated.length} sprite sheets; all other pixels preserved.`);
