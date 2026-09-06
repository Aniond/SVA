# Shared NPC atlas reconciliation

Read-only source/artwork snapshot: September 5, 2026. This report compares native code and extracted PNGs against the registered source artwork, not a live game run. Joja opening and Krobus parade are excluded because other workers own them. No production files were changed by this audit.

**There are concrete remaining character drawings.** The three TV presenters, world-repair Junimos, construction workers, three rising mermaids, four witch flight cells, and the golden-parrot purchase scene worker below are still native. An atlas marked partial is not itself evidence of missing NPC artwork: most unpatched pixels are intentionally preserved scenery, effects, objects, or unused space.

## Exact missing TV presenters

Coordinates are native `(x,y,width,height)`. Every cell below has **zero changed pixels and zero overlap with registered PatchAreas** at this snapshot.

| Identity and use | Native asset / rectangles | Native code evidence |
| --- | --- | --- |
| Weather presenter, speaking intro | `LooseSprites/Cursors`: `(413,305,42,28)`, `(455,305,42,28)` | `StardewValley.Objects/TV.cs:103`: 2 frames, 150 ms |
| Weather presenter, ordinary forecast | `LooseSprites/Cursors`: `(497,305,42,28)` | `TV.cs:215` |
| Weather presenter, island forecast | `LooseSprites/Cursors2`: `(148,62,42,28)` | `TV.cs:226`, after ordinary forecast when island visited |
| Weather presenter, tomorrow's green rain | `LooseSprites/Cursors_1_6`: `(213,335,43,28)` | `TV.cs:208`; note width **43**, not 42 |
| Livin' Off The Land host | `LooseSprites/Cursors`: `(517,361,42,28)`, `(559,361,42,28)` | `TV.cs:115`, 2 frames, 150 ms |
| Queen of Sauce | `LooseSprites/Cursors`: `(602,361,42,28)`, `(644,361,42,28)` | `TV.cs:121`, 2 frames, 150 ms; same art for rerun |

The weather presenter has five actual body drawings across three atlases. Native weather-season changes affect dialogue/forecast overlays; this inspection found no additional seasonal presenter artwork. Current-day green rain uses three static/noise frames at Cursors_1_6 `(386,334,42,28)` advancing horizontally (`TV.cs:93`), not presenter bodies. Preserve those.

The Fishing channel at Cursors2 `(172,33,42,28)` and `(214,33,42,28)` contains a fish and chart graphic, **no host body** (`TV.cs:134`). The island weather rectangle below it is the weather presenter, not a fishing host. The TV's `???` channel uses a cursed doll object from `Maps/springobjects` (`TV.cs:128`), not the Data/Characters `???` alias.

Visual references, in `artifacts/npc-modern/work/WillySharedScenes/`:

- `tv-weather-native.png`: all three ordinary weather frames.
- `tv-other-hosts-native.png`: the two Livin' frames followed by the two Queen of Sauce frames. The source crop includes the one-pixel separator before Queen of Sauce; crop each frame by the exact table coordinates, not equal quarters of this preview.
- `tv-fishing-native.png`: top fish graphics; lower left island presenter; lower right Willy bait prop.
- `tv-green-rain-native.png`: fifth weather presenter body.

Native TV messages use `Game1.drawObjectDialogue`, not a named NPC portrait dialogue. No native presenter portrait asset exists in the audited TV routes. Parent owns any message-preserving portrait routing; no new dialogue is warranted. Weather presenter artwork is assigned to this worker next; the other two hosts remain for separate assignment.

## Other concrete native character drawings

All rectangles in this section also have zero pixel changes and zero registered-area overlap. Detailed per-cell results are in `shared-body-cell-reconciliation.json`.

| Identity | Native asset and layout | Evidence / meaning |
| --- | --- | --- |
| World-repair Junimos | Cursors `(294 + 16*i,1432,16,16)`, `i=0..3` | `StardewValley.Events/WorldChangeEvent.cs:333,420,496,507,519` and later repair scenes. Four frames, 300 ms. These are visibly Junimo bodies, separate from `Characters/Junimo`. |
| Green construction worker, hammer/tool cycle | Cursors `(288 + 19*i,1349,19,28)`, `i=0..4` | `WorldChangeEvent.cs:307,348,472` |
| Red construction worker, tool cycle | Cursors `(288 + 19*i,1377,19,28)`, `i=0..4` | `WorldChangeEvent.cs:312,353,477,548,668` |
| Construction supervisor | Cursors `(390 + 18*i,1405,18,32)`, `i=0..1` | `WorldChangeEvent.cs:317,358,482,558,626,686` |
| Green worker, sawing | Cursors `(288 + 22*i,1406,22,26)`, `i=0..1` | `WorldChangeEvent.cs:553,611` |
| Red worker carrying lumber | Cursors `(383 + 28*i,1378,28,27)`, `i=0..1` | `WorldChangeEvent.cs:605` |
| Green worker with jackhammer | Cursors `(387 + 17*i,1340,17,37)`, `i=0..1` | `WorldChangeEvent.cs:673` |
| Rising mermaid | temporary_sprites_1 `(67 + 24*i,189,24,53)`, `i=0..2` | `MermaidHouse.cs:449–525`, 3-frame ping-pong, 192 ms, rising overlays during performance. `Submarine.cs:438–460` reuses them with blue tint. These are visibly mermaids, not scenery. |
| Golden-parrot purchase scene worker | Cursors_1_6 `(200 + 28*i,89,28,32)`, `i=0..1` | `WorldChangeEvent.cs:198–208`, case 15 (`goldenParrots`), 700 ms. Visible suited worker; native code does not name an NPC identity. Preserve money bags at `(184,104,14,15)`. |
| Ordinary witch flight | Cursors `(277,1886 + 29*i,34,29)`, `i=0..1` | `StardewValley.Events/WitchEvent.cs:194` |
| Golden witch flight | Cursors2 `(215,262 + 29*i,34,29)`, `i=0..1` | `WitchEvent.cs:190` |

The six construction strips contain 18 drawings. They are used by world-change repair events, independently of the excluded Joja opening. Preserve held tools and lumber, surrounding toolboxes and saw equipment, and timing/facing. The witch is a wordless event character; no invented conversation or portrait is needed merely because her art remains native.

References: `world-repair-native.png`, `cursor-middle-native.png`, `cursors2-full-native.png`, `cursors16-full-native.png`, and `mermaid-seahorse-native.png` in the same work folder. The last filename records a provisional guess made before inspection; the image clearly shows three **mermaids**.

## Coverage already established

The raw pixel comparison checked every registered rectangle, and every listed registered rectangle changed at least one pixel. All eight inspected textures have **zero changes outside registered patch areas**. This confirms isolation, not artistic quality or complete scene playback.

| Asset | Registered NPC coverage confirmed | Covered pixels | Unpatched pixels preserved exactly |
| --- | --- | ---: | ---: |
| Maps/townInterior | Gil's three 32x32 chair poses `(176,656)`, `(176,624)`, `(208,656)` | 3,072 | 553,984 |
| LooseSprites/Cursors | Grandpa ghost; three Welwick TV cells; Hat Mouse shop head; traveling merchant body/blinks; robot/MarILDA strip | 6,186 | 1,582,038 |
| LooseSprites/Cursors2 | Grandpa thumbs-up and Trash Bear region | 5,900 | 76,020 |
| LooseSprites/Cursors_1_6 | Bookseller/Marcello poses and four small overlays; Welwick's two special luck forecasts | 3,440 | 258,704 |
| LooseSprites/temporary_sprites_1 | Desert Trader; seven island mermaids; nine small performance poses; two large mermaids; swimmer and separate tintable hair | 29,292 | 298,388 |
| Maps/island_tilesheet_1 | Island Trader's 18 animated 16x16 body tiles | 4,608 | 527,872 |
| LooseSprites/parrots | 55 body cells in upper 264x120 | 31,680 | 44,352 |
| Characters/Junimo | 48 body cells in upper 128x96 | 12,288 | 4,096 |

Proof details:

- Gil: `evidence/guild-animation-frames.json` proves four map tiles with eleven animation steps and three unique 32x32 source poses. `AdventureGuild.cs` uses an NPC for dialogue, but the visible chair figure is map animation. The older note that only one chair pose was mapped is superseded by that export and current three patches.
- Welwick: `TV.cs:109` uses `(540,305,42,28)` plus the next adjacent cell; the forecast uses `(624,305,42,28)`. Special luck forecasts are Cursors_1_6 `(424,447,42,28)` and `(424,476,42,28)`. All five are registered/changed. This does not cover the other TV hosts.
- Grandpa: `Event.cs:8449` changes the ghost to Cursors2 `(186,265,22,34)` for thumbs-up. The filename `assets/TrashBear/Cursors2.png` does not imply that every patch in that file belongs to Trash Bear.
- Island Trader: `work/IslandTrader/island-trader-tiles.json` maps tile indices 2001–2004, 2032–2037, and 2065–2072. Those 18 tiles are all registered and changed; stall activation tiles 2074–2078 are a separate interaction/prop footprint.
- Mermaid: `IslandSouthEast.cs:269` uses the seven island cells starting `(304,592)`; idle/wave/reward/dance refer to frames 0–6. `MermaidHouse.cs:329` uses the nine small cells at y80; lines 363–373 use swimmers/hair at `(192,0)`/`(208,0)` and large 57x70 art at x0/57. This existing coverage does **not** include the three rising bodies at y189.
- Parrots: `ParrotUpgradePerch.cs:284,1015`, `OverheadParrot.cs:39–41,78,112`, and nested `ParrotPlatform.Parrot` at lines 59,139,152 address the upper body rows. Per-cell checks: all 55 cells differ, with 115–313 changed pixels each. `ParrotPlatform.cs:250` loads a different `LooseSprites/ParrotPlatform` texture for the platform and ropes, not the bird atlas. Lower silhouettes at y264 are preserved; no draw use for them was found in those classes. Do not declare them either missing live bodies or confirmed unused globally without more evidence.
- Junimos: `Junimo.cs:713` draws the main sprite with its runtime tint; `JunimoHarvester.cs` uses the same sheet. All 48 cells differ, with 129–144 changed pixels each. The bottom art is a bundle `(0,96,16,13)` and eight star frames from `(0,109,16,19)`, explicitly used at `Junimo.cs:431,716,720`. They are props, not missing Junimo bodies. The separate Cursors world-repair bodies remain a real gap.

## Aliases and remaining limits

- Data/Characters `???` (`characters.json:4395`) is hidden, cannot socialize, has `SpawnIfMissing=false`, no home, and texture `Monsters\\Shadow_Brute`. No active spawn or named social NPC use was established in this bounded pass. Several shadow monster classes consult friendship under `???`, but that alone does not prove an active NPC body route. Keep this **unresolved/dormant alias**, not completed and not automatically a new NPC art task.
- Winter Mystery's concealed identity uses Krobus sprites (`Town.initiateMagnifyingGlassGet` / `mgThief_speech` and native BusStop event), so a visible `???` label does not prove the Shadow_Brute alias is drawn. `DefaultPhoneHandler.cs:112` also creates a placeholder caller named `???`; TV's cursed-doll channel is yet another unrelated use.
- `ParrotBoy` is Leo's native texture alias, not a second uncovered boy. Willy/Leo fishing-body coverage and preserved rods/bait are documented in `work/WillySharedScenes/ARTWORK-HANDOFF.md`.
- Native full-atlas survey also shows the mystery-box airplane pilot in Cursors_1_6. Its exact draw rectangle and accepted-identity relationship were not resolved in this pass; retain it as a candidate, not a proven covered or missing NPC. Monster/fish/turtle/effect art was not silently promoted to NPC scope.
- No assertion here means that every possible native event was played. Code/geometry/pixel evidence and visual crop review were performed; live routing, animation playback, dialogue and installed package verification remain parent integration work.

## Reproduction

Run from repo root with the bundled Node:

```powershell
& C:/nvm4w/nodejs/node.exe artifacts/npc-modern/work/WillySharedScenes/reconcile-shared.mjs
& C:/nvm4w/nodejs/node.exe artifacts/npc-modern/work/WillySharedScenes/reconcile-body-cells.mjs
```

Outputs are `shared-atlas-pixel-reconciliation.json` and `shared-body-cell-reconciliation.json` in that folder. They read current registry/source artwork; rerunning after integration will intentionally produce different missing-cell results. This report preserves the pre-integration findings.
