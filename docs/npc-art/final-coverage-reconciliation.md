# Final NPC coverage reconciliation

Reviewed September 5, 2026 against the native 1.6.15 inventory, exported Data/Characters, current production files and recorded integration evidence. This is a bounded source/artwork reconciliation, not a claim that every native event has been played.

**One additional active NPC gap was found: the Lost Items merchant in `Characters/Crow`.** The filename must not be classified as an ordinary bird. Native Woods code loads its animated body and opens its shop. The coordinator assigned this newly identified actor to `work/CrowLostItems` during this review. No other concrete uncovered active NPC body or missing native portrait was identified in the inspected standalone inventory and known shared-atlas routes.

The original three known items changed status while this review was running: Mermaid portrait cleanup is installed in **0.7.91**; Weather Presenter is present in **0.7.92 source** with its new portrait route, pending final integration evidence at the snapshot below; QiPlanePilot remains an active separate assignment. Crow is an additional task, not a replacement for those checks. Full completion remains unproven.

## Snapshot and method

- Native inventory: `artifacts/npc-modern/source-inventory.json`, 236 assets: 125 Characters, 101 Portraits and 10 shared textures. Its 226 top-level Characters/Portraits filenames were compared directly with the installed game's Content directory: **zero omitted top-level XNB files**. This is not a recursive inventory of Animals, monsters or every miscellaneous game texture.
- Data/Characters and roster: 48 exported entries. All 71 explicit sprite/portrait appearance references, representing 69 unique paths, resolve to the registry. This includes native winter, employment and other configured appearances; inventory comparison independently covers beach sheets. Shared actors and merchants cannot be inferred solely from this roster.
- Current source: `src/AbigailModern/manifest.json` version **0.7.92**, `artwork.json` SHA256 **6c5a064ae914600b89ae0a3928430f6f1519c23bcbbdf8d97bae07e1e5d7bfdd**, 283 registered textures, including 52 assets added beyond the native inventory. The review began at 282 textures; Weather Presenter integration accounts for the additional portrait. All 283 entries had existing PNGs at their declared dimensions in the final read-only check, with the registry hash unchanged. Weather Presenter registrations and changed shared pixels were also checked separately.
- Installed evidence read: `artifacts/npc-modern/evidence/installed-0.7.91.json` records 282 textures, installation and a passing ordinary title-screen loader. Its scope explicitly says no farm loaded and no full scene playback. Source 0.7.92 is not silently equated with installed 0.7.91.
- A raw RGBA comparison scanned 4,462 regular candidate cells across registered native character/portrait sheets, using 64×64 portraits, 16×32 characters, 16×24 Dwarf/Krobus and 32×32 Bear. Irregular child, Junimo, parade and other special layouts were excluded from that simple grid scan and reconciled through their native mappings and dedicated evidence. Unchanged or newly occupied fragments were investigated rather than automatically labelled missing art.
- Fresh raw comparisons covered all ten registered partial textures, every registered patch rectangle, and the body rectangles listed in the older shared report. **Every registered patch rectangle changes native pixels; all ten textures have zero changed pixels outside their registered areas.** This proves patch isolation and changed coverage, not the visual correctness of every changed pixel.

No production files, registry, generation sources, runtime wiring or game processes were changed by this reconciliation. Only this report was written.

## Concrete remaining work

| Item | Current evidence | Required closure |
|---|---|---|
| **Crow / Lost Items merchant — newly found** | Native `Characters/Crow.png` is 512×32; neither the sprite nor a Crow portrait is registered. Woods explicitly uses it for an active merchant. Assigned to `work/CrowLostItems` after this finding. | Modernize its 32-frame body strip while preserving native frame repetition and 100 ms timing. Inspect the native LostItems shop data, prepare an appropriate missing shop portrait, and connect/test it without changing inventory, prices, mutex or close behavior. |
| Weather Presenter — previously known | Current source has all five updated TV drawings, `Portraits/WeatherPresenter`, and channel-2 handling in `TvHostPortraits.cs`. Source differs from the last installed snapshot read. | Coordinator completes native intro/forecast/green-rain/island routing and installed verification. Treat 0.7.92 source presence separately from successful deployment. |
| Qi plane pilot — previously known | Native route confirmed in `QiPlaneEvent.cs`; work exists in `work/QiPlanePilot`. The plane/pilot rectangle has no registered production patch in this snapshot. | Integrate the approved pilot-only mask, preserve plane/propeller/boxes, and verify the native draw. A second Qi portrait or invented conversation is unnecessary for this announcement route. |
| Mermaid portrait cleanup — closed during review | `installed-0.7.91.json`, updated progress entry and `mermaid-checks.json` record the cleaned six expressions and retained animation mapping. | No additional redraw follows from the old detached-strip finding. Full encounter/HUD placement remains a separate verification limit. |

### Crow: exact native evidence

Native image: `artifacts/npc-modern/originals/Characters/Crow.png`, SHA256 **725fe8e36b151de1c7ec9808f8bfcd4ed542b8c60290555ac1b798de6a60ddee**. It was visually inspected: the strip depicts the bundled, red-eyed merchant. It is not the ordinary ambient crow sprite class.

`artifacts/npc-modern/game-source/StardewValley.Locations/Woods.cs`:

- Lines 347–357: `UpdateLostItemsShopTile` removes invalid inventory items, activates when the LostItemsShop inventory has items, loads `Characters\\Crow`, and registers it as map tilesheet `lostItemsShop`.
- Lines 359–360: Front tile `(12,4)` animates native tiles 0–31, while Buildings tile `(12,5)` animates tiles 32–63; both advance every **100 ms**. Together these are **32 full 16×32 poses** across the 512×32 strip, not 64 independent body drawings. Every full pose is occupied; there are **seven distinct RGBA images**. Repeated phases are intentional native timing.
- Lines 361–363: nearby Buildings action tiles also receive `LostItemsShop`.
- Lines 465–473: `performAction` requests the Lost Items mutex, calls `Utility.TryOpenShopMenu("LostItems", null, playOpenSound: true)` and assigns `OnLostItemsShopClosed` as cleanup.
- Lines 480–495: global inventory and mutex are keyed `LostItemsShop`; cleanup releases the mutex.

`src/AbigailModern/PortraitPanel.cs` currently attaches portraits to IslandTrade, DesertTrade, Traveler, HatMouse, Bookseller and BooksellerTrade, with no LostItems branch. No native `Portraits/Crow` file exists and no replacement Crow portrait is registered. A shop portrait is the applicable missing-portrait work under the established merchant treatment; no dialogue should be invented. The exact native shop greeting/text was **not established** by this pass: the installed `Data/Shops.xnb` exists, but the generic JS unpacker cannot resolve `StardewValley.GameData.Shops.ShopData`. The assigned worker/coordinator should use native data loading or an existing typed export to preserve its actual text and behavior.

## Standalone sheets and expressions

Only five inventoried assets lack registration. Four are intentional exclusions or placeholders; Crow is the exception:

| Asset | Classification and evidence |
|---|---|
| `Characters/Crow` | Active Lost Items merchant; concrete gap above. |
| `Characters/Grandpa` | Native 1×1 placeholder. Visible Grandpa is covered through jojacorps, spectral Cursors/Cursors2 and his portrait. Keep the placeholder. |
| `Portraits/AnsweringMachine` | Object portrait used by `Game1.DrawAnsweringMachineDialogue`; not a character portrait requiring a redesign. |
| `LooseSprites/raccoon_bundle_menu` | Menu skin used by Raccoon.cs; the actual raccoon body and portraits are separate completed assets. |
| `Maps/island_tilesheet_2` | Reviewed scenery/objects, with no established NPC body rectangle. |

Every other inventoried standalone NPC sprite/portrait path is registered. All non-placeholder portrait cells encountered in the raw scan differ from native art; no unchanged native expression was found. Some former portrait blanks are now useful additional expressions, which is authorized enrichment rather than a missing-slot error. An occupied cell does not by itself prove expression fidelity; the prior per-NPC visual reviews remain the evidence for that.

The thirteen-NPC [standalone report](standalone-npc-reconciliation.md) is historical. Its three required corrections are closed in 0.7.82: current Marlon, Morris and Krobus sprite file hashes **exactly match their corrected** `work/<Name>/correction-qa.json` records. Marlon retains ten actual poses and all six exact native placeholders; Morris22 is 28 pixels tall and Krobus22 is 20 pixels tall. Do not repeat those old defects as pending. The report's other ten complete-sheet findings remain applicable; no new native-equal occupied pose was found for that group.

The simple scan's unchanged or newly occupied cells have concrete explanations:

| Cells | Reconciliation |
|---|---|
| Demetrius Base23; Winter/Beach22–23 | Two-color native placeholder patterns. `scripts/prepare-demetrius.mjs:31` explicitly checks the exact native pixels and allows at most two visible colors. A one-color-only placeholder detector misclassifies these as artwork. |
| Pam Base/Winter20–23 | Lower prop continuations of the adjacent modern tall poses, including native partial alpha. `work/PamBase/ARTWORK-HANDOFF.md`. |
| Willy Base/Winter20–23 and37–38 | Lower fishing lines/rods/bobbers, deliberately joined to updated upper bodies. `work/WillyBase/audit-plan.md` and `work/WillySharedScenes/verification.json`. |
| Fishing contestant cells4–7,12–13,17–18,21,23–24 in both sheets | Retained fishing gear/fragments inside mixed 16×64, 32×64 and 32×32 actor rectangles. Native actor mapping in `FishingContestantAudit.cs`; do not apply a regular 16×32 body interpretation. |
| Clint Base/Winter24 newly occupied | A fragment inside the full 32×48 geode pose rectangle. Native GeodeMenu uses these larger rectangles; this is not an independent blank body slot. `work/ClintBase/HANDOFF.md`. |

The older [unchanged-cell review](unchanged-cell-review.md) correctly classifies Pam/Willy/fishing gear but its more-than-three-colors filter omits the two-color Demetrius placeholders. None of these unchanged pixels justifies new character generation.

## Shared atlas reconciliation

The [older shared report](shared-npc-reconciliation.md) records a pre-integration state. Fresh per-cell comparisons now show changed, registered coverage for its active character findings: both other TV hosts, five Weather Presenter drawings, four repair Junimos, eighteen construction poses, three rising mermaids, two golden-parrot purchase-worker poses, and four ordinary/golden witch flight poses. Only its two fishing-channel graphic cells remain unchanged, correctly: they contain a fish/chart and no host body.

| Partial texture | Current patch rectangles | Changed native pixels | Changes outside registered areas |
|---|---:|---:|---:|
| Maps/townInterior | 3 | 1,376 | 0 |
| LooseSprites/Cursors_1_6 | 7 | 4,239 | 0 |
| Characters/KrobusRaven | 3 | 7,521 | 0 |
| LooseSprites/Cursors | 22 | 14,769 | 0 |
| LooseSprites/Cursors2 | 5 | 4,319 | 0 |
| LooseSprites/temporary_sprites_1 | 22 | 20,409 | 0 |
| Maps/island_tilesheet_1 | 18 | 3,046 | 0 |
| LooseSprites/parrots | 1 | 10,999 | 0 |
| Characters/Junimo | 1 | 6,576 | 0 |
| Minigames/jojacorps | 52 | 6,472 | 0 |

These snapshot numbers include source Weather Presenter integration; they are not inferred from the last installed texture count. The Qi pilot remains outside the listed Cursors_1_6 areas. A partial atlas label is expected because room, map, vehicle, UI, effect and prop pixels are preserved.

Specific older concerns now have evidence:

- **Gil:** all three chair poses are patched in townInterior; the map animation export establishes the source poses. Portrait and native interaction checks are present. The old one-pose note in `special-actor-routes.md` is superseded.
- **Welwick:** all five TV drawings and her forecast portrait route are covered, including both special luck forecasts. Her remaining `.source` text in the generated ledger is stale.
- **MarILDA/robot:** ordinary body sheet, Cursors flight strip and `Portraits/robot` are registered; `robot-art-checks.json` and `robot-portrait-checks.json` pass.
- **Clothing therapy:** the full 24-frame costume sheet is registered, and `clothes-therapy-checks.json` passes native temporary actor/offset draws. These reuse existing costume portrait fallbacks; no second invented set is required merely because the shared sprite sheet lacks per-NPC filenames.
- **Sasquatch:** the oddly named native `Characters/asldkfjsquaskutanfsldk` is registered; `sasquatch-checks.json` passes mapped animation fixtures. No speaking route was established.
- **Parade creatures:** KrobusRaven's upper poses and lower pig patch are present; `krobus-parade-checks.json` covers the native pig branch. Plane/banner-only rectangles remain props. SeaMonsterKrobus is a separate registered actor sheet, not a normal monster exclusion.
- **Joja office employees:** 46 employee patches cover 19 static figure instances and 27 native overlay frames; `joja-opening-checks.json` passes 70 draws and 6,534 pixel comparisons, including all 3,650 retained Grandpa edit pixels. This does not mean complete story playback.
- **Previously missing shared bodies:** `queen-of-sauce-checks.json`, `livin-off-the-land-checks.json`, `junimo-repairs-checks.json`, `construction-workers-checks.json`, `mermaid-rising-checks.json`, `golden-parrot-worker-checks.json` and `witch-flight-checks.json` all report passing their recorded bounded checks. Read each file's scope before claiming full events.

## Dormant entries and excluded assets

The independently researched `work/RemainingSharedCandidates/HANDOFF.md`, native excerpts and literal-reference index were read. Its important distinctions remain:

- **Data/Characters `???`:** `SpawnIfMissing=false`, no home, hidden social entry and `CanSocialize=FALSE`, with `Monsters\\Shadow_Brute` texture alias. No active named NPC body route was established. The monster classes' friendship checks do not instantiate that NPC. The active ordinary Shadow Brute monster uses a different space-separated name. Keep the alias dormant/unresolved rather than counting it completed or assigning a new social NPC on this evidence.
- **Winter mystery:** the visible thief uses `Characters/Krobus`, already covered. TV's `???` cursed doll and telephone's anonymous caller are unrelated uses of the same punctuation.
- **Lower parrots:** six one-color components occupy y244–259, while y264–287 is empty. Native consumers found in the earlier 948-file source-reference survey use upper body rows through y96. Their interpretation as shadows is an inference; no active lower-body draw route was found. Preserve them as unused in inspected routes, not six missing speaking birds.
- **Junimo bottom rows:** the bundle and stars are props; the 48 main body cells and four separate world-repair cells are covered.
- **Fishing channel TV art:** fish and chart, no presenter. Do not invent a fishing-channel host.
- **Pets, farm animals, ordinary monsters and ambient animals:** the ledger still records their broader scope as unanswered. This report does not silently add them to, or claim completion of, the people/talking-creature objective. A concrete merchant such as Crow is different because native code establishes its shopkeeper role. Existing included story creatures, parrots, Junimos, raccoons, bears, mermaids and parade actors retain their accepted scope.

## Ledger corrections and verification limits

`coverage.json` is useful for file presence and installed/hash evidence but its text is not an up-to-date task list. `scripts/refresh-npc-coverage.mjs` still emits static unresolved sentences about robot, KrobusRaven, SeaMonsterKrobus, clothing therapy, Gil, Welwick, alternate outfits and top-128-pixel movement patches. Those statements are superseded by completed files and the evidence above. Some added actors also retain old `.source` wording such as “dialogue audit pending”. Empty `owners` arrays are filename-matching results, not uncovered-character proof: shared art, merchant portraits and raccoon names routinely lack matching ownership strings.

After the active Crow, Weather Presenter and Qi pilot work closes, the coordinator should refresh the ledger's actor/ownership classifications and unresolved text, retain the deliberate noncharacter/dormant distinctions, and run final integration checks against a single stable source/package version. Crow must be added explicitly; it currently has no owner in the ledger. This reconciliation did not edit the generator or ledger.

No further sprite/portrait generation is justified solely by an old `RuntimeVerified:false` work flag, a partial shared atlas, native blanks, preserved equipment, or lack of complete scene playback. Conversely, a changed cell or a passing 283-texture loader cannot prove all action poses, expressions, rare scenes or shop portrait routes. The concrete open art work is listed above; full saved-game placement, every event transition and broad mod compatibility remain verification limits rather than newly discovered missing character drawings.
