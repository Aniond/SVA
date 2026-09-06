import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
const repo = fileURLToPath(new URL('../', import.meta.url));
const root = path.join(repo, 'artifacts/npc-modern');
const inventory = JSON.parse(await fs.readFile(path.join(root, 'source-inventory.json'), 'utf8'));
const roster = JSON.parse(await fs.readFile(path.join(root, 'roster.json'), 'utf8'));
const data = JSON.parse(await fs.readFile(path.join(root, 'characters.json'), 'utf8'));
const registry = JSON.parse(await fs.readFile(path.join(repo, 'src/AbigailModern/artwork.json'), 'utf8'));
const currentEvidence = await fs.readFile(path.join(root, 'evidence/registered-loader-check.txt'), 'utf8').catch(() => '');
const marinerCheck = await fs.readFile(path.join(root, 'evidence/mariner-interaction-checks.json'), 'utf8').then(JSON.parse).catch(() => ({}));
async function checkLoader(entry, installedMatches) {
  if (!entry || !installedMatches) return false;
  const bytes = await fs.readFile(path.join(repo, 'src/AbigailModern', entry.File));
  const hash = crypto.createHash('sha256').update(bytes).digest('hex').toUpperCase();
  return currentEvidence.split('\n').some(line => line.includes(`Verified ${entry.Name}: ${entry.Width}x${entry.Height} pixels,`) && line.includes(`SHA256 ${hash}`));
}
async function checkInstalled(entry) {
  if (!entry) return false;
  try {
    const [prepared, installed] = await Promise.all([
      fs.readFile(path.join(repo, 'src/AbigailModern', entry.File)),
      fs.readFile(path.join(inventory.gamePath, 'Mods/AbigailModern', entry.File))
    ]);
    return crypto.createHash('sha256').update(prepared).digest('hex') === crypto.createHash('sha256').update(installed).digest('hex');
  } catch { return false; }
}
const additional = [
  { Name: 'Lost Items crow merchant', TextureName: 'Crow', source: 'Woods LostItemsShop; 32 paired tile frames and silent LostItems shop; crow-lost-items-checks.json' },
  { Name: 'Queen of Sauce', TextureName: 'QueenOfSauce', source: 'TV channel 5; Cursors (602,361,84,28) and native opening/recipe dialogue with new portrait' },
  { Name: "Livin' Off the Land host", TextureName: 'LivinOffTheLand', source: 'TV channel 4; Cursors (517,361,84,28) and native introduction/tip portrait route' },
  { Name: 'Weather presenter', TextureName: 'WeatherPresenter', source: 'TV channel 2; five shared-atlas presenter frames; shared-npc-reconciliation.md' },
  { Name: 'MarILDA', TextureName: 'robot', source: 'Data/Events/ScienceHouse event 10; Characters/robot, Cursors flight frames and new Portraits/robot; robot-art-checks.json and robot-portrait-checks.json' },
  { Name: 'Fizz', TextureName: 'Fizz', source: 'GameLocation.cs:1086' },
  { Name: 'Professor Snail', TextureName: 'SafariGuy', source: 'IslandFieldOffice.cs:365' },
  { Name: 'Gourmand Frog', TextureName: 'Gourmand', source: 'IslandFarmCave.cs:288; uses plain dialogue and a placeholder SafariGuy portrait internally' },
  { Name: 'Bookseller', TextureName: 'Marcello', source: 'Event.cs:10062; bookseller-interaction-checks.json' },
  { Name: 'Kel (female)', TextureName: 'LeahExFemale', source: 'asset inventory; kel-checks.json' },
  { Name: 'Kel (male)', TextureName: 'LeahExMale', source: 'asset inventory; kel-checks.json' },
  { Name: 'Mr. Raccoon', TextureName: 'raccoon', source: 'NPC subclass Raccoon; raccoon-checks.json' },
  { Name: 'Mrs. Raccoon', TextureName: 'mrs_raccoon', source: 'asset inventory; raccoon-shop-checks.json' },
  { Name: 'Trash Bear', TextureName: 'TrashBear', source: 'NPC subclass TrashBear' },
  { Name: 'Junimos', TextureName: 'Junimo', source: 'NPC subclasses Junimo and JunimoHarvester' },
  { Name: 'Island Parrots', TextureName: 'IslandParrot', source: 'asset inventory; island-parrot-checks.json' },
  { Name: 'Player children', TextureName: 'Baby', otherTextures: ['Baby_dark', 'Toddler', 'Toddler_dark', 'Toddler_girl', 'Toddler_girl_dark'], source: 'NPC subclass Child' },
  { Name: 'Fishing contestants', TextureName: 'Assorted_Fishermen', sharedSheet: true, source: 'Beach.cs: assorted winter_derby_contestent actors; fishing-winter-checks.json and fishing-summer-checks.json' },
  { Name: 'Traveling Cart Merchant', TextureName: null, source: 'embedded sprite/shop paths; dedicated merchant runtime evidence' },
  { Name: 'Hat Mouse', TextureName: null, source: 'embedded sprite/shop paths; dedicated merchant runtime evidence' },
  { Name: 'Desert Trader', TextureName: null, source: 'Desert.cs:OnDesertTrader; embedded sprite/shop paths; dedicated merchant runtime evidence' },
  { Name: 'Island Trader', TextureName: null, source: 'IslandNorth.cs:ApplyIslandTraderHut; embedded sprite/shop paths; dedicated merchant runtime evidence' },
  { Name: 'Mermaid', TextureName: null, source: 'MermaidHouse location; mermaid-checks.json and mermaid-rising-checks.json' }
];
const actors = [...roster.map(actor => ({ ...actor, source: 'live Data/Characters export', appearances: data[actor.Name]?.Appearance ?? [] })), ...additional];
const assets = [];
const nonCharacterAssets = JSON.parse(await fs.readFile(path.join(repo, 'docs/npc-art/non-character-assets.json'), 'utf8'));
for (const asset of inventory.assets) {
  const registered = registry.find(entry => entry.Name === asset.asset);
  const owners = actors.filter(actor => actor.TextureName && (asset.asset.endsWith('/' + actor.TextureName) || asset.asset.split('/')[1].startsWith(actor.TextureName + '_') || (actor.otherTextures ?? []).includes(asset.asset.split('/')[1]))).map(actor => actor.Name);
  let installedMatches = false;
  if (registered) {
    try {
      const prepared = await fs.readFile(path.join(repo, 'src/AbigailModern', registered.File));
      const installed = await fs.readFile(path.join(inventory.gamePath, 'Mods/AbigailModern', registered.File));
      installedMatches = crypto.createHash('sha256').update(prepared).digest('hex') === crypto.createHash('sha256').update(installed).digest('hex');
    } catch {}
  }
  const basename = asset.asset.split('/')[1];
  const loaderVerified = await checkLoader(registered, installedMatches);
  assets.push({ ...asset, owners, preparedFile: registered?.File ?? null, updatedArea: registered && !registered.PatchAreas ? (registered.PatchArea ?? { X: 0, Y: 0, Width: registered.Width, Height: registered.Height }) : null, updatedAreas: registered?.PatchAreas ?? null, coverage: !registered ? 'pending' : registered.PatchArea || registered.PatchAreas ? 'partial' : 'full-sheet-prepared', installedMatches, loaderVerified });
}
const covered = new Set(assets.map(asset => asset.asset));
for (const registered of registry.filter(entry => !covered.has(entry.Name))) {
  const installedMatches = await checkInstalled(registered);
  assets.push({ asset: registered.Name, width: registered.Width, height: registered.Height, owners: actors.filter(actor => actor.TextureName && (registered.Name.endsWith('/' + actor.TextureName) || registered.Name.split('/')[1].startsWith(actor.TextureName + '_') || (actor.otherTextures ?? []).includes(registered.Name.split('/')[1]))).map(actor => actor.Name), preparedFile: registered.File, coverage: 'new-asset-prepared', installedMatches, loaderVerified: await checkLoader(registered, installedMatches), dialogueDisplayVerified: registered.Name === 'Portraits/Mariner' && marinerCheck.Passed === true, dialogueEvidence: registered.Name === 'Portraits/Mariner' ? 'artifacts/npc-modern/evidence/mariner-interaction-checks.json' : null });
}
for (const asset of assets) {
  const classification = nonCharacterAssets[asset.asset];
  if (classification) {
    asset.npcScope = 'non-character';
    asset.scopeReason = classification.reason;
    asset.scopeEvidence = classification.evidence;
  }
}
const manifest = JSON.parse(await fs.readFile(path.join(repo, 'src/AbigailModern/manifest.json'), 'utf8'));
const installedEvidence = await fs.readFile(path.join(root, `evidence/installed-${manifest.Version}.json`), 'utf8').then(JSON.parse).catch(() => ({}));
const crowEvidence = await fs.readFile(path.join(root, 'evidence/crow-lost-items-checks.json'), 'utf8').then(JSON.parse).catch(() => ({}));
const artworkInstallationComplete = installedEvidence.Passed === true && installedEvidence.RegisteredTextures === registry.length
  && assets.filter(asset => asset.preparedFile).every(asset => asset.installedMatches && asset.loaderVerified)
  && crowEvidence.Passed === true;
const ledger = {
  nonCharacterAssets,
  objective: 'Update every existing NPC sprite and portrait; create needed missing portraits and emotions.',
  updatedAt: new Date().toISOString(),
  gameVersion: '1.6.15', smapiVersion: '4.5.2',
  sourceEvidence: ['artifacts/npc-modern/roster.json', 'artifacts/npc-modern/characters.json', 'artifacts/npc-modern/source-inventory.json', 'artifacts/npc-modern/game-source'],
  scopeClarification: 'People and talking creatures are being completed. Optional question about pets, farm animals and ordinary monsters is pending; do not assume a submitted answer.',
  completionProven: artworkInstallationComplete,
  completionScope: 'Established people and talking-creature artwork and installation scope, reconciled in final-installation-closure.md. This does not claim exhaustive gameplay or quest implementation.',
  actors,
  assets,
  unresolved: artworkInstallationComplete ? [] : ['Current installed artwork verification is incomplete.'],
  reconciliationReports: ['docs/npc-art/final-coverage-reconciliation.md', 'docs/npc-art/portrait-quality-reconciliation.md', 'docs/npc-art/progress.md', 'docs/npc-art/portrait-cleanup-closure.md', 'docs/npc-art/final-installation-closure.md'],
  verificationLimit: 'Targeted native animation, dialogue, shop and draw fixtures plus installed file verification; full scene-by-scene gameplay has not been performed.'
};
await fs.mkdir(path.join(repo, 'docs/npc-art'), { recursive: true });
await fs.writeFile(path.join(repo, 'docs/npc-art/coverage.json'), JSON.stringify(ledger, null, 2));
console.log(`${actors.length} roster entries; ${assets.length} asset records; ${assets.filter(asset => asset.preparedFile).length} registered; ${assets.filter(asset => asset.loaderVerified).length} verified installed assets. Artwork installation complete: ${artworkInstallationComplete}.`);

