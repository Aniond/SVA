# Portrait cleanup closure

Read-only closure review, September 5, 2026. Scope: the seven files and16damaged cells confirmed in [portrait-quality-reconciliation.md](portrait-quality-reconciliation.md), plus the separately corrected Mermaid sheet. No wider portrait scan, new generation, production edit, build or game process was performed.

**All confirmed crop-fragment defects in this scope are closed.** All eight current production PNGs match their accepted cleanup outputs byte for byte. Their SHA256 values also match the corresponding entries in `artifacts/npc-modern/evidence/installed-0.7.96.json`, which records a passing283-texture ordinary installed loader. All54expression cells remain occupied and each sheet retains its expected number of distinct images. No new visible crop fragments were found in the corrected cells.

## Exact current files and accepted outputs

Production paths below are relative to `src/AbigailModern/`. Accepted paths are relative to `artifacts/npc-modern/work/`. Every row passed exact PNG-byte equality and recorded installed-hash equality.

| Production file | Accepted output | Dimensions / distinct cells | Current SHA256 |
|---|---|---|---|
| `assets/Raccoons/MrsRaccoon-portraits.png` | `MrsRaccoonPortraitCleanup/portraits-prepared.png` |128×192 /6 |`2f45332c3770a03c18a4b6200987667293496c6afb9c2a16e32ef7aa14aaf19f` |
| `assets/Marcello/portraits.png` | `MarcelloPortraitCleanup/portraits-prepared.png` |128×192 /6 |`ae109cdad9e70adc51896575345dfb48645bb33635b6aa20cc4e652fa8afee8e` |
| `assets/FishingContestants/winter-portrait-1.png` | `FishingPortraitCleanup/winter-portrait-1.png` |128×192 /6 |`6b7a621614983d90c1cbb18135d5c366b186fe11aa59b58c8f3e0ddf03678b38` |
| `assets/FishingContestants/summer-portrait-1.png` | `FishingPortraitCleanup/summer-portrait-1.png` |128×192 /6 |`6b7a621614983d90c1cbb18135d5c366b186fe11aa59b58c8f3e0ddf03678b38` |
| `assets/FishingContestants/winter-portrait-2.png` | `FishingPortraitCleanup/winter-portrait-2.png` |128×192 /6 |`5344a671644f547ecde7bafce2eeabc5ba600a1ff9696c5178085f7e14469305` |
| `assets/FishingContestants/summer-portrait-2.png` | `FishingPortraitCleanup/summer-portrait-2.png` |128×192 /6 |`5344a671644f547ecde7bafce2eeabc5ba600a1ff9696c5178085f7e14469305` |
| `assets/Alex/portraits.png` | `AlexPortraitCleanup/portraits-prepared.png` |128×384 /12 |`a42d2c97bb5d66de69f7009b3fc6a5c18d1bea18945d031372c04316631f7269` |
| `assets/Mermaid/portraits.png` | `MermaidPortraitCleanup/portraits-prepared.png` |128×192 /6 |`617fcfb2f53e83b7431a33756d56dee826f846f1d5a168cc6c91d3fb7e6a1d7d` |

The two seasonal versions of each fishing contestant remain intentionally byte-identical; distinct-cell counts are per sheet, not a requirement to invent seasonal expression differences.

## Per-defect closure

All coordinates below are local to a64×64cell, numbered left-to-right/top-to-bottom from zero. Fresh comparisons used the frozen pre-cleanup PNGs in the respective work folders and current production RGBA.

| Defect | Fresh result | Preservation |
|---|---|---|
| MrsRaccoon2–5, detached strips across local y0–2 | All four top bands contain zero nonzero-alpha pixels. Removed149,145,173,166pixels respectively:633total. |Only those four cells changed; every face, tail, ear and body pixel, and both top cells, remains exact. |
| Marcello0–3, detached purple strips at y63 | Complete final rows now transparent in these four cells. Removed6,10,10,10pixels:36total. |Every other pixel is exact, including legitimate purple clothing and both bottom expressions. |
| Fishing contestant1, both seasons, cells2/3 at y63 | Both final rows now transparent. Removed21and17nonzero-alpha pixels per sheet:38per season,76total. |Other four cells and every retained pixel remain exact. The original component report counted12visible pixels per strip using alpha>30; the extra removed pixels are faint fringe within the same detached row. |
| Fishing contestant2, both seasons, cell2 at y63 | Final row now transparent. Removed11nonzero-alpha pixels per sheet:22total. |Other five cells and all retained pixels remain exact. The original report's8visible pixels used alpha>30; the extra3are faint fringe of the same strip. |
| AlexBase1/3, detached beige sleeve scraps | Cell1 local(0,54,3,10) and cell3(0,57,2,7) contain zero alpha after cleanup. Removed26and12pixels:38total. |Other ten cells and every accepted face, attached sleeve, hand and football pixel remain exact. |
| Mermaid3, old detached line above the head | Former atlas(65,67,62,1) contains zero nonzero-alpha pixels. All six accepted cleaned expressions match production. |Mermaid used a broader accepted source/background cleanup: all six cells changed. Do not describe this as a strip-only or original-pixel-preserving edit. Six roles and recognizable blue-haired identity are retained; current alpha is binary with no measured bright-magenta candidates. |

The seven files from the original report remove **805pixels across exactly16cells**:633MrsRaccoon +36Marcello +98fishing +38Alex. The other195,803pixels across those seven files remain byte-exact at decoded RGBA level. None of these seven repairs adds opaque pixels or recolors retained art. Mermaid is measured separately because its accepted cleanup re-prepared all six portraits.

All sheets remain at the declared sizes. The seven original-report files retain48occupied expression cells; Mermaid adds6, for54total. Each six-cell sheet has six distinct decoded images and Alex has twelve. Native expression order and accepted added cells were not shuffled or deleted.

## Visual closure

Current production bytes match the accepted files used to render these reviewed previews:

- MrsRaccoon: `MrsRaccoonPortraitCleanup/before-after-cells.png`. All four formerly contaminated top bands are clear, with intact tails/ears and no new detached strip.
- Marcello: `MarcelloPortraitCleanup/preview-light.png` and the previously reviewed dark counterpart. The detached bottom bars are gone; hats, coat edges and expressions remain intact.
- Alex: `AlexPortraitCleanup/preview-light-0.png`, previously reviewed dark counterpart and complete final sheet. Both stray left sleeves are absent; the actual shoulder and complete football/hand remain attached.
- Fishing contestants: `FishingPortraitCleanup/winter-1-light.png`, `winter-1-dark.png`, `winter-2-light.png`, `winter-2-dark.png`. All affected cells were inspected. Byte-identical seasonal files make these previews valid for both seasons. No detached bottom fragment remains.
- Mermaid: `MermaidPortraitCleanup/light-preview.png` and `dark-preview.png`. The top strip is absent, heads/hair are complete within their accepted crops, all six expressions remain readable, and no new detached crop fragment was seen.

The fishing sheets retain their preexisting soft antialiasing. Near-magenta candidates remain at maximum alpha28/255for contestant1 and9/255for contestant2, unchanged outside the corrected row. Light/dark inspection did not reveal a new opaque background leak. This bounded fragment repair does not promise binary alpha for those four sheets and does not promote normal soft edges into another art task. MrsRaccoon, Marcello, Alex and Mermaid have binary alpha.

## Integration evidence and remaining limits

- **0.7.91 Mermaid:** `MermaidPortraitCleanup/validation.json` records installation, six expressions, native animation-expression mapping and runtime verification, with full scene verification false.
- **0.7.94 fishing:** `FishingPortraitCleanup/validation.json` records installation,98removed pixels and84winter/70summer reaction checks; a complete festival scene was not played.
- **0.7.95 MrsRaccoon/Marcello:** each `installation-validation.json` records native shop portrait verification and six expressions. Purchases were not tested. Recorded native checks include MrsRaccoon's shop, Marcello's greeting and buy/trade portrait paths.
- **0.7.96 Alex:** `installed-0.7.96.json`, `isolated-0.7.96.json` and `alex-base-checks.json` are recorded by the coordinator. The ordinary installed loader passed283/283textures; this review independently matched all eight current PNG hashes to that installed evidence.

**No confirmed fragment defect remains open within these eight files.** The original quality report's tiny Abigail_Winter8/9curl-edge candidates and Sam_Beach11arm-edge candidate were explicitly unconfirmed and are outside this closure assignment; they were neither erased nor reclassified here. No broad scan or new visual finding was made for those assets.

This document closes the stated crop defects, not every possible portrait expression/scene behavior or the overall NPC project. Runtime evidence is inherited from the cited coordinator runs; this review itself did not launch the game, transact with shops, load a farm or play complete events. Other active work, including the separately assigned Lost Items Crow merchant, remains outside portrait-cleanup closure.
