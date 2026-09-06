import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
const require = createRequire(new URL('../.tools/abigail-art/package.json', import.meta.url));
const sharp = require('sharp');
const names = process.argv.slice(2);
assert.ok(names.length, 'Specify character names to prepare.');
const allowed = ['Emily', 'Haley', 'Leah', 'Maru', 'Penny'];

async function read(file, key = false) {
  const image = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  if (key) for (let p = 0; p < image.data.length; p += 4) {
    const [r, g, b] = image.data.subarray(p, p + 3);
    if (r > 170 && b > 170 && g < 100 && Math.abs(r - b) < 60)
      image.data.fill(0, p, p + 4);
  }
  return image;
}

function rowBoundaries(source, rows) {
  const counts = [];
  for (let y = 0; y < source.info.height; y++) {
    let visible = 0;
    for (let x = 0; x < source.info.width; x++) if (source.data[(y * source.info.width + x) * 4 + 3]) visible++;
    counts.push(visible < source.info.width * 0.01 ? 0 : visible);
  }
  const bounds = [0], step = source.info.height / rows;
  for (let row = 1; row < rows; row++) {
    const expected = row * step;
    let best = Math.round(expected), score = Infinity;
    for (let y = Math.round(expected - step * 0.45); y <= Math.round(expected + step * 0.45); y++) {
      const candidate = counts[y] * source.info.height + Math.abs(y - expected);
      if (candidate < score) { best = y; score = candidate; }
    }
    bounds.push(best);
  }
  bounds.push(source.info.height);
  return bounds;
}

async function cell(source, cols, rows, index, width, height, flip = false, boundaries = null, cropFraction = 1) {
  const col = index % cols, row = Math.floor(index / cols);
  const left = Math.round(col * source.info.width / cols), top = boundaries ? boundaries[row] : Math.round(row * source.info.height / rows);
  const right = Math.round((col + 1) * source.info.width / cols), bottom = boundaries ? boundaries[row + 1] : Math.round((row + 1) * source.info.height / rows);
  const extracted = await sharp(source.data, { raw: source.info }).extract({ left, top, width: right - left, height: bottom - top }).png().toBuffer();
  let cropped = sharp(extracted).trim({ background: '#00000000', threshold: 1 });
  if (cropFraction < 1) {
    const trimmed = await cropped.png().toBuffer();
    const dimensions = await sharp(trimmed).metadata();
    cropped = sharp(trimmed).extract({ left: 0, top: 0, width: dimensions.width, height: Math.round(dimensions.height * cropFraction) });
  }
  if (flip) cropped = cropped.flop();
  const pixels = await cropped.resize(width, height, { fit: 'contain', position: 'bottom', background: '#00000000', kernel: 'lanczos3' }).ensureAlpha().raw().toBuffer();
  for (let p = 0; p < pixels.length; p += 4) {
    if (pixels[p + 3] < 160) pixels.fill(0, p, p + 4);
    else pixels[p + 3] = 255;
  }
  const visible = pixels.filter((v, i) => i % 4 === 3 && v === 255).length;
  assert.ok(visible > width * height * 0.12, `Empty or undersized frame ${index}`);
  assert.ok(visible < width * height * 0.95, `Background not removed in frame ${index}`);
  return sharp(pixels, { raw: { width, height, channels: 4 } }).png().toBuffer();
}

for (const name of names) {
  assert.ok(allowed.includes(name), 'Unknown character');
  const input = fileURLToPath(new URL(`../artifacts/bachelorettes-modern/${name}/`, import.meta.url));
  const output = fileURLToPath(new URL(`../src/AbigailModern/assets/${name}/`, import.meta.url));
  await fs.mkdir(output, { recursive: true });
  const originalPortraits = await read(input + 'original-portraits.png');
  const original = await read(input + 'original-characters.png');
  const portraits = await read(input + 'modern-portraits.png', true);
  const sprites = await read(input + 'modern-characters.png', true);
  const layout = JSON.parse(await fs.readFile(input + 'layout.json', 'utf8'));
  const rows = originalPortraits.info.height / 64;
  const portraitY = layout.portraitY ?? rowBoundaries(portraits, rows);
  const repair = layout.repairLastRow ? await read(input + 'portraits-last-row.png', true) : null;
  const portraitLayers = [];
  const blankCells = [];
  for (let i = 0; i < rows * 2; i++) {
    const left = (i % 2) * 64, top = Math.floor(i / 2) * 64;
    let occupied = false;
    for (let y = top; y < top + 64; y++) for (let x = left; x < left + 64; x++)
      if (originalPortraits.data[(y * 128 + x) * 4 + 3]) occupied = true;
    if (!occupied) { blankCells.push(i); continue; }
    const sourceIndex = layout.portraitFrameMap?.[i] ?? i;
    const useRepair = repair && (i >= (rows - 1) * 2 || Object.hasOwn(layout.repairFrames ?? {}, i));
    const replacement = useRepair
      ? await cell(repair, 2, 1, layout.repairFrames?.[i] ?? i % 2, 64, 64, false, null, layout.repairCropFraction ?? 1)
      : await cell(portraits, 2, rows, sourceIndex, 64, 64, false, portraitY);
    portraitLayers.push({ input: replacement, left, top });
  }
  await sharp({ create: { width: 128, height: rows * 64, channels: 4, background: '#00000000' } }).composite(portraitLayers).png().toFile(output + 'portraits.png');
  const sheet = Buffer.from(original.data);
  // Image review determines whether generated side-facing cells need flipping.
  for (let i = 0; i < 12; i++) {
    const frame = await sharp(await cell(sprites, 4, 3, i, 14, 28, layout.flipSide && i >= 4 && i < 8)).ensureAlpha().raw().toBuffer();
    const x = (i % 4) * 16, y = Math.floor(i / 4) * 32;
    for (let dy = 0; dy < 32; dy++) for (let dx = 0; dx < 16; dx++) {
      const p = ((y + dy) * 64 + x + dx) * 4;
      sheet.fill(0, p, p + 4);
      if (dx >= 1 && dx < 15 && dy >= 3 && dy < 31) frame.copy(sheet, p, ((dy - 3) * 14 + dx - 1) * 4, ((dy - 3) * 14 + dx - 1) * 4 + 4);
    }
  }
  assert.deepEqual(sheet.subarray(64 * 96 * 4), original.data.subarray(64 * 96 * 4));
  await sharp(sheet, { raw: original.info }).png().toFile(output + 'characters.png');
  const evidence = { name, portraitSize: [128, rows * 64], spriteSize: [64, original.info.height], portraitY, blankCells, specialFramesPreserved: true };
  for (const [file, w, h] of [['portraits.png', 128, rows * 64], ['characters.png', 64, original.info.height]]) {
    const { data, info } = await read(output + file);
    assert.equal(info.width, w); assert.equal(info.height, h);
    assert.ok(data.some((v, i) => i % 4 === 3 && v === 0));
    const checkedBytes = file === 'characters.png' ? 64 * 96 * 4 : data.length;
    for (let p = 0; p < checkedBytes; p += 4)
      assert.ok(!(data[p + 3] && data[p] > 220 && data[p + 2] > 220 && data[p + 1] < 60), `Magenta remains in ${name}/${file}`);
    await sharp(output + file).resize(w * 4, h * 4, { kernel: 'nearest' }).png().toFile(input + 'prepared-' + file);
  }
  await fs.writeFile(input + 'validation.json', JSON.stringify(evidence, null, 2));
  console.log(JSON.stringify(evidence));
}
await import('./complete-left-facing-art.mjs');
