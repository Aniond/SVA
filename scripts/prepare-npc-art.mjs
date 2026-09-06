import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
const require = createRequire(new URL('../.tools/abigail-art/package.json', import.meta.url));
const sharp = require('sharp');
const repo = fileURLToPath(new URL('../', import.meta.url));
const names = process.argv.slice(2);
assert.ok(names.length, 'Specify NPC asset names.');

async function read(file, removeKey = false) {
  const result = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  if (removeKey) for (let p = 0; p < result.data.length; p += 4) {
    const [r, g, b] = result.data.subarray(p, p + 3);
    if (r > 170 && b > 170 && g < 100 && Math.abs(r - b) < 60) result.data.fill(0, p, p + 4);
  }
  return result;
}

function boundaries(image, rows) {
  const counts = [];
  for (let y = 0; y < image.info.height; y++) {
    let count = 0;
    for (let x = 0; x < image.info.width; x++) if (image.data[(y * image.info.width + x) * 4 + 3]) count++;
    counts.push(count < image.info.width * 0.01 ? 0 : count);
  }
  const edges = [0], step = image.info.height / rows;
  for (let row = 1; row < rows; row++) {
    const expected = row * step;
    let best = Math.round(expected), score = Infinity;
    for (let y = Math.round(expected - step * 0.4); y <= Math.round(expected + step * 0.4); y++) {
      const next = counts[y] * image.info.height + Math.abs(y - expected);
      if (next < score) { best = y; score = next; }
    }
    edges.push(best);
  }
  return [...edges, image.info.height];
}

async function cell(image, columns, rows, index, width, height, flip = false, edges = null, columnEdges = null) {
  const col = index % columns, row = Math.floor(index / columns);
  const left = columnEdges?.[col] ?? Math.round(col * image.info.width / columns), right = columnEdges?.[col + 1] ?? Math.round((col + 1) * image.info.width / columns);
  const top = edges?.[row] ?? Math.round(row * image.info.height / rows), bottom = edges?.[row + 1] ?? Math.round((row + 1) * image.info.height / rows);
  const extracted = await sharp(image.data, { raw: image.info }).extract({ left, top, width: right - left, height: bottom - top }).png().toBuffer();
  let resized = sharp(extracted).trim({ background: '#00000000', threshold: 1 });
  if (flip) resized = resized.flop();
  const pixels = await resized.resize(width, height, { fit: 'contain', position: 'bottom', background: '#00000000', kernel: 'lanczos3' }).ensureAlpha().raw().toBuffer();
  let occupied = 0;
  for (let p = 0; p < pixels.length; p += 4) {
    if (pixels[p + 3] < 160) pixels.fill(0, p, p + 4);
    else { pixels[p + 3] = 255; occupied++; }
  }
  assert.ok(occupied > width * height * 0.12 && occupied < width * height * 0.97, `Invalid foreground occupancy in frame ${index}: ${occupied}/${width * height}; source region ${left},${top},${right-left},${bottom-top}`);
  return sharp(pixels, { raw: { width, height, channels: 4 } }).png().toBuffer();
}

const registryPath = path.join(repo, 'src/AbigailModern/artwork.json');
let registry = JSON.parse(await fs.readFile(registryPath, 'utf8'));
for (const name of names) {
  assert.match(name, /^[A-Za-z0-9_-]+$/);
  const input = path.join(repo, 'artifacts/npc-modern/work', name);
  const output = path.join(repo, 'src/AbigailModern/assets', name);
  await fs.mkdir(output, { recursive: true });
  const layout = JSON.parse(await fs.readFile(path.join(input, 'layout.json'), 'utf8'));
  const frameHeight = layout.frameHeight ?? 32;
  const frameWidth = layout.frameWidth ?? 16;
  assert.ok([16,32].includes(frameWidth), 'Unsupported frame width.');
  assert.ok([24,32].includes(frameHeight), 'Unsupported frame height.');
  const spriteHeight = layout.spriteHeight ?? frameHeight - 4;
  assert.ok(Number.isInteger(spriteHeight) && spriteHeight >= 16 && spriteHeight <= frameHeight - 4, 'Invalid sprite height.');
  const original = await read(path.join(input, 'original-characters.png'));
  const isStatic = layout.spriteMode === 'static';
  const isNative = layout.spriteMode === 'native';
  const spriteColumns = isStatic ? 1 : layout.spriteColumns ?? 4;
  assert.ok([1,2,4].includes(spriteColumns) && (isNative || spriteColumns === (isStatic ? 1 : 4)), 'Invalid sprite columns.');
  if (frameWidth !== 16) assert.ok(isNative, 'Wide frames require native pose mapping.');
  if (isNative) assert.equal(original.info.height % frameHeight, 0, 'Native pose sheet must contain complete rows.');
  if (frameHeight !== 32) assert.ok(isNative, 'Short frames require native pose mapping.');
  const sourceRows = isStatic ? 1 : isNative ? original.info.height / frameHeight : 3;
  assert.equal(original.info.width, frameWidth * spriteColumns, 'Nonstandard sprite width needs explicit atlas support.');
  assert.ok(isStatic ? original.info.height === 32 : isNative ? sourceRows >= 1 : original.info.height >= 128, 'Nonstandard sprite height needs explicit atlas support.');
  const originalPortraits = layout.newPortrait ? null : await read(path.join(input, 'original-portraits.png'));
  const portraitHeight = originalPortraits?.info.height ?? 192;
  const portraitWidth = originalPortraits?.info.width ?? 128;
  assert.ok([64, 128].includes(portraitWidth));
  const portraitColumns = portraitWidth / 64;
  const chunks = [];
  for (const chunk of layout.portraitChunks) {
    const image = await read(path.join(input, chunk.file), true);
    chunks.push({ ...chunk, image, edges: chunk.edges ?? boundaries(image, chunk.rows) });
  }
  const layers = [], blankFrames = [], updatedPortraitFrames = [];
  for (let i = 0; i < portraitHeight / 64 * portraitColumns; i++) {
    const left = i % portraitColumns * 64, top = Math.floor(i / portraitColumns) * 64;
    let occupied = !originalPortraits, uniform = !!originalPortraits;
    if (originalPortraits) {
      const first = originalPortraits.data.subarray((top * portraitWidth + left) * 4, (top * portraitWidth + left) * 4 + 4);
      for (let y = top; y < top + 64; y++) for (let x = left; x < left + 64; x++) {
        const p = (y * portraitWidth + x) * 4;
        if (originalPortraits.data[p + 3]) occupied = true;
        if (!originalPortraits.data.subarray(p, p + 4).equals(first)) uniform = false;
      }
    }
    if (!occupied || uniform) {
      blankFrames.push(i);
      layers.push({ input: await sharp(originalPortraits.data, { raw: originalPortraits.info }).extract({ left, top, width: 64, height: 64 }).png().toBuffer(), left, top });
      continue;
    }
    const matches = chunks.filter(chunk => i >= chunk.start && i < chunk.start + chunk.rows * portraitColumns);
    assert.equal(matches.length, 1, `Portrait ${name}/${i} must be covered exactly once.`);
    const chunk = matches[0];
    const sourceIndex = chunk.order?.[i - chunk.start] ?? i - chunk.start;
    assert.ok(Number.isInteger(sourceIndex) && sourceIndex >= 0 && sourceIndex < chunk.rows * portraitColumns, `Invalid portrait source index for ${name}/${i}.`);
    layers.push({ input: await cell(chunk.image, portraitColumns, chunk.rows, sourceIndex, 64, 64, false, chunk.edges), left, top });
    updatedPortraitFrames.push(i);
  }
  await sharp({ create: { width: portraitWidth, height: portraitHeight, channels: 4, background: '#00000000' } }).composite(layers).png().toFile(path.join(output, 'portraits.png'));
  const sprites = await read(path.join(input, 'modern-characters.png'), true);
  const spriteEdges = layout.spriteEdges ?? boundaries(sprites, sourceRows);
  const sheet = Buffer.from(original.data);
  const frameCount = isStatic ? 1 : isNative ? sourceRows * spriteColumns : 16;
  const updatedHeight = isNative ? original.info.height : isStatic ? 32 : 128;
  const updatedSpriteFrames = [], unusedSpriteFrames = [];
  for (let i = 0; i < frameCount; i++) {
    if (layout.preserveUnusedSpriteCells) {
      const colors = new Set();
      for (let y = 0; y < frameHeight; y++) for (let x = 0; x < frameWidth; x++) {
        const p = ((Math.floor(i / spriteColumns) * frameHeight + y) * original.info.width + (i % spriteColumns) * frameWidth + x) * 4;
        colors.add(original.data.readUInt32LE(p));
      }
      if (colors.size <= 2) { unusedSpriteFrames.push(i); continue; }
    }
    updatedSpriteFrames.push(i);
    const logicalIndex = !isNative && i >= 12 ? i - 8 : i;
    const sourceIndex = layout.spriteOrder?.[logicalIndex] ?? logicalIndex;
    assert.ok(Number.isInteger(sourceIndex) && sourceIndex >= 0 && sourceIndex < (isStatic ? 1 : sourceRows * spriteColumns), 'Invalid sprite source cell.');
    const flip = isNative ? false : i >= 12 ? !layout.flipSide : layout.flipSide && i >= 4 && i < 8;
    const innerWidth = frameWidth - 2;
    const poseHeight = layout.spriteHeights?.[i] ?? spriteHeight;
    assert.ok(Number.isInteger(poseHeight) && poseHeight >= 1 && poseHeight <= frameHeight - 1, 'Invalid pose height.');
    const spriteTop = frameHeight - 1 - poseHeight;
    const frame = await sharp(await cell(sprites, spriteColumns, sourceRows, sourceIndex, innerWidth, poseHeight, flip, spriteEdges, layout.spriteColumnEdges)).ensureAlpha().raw().toBuffer();
    const x = i % spriteColumns * frameWidth, y = Math.floor(i / spriteColumns) * frameHeight;
    for (let dy = 0; dy < frameHeight; dy++) for (let dx = 0; dx < frameWidth; dx++) {
      const p = ((y + dy) * original.info.width + x + dx) * 4;
      sheet.fill(0, p, p + 4);
      if (dx >= 1 && dx < frameWidth - 1 && dy >= spriteTop && dy < frameHeight - 1) frame.copy(sheet, p, ((dy - spriteTop) * innerWidth + dx - 1) * 4, ((dy - spriteTop) * innerWidth + dx - 1) * 4 + 4);
    }
  }
  assert.deepEqual(sheet.subarray(original.info.width * updatedHeight * 4), original.data.subarray(original.info.width * updatedHeight * 4));
  await sharp(sheet, { raw: original.info }).png().toFile(path.join(output, 'characters.png'));
  for (const [file, width, height, checkedHeight] of [['portraits.png', portraitWidth, portraitHeight, portraitHeight], ['characters.png', original.info.width, original.info.height, updatedHeight]]) {
    const image = await read(path.join(output, file));
    assert.equal(image.info.width, width); assert.equal(image.info.height, height);
    assert.ok(image.data.some((v, i) => i % 4 === 3 && v === 0));
    for (let p = 0; p < width * checkedHeight * 4; p += 4)
      assert.ok(!(image.data[p + 3] && image.data[p] > 220 && image.data[p + 2] > 220 && image.data[p + 1] < 60), `Magenta remains in ${name}/${file}.`);
    await sharp(path.join(output, file)).resize(width * 4, height * 4, { kernel: 'nearest' }).png().toFile(path.join(input, 'prepared-' + file));
  }
  const evidence = { name, newPortrait: !!layout.newPortrait, updatedPortraitFrames, blankFrames, spriteUpdatedFrames: updatedSpriteFrames, unusedSpriteFrames, remainingSpriteRows: original.info.height === updatedHeight ? [] : [128, original.info.height], baseSpriteComplete: original.info.height === updatedHeight, specialFramesPreserved: true, portraitChunkEdges: chunks.map(chunk => ({ file: chunk.file, edges: chunk.edges })), installationVerified: false, dialogueDisplayVerified: false };
  await fs.writeFile(path.join(input, 'validation.json'), JSON.stringify(evidence, null, 2));
  registry = registry.filter(asset => ![`Portraits/${name}`, `Characters/${name}`].includes(asset.Name));
  registry.push({ Name: `Portraits/${name}`, File: `assets/${name}/portraits.png`, Width: portraitWidth, Height: portraitHeight });
  registry.push({ Name: `Characters/${name}`, File: `assets/${name}/characters.png`, Width: original.info.width, Height: original.info.height, ...(original.info.height === updatedHeight ? {} : { PatchArea: { X: 0, Y: 0, Width: 64, Height: 128 } }) });
  console.log(JSON.stringify(evidence));
}
await fs.writeFile(registryPath, JSON.stringify(registry, null, 2));
await import('./complete-left-facing-art.mjs');
execFileSync(process.execPath, [path.join(repo, 'scripts/prepare-npc-special-art.mjs'), '--available'], { stdio: 'inherit' });
