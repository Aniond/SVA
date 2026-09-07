# Mining and cave artwork inventory

Checked 2026-09-06 against the current 606-entry artwork registry and native installed XNB textures. No production artwork, registry entries, maps, collision, or running game were changed.

Machine-readable exact rectangles and live pixel counts: `artifacts/mining-modern/inventory.json`. Reproduce with `node artifacts/mining-modern/inventory.mjs`. Object names/texture indices come from the earlier installed-native typed export `artifacts/crops-trees-modern/contracts/crops-native-objects.json`; this task checked the actual texture dimensions and current registered pixels again. Refresh the typed object export if game data changes before implementation.

## Existing location batch is present

All 103 previous location manifest entries still have a current registry entry. This includes 22 mine/volcano atlases, `Maps/cave`, `Maps/masteryCaveTilesheet`, and the broader interior/seasonal sheets. These are predominantly explicit partial patch regions, not proof every visible tile was remade. Preserve every current approved patch and its exact pixels when extending a shared sheet; do not run a full-sheet replacement over them.

`Maps/cave` currently changes 4,655 visible pixels, with 15,950/18,004 visible pixels inside registered regions. Mastery cave changes 7,988, with 28,699/56,029 inside regions. The covered region includes intentionally unchanged seams and outlines, so neither the uncovered count nor the changed count alone establishes a defect.

Highest-priority visual review: `Maps/Mines/mine_frost_dark` has just 762 changed pixels and 9,690/57,391 visible pixels inside registered regions. `volcano_dungeon` changes 10,439 with 62,803/101,968 covered. Review full native/prepared comparisons and actual tile use before selecting additions. Existing lava, water, ladder/exit cues, tile edges, and saturated signal colors were deliberately protected in `artifacts/locations-modern/mines/prepare.mjs`; preserve those semantics rather than treating them as accidental omissions.

## Confirmed missing mining objects

All 55 native `Name=Stone, Type=Litter` entries have **zero visible pixels covered or changed** by current patches. Their exact IDs, actual texture names, sprite indices and 16x16 source rectangles are in `inventory.json.nodes`. This captures ordinary stones, ore/gem-rich stones, geode nodes, radioactive/cinder/volcano variants and festival/coal nodes without confusing their inventory rewards with the breakable node sprites.

The default object texture is `Maps/springobjects`, confirmed by native `Game1.objectSpriteSheetName` and loads. Its native width is 384 pixels: 24 columns, not a guessed 16-column atlas. `TileSheets/Objects_2` is 128 pixels wide: 8 columns. Source rectangles must derive from each real width. Both sheets contain unrelated approved objects/items; extend only selected node cells and retain all existing registry patches.

Newer explicit IDs on `TileSheets/Objects_2`:

| ID | Sprite index | Exact source rectangle X,Y,W,H |
|---|---:|---|
| CalicoEggStone_0 | 10 | 32,16,16,16 |
| CalicoEggStone_1 | 11 | 48,16,16,16 |
| CalicoEggStone_2 | 12 | 64,16,16,16 |
| VolcanoGoldNode | 60 | 64,112,16,16 |
| VolcanoCoalNode0 | 136 | 0,272,16,16 |
| VolcanoCoalNode1 | 137 | 16,272,16,16 |
| BasicCoalNode0 | 146 | 32,288,16,16 |
| BasicCoalNode1 | 147 | 48,288,16,16 |

Native MineShaft.createLitterObject (line 4351 onward) picks ordinary rock variations, colors some through ColoredObject, and selects special nodes. Preserve base chroma/luminance behavior for tinted dangerous mine rocks. MineShaft.getRandomGemRichStoneForThisLevel (line 3992) maps generated gem choices to node IDs 8,10,12,6,4,14; do not mistake reward gem sprite IDs 60–70 for the node cells. The data export says ColorOverlayFromNextIndex=false for these 55 nodes; inspect any future extra colored variants before including neighboring cells.

Seven uncovered 32x32 resource clumps are also enumerated: meteorite index622, boulder672, mine rocks752/754/756/758 on springobjects, and quarry boulder148 on Objects_2. Native ResourceClump.draw computes the origin as a 16px index then expands width/height according to the clump; these are not single 16px node sprites. MineShaft explicitly supplies Objects_2 for quarry boulders at lines1332–1333. Keep stumps/logs and green-rain vegetation out of this rock batch unless separately requested.

## Unregistered cave decoration assets

| Asset | Native dimensions | Evidence and next step |
|---|---|---|
| TerrainFeatures/Stalagmite | 128x256 | Native texture exists, no current registry entry. No literal caller found in the decompiled source; verify actual use/legacy status before claiming a live-room effect. |
| TerrainFeatures/Stalagmite_Frost | 128x256 | Same coverage/usage qualification. |
| TerrainFeatures/Stalagmite_Lava | 128x256 | Same coverage/usage qualification. |
| TerrainFeatures/upperCavePlants | 144x24 | Confirmed live native CosmeticPlant.textureName/draw. Draw cells are `(grassType*16,0,16,24)`; MineShaft generates types0–2. Tool debris uses `(grassType*16,6,7,6)`. |

upperCavePlants is shared with Summit's ending scene: source rectangles `(0,0,48,21)` and `(96,0,48,21)` are explicitly drawn there. A full-sheet remake therefore also changes that scene; preserve geometry and inspect all nine 16px columns, not just the first three mine plant types. Layout and alpha must remain exact.

**Exclude `VolcanoLayouts/Layouts`.** Although it is encoded as a texture, VolcanoDungeon reads its pixels to generate map layout (`ApplyPixels`, lines644–685). Repainting it would change gameplay geometry, not merely cave appearance.

## Implementation order and acceptance

1. Remake the 55 exact node cells and seven exact clump rectangles using native silhouettes, clear ore/gem colors, and the approved polished style. Preserve all other shared-sheet pixels and existing approved patches.
2. Remake upperCavePlants with its mine and Summit uses accounted for. Inspect the three Stalagmite sheets and establish live caller/layout before integration.
3. Visually review the already registered cave/interior atlases, prioritizing dark-frost and volcano. Add only identified missing material regions; do not redo the complete 103-sheet batch by default.

Validate alpha/hidden pixels, outlines, repeated variants, source bounds, actual patched-cell coverage, tinted rock recognition, and preservation of existing artwork. Later isolated native checks should cover breaking ordinary rocks/ore/gem nodes, big rock tool interactions, dangerous/frost/lava/Skull Cavern/volcano themes, debris and ladders. This inventory does not claim those runtime checks have happened.

## Expanded atmospheric cave scope

User has also requested colored rocks, stalactites, wet surfaces, and localized dripping water. The other agent is preparing a temperate-cave concept; this inventory does not duplicate that creative work.

Keep the first atmosphere implementation cosmetic. Existing `AbigailModern/Visuals/VisualEffectsController.cs` already uses per-screen state, UpdateTicked time, RenderedStep.World_Background, RenderedWorld, bounded viewport collection, and cleanup on save exit. A separate cave effect can follow this architecture without changing existing lighting/weather state. Native MineShaft.UpdateWhenCurrentLocation already plays `cavedrip` at line551; avoid adding a second constant ambient sound loop. VolcanoDungeon and Sewer also use that cue.

Suggested bounded route: allowlist mine/cave themes; identify eligible wall-to-floor ceiling edges from loaded tiles, or use approved map/template anchor metadata. Spawn a small capped local particle pool (e.g. <=24 visible droplets), deterministic cosmetic RNG independent of Game1.random/mineRandom, staggered emission, short downward travel and a tiny ground splash. Clip against viewport and occlusion; exclude ladder/elevator, door and narrow gameplay-signaling regions. Suspend on menus/events/map screenshots and clear on warp/title. Wet sheen/puddles are alpha overlays drawn beneath characters, with no Water tile properties, no fishing eligibility, no collision and no changes to ore abundance or floor RNG. If added sound is desired, rate-limit a spatial cue to occasional nearby impacts and respect SFX volume; the existing music-only mute must remain intact.

Use native tile indices and approved anchors for hanging stalactites so they attach to actual ceilings instead of floating over walkable terrain. Their visuals must not obscure interactable ladders, enemies, ore identity, or bomb/attack cues. Decorative stalactites should not become ResourceClump objects. Existing mine water/lava animated tiles remain native animation with unchanged frame count/timing/source positions; new wet-rock detail belongs within their safe art regions or the separate effect layer. All limits above are a proposal for implementation, not a currently installed effect.

## Feasibility: Gemini-generated random cave floors

Feasible as a constrained floor recipe, not by treating generated raster art as a playable map. No floor generator or new API calls are implemented here.

Native evidence: `MineShaft.generateContents` (line643) runs `loadLevel(mineLevel)`, `chooseLevelType()`, `findLadder()`, then `populateLevel()`. `loadLevel` (line2680) selects a `Maps/Mines/{number}` map, optionally honors `forceLayout` only when the map asset exists, sets `mapPath`/`loadedMapNumber`, and calls `updateMap` at line2789. Skull Cavern already chooses randomized nonrepeating templates; native special/elevator/treasure/quarry floors have separate rules. Population (line1321 onward) handles objects, ore, enemies, clumps and vegetation separately from layout. VolcanoDungeon uses a different pixel-layout generator and must be handled independently.

Lowest-risk design: Gemini returns a small schema such as `{schemaVersion, templateId, themeId, wetnessBand, rockPaletteId, stalactiteDensityBand, dripDensityBand}` using only approved enum values. The host chooses an allowed native template and applies approved cosmetic assets/anchors. No free-form code, file paths, arbitrary tile indices, monster rewards, or ore quantities are accepted. This enables varied atmosphere without changing collision, progression, or economy.

If the user later wants genuinely new floor topology, use a local deterministic room/corridor builder driven by constrained room-size/count/connection parameters, with Gemini selecting only bounded recipe parameters. Validate the resulting grid BEFORE activation: every tile index exists; map and layer dimensions agree; flood-fill connects arrival to a guaranteed valid exit/progression opportunity; elevator/ladder tiles and reserved arrival area stay clear; interactable nodes have a reachable approach; no forbidden water/lava spawn; resource/enemy density and loot remain within native-approved bands. Retry local generation with fixed derived seeds a bounded number of times, then fall back to a native template. Arbitrary Gemini coordinates must never become trusted collision data.

Cache recipes ahead of entry, keyed by save ID, native day/run identity, floor number, theme and schema version. Include day/run because native mine floors can regenerate between visits/days; a permanent floor-number-only cache would silently change that behavior. Store accepted recipes and deterministic seeds so re-entry, save reload and all multiplayer clients agree. Only the host requests and validates recipes; clients consume the accepted seed/recipe. No network call or await belongs in loadLevel, populateLevel, or the player's doorway transition. If no validated cached recipe exists, use native generation immediately. Fetch a small bounded batch while idle or through an explicit preparation action, with a persisted request budget and no entry-triggered retry storm.

Apply geometry/template selection before native ladder discovery/population, not after objects are placed. Preserve original floor number, depth, quests, elevator unlocks, festivals and progression. Restricted floors (elevator, treasure, quest, quarry, festival and special scripted rooms) should initially bypass recipe selection. This is a separate gameplay feature requiring its own approval/plan and connectivity/native lifecycle tests; the current authorized art batch does not imply map-generation changes.
