# Pelican Town buildings — first batch

Approved scope: Pierre's General Store and Harvey's Clinic as a joined exterior, followed by the Stardrop Saloon. The direction is Polished Stardew: richer roof, wall and window detail while retaining familiar shapes, colors, signs and entrances. This batch covers three of the twelve exterior entries in the base Town review. Other buildings and their restoration/upgrade versions are separate work.

Version 0.8.4 is installed and verified through a normal SMAPI startup with the other installed mods: 345/345 textures and 349 files passed. Evidence: `artifacts/npc-modern/evidence/installed-0.8.4.json`. Installed session: `artifacts/npc-modern/installed-audits/0.8.4-20260905-221225-294`. Both final owned test processes were stopped after verification. No farm was loaded or save written.

The joined building's main Town-atlas facade uses five rectangles including roof pieces stored separately from the main facade. The Saloon uses its main building region and the separate nighttime window, native tile 653 replacing daytime tile 560. All four seasonal Town sheets are included. Native door actions and map layout are unchanged.

Lighting remains native: the Saloon's sconce light and the winter clinic/store light overlays use their existing rules and lighting texture. These are separate from the artwork. No new building, collision, shop, schedule or lighting behavior is introduced.

Source artwork, built-in image generation prompts, masks, prepared full-size carriers, visual comparisons and preservation reports are stored under `artifacts/town-buildings-modern/`. The earlier twelve-building review remains under `artifacts/town-buildings-review/`.

Artwork coverage and review:

- Pierre/Clinic: four separately generated seasonal sources, fitted to the native joined facade and five atlas regions. Signs, the medical cross, posted notices, plants and seasonal decorations are protected. Roof, siding, glass and foundation detail is updated inside the original silhouettes.
- Saloon: all four seasons, main roof/facade plus separate night window. Signs, vines, flowers, source tile borders and silhouettes are protected. Two compact patch regions per season avoid thousands of tiny patch operations.
- All four seasonal building comparisons were visually reviewed. Root also reviewed the combined spring Town placement and winter clinic/store placement. Seasonal Saloon map contexts and native day/night window comparisons are included.
- Independent source mapping found no use of the selected building tiles outside these locations in 15 regular maps. Eight festival maps reuse most building material; separate festival decoration replacements remain unchanged. No separate renovation for these three exteriors was found in native Town map-modification code.

Preservation checks passed: 341 prior textures remain byte-identical; only the four seasonal Town sheets changed. All seasonal alpha values, all 168,448 prior registered Town pixel visits, and 2,149,376 pixels outside the approved building regions remain exact. All 209,920 prepared-region pixel visits match the staged package artwork. Evidence: `artifacts/town-buildings-modern/preservation-checks.json`.

Both projects build successfully. The production build has zero warnings/errors. The full audit project retains four existing analyzer/nullability warnings. Package: `dist/NpcModern-0.8.4.zip`, containing 345 registered textures and 349 files.

All 102 isolated audit reports passed in `artifacts/npc-modern/runtime-audits/0.8.4-20260905-220934-181`. The Town building audit passed 446 checks, including all four entrance tiles/actions, Saloon day/night selection, lighting anchors, seasonal source regions and unchanged live globals. All 345 loaded textures and 349 package files matched their source/ZIP hashes. Isolated evidence: `artifacts/npc-modern/evidence/isolated-0.8.4.json` and `town-buildings-checks.json` in the same folder.

Installed folder: `C:/Program Files (x86)/Steam/steamapps/common/Stardew Valley/Mods/AbigailModern`. Previous 0.8.3 backup: `artifacts/mod-backups/AbigailModern-20260905-221221`.

![First building batch, before and after](../../artifacts/town-buildings-modern/first-buildings-before-after.png)

These previews assemble actual map tiles using the staged artwork; they are not screenshots of a loaded save. The read-only native map audit checks door tiles/actions, Saloon day/night selection, light anchors and loaded seasonal art. It does not walk through doors, play festivals, draw live sconce lighting or modify a save.

After restarting through SMAPI, visit Pierre's, the clinic and the Saloon; check entrances, signs, roof joins and the Saloon window after dark. Seasonal artwork is selected by the existing game rules. Three of the twelve reviewed Town exteriors are upgraded in this batch; the remaining nine have not been changed by this batch.
