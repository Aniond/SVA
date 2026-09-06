## Coordinator closure — installed 0.7.97

This section supersedes the pending Crow status in the historical handoff below. NPC Modern 0.7.97 is installed and verified: 285 registered textures and 289 package-file hashes match source. No identified NPC artwork or installation item remains in the established people/talking-creature scope.

Crow closure evidence: artifacts/npc-modern/evidence/isolated-0.7.97.json, installed-0.7.97.json, crow-lost-items-checks.json and crow-native-shop-preview.png. All 32 paired map frames retain 100ms timing and wrap at 3200ms. The native Woods shop opens with the new portrait; inventory references/order, 10000g prices, currency, silent text and native lock-release callback are preserved. Root reviewed the real shop render. The test used a temporary farmer/team and performed no purchase; original state was restored.

All eight corrected portrait files remain in the verified cumulative installation. The earlier coverage reconciliation's Weather Presenter, Qi pilot, Crow and confirmed portrait defects now have explicit installed closure. Original bachelorettes and Abigail's adventure outfit remain included. The separate quest's equip/restore implementation and full scene-by-scene gameplay are outside this artwork completion claim.

Normal installed startup passed after the isolated tests. The prior installation was backed up to artifacts/mod-backups/AbigailModern-20260905-174956. All owned test game processes were stopped. The Blender review file was refreshed with the final art.

The rest of this document preserves the earlier agent handoff and its evidence limits as a dated snapshot.

# Final installation closure addendum

Evidence snapshot: September 5, 2026. **Verified installation is closed through NPC Modern 0.7.96. Crow's planned 0.7.97 release is still pending coordinator closure in this document.** This addendum does not claim that 0.7.97 is installed or that the entire game has been visually played through.

This is a read-only summary of existing evidence and the preceding reconciliation reports. Only this document was written. No artwork, code, registry, generation, build, installation or game process was changed.

## Previously remaining items

Evidence filenames in the table are under `artifacts/npc-modern/evidence/` unless another path is given. Each installed release from 0.7.92 through 0.7.96 records **283 registered textures, 287 package-file hashes, a passing ordinary installed title-screen loader, no farm loaded and no full scene playback**. Isolated evidence is separate from ordinary installed evidence.

| Item | Release and closure | Supporting evidence | Exact limit |
|---|---|---|---|
| Weather Presenter | **Installed and verified 0.7.92.** Five native TV drawings and four portrait expressions; channel 2 preserves native messages and continuation. | `installed-0.7.92.json`, `isolated-0.7.92.json`, `weather-presenter-checks.json`, runtime panel PNGs. | Six recorded cases cover opening, ordinary, island, before-green-rain opening, tomorrow's green rain and current-day static. Dialogue pages, overlay/timing properties, callbacks, turn-off and restoration pass. This is not real input dismissal, timed playback, furnished-TV placement or a farm-save test. Current-day static channel 9999 intentionally has no presenter portrait. |
| Mr. Qi airplane pilot | **Installed and verified 0.7.93.** One pilot pose in the native plane; 154 changed body pixels within a 162-pixel mask. | `installed-0.7.93.json`, `isolated-0.7.93.json`, `qi-plane-pilot-checks.json`, `qi-plane-pilot-runtime-preview.png`. | Actual `QiPlaneEvent.draw` compares 1,578 opaque plane pixels at native scale; constructor is skipped and mail remains unchanged. Full flight, announcement and travel timing are not tested. Plane, cockpit, propeller and surrounding shared art are preserved. No new Qi dialogue/portrait is required for this announcement route. |
| Four fishing-contestant portrait sheets | **Installed and verified 0.7.94.** Contestant 1 cells 2/3 and contestant 2 cell 2 in summer and winter are cleaned; 98 nonzero-alpha fragment pixels removed. | `installed-0.7.94.json`, `isolated-0.7.94.json`, `fishing-winter-checks.json`, `fishing-summer-checks.json`, `work/FishingPortraitCleanup/validation.json`. | 84 winter and 70 summer reaction mappings across 22 actors pass. Other portrait pixels remain exact. Complete festivals and HUD proximity behavior are not verified. |
| Mrs. Raccoon and Marcello portrait sheets | **Installed and verified 0.7.95.** Four Mrs. Raccoon top strips and four Marcello bottom strips removed: 633 and 36 pixels. | `installed-0.7.95.json`, `isolated-0.7.95.json`, `raccoon-shop-checks.json`, `bookseller-interaction-checks.json`, both cleanup folders' `installation-validation.json`. | Native shop portrait loading, Marcello greeting/response keys and buy/trade portrait routes pass. No purchase transaction or complete scene is claimed. All accepted faces, expression order and other pixels remain exact. |
| Alex Base portrait sheet | **Installed and verified 0.7.96.** Detached sleeve scraps in cells 1/3 removed: 38 pixels; all other 49,114 pixels and 12 expression roles remain exact. | `installed-0.7.96.json`, `isolated-0.7.96.json`, `alex-base-checks.json`, `work/AlexPortraitCleanup/validation.json`. | Native Base audit and installed loader pass; not every Alex dialogue/event was played. Other outfits were not changed by this cleanup. |
| Lost Items Crow merchant | **Prepared; planned 0.7.97 remains pending here.** The active merchant was identified by native Woods shop code, not inferred from its filename. | `work/CrowLostItems/HANDOFF.md`, `sprite-static-qa.json`, `portrait-static-qa.json`, `generation-provenance.json`; runtime work belongs to `work/CrowLostItemsRuntime` and coordinator integration. | Do not convert preparation or a staged registry into installed verification. Coordinator must finish the native silent-shop portrait/paired-tile checks and final isolated/ordinary installed evidence before marking 0.7.97 closed. |

Mermaid's separately identified portrait defect was already closed in **0.7.91**: six cleaned expressions, native animation-to-expression mapping and recorded installation. Pointers: `installed-0.7.91.json`, `isolated-0.7.91.json`, `mermaid-checks.json`, and `work/MermaidPortraitCleanup/validation.json`. That release records 282 textures and 286 file hashes. Full Mermaid encounter/HUD placement is a separate limit.

The bounded [portrait cleanup closure](portrait-cleanup-closure.md) independently checks all eight current production PNGs against their accepted cleanup outputs and the 0.7.96 installed hashes. All 16 originally reported damaged cells are clear, Mermaid's old detached strip is absent, and all 54 expression cells remain occupied. The exact eight PNG hashes and per-cell removal counts are recorded there. No confirmed crop defect remains open within that eight-file scope.

## Original bachelorette work remains part of the result

The original requested group was **Abigail, Emily, Haley, Leah, Maru and Penny**. Their historical first installation is recorded in `artifacts/npc-modern/evidence/bachelorettes-loader-verified.txt`: Bachelorettes Modern 0.2.0, six individual successful portrait/walking checks at lines 76–91 and **6/6** at line 92. This is original everyday-walking/portrait evidence, not proof that the early release already contained every special or seasonal pose.

Later NPC Modern work expanded those same characters rather than replacing them with the later visitor/merchant scope. The accumulated **0.7.46** installation entry in [progress.md](progress.md) explicitly includes the seasonal bachelorette full sheets, Emily's completed sheets, Maru hospital, Penny everyday/winter/beach, Leah's additional expression and retained earlier Abigail adventure art. Supporting recorded installation: `artifacts/npc-modern/evidence/installed-0.7.46.json` and `loader-installed-0.7.46.txt` (199/199 textures in that release).

The current [coverage reconciliation](final-coverage-reconciliation.md) checks native inventory and configured appearance paths against production, including those original characters. The latest 0.7.96 installed evidence verifies the cumulative registered package; it does not mean that every older character-specific scenario was replayed at 0.7.96. Keep original and later native-route evidence together when describing their coverage.

## Abigail's adventure outfit and quest boundary

The requested mining/adventure outfit is separately documented in [abigail-adventure-outfit.md](abigail-adventure-outfit.md). It supplies `Characters/Abigail_Adventure` at 64×448 and `Portraits/Abigail_Adventure` with ten 64-pixel expression cells. Forty-two adventure poses plus twelve retained modern formal/wedding/dance poses give **54 occupied slots**, with two exact blank slots.

`artifacts/npc-modern/evidence/abigail-adventure-checks.json` records **Passed=true**, nine native appearance-selection/restoration cases, 54 occupied frames, two blanks, ten portrait slots and restoration of winter/beach attire. Its explicit limits are `FarmLoaded=false` and `QuestEventPlayback=false`. The outfit document records the original **0.7.31** validation and a later **0.7.50** installed-file recheck. The outfit remains included in the cumulative NPC Modern package.

The host-controlled key is `David.AbigailModern/AbigailAdventureOutfit`. The quest must set it when an actual outing starts, remove it when the outing ends/cancels, and refresh Abigail's native appearance. The artwork does not itself decide quest stages, schedules, inventory, friendship, promises, combat or whether a trip begins. The earlier document records that quest-side equip/restore wiring was still needed at its recheck. This addendum performs no new quest inspection and therefore does **not** claim the Living Memory/SolaceWeather quest is now wired or that overnight save/outing playback is complete.

## Crow closure still owned by the coordinator

Prepared Crow assets are a full native **512×32** sheet plus six portraits. Native Woods pairs Front tile `(12,4)` indices 0–31 with Buildings tile `(12,5)` indices 32–63 at **100 ms**: 32 full 16×32 slots, seven distinct drawings, 3,200 ms loop. This is one merchant animation, not 64 independent bodies. Preparation preserves native repetition, blink, eye glow, silhouette, satchel and shadow geometry.

The production release must preserve the native LostItems shop's inventory, prices/currency, mutex and cleanup callback. The merchant is a concrete active NPC even though it has no conventional Data/Characters roster entry and uses map tiles. Portrait attachment must preserve the native silent shop; new conversation is not authorized by preparing expressions.

The coordinator's remaining 0.7.97 closure is to record final accepted source/package hashes, native paired-tile/portrait results, isolated loader evidence, actual installation and a fresh ordinary installed loader. **No 0.7.97 installed success is asserted in this addendum.** Its eventual evidence should supersede this pending row explicitly rather than reinterpret prepared artwork as prior installation proof.

## Overall scope and limits

- [Final coverage reconciliation](final-coverage-reconciliation.md) and [portrait quality reconciliation](portrait-quality-reconciliation.md) are dated snapshots; this addendum and the cleanup closure resolve their listed Weather/Qi/confirmed-portrait findings. Old static `coverage.json` unresolved text is not an accurate list of completed work.
- The people/talking-creature scope includes the established merchants and story creatures. Pets, farm animals and ordinary monsters were not silently added or marked complete. The dormant `???` alias and lower one-color parrot silhouettes have no established active NPC body gap in the inspected routes. Their distinction remains documented in the coverage report.
- The tiny Abigail_Winter curl-edge and Sam_Beach arm-edge candidates were unconfirmed and outside the accepted cleanup set; no new defect or correction was inferred here.
- Passing asset loading, exact hashes, occupied frames and isolated native draw/selector checks prove those specific things. They do not prove every event, animation sequence, expression against every localized line, camera position, rare encounter, shop transaction, input interaction, save cycle or third-party mod combination.
- Current verified evidence through 0.7.96 is predominantly title-screen/isolated-fixture work with no farm loaded. Full scene and quest playback remain explicitly separate. The overall project should not be labelled fully verified on the strength of the texture count alone.

The concrete remaining installation item at this addendum's handoff is Crow 0.7.97. Weather 0.7.92, Qi 0.7.93 and the confirmed portrait cleanup releases 0.7.94–0.7.96 have recorded installed closure within their stated limits.
