# Seasonal civic buildings — 0.8.6

Approved scope: finish the Community Center, Blacksmith, Museum/Library and JojaMart across spring, summer, fall and winter, matching the installed Polished Stardew houses and shops. Roofs, siding, masonry and metal panels receive richer material detail while preserving familiar building shapes, colors and gameplay.

| Building | Included appearances |
|---|---|
| Community Center | Worn and restored buildings in all four seasons; Joja warehouse facade with the original winter snow overlay |
| Blacksmith | Four seasonal exteriors; original separate day/night window retained |
| Museum / Library | Four seasonal exteriors, retaining book signage and decorations |
| JojaMart | Intact and abandoned buildings in all four seasons; native cracked/open doors preserved |
| Theater replacement | Seasonal facade used at both Town sites, including the alternate Halloween map |

This finishes the twelve entries in the base Town exterior review. It does not mean all buildings or decorations throughout the game have been remade. Outlying homes, farmhouse and cabins remain separate future groups.

## Seasonal comparisons

- [Spring](../../artifacts/town-civic-modern/spring-civic-before-after.png)
- [Summer](../../artifacts/town-civic-modern/summer-civic-before-after.png)
- [Fall](../../artifacts/town-civic-modern/fall-civic-before-after.png)
- [Winter](../../artifacts/town-civic-modern/winter-civic-before-after.png)

The 32 main comparisons and 13 additional state comparisons use native map placements and the actual restored, abandoned, opened-door and theater-map operations on detached maps. Warehouse panels include the native facade positions and winter overlay. These are static reconstructions, not screenshots from a loaded save. Dynamic clock hands, movie posters, live lights, characters and weather are omitted. [Rendering details](../../artifacts/town-civic-modern/preview-method.json).

## Verification

- Five textures changed: the four seasonal Town sheets and the warehouse region of `LooseSprites/Cursors`. All 340 other registered textures and their registry metadata remain exact.
- The merge changes 154,891 pixels. All 3,947,520 alpha values, 971,813 prior-artwork pixel visits and 3,204,954 pixels outside the selected areas remain exact. The staged files equal the prepared material changes pixel for pixel. [Preservation evidence](../../artifacts/town-civic-modern/preservation-checks.json).
- All 16 base-building seasonal coverage checks pass. They first failed against the captured 0.8.5 artwork, proving the missing building updates were detected. Worker checks and independent visual review cover the additional appearances. [Coverage checks](../../artifacts/town-civic-modern/coverage-checks.json).
- Full source reuse was checked across 26 native maps, including ordinary maps, Town festivals and all three theater replacement maps. All 2,797 placements of the 459 changed Town source cells are within the selected civic/theater windows.
- Native audit checks entrance sources/actions, the Blacksmith night selector, detached Community Center restoration, abandoned and cracked Joja doors, actual theater map application, loaded seasonal material bounds and the warehouse source rectangles. Player, location, random object, clock and viewport references/state are unchanged; the shared Town map remains exact.
- Production build: zero warnings and errors. Audit build: four existing unrelated warnings.
- [Independent review](../../artifacts/town-civic-modern/independent-review.md) checks prepared and staged pixels, masks, visuals and the audit's stated scope.

## Installed release

Version **0.8.6 is installed and verified** in the normal Stardew Valley Mods folder. All **104 isolated audit reports** passed, including **74 civic appearance checks**. Normal startup verified **345/345 registered textures and all 349 installed files** against the workspace. The previous 0.8.5 installation is backed up at `artifacts/mod-backups/AbigailModern-20260906-010521`.

- [Final isolated evidence](../../artifacts/npc-modern/evidence/isolated-0.8.6.json), session `artifacts/npc-modern/runtime-audits/0.8.6-20260906-010045-031`.
- [Installed startup evidence](../../artifacts/npc-modern/evidence/installed-0.8.6.json), session `artifacts/npc-modern/installed-audits/0.8.6-20260906-010525-710`.
- [Package](../../dist/NpcModern-0.8.6.zip) and [completed Town exterior coverage](../../artifacts/town-civic-modern/coverage.json).

Independent review has no unresolved findings. The preview-only theater-ground issue was corrected using the game's actual map replacement operation and the final seasonal panels were independently reinspected. All owned test processes are stopped. No farm was loaded or save written.

## Artwork files and exact prompts

Nine fresh donors were generated with the **built-in ImageGen tool**. The original images, exact prompts, local copies, registration and preparation scripts, masks and comparisons are retained in the project:

- [Community Center and warehouse prompts/provenance](../../artifacts/town-civic-modern/community-center/provenance.json)
- [Joja and theater prompt set](../../artifacts/town-civic-modern/joja/prompts.json)
- [Blacksmith and Museum exact prompts](../../artifacts/town-civic-modern/blacksmith-museum/prompts.json) and [source provenance](../../artifacts/town-civic-modern/blacksmith-museum/provenance.json)

Production files are the four `src/AbigailModern/assets/OutdoorProps/Maps-{spring,summer,fall,winter}_town.png` sheets and the warehouse regions of `src/AbigailModern/assets/Grandpa/Cursors.png`. Earlier artwork in that shared file remains exact. [Artwork registry](../../src/AbigailModern/artwork.json) and [prepared file manifest](../../artifacts/town-civic-modern/install-manifest.json).

## Player check

Restart through SMAPI and visit the four buildings. Check entrances, roof edges and signs, then visit the Blacksmith after dark. Inspect whichever restored, abandoned, warehouse or theater state already exists in your save. Seasonal artwork follows the game's normal season selection. No farm was loaded or save written during automated checks; live lighting, clock/poster motion, walking through doors, festivals and progression remain hands-on checks.
