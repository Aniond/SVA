# Asset remake audit — September 6, 2026

**Follow-up completed:** the 17 remaining farm structures and 42 animal/pet sheets listed below are now installed in version 0.15.0, verified at 606 textures and 610 files. See [structure completion](utility-buildings-progress.md) and [animal completion](animals-progress.md). The scan counts below preserve the earlier same-day snapshot; player/equipment, monsters/wildlife and remaining item icons are now the largest next batches.

Fresh scan of the installed game's texture files against the current source artwork registry. Installed artwork is version 0.13.0 (547 entries); source is 0.14.0 (589 entries). The additional 42 animal/pet sheets are prepared but await final runtime validation and installation.

## Findings

Decoded 999 textures with zero errors. These represent 702 base texture files and 297 language variants. Of the base files, 478 have registrations and 224 do not. Missing registration is a review candidate, not automatically a remake requirement: technical masks, placeholders and legacy assets are included. Another 2,562 files were excluded by type/folder, including maps, data, fonts, shaders and audio.

| Suggested order | Remaining work | Evidence and scope |
|---|---|---|
| 1 | Remaining farm structures | 17 non-mask building sheets: Stable, Silo, Mill, Well, Fish Pond, Shipping Bin, Slime Hutch, Junimo Hut, Gold Clock, four obelisks, Mailbox and three pet bowls. Existing houses, cabins, barns, coops, sheds and greenhouse are covered. |
| 2 | Player and equipment | 17 Farmer sheets have no registration: bodies, mannequins, hair, shirts, pants, hats and accessories, plus two color tables that should be preserved. Tools and weapons are also untouched. Coordinate hats on animals with the new pet artwork. |
| 3 | Monsters, wildlife and companions | All 73 monster-folder sheets remain unregistered, including dangerous variants. Separate critters, companions, aquarium fish, birds and special creatures also need review. Check active use before counting legacy sheets as enemies. |
| 4 | Item icons and small effects | Most item art remains native: springobjects has changes in 99 of 925 occupied cells; Objects_2 has changes in 4 of 151. Review remaining food, fish, minerals, resources and equipment icons, bobbers, projectiles, debris, animations and emotes. Preserve completed crop/fruit icons, furniture and craftables. |
| 5 | Remaining ground and vegetation | Three tilled-soil sheets, three stalagmite sheets and upperCavePlants are unregistered. Grass has changes in only 6 of 49 occupied cells. Review remaining grass/plant regions against actual seasonal uses. Completed crops, common and special trees should be retained. |
| 6 | Special scenes and minigames | Review Junimo Kart, darts, boat journey maps, title/intro art, movies/crane game, panorama and special backgrounds. Some files contain shared UI or legacy platform art, so select individual regions after checking their use. |
| 7 | Overview maps and remaining shared artwork | Seasonal world-map pictures, island map and ranching maps remain unregistered. These are distinct from the remade playable locations. Review remaining shared-sheet regions without repeating the finished blue UI. |

## Already covered or pending release

- NPC portraits and the established character remake set; all twelve reviewed Town exteriors, including Pam's trailer and rebuilt home.
- Farmhouse/cabins, barns/coops, greenhouse and sheds; approved crop and special-tree batch.
- 103 location atlases covering interiors, outdoor locations, mines and seasonal art; the blue UI release covers 87 base/localized sheets.
- Roads, fences, furniture, craftables and the reviewed weather/water artwork.
- Animals/pets: 42 sheets prepared in source, including horse, baby/adult variants, cats, dogs and turtles. Do not start another remake of these; finish validation and installation.
- Lighting, shadows, fog, moonlight and stars are runtime features, not evidence that every underlying sprite has been remade.

## Base-file coverage

| Folder | Files | Unregistered |
|---|---:|---:|
| Animals | 43 | 1 |
| Buildings | 47 | 30 |
| Characters | 215 | 91 |
| LooseSprites | 90 | 56 |
| Maps | 116 | 10 |
| Minigames | 14 | 13 |
| Portraits | 101 | 1 |
| TerrainFeatures | 38 | 7 |
| TileSheets | 37 | 14 |
| VolcanoLayouts | 1 | 1 |
| Total | 702 | 224 |

Preserve paint masks, Error textures, skin/shoe color tables, procedural VolcanoLayouts, map path/control data, shadows and deliberately retained silhouettes/placeholders. Localized texture files may inherit base patches through the loader; absence of a separate locale entry is not automatically missing artwork.

This is an exhaustive texture-file and registered-pixel coverage scan, reconciled with completed scope reports. It is not a new visual approval of every animation, or a claim that every unchanged pixel needs replacement. No gameplay, saves or installed artwork were changed for this scan.

Evidence: [full inventory](../../artifacts/asset-remake-scan/inventory.json), [base inventory](../../artifacts/asset-remake-scan/base-inventory.csv), [unregistered base files](../../artifacts/asset-remake-scan/unregistered-base-assets.csv). Counts describe current source coverage; installation status is stated separately above.
