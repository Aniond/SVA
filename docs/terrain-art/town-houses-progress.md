# Seasonal town homes — 0.8.5

User-approved scope: the five town homes, including Pam's rebuilt house, across spring, summer, fall and winter. The finish follows the installed Polished Stardew shop and clinic artwork: richer roofing, siding, stone and glass while retaining familiar shapes and colors.

## Coverage

| Home | Included |
|---|---|
| Jodi, Kent, Sam and Vincent | Blue house with purple roof; all four seasons |
| Emily and Haley | Light siding and orange roof; all four seasons |
| George, Evelyn and Alex | Blue timbered house and dark roof; all four seasons |
| Mayor Lewis | Manor roof, walls and doors; all four seasons |
| Pam and Penny | Original trailer, its separate nighttime window, and rebuilt house; all four seasons |

Native snow coverage, seasonal plants and ornaments, window and door positions, transparency, tile joins, map layout, entrances and lighting rules are retained. The artwork does not change the requirements for rebuilding Pam's house.

This brings the base Town exterior review to eight completed entries out of twelve, counting the previously finished shop, clinic and Saloon. Community Center, Blacksmith, Museum/Library and JojaMart remain outside this update. The farmhouse, cabins and outlying homes were excluded by the user's scope choice.

## Visual review

- [Spring comparison](../../artifacts/town-houses-modern/spring-homes-before-after.png)
- [Summer comparison](../../artifacts/town-houses-modern/summer-homes-before-after.png)
- [Fall comparison](../../artifacts/town-houses-modern/fall-homes-before-after.png)
- [Winter comparison](../../artifacts/town-houses-modern/winter-homes-before-after.png)

These assemble actual native map tiles using the prepared seasonal artwork. They are static map previews, not screenshots from a loaded save. All 24 seasonal home/state comparisons were reviewed, along with Pam's day/night and rebuilt-state material. Native light glows, live NPCs and weather particles are not drawn in the previews.

The initial review had the two southern houses labeled in reverse. Fresh NPC home data and Town entrances confirmed the correct residents above. Final combined previews, native contracts and runtime checks use those names. Historical worker filenames and verbatim generation prompts remain unchanged as provenance; their aliases are recorded in [house-identities.json](../../artifacts/town-houses-modern/house-identities.json).

## Preservation and verification

- Only the four seasonal Town texture files changed. All 341 other registered texture files remain byte-identical to 0.8.4.
- All 2,359,296 seasonal alpha values, 378,368 visits to previously registered patch pixels and 1,890,304 pixels outside the new house regions remain exact. All 468,992 visits to prepared source regions match the merged artwork. [Preservation evidence](../../artifacts/town-houses-modern/preservation-checks.json).
- All 28 coverage checks pass: six home appearances in four seasons, plus the trailer night tile in each season. The test first failed for all 28 missing-art cases against the captured 0.8.4 baseline. [Coverage evidence](../../artifacts/town-houses-modern/coverage-checks.json).
- Independent review checked all 23 complete native maps: 15 regular maps and eight Town festival maps. None of the 3,730 placements using the 399 changed source cells were outside the five home windows. Festival decorations that replace ordinary building tiles remain native. [Independent review](../../artifacts/town-houses-modern/independent-review.md).
- The production build has zero warnings or errors. The audit project builds with four existing unrelated warnings.
- The native house audit passes 694 checks, including six doorway tiles/actions, the trailer day/night source pair, all four loaded seasonal sheets, and the native rebuilt-house operation on detached maps starting from both day and night states. Global player/location/time state stays unchanged.

## Installed release

Version **0.8.5** is installed in the normal Stardew Valley `Mods/AbigailModern` folder. Normal SMAPI startup verified all **345 registered textures and 349 installed files** against the workspace. The final isolated package passed all **103 audit reports** and matched all 349 packaged files. Independent review has no unresolved actionable findings.

- [Installed startup evidence](../../artifacts/npc-modern/evidence/installed-0.8.5.json), from session `artifacts/npc-modern/installed-audits/0.8.5-20260906-003707-772`.
- [Isolated package evidence](../../artifacts/npc-modern/evidence/isolated-0.8.5.json), from final session `artifacts/npc-modern/runtime-audits/0.8.5-20260906-003022-883`.
- [Downloadable package](../../dist/NpcModern-0.8.5.zip).
- Previous 0.8.4 installation backup: `artifacts/mod-backups/AbigailModern-20260906-003656`.
- [Current Town coverage](../../artifacts/town-houses-modern/coverage.json).

All owned verification processes were stopped. The audit stayed at the title screen; no farm was loaded or save written.

## Artwork sources

Twelve new donors were created using the built-in ImageGen tool: an ordinary and a winter donor for each of the six home appearances. Generated detail is fitted inside the original material regions; the native seasonal colors and protected features are retained. The four production sheets live under `src/AbigailModern/assets/OutdoorProps/Maps-{spring,summer,fall,winter}_town.png`.

Exact prompts, copied source images, masks, prepared carriers and generation provenance are retained in:

- [Blue-house sources and prompts](../../artifacts/town-houses-modern/blue-houses/provenance.json)
- [Orange-roofed house and manor sources and prompts](../../artifacts/town-houses-modern/jodi-lewis/provenance.json)
- [Trailer and rebuilt-house sources and prompts](../../artifacts/town-houses-modern/pam-homes/provenance.json)

## Player check

Restart through SMAPI, visit the five homes, and check the roof joins, windows, seasonal decorations and entrances. Visit the trailer after dark. If Pam's house is already rebuilt in your save, check that version too. Seasonal art follows the game's normal season selection. No save was loaded or written during the automated verification; walking through doors and playing festivals remain hands-on checks.
