# Farm building remake — NPC Modern 0.10.0

Installed and verified on September 6, 2026. Normal startup passed 362/362 texture checks and 366 installed file hashes. Previous installation backup: `artifacts/mod-backups/AbigailModern-20260906-131642`. Both owned test launches were closed without loading a farm. Music mute also passed the fresh installed launch with sound and ambience unchanged.

The approved farmhouse/cabin/barn/coop/greenhouse/shed batch now has prepared Polished Stardew material detail. Seventeen building textures cover34distinct native appearances:

- Farmhouse base, first upgrade and second-or-later upgrade.
- Stone, Plank, Log, Neighbor, Rustic, Beach and Trailer cabins, each with three visible upgrade appearances.
- Barn, Big Barn and Deluxe Barn; Coop, Big Coop and Deluxe Coop.
- Shed and Big Shed.
- Broken and repaired Greenhouse.

Native definitions for every scoped building have zero seasonal offset, so the same upgraded artwork is used in spring, summer, fall and winter. No separate unsupported seasonal atlas was invented. Existing native paint masks, doors, footprints, upgrade rules, mailbox routing and source dimensions are retained.

[Overview of prepared artwork](../../artifacts/farm-buildings-modern/overview.png). [Farmhouse before/after](../../artifacts/farm-buildings-modern/houses-cabins/houses-before-after.png). [Cabin examples](../../artifacts/farm-buildings-modern/houses-cabins/Stone%20Cabin-before-after.png). [Barn example](../../artifacts/farm-buildings-modern/barn-coop/barn-before-after.png). [Greenhouse before/after](../../artifacts/farm-buildings-modern/greenhouse-shed/greenhouse-before-after.png).

## Preparation and preservation

Thirteen fresh built-in ImageGen donors were generated: eight farmhouse/cabin atlases, two barn/coop material donors shared across their native upgrades, and three greenhouse/shed donors. Exact prompts, copied sources, preparation scripts and provenance are retained in the three artifact subfolders. Only bounded material detail is transferred into native geometry. Generated atlas drift is not installed directly.

All17prepared sheets retain exact native dimensions and alpha, and preserve hidden transparent pixels. Each of34visible source appearances has changed material pixels. Barn/coop door-layer source strips remain byte-identical. Farmhouse inactive atlas columns, separate mailbox and greenhouse unused silhouettes remain unchanged. Native paint-mask files are not replaced. All345previous registered textures retain their hashes and metadata.

[Preservation checks](../../artifacts/farm-buildings-modern/preservation-checks.json). [Native contracts and source rectangles](../../artifacts/farm-buildings-modern/contracts/native-contract.md). [Independent house/cabin review](../../artifacts/farm-buildings-modern/barn-coop/houses-cabins-review.md). Greenhouse/shed contract checks and audit review are retained in its artifact folder.

## Validation and release

The new registration check first failed against the old registry, then passed after adding all17entries. Total registry362textures; package366files. Production build has zero warnings/errors. Audit build has four existing unrelated warnings.

Final isolated validation passed 399 building checks across 136 seasonal/upgrade/state cases and 23 visual-effects checks. All 362 registered textures and 366 package files passed verification. Building GPU fixtures use actual native source rectangles, unconditional layers and native BuildingPainter.Apply on loaded textures. They are detached composites, not Building.draw or saved-farm gameplay. All four seasonal selections, upgrade appearances and both greenhouse states are checked. Animation bounds are sampled; the scoped native door layers are single-frame. See artifacts/farm-buildings-modern/STATUS.md for installation status.

The user requested muted game music. Changing startup_preferences alone was insufficient because the game resets title music and can restore saved music settings. A local audio-preferences.json setting now enables music-only muting at startup and after save loading. The isolated runtime confirms music and player volume 0, sound effects 1, and ambience 0.75. No farm was loaded or save written. A future ordinary save can naturally record the muted music value. To disable this local preference, set MuteMusic to false and restart. Save-load enforcement was reviewed in code; live verification covers title startup.

## Player check

Restart through SMAPI and inspect existing buildings/upgrades in your farm. Check human and animal doors, repainting where available, cabin styles, and the current greenhouse state. Verify night lighting and preferred zoom. No farm is loaded or save written during automated checks; entering buildings, buying upgrades, live door animation and split-screen remain hands-on checks. Music stays muted as requested; sound effects stay enabled.
