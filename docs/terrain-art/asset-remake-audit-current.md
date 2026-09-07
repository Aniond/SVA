# Current art coverage audit - installed 0.22

A fresh read-only scan completed September 6, 2026 (September 7 at 03:40 UTC) against source version 0.22.0 and its integrated 739-entry registry. Normal installed version 0.22.0 and all 772 package files are verified. The native scene audit passed 217 checks across 50 routes; 739 registered texture checks and production HD checks passed, followed by normal startup verification. The scan itself did not build or run the game.

## Measured inventory

| Measure | Count |
|---|---:|
| Native Texture2D XNB files decoded |999|
| Decode errors |0|
| Non-texture/non-art files skipped |2,562|
| Exact native-file names present in artwork registry |685|
| Registered full sheets / partial sheets |463 / 222|
| Native files without exact registry entries |314|
| Base texture files / localized variants |702 / 297|
| Registered base files / unregistered base files |604 / 98|
| Current artwork registry entries |739|
| Separate 2x HD player companions |17 (eight bodies, nine equipment)|
| Custom logical clothing atlases, outside native XNB inventory |2|
| New clothing catalog items |12 (six shirts, six pants)|
| HD or custom logical dimension errors |0|

The 54 registry names without an exact native XNB are custom assets, mostly added portraits; they are not automatically missing files. Localized files are counted separately and matched by exact name. This scan does not assume that a base-name patch safely covers a localized atlas.

“Unregistered” means no exact artwork-registry entry, **not** “needs a remake.” Registration also does not prove every pixel or live usage is covered. Partial sheets deliberately retain original sprites, borders, lettering and functional regions; unchanged occupied 16px cells are review leads, not failures.

## Intentional preservation and HD coverage

All 15 original player textures intentionally remain outside the ordinary artwork registry. The runtime adapter uses separate companions while logical textures continue to select clothing, animation frames, colors and dyes. The two custom clothing atlases also sit outside that registry: the clothing catalog supplies them, so they are not missing native XNB files. Their logical sizes are 256 × 32 for shirts and 1152 × 688 for pants, verified against actual source PNGs. All 17 companions exist and measure exactly 2x their corresponding logical textures. hats_animals remains a prepared but dormant animal route; do not claim upgraded pet hats. This file scan does not independently prove runtime correctness.

The scan flags 17 explicit technical/support textures: paint masks, error placeholders, skinColors/shoeColors tables, and VolcanoLayouts/Layouts. Preserve these; the volcano layout image is generation data. Also review map path markers, light/shadow masks, font atlases and platform/mobile support assets by their functional roles before proposing art changes. The three Stalagmite sheets still lack a confirmed native caller and are excluded from live-coverage claims. They are not missing proof of the new cave atmosphere, which uses actual mine/cave tile art and scoped effects.

Prepared artwork covers farm animals, many characters and portraits, buildings, selected terrain and locations, the blue interface, player detail and modern clothing, and selected mining art. Version 0.19 adds all 73 monster sheets and eight additional wildlife sheets, plus Emily's exact shared-sheet parrot animation. Existing island parrots were already complete. The artwork is installed and its final native rendering checks passed. Those checks do not prove every animation, spawn route or gameplay behavior.

Compared with the 0.20 scan, the registry gains 40 names, rising from 699 to 739. Exact native registrations rise from 645 to 685, and unregistered base files fall from 120 to 98. The additions cover soil/floor gaps and the installed scene batch. All 17 HD companions, two custom logical clothing atlases and twelve clothing catalog entries still pass their separate checks. Historical scans are retained.

All 73 Characters/Monsters sheets now have registrations; none remain in the unregistered monster group. Cat, Crow, Frog and Fireball still have unconfirmed live callers. Ambient crow and frog art is separately covered through critters. Farm livestock and pets remain a completed group: 42 Animals sheets are registered, with only the technical Error file excluded. The final native audit passed 14 actual monster drawing fixtures and three actual critter fixtures; see [monster and wildlife progress](monsters-wildlife-progress.md).

## Remaining priorities

1. **Other effects and item types:** the Tools/Weapons/Objects export is now fully routed through prepared textures: 37 tools, 67 weapons and 807 objects, with zero missing routes or source-rectangle mismatches. This is 911 native item definitions, not proof of every animation or unrelated item type. TileSheets/animations remains a separate mixed-effects review. Preserve its gameplay signals and functional regions; do not describe tools or weapons as still unregistered. See [item progress](items-progress.md).
2. **Remaining terrain and maps:** the three tilled-soil atlases, legacy Floors and missing indoor-floor patterns are now installed and verified in 0.21. Existing outdoor paths remain intact; Maps/paths is technical map data. Other unregistered map or terrain sheets still need live-route and region review before any remake. See [soil/floor progress](soil-floor-progress.md).
3. **Title/platform and remaining special scenes:** 28 of 53 native Minigames texture files are now registered. The remaining 25 are Amuzio, twelve TitleButtons base/locale files and twelve Xb1ProfileButton base/locale files. These contain branding, interface lettering or platform graphics; they are not 25 unfinished playable minigames. The prepared 50-route scene batch includes dedicated scenes, selected shared arcade/mermaid art and language variants. Its runtime audit is pending; see [scene progress](scenes-progress.md).
4. **Remaining maps, interface and special routes:** Maps contains 304 texture files, with 7 registered full sheets, 105 partial sheets and 192 unregistered files, heavily affected by localization and support art. Base review leads include busPeople, characterSheet and desert-festival text sheets; world-map screens, daybg/nightbg and platform-specific atlases should be reviewed by actual usage. Do not equate these counts with 192 unfinished locations. Special unregistered entries such as Characters/Grandpa and Portraits/AnsweringMachine need route/legacy verification rather than automatic generation.

The 739-entry snapshot retains the mining plant registration and deliberate patches to shared node and cave sheets. Ice/lava identity and unrelated prior art were preserved in that scope. Subsequent native checks passed 19 mining interactions before the art update and 19 afterward. Full mine-room captures at levels 15, 20, 60 and 100, including an effects-off/on comparison, are under `artifacts/new-game-persistence/phone-combined-20260906/Profile`. Human listening to cave audio remains pending; these captures and checks do not establish exhaustive performance or every biome route.

## Evidence and reproducibility

Fresh files are under artifacts/asset-remake-scan-0.22: scan.mjs, inventory.json/csv, summarize.mjs, classified-inventory.json, hd-companions.json, custom-clothing-inventory.json and unregistered-base-assets.csv. The scan enumerates native Content XNBs, compares visible pixels within registered patch areas, and separately checks HD dimensions and clothing catalog entries. It does not mutate content or use saves. Historical scan folders through asset-remake-scan-0.20 were left untouched.
