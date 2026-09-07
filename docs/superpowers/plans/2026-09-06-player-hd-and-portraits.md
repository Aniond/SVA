# Player HD and generated portraits implementation plan

**Goal:** Apply the approved higher-definition player style while preserving every native customization and animation, and generate a matching reusable player portrait after confirmed creation.

**Approved direction:** artifacts/player-modern/clarity-hd-concept.png. User approved the concept and authorized real Gemini image tests and implementation. Native contracts: artifacts/player-modern/contracts.md, hd-feasibility.md, creator-review.md and portrait-feasibility.md.

**Architecture:** Keep logical native sheets and game coordinates unchanged. A scoped FarmerRenderer adapter draws separate 2× companion textures. Body shading is applied to the actual native recolored body, preserving skin/eye/sleeve/shoe palette logic. An independent asynchronous portrait service captures a composed farmer clone, makes a separate image-model request, and owns per-farm/player cached images for chat consumers.

**Ownership:** root owns body art, manifest/wiring, runtime integration and release. Equipment worker owns seven hair/clothing/accessory/hat companions. Renderer worker owns the scoped adapter and renderer audits. Portrait worker owns the service/provider and tests. Phone remains a separate task; coordinate shared avatar API and merge overlapping Solace wiring deliberately.

- [x] Inventory all 17 Farmer textures and native creator/recolor/layer contracts; preserve two color lookup tables.
- [x] Obtain approved style concepts and prove actual Gemini image output from a native composed test farmer.
- [x] Prove nearest-neighbor 2× mapping preserves native rendered pixels, including creator, body variants, facings, dyes and equipment. Keep an intentionally incorrect mapping as a negative control.
- [x] Prepare eight body companions and grayscale shade maps, plus seven equipment companions with genuine subpixel surface detail. Preserve logical frame/alpha boundaries, dye swatches and reference IDs.
- [x] Implement PlayerHdRenderer with dedicated player-hd.json assets, correct source/origin/scale remapping, recolor-aware cache invalidation and bounded texture lifetime. No global world drawing changes.
- [x] Add a creator-only enlarged view of the complete layered player while retaining native selection controls, blue UI and saved appearance IDs.
- [ ] Implement one-time confirmed-creation portrait queue, secure separate image provider/model, per-identity cache, native fallback, stale-save/cancellation handling and explicit retry behavior. Connect chat and agreed phone portrait consumer API.
- [ ] Test offline failures, cache/restart, save switches, successful creation versus cancellation and actual live generation in disposable profiles. No provider calls from drawing or ordinary outfit changes.
- [ ] Validate authored HD rendering in creator and gameplay, including animation/equipment alignment; run relevant regressions, package/install after passing, preserve real saves/config/audio and report remaining player checks.

Do not claim conceptual images are installed sprites, nearest-neighbor proof is authored HD art, or prototype cache tests are a wired automatic feature. Follow-up source changes remain local unless the user requests Git publication.


Runtime update: authored HD and creator checks passed 101 assertions in isolated session `0.16.0-20260906-171825-096`, alongside 606/606 registered textures and 634 package-file hashes. The first run caught a creator-hook overload error; it was corrected and rerun. All eight skin/eye masks were revised to remove donor speckling before this passing run. Full tool-swing animation, all body variants in gameplay, and actual magnifier mouse input remain manual checks.

Portrait source is wired and has 177 passing core tests, including PNG/JPEG bounds and cache recovery. First native automatic attempt and an explicit retry used an older Release binary and safely retained the fallback; no success is claimed. The corrected Release build is being tested in a fresh isolated farm. Phone integration remains coordinated with its separate task.
