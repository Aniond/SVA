# Crops and special trees — NPC Modern 0.11.0

The full approved crops and special-tree batch is prepared and installed in the established Polished Stardew style. [Selected artwork overview](../../artifacts/crops-trees-modern/overview.png).

## Completed scope

- All 50 crop definitions: seed and growth stages, mature appearance, defined regrowth, seasonal fiber, wild-seed variants, dead crops, and native flower color layers.
- All five giants: cauliflower, melon, pumpkin, powdermelon and Qi fruit.
- Spring onion and ginger variants, all 15 mature wild-seed forage replacements, crop harvest icons and fruit-tree produce. Exact shared-sheet coverage contains 75 item rectangles, two giant rectangles and three forage strips.
- Eight fruit trees: cherry, apricot, orange, peach, pomegranate, apple, banana and mango. All active growth, seasonal canopy and stump regions covered.
- Seventeen special-tree sheets: mushroom, mystic, both palms, four mahogany seasons, and three seasonal sheets for each of the three green-rain families. Spring and summer share the unsuffixed green-rain sheets, as in the native definitions.

This adds 20 registrations and extends three existing shared textures, for 382 total registrations. All 359 other texture files remain byte-identical. Every previous patch on the three extended sheets remains unchanged. Approved buildings and ordinary oak/maple/pine are preserved.

## Preparation and preservation

Twenty-two fresh built-in ImageGen reference images, exact prompts, provenance, source PNGs, preparation scripts, masks and before/after previews are retained under `artifacts/crops-trees-modern`. Bounded material detail is transferred into the original layout; generated layout drift is not installed.

All native alpha and hidden transparent pixels are retained. Neutral crop tint layers and two tiny neutral seed frames remain exact. Fruit-tree unused row 6, shadow strips and leaf swatches are preserved. Special-tree seeds, leaf particles, unused slots and snow cues are protected. Coconut-ready palm and native moss variants retain their silhouettes and colors. No growth, harvest, weather, spawning or save data is changed.

[Preservation checks](../../artifacts/crops-trees-modern/preservation-checks.json). [Exact crop contract](../../artifacts/crops-trees-modern/contracts/crops-scope.md). [Tree contract](../../artifacts/crops-trees-modern/contracts/trees-contract.md). [Shared-sheet coverage](../../artifacts/crops-trees-modern/shared/install-manifest.json).

## Validation

The new registration check first failed against the old registry, then passed after integration. Production build: zero warnings or errors. Audit build: zero errors and four existing unrelated warnings.

Final isolated session `0.11.0-20260906-133419-336` passed 5,113 crop checks across 2,490 cases, 572 tree checks across 454 cases, and 23 existing visual-effects checks. All 382 loaded textures and 386 package file hashes passed. Representative crop, giant, tree and assembled fruit-bearing previews were inspected. Independent art and audit reviews found no blockers.

These are detached native source-selection and GPU composite checks. Fruit trees include fruit-bearing, lightning/coal and falling canopy composites. Wild trees use separate-region fixtures. This does not claim native full-scene drawing, growth or harvesting gameplay. No farm was loaded or saved.

Package: `dist/NpcModern-0.11.0.zip`. Previous installation backup: `artifacts/mod-backups/AbigailModern-20260906-133554`. Fresh normal startup verified 382/382 textures and 386 file hashes. Both launches recorded music and player volume 0, sound 1, ambience 0.75. Local MuteMusic=true retained. All owned test processes stopped. [Installed evidence](../../artifacts/npc-modern/evidence/installed-0.11.0.json).

## Player check

Restart through SMAPI. Check existing crops at your usual zoom, harvest and regrowth, trellis readability, flower colors, fruit-bearing trees, shaking/chopping and the current season. Check nighttime appearance alongside the approved buildings. Growth over days, harvesting, tree interactions, split-screen and full farm playback remain hands-on checks. Music-only muting remains enabled; sound effects and ambience retain their settings.
