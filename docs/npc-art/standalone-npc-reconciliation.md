# Standalone NPC artwork reconciliation

Reviewed 2026-09-05 against current source assets and the captured 0.7.80 runtime package (`artifacts/npc-modern/runtime-audits/0.7.80-20260905-162035-718`). This is an artwork reconciliation, not a claim of full event playback.

There are **190 native occupied sprite cells and 41 portrait expressions** across these thirteen NPCs. Every occupied native cell has replacement art. Three concrete corrections remain: restore Marlon's six native placeholder cells, correct Morris's shrunken punch frame22, and correct Krobus's shrunken gesture frame22. No additional portrait generation is supported by this review.

## Evidence and method

Native files are under `artifacts/npc-modern/originals/{Characters,Portraits}`. Current files are under `src/AbigailModern/assets/<asset>`. Every native/current sheet was viewed side by side, with native on the left and current on the right in [the saved comparisons](standalone-reconciliation-evidence/). Occupancy, solid placeholders, native equality, dimensions and package equality are recorded in [counts.json](standalone-reconciliation-evidence/counts.json). The older work `prepared-*.png` files are four-times enlarged previews; reducing them with nearest-neighbor sampling gives pixels identical to current production for all26 images. All26 current PNG files also match the captured0.7.80 package byte for byte.

Use16x24 cells for Dwarf and Krobus,32x32 cells for Bear, and16x32 for the others. Bear's32x32 drawing size comes from the native Woods `addTemporaryActor Bear 32 32`, not Data/Characters' default size. Portrait cells are64x64. “Placeholder” includes both transparent cells and native solid-color cells. All placeholder pixels match native except Marlon's six cells identified below.

## Sheet reconciliation

| NPC / native asset | Native sprite sheet and occupied roles | Portraits | Finding |
|---|---|---|---|
| Gunther |64x128;7 occupied:0-4,8,12. Four front phases plus directional standing poses. Nine transparent cells preserved. |64x64;1 |Complete occupied-cell coverage; no concrete missing pose or expression found. Do not invent nine walking poses for native blanks. |
| Morris |64x192;24 occupied. Walking0-15, overhead blue paper16-18, lowered-arm19, confrontation/punch20-23. |128x128;4 |Frame22 needs correction; other cells and all expressions present. |
| Marlon |64x128;10 occupied:0-8,12. Native rear/left rows only contain standing cells. |64x64;1 |Native blank9-11 and solid-white13-15 were incorrectly replaced with generated movement. Restore those six cells exactly. The old validation incorrectly lists16 occupied/updated cells and no unused cells. |
| Sandy |64x160;18 occupied. Walking0-15 and hand-to-head poses16/17; solid placeholders18/19 preserved. |128x128;4 |Complete occupied-cell coverage; distinct special poses and expression slots retained. |
| Wizard |64x192;23 occupied. Walking0-15 and seven special/front/rear/side poses16-22; transparent23 preserved. |128x64;2 |Complete occupied-cell coverage; both stern and raised-eyebrow expressions present. No supported redraw request. |
| Dwarf |64x120;20 occupied16x24 cells. Walking0-15 and four front gesture/sparkle phases16-19. |64x64;1 |Complete native layout, gesture row and portrait. |
| Krobus |64x144;24 occupied16x24 cells. Walking0-15 and eight expression/gesture poses16-23. |128x320;9 plus transparent9 |Frame22 needs correction. All nine expressions, including disguise slot8, are populated; blank9 preserved. Separate trenchcoat/parade assets do not change this Base finding. |
| Bouncer |16x32;1 static occupied figure. |64x64;1 |Complete native static scope. No absent walking sheet should be invented. |
| Governor |64x128;12 occupied:0-4,8-14. Standing/front phases, tasting8-11, reactions12-14. Transparent5-7/15 preserved. |128x128;4 |Complete native pose mapping, including green nauseated reaction; this is not a four-direction walking sheet. |
| Henchman |64x96;12 occupied. Two front rows and rear row; no native left-facing row. |128x128;3 plus solid purple3 |Complete native rows and expressions; exact purple placeholder preserved. |
| Forest Bear / Bear |128x160;18 occupied32x32 cells. Walking0-15 and talking16/17; transparent18/19 preserved. |128x128;4 |Complete native bodies and mouth poses. Golden-treat portrait retained. |
| Fizz |16x32;1 static occupied figure. |128x128;4 |Complete native static scope, cup/glasses/hood and fourth two-finger salute retained. |
| Professor Snail / SafariGuy |64x160;20 occupied. Walking0-15, looking-down and specimen-examination poses16-19. |128x128;3 plus transparent3 |Complete occupied-cell coverage, three expressions and exact blank. Use SafariGuy asset names. |

## Required corrections

1. **Marlon placeholders9,10,11,13,14,15.** [Comparison](standalone-reconciliation-evidence/Marlon.png). Native9-11 have zero opaque pixels; native13-15 are solid white512-pixel cells. Current artwork contains generated figures in all six. This is a concrete violation of native placeholder preservation, independent of whether an event reaches those indices. Copy the six native cells only, preserve the ten completed figures and portrait, then correct `work/Marlon/validation.json` and any coverage count that claims16 native poses. Native occupied count is10.

2. **Morris punch frame22.** [Comparison](standalone-reconciliation-evidence/Morris.png). Native frame22 is a full-size14x30 standing punch silhouette with296 opaque pixels. Current frame22 is14x21 with132 opaque pixels, visibly a miniature figure between26-28-pixel-tall fight frames. Its horizontal reach remains wide while the body is compressed. The retained `work/Morris/correction-prompts.json` explicitly requests a standing left-facing forward punch in the third cell of the second special row, not this large size drop. Correct only this pose's source/crop/scale; preserve paper16-18, other fight phases and all portraits. Review a20-23 sequence at native pixels after correction.

3. **Krobus gesture frame22.** [Comparison](standalone-reconciliation-evidence/Krobus.png). Native20-23 all have24-pixel-tall figures; native22 is13x24 with192 opaque pixels. Current22 is14x13 with73 opaque pixels, compared with current20/21/23 heights18/20/19. The native pose retains the same tall body; the prepared cell becomes a miniature horizontal figure. Correct this single lower-row gesture using native22 as pose authority, then review20-23 together. Preserve all portraits and other completed poses.

## Runtime evidence and limits

The captured0.7.80 `loader.txt` explicitly loads these26 assets at lines175-245; its files match current production byte for byte. Thus the three defects above are in the packaged artwork, not stale preview files. The general `portrait-checks.json` reports passing panel attachment and preservation of question/plain-dialogue handling. It does not validate these41 individual expression mappings or the special-pose silhouettes. The current `src/NpcArtAudit/ModEntry.cs` has dedicated audits for Krobus disguise/parade and related actors, but no dedicated Base frame-sequence audits for this thirteen-NPC group. The work validations' `installationVerified:false` and `dialogueDisplayVerified:false` flags are older local-preparation flags and should not be treated as evidence that textures failed to load.

The review read each NPC's work validation and available design notes, including Dwarf/Krobus24-pixel layout, Governor tasting mapping, Henchman native rows, Bear Woods actor size, Fizz static scope, and SafariGuy identity. All occupied native cells differ from native artwork; no lower row silently remains native. Absence of a full saved-game scene test is recorded as a verification limit, **not** a reason to regenerate otherwise-correct art.

No production artwork, registry, audit wiring, runtime package or game state was changed for this reconciliation. Only this document and its read-only comparison evidence were written.
