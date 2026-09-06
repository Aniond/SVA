import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import { createRequire } from 'node:module';
const sharp = createRequire('C:/Users/david/SDV/.tools/abigail-art/package.json')('sharp');
const work = 'artifacts/npc-modern/work/QiPlanePilot/';
const nativeFile = 'artifacts/npc-modern/originals/LooseSprites/Cursors_1_6.png';
const native = await sharp(nativeFile).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
const box = { left: 138, top: 207, width: 17, height: 15 };
const original = await sharp(nativeFile).extract(box).ensureAlpha().raw().toBuffer();
const art = await sharp(work + 'pilot-source.png').resize(17, 15, { fit: 'fill' }).ensureAlpha().raw().toBuffer();
const patch = Buffer.from(original), allowed = Buffer.alloc(original.length), changes = Buffer.alloc(original.length);
// The upper twelve rows contain only the pilot silhouette. At the cockpit junction,
// preserve the native aircraft rim and select only the blue head/neck interior.
// The bottom row is the native black cockpit rim and is entirely preserved.
const cockpitInterior = { 12: [6, 12], 13: [6, 13] };
let bodyPixels = 0, changedPixels = 0, neutralSourceFallbackPixels = 0;
for (let y = 0; y < 15; y++) for (let x = 0; x < 17; x++) {
  const p = (y * 17 + x) * 4;
  const span = cockpitInterior[y];
  if (!original[p + 3] || !(y < 12 || (span && x >= span[0] && x <= span[1]))) continue;
  allowed.fill(255, p, p + 4); bodyPixels++;
  const rgb = [...art.subarray(p, p + 3)];
  // Imagegen may move its matte edge inward slightly. Keep the native dark outline
  // at neutral-gray sampling points rather than importing the source backdrop.
  const neutral = Math.max(...rgb) - Math.min(...rgb) < 25 && Math.min(...rgb) > 70;
  if (neutral) { neutralSourceFallbackPixels++; continue; }
  art.copy(patch, p, p, p + 3);
  if (!patch.subarray(p, p + 4).equals(original.subarray(p, p + 4))) { changes.fill(255, p, p + 4); changedPixels++; }
}
const result = Buffer.from(native.data), atlasMask = Buffer.alloc(native.data.length);
for (let y = 0; y < 15; y++) {
  patch.copy(result, ((207 + y) * native.info.width + 138) * 4, y * 17 * 4, (y + 1) * 17 * 4);
  allowed.copy(atlasMask, ((207 + y) * native.info.width + 138) * 4, y * 17 * 4, (y + 1) * 17 * 4);
}
let outsideMaskPreserved = 0;
for (let p = 0; p < result.length; p += 4) {
  assert.equal(result[p + 3], native.data[p + 3]);
  if (!atlasMask[p + 3]) { assert(result.subarray(p, p + 4).equals(native.data.subarray(p, p + 4))); outsideMaskPreserved++; }
}
const propellerRects = [192, 196, 200].map(left => ({ left, top: 204, width: 4, height: 44 }));
for (const r of propellerRects) for (let y = r.top; y < r.top + r.height; y++) for (let x = r.left; x < r.left + r.width; x++) {
  const p = (y * native.info.width + x) * 4;
  assert(result.subarray(p, p + 4).equals(native.data.subarray(p, p + 4)));
}
for (const [name, data] of [['pilot-patch', patch], ['pilot-body-mask', allowed], ['pilot-changed-pixels-mask', changes]])
  await sharp(data, { raw: { width: 17, height: 15, channels: 4 } }).png().toFile(work + name + '.png');
const overlay = Buffer.alloc(patch.length);
for (let p = 0; p < patch.length; p += 4) if (allowed[p + 3]) patch.copy(overlay, p, p, p + 4);
await sharp(overlay, { raw: { width: 17, height: 15, channels: 4 } }).png().toFile(work + 'pilot-overlay.png');
const context = { left: 113, top: 204, width: 91, height: 44 };
await sharp(result, { raw: native.info }).extract(context).resize(1092, 528, { kernel: 'nearest' }).flatten({ background: '#31323e' }).toFile(work + 'prepared-plane-preview.png');
await sharp(patch, { raw: { width: 17, height: 15, channels: 4 } }).resize(408, 360, { kernel: 'nearest' }).flatten({ background: '#dcd9c9' }).toFile(work + 'prepared-pilot-preview.png');
const before = await sharp(native.data, { raw: native.info }).extract(context).png().toBuffer();
const after = await sharp(result, { raw: native.info }).extract(context).png().toBuffer();
const comparison = await sharp({ create: { width: 91, height: 96, channels: 4, background: '#31323e' } }).composite([{ input: before, left: 0, top: 0 }, { input: after, left: 0, top: 52 }]).png().toBuffer();
await sharp(comparison).resize(910, 960, { kernel: 'nearest' }).png().toFile(work + 'native-modern-plane-preview.png');
await sharp(comparison).resize(364, 384, { kernel: 'nearest' }).png().toFile(work + 'native-modern-game-scale-preview.png');
const metadata = {
  asset: 'LooseSprites/Cursors_1_6', nativeDrawRectangle: [113, 204, 79, 43], patchRectangle: [138, 207, 17, 15],
  patch: 'pilot-patch.png', bodyMask: 'pilot-body-mask.png', overlay: 'pilot-overlay.png',
  instruction: 'Merge only body-mask pixels into the current composite; do not overwrite the full shared atlas.',
  bodyMaskPixels: bodyPixels, changedPixels, outsideMaskPreserved, totalAtlasPixelsPreserved: native.info.width * native.info.height - changedPixels, neutralSourceFallbackPixels,
  allNativeAlphaExact: true, aircraftAndCockpitRimExact: true, threePropellerPhasesExact: true,
  allOtherAtlasPixelsExact: true, generatedSourceId: 'd2484368-bd9b-4189-97dc-f00b166192d4',
  patchSha256: crypto.createHash('sha256').update(await fs.readFile(work + 'pilot-patch.png')).digest('hex'),
  runtimeVerified: false, fullSceneVerified: false, installed: false
};
await fs.writeFile(work + 'static-qa.json', JSON.stringify(metadata, null, 2) + '\n');
console.log(metadata);
