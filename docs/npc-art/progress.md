## Current installed and verified release: 0.7.97

All identified NPC artwork work is installed: 285 textures, 289 package files. The final NPC, Lost Items crow merchant, has a modern 32-frame sprite strip and six-expression portrait sheet. Its native silent shop displays the neutral expression. Root reviewed the sprite/portrait previews and actual native shop render.

Crow verification passed all paired 100ms frames and wrap, native shop activation, portrait pixels, unchanged inventory/order/prices/currency, and lock release on close. The pre-hook test failed specifically for the absent portrait; the updated build passed. Ordinary installed SMAPI verified 285/285 textures and all 289 file hashes. No farm was loaded or purchase made.

Evidence: isolated-0.7.97.json, installed-0.7.97.json, crow-lost-items-checks.json, crow-native-shop-preview.png. Backup: artifacts/mod-backups/AbigailModern-20260905-174956. Owned test processes stopped. Blender review file refreshed.

Zero identified NPC artwork/installation items remain. Confirmed portrait cleanup is closed in portrait-cleanup-closure.md. Full scene-by-scene gameplay and the separate chat's Abigail quest implementation are not claimed; see final-installation-closure.md.

## Previously installed and verified batch: 0.7.96 - Alex portrait cleanup

Removed38 detached sleeve-fragment pixels from Alex cells1/3; all49,114other pixels and12expression roles remain exact. Root reviewed before/after. AlexBase runtime audit passed and ordinary installed SMAPI verified283/283 textures and287 file hashes. No full event playback claimed.

Evidence: isolated-0.7.96.json, installed-0.7.96.json, alex-base-checks.json. Backup: artifacts/mod-backups/AbigailModern-20260905-173630. Owned processes stopped after identity checks.

One identified NPC remains: Lost Items Crow merchant. Art is ready; native silent shop portrait and paired tile animation verification are pending. Portrait cleanup closure report is being finalized; do not expand scope without concrete evidence.
## Previously installed and verified batch: 0.7.95 - MrsRaccoon and Marcello portrait cleanup

Both six-expression sheets are cleaned and installed. MrsRaccoon removes633 foreign pixels from four top bands; Marcello removes36 detached bottom-row fragments. Every other decoded pixel and all accepted faces/expressions remain unchanged. Root visually reviewed both before/after sheets.

MrsRaccoon's native shop activation and loaded portrait pixels passed. Marcello's native greeting, response keys and buy/trade portraits passed. Purchases and full scenes were not exercised. Fresh isolated and ordinary SMAPI passed283/283 textures and287 package-file hashes. Evidence: isolated-0.7.95.json, installed-0.7.95.json, raccoon-shop-checks.json and bookseller-interaction-checks.json. Backup: artifacts/mod-backups/AbigailModern-20260905-173313. Owned processes stopped after identity checks; no farm loaded.

Lost Items crow art is prepared; native shop integration research and Alex portrait cleanup continue. Full coverage remains incomplete.
## Previously installed and verified batch: 0.7.94 - Fishing portrait crop cleanup

Four seasonal portrait sheets are corrected: contestant1 cells2/3 and contestant2 cell2 in both summer and winter. Crop/padding removes98 nonzero-alpha detached pixels including faint edges; all other pixels remain exact. Root reviewed light/dark prepared previews. Two imagegen attempts were rejected for unwanted backgrounds and face changes; accepted faces were preserved by correcting existing cell crops.

Native reaction checks passed84 winter and70 summer mappings across22 actors. Full festival playback and HUD proximity remain unverified. Fresh isolated and ordinary SMAPI passed283/283 textures and287 package-file hashes. Evidence: isolated-0.7.94.json, installed-0.7.94.json, fishing-winter-checks.json and fishing-summer-checks.json. Backup: artifacts/mod-backups/AbigailModern-20260905-173113. Owned processes stopped after identity checks; no farm loaded.

MrsRaccoon and Marcello cleanup are prepared; Alex cleanup and Lost Items crow merchant continue. Full coverage remains incomplete.
## Previously installed and verified batch: 0.7.93 - Qi airplane pilot

The modern Qi pilot is installed in the native airplane. Root reviewed the native-scale before/after and actual game draw.154 body pixels changed within a162-pixel mask;261,982 outside-mask pixels remain exact to0.7.92. Aircraft, cockpit edge, propeller and other shared artwork are preserved.

QiPlaneEvent.draw passed opaque-plane pixel comparison at native4x scale. Constructor and full flight were skipped to avoid setting sawQiPlane or starting event audio; announcement and travel timing remain untested. Weather presenter regressions passed.

Fresh isolated and ordinary installed SMAPI passed283/283 textures and287 package-file hashes. Evidence: isolated-0.7.93.json, installed-0.7.93.json, qi-plane-pilot-checks.json and runtime preview. Backup: artifacts/mod-backups/AbigailModern-20260905-172543. Owned processes stopped after identity checks; no farm loaded.

Remaining concrete work: Lost Items Crow merchant; portrait crop cleanup for MrsRaccoon, Marcello, AlexBase and four fishing-contestant sheets. Latest coverage and portrait-quality reports document evidence and their earlier snapshots. Full coverage remains incomplete.
## Previously installed and verified batch: 0.7.92 - Weather presenter

Five native TV drawings and four portrait expressions are installed. Channel2 displays welcoming/ordinary/startled portraits while retaining original text, forecast overlays and callbacks. Current-day green-rain channel9999 remains portrait-free. Root reviewed island, tomorrow-greenrain and static runtime panels plus prepared art.

Native opening, ordinary forecast, island forecast, tomorrow-greenrain and current-day static routes passed exact dialogue-page, overlay, timing-property, callback, turn-off and state-restoration checks. All other TV host regressions passed. No real input dismissal, timed animation playback, furnished scene or farm save was tested.

The pre-implementation check failed for the missing portrait as expected. Test TVs then needed Location assigned for stable light IDs; an old Welwick test was updated to require the new weather presenter instead of no panel. Failed test evidence is retained. Final isolated session0.7.92-20260905-172237-810 and ordinary session0.7.92-20260905-172316-162 passed283/283 textures and287 package-file hashes. Backup: artifacts/mod-backups/AbigailModern-20260905-172313. Owned processes stopped after identity checks.

Qi pilot patch is ready; Lost Items crow merchant is newly identified and assigned. Additional portrait crop-contamination fixes are under review. Full coverage remains incomplete.
## Previously installed and verified batch: 0.7.91 - Mermaid portrait cleanup

Six cleaned expressions are installed. The detached line above the thoughtful expression and magenta fringe are removed. All six cells are unique with binary transparency and clear top gutters. Root reviewed light/dark previews and the game-rendered expression panel. Existing island animation-to-expression mapping passed without changing reward state; full encounter playback remains unverified.

Fresh isolated and ordinary installed SMAPI passed282/282 textures and286 package-file hashes. Mermaid rising and Joja regression checks passed. Evidence: isolated-0.7.91.json, installed-0.7.91.json, mermaid-checks.json. Backup: artifacts/mod-backups/AbigailModern-20260905-171810. Owned test processes stopped after identity checks; no farm loaded.

WeatherPresenter audit draft is compiled and registered for its pre-implementation failure check next. This additional audit has not yet run and must not be treated as part of the successful0.7.91 isolated evidence. Weather presenter and Qi pilot artwork remain pending integration.
## Previously installed and verified batch: 0.7.90 - Joja office employees

Installed46 masked patches covering19 static figure instances and27 animation overlays in the opening office. Root reviewed the overview, all three pan previews and a native runtime render.2,822 pixels changed;956,738 outside-mask pixels remain exact to0.7.89, preserving room details and all previous Grandpa artwork.

Actual GrandpaStory.draw checks passed70 timed states across eight animation groups and6,534 overlay pixel comparisons. All3,262 employee-mask pixels and3,650 retained Grandpa pixels match expected loaded art. Constructor, tick and player draw were skipped; full opening-story playback remains unverified. No farm loaded.

Fresh isolated and ordinary installed SMAPI passed282/282 textures and286 package-file hashes. Evidence: isolated-0.7.90.json, installed-0.7.90.json, joja-opening-checks.json and joja-opening-runtime-previews. Backup: artifacts/mod-backups/AbigailModern-20260905-171554. Owned test processes stopped after identity checks.

Weather presenter and cleaned mermaid portraits await integration; Qi airplane pilot artwork is in progress. RemainingSharedCandidates documents two dormant/unused candidates separately from active coverage.
## Previously installed and verified batch: 0.7.89 - Witch travel and casting

Four modern34x29 poses are installed: ordinary and golden variants, each with travel and casting. Before/after artwork was visually reviewed. The two shared atlases preserve1,586,252 and79,948 pixels outside the respective patches exactly to0.7.88.

The actual WitchEvent.draw method passed six pixel-exact draws at native4x scale, selecting0/1/0 for both variants. Native source confirms timed travel/cast/departure selection; the test does not execute visit timing, spell particles, building selection or saved-location changes. No farm loaded.

Fresh isolated and ordinary installed SMAPI passed282/282 textures and286 package-file hashes. Evidence: isolated-0.7.89.json, installed-0.7.89.json, witch-flight-checks.json. Backup: artifacts/mod-backups/AbigailModern-20260905-170857. Owned test processes stopped after identity checks.

Weather presenter and Joja employee runtime drafts are being prepared. Remaining shared-art candidates and mermaid portrait edge quality are under review. Full coverage remains incomplete.
## Previously installed and verified batch: 0.7.88 - Golden-parrot purchase worker

Both28x32 reclining-worker poses are installed. Prepared artwork was visually reviewed. The chair, shadow, money bags and native foot-motion region are preserved;260,352 pixels outside the rectangle remain exact to0.7.87. Twelve native animation transitions passed at700ms with the original stationary position.

An initial test mistakenly supplied layer depth as fade-out speed. This test-only setup was corrected to the native constructor and separate layerDepth field; its failed evidence is retained in session0.7.88-20260905-170454-884. The successful isolated session is0.7.88-20260905-170604-708. Full purchase event and parrot callbacks were not triggered; no farm loaded or purchase made.

Fresh isolated and ordinary installed SMAPI passed282/282 textures and286 package-file hashes. Evidence: isolated-0.7.88.json, installed-0.7.88.json, golden-parrot-worker-checks.json. Backup: artifacts/mod-backups/AbigailModern-20260905-170622. Owned test processes stopped after identity checks.

Witch artwork is reviewed and awaits integration. Weather presenter native-route audit preparation, Joja employees and remaining coverage investigation continue.
## Previously installed and verified batch: 0.7.87 - Rising mermaid

Three modern24x53 rising mermaid poses are installed. Full-color and underwater-tint previews were reviewed; tail phases move left/center/right while the face and raised hands remain aligned. All323,864 pixels outside the patch remain exact to0.7.86. Existing mermaid portraits remain unchanged pending a separate edge-quality review.

Native animation checks passed24 transitions across concert and submarine configurations,192ms ping-pong order, upward movement and retained underwater tint. Full concert timing/placement and the rare submarine encounter were not played through. Fresh isolated and ordinary SMAPI passed282/282 textures and286 package-file hashes. Evidence: isolated-0.7.87.json, installed-0.7.87.json, mermaid-rising-checks.json. Backup: artifacts/mod-backups/AbigailModern-20260905-170255. Owned test processes stopped after identity checks; no farm loaded.

Weather presenter and golden-parrot worker await integration. Workers continue witch flight, Joja opening employees and remaining shared-art coverage investigation.
## Previously installed and verified batch: 0.7.86 - Construction workers

Installed18 native poses across the green worker, red worker and supervisor. Six isolated patches preserve tools, lumber, plans, shadows and all unrelated shared art, including the recently updated repair Junimos. Changed4,290 body pixels;1,577,838 pixels outside the six rectangles remain exact to0.7.85. Final regional artwork was visually reviewed.

Isolated runtime checks passed18 animation routes mapped to six native repair scenes,120 frame transitions and four lumber-motion steps. Full overnight events were not triggered; no farm was loaded. Fresh isolated and ordinary SMAPI passed282/282 textures and286 package-file hashes. Evidence: isolated-0.7.86.json, installed-0.7.86.json, construction-workers-checks.json. Backup: artifacts/mod-backups/AbigailModern-20260905-170042. Owned test processes stopped after identity checks.

Weather presenter and rising mermaid artwork await integration. Workers continue golden-parrot purchase worker, witch flight and Joja opening employees. Full coverage remains incomplete.
## Previously installed and verified batch: 0.7.85 - Repair Junimos

Four modern 16x16 ritual-dance frames are installed in the native world-repair atlas. All four are occupied and unique, with binary transparency; 1,587,200 pixels outside the patch remain exact. Prepared artwork was visually reviewed. Native TemporaryAnimatedSprite playback passed 16 transitions at the original 300ms interval. Full world-change scenes were not triggered and no farm was loaded.

Fresh isolated and ordinary installed SMAPI passed 282/282 textures and 286 package-file hashes. TV host regression checks passed. Evidence: isolated-0.7.85.json, installed-0.7.85.json and junimo-repairs-checks.json. Backup: artifacts/mod-backups/AbigailModern-20260905-165806. Owned test processes stopped after identity checks.

Weather presenter and construction-worker artwork await integration. Workers continue rising mermaids, golden-parrot purchase worker, and Joja opening employees.
## Previously installed and verified batch: 0.7.84 - Livin' Off the Land

Both TV speaking frames and six matching portraits are installed. Native introduction and tip messages show welcoming/explaining portraits. The original no-tip fallback and a real day-one tip both passed the native flow check, including continuation callbacks and TV turn-off. Test date and recipe state were restored. Queen of Sauce regression checks also passed.

Prepared art and both game-rendered message panels were visually reviewed. Window, wallpaper, table and mug remain exact; 1,585,872 pixels outside the shared-atlas patch remain exact. No farm loaded; all calendar dates, green-rain menu selection, other languages and furnished TV placement remain unverified.

Fresh isolated and ordinary installed SMAPI passed 282/282 textures, with 286 package files hash-verified. Backup: artifacts/mod-backups/AbigailModern-20260905-164642. Ordinary launch16:46:45. Evidence: installed-0.7.84.json, isolated-0.7.84.json and livin-off-the-land-checks.json. Initial fallback test retained in session0.7.84-20260905-164607-562; real-tip test in0.7.84-20260905-164719-355. Owned processes stopped after identity checks; Blender refreshed.

Workers continue weather presenter, construction workers and Joja opening employees. Shared-npc-reconciliation.md lists the remaining character art gaps.
## Previously installed and verified batch: 0.7.83 - Queen of Sauce

Two modern TV frames and six matching portrait expressions are installed. The native opening displays the welcoming portrait, and the recipe uses the explaining portrait. Original recipe messages, recipe-learning result, continuation callback and TV turn-off passed the actual native route test. Test recipe state was restored. The missing-portrait test failed before implementation; an audit reflection-overload issue was fixed before the successful run.

Prepared TV/portraits and the game-rendered message panel were visually reviewed. The complete TV frame patch retains the original composition; 1,585,872 pixels outside its shared-atlas rectangle remain exact. No farm loaded; furnished TV placement, all recipe/rerun dates and other languages remain unverified.

Fresh isolated and ordinary installed SMAPI passed 281/281 textures, with 285 package files hash-verified. Backup: artifacts/mod-backups/AbigailModern-20260905-164248. Ordinary launch16:42:51. Evidence: installed-0.7.83.json, isolated-0.7.83.json and queen-of-sauce-checks.json. Owned processes stopped after identity checks; Blender refreshed.

Livin' Off the Land artwork is reviewed and ready for integration. Workers continue weather presenter, construction workers and Joja opening employees; other shared character gaps remain listed in shared-npc-reconciliation.md.
## Previously installed and verified batch: 0.7.82 - Three standalone corrections

Marlon's six native placeholders are restored, preserving his ten genuine body poses. Morris's left-facing punch22 is restored to 28 pixels tall, and Krobus's raised-arm gesture22 to 20 pixels tall, using clean crops from existing accepted generated sources. Prepared previews were visually reviewed. All other character cells and all portraits remain exact.

Fresh isolated and ordinary installed SMAPI both passed 280/280 textures. The 284 package files were hash-verified; production build passed without warnings. These checks prove the corrected pixels load, not full Morris fight or Krobus event playback. Backup: artifacts/mod-backups/AbigailModern-20260905-163611. Ordinary launch at 16:36:14; evidence: installed-0.7.82.json, isolated-0.7.82.json and each character's correction-qa.json. Owned processes stopped after identity checks; Blender refreshed.

Queen of Sauce TV frames and portraits are prepared but not integrated. Workers continue weather presenter, Livin' Off the Land and Joja opening employees. Additional shared character drawings are listed in shared-npc-reconciliation.md, including construction workers, repair Junimos, rising mermaids, witch flights and golden-parrot purchase workers. Full coverage remains incomplete.
## Previously installed and verified batch: 0.7.81 - Parade pig

The purple pig's body on the flying saucer is modernized and installed. Its silhouette, accessories and vehicle remain exact, as do the completed upper parade poses and prop-only planes/banners. Reviewed isolated patch changes 279 body pixels and preserves 25,321 other atlas pixels. An initial merge mismatch was caught before installation and corrected with exact pixel-row copying; the corrected merge matches the reviewed assembly.

Fresh isolated SMAPI passed 280/280 textures, all 14 earlier parade animation frames, and the native high-earnings pig branch. Forty native pig update steps confirmed the fixed frame, leftward motion and periodic bobbing. Temporary earnings were restored. No farm loaded; full timed parade playback remains unverified.

NPC Modern 0.7.81 installed with 284 files hash-verified. Backup: artifacts/mod-backups/AbigailModern-20260905-162518. Ordinary SMAPI launched at 16:25:21 and passed 280/280 textures. Evidence: installed-0.7.81.json, isolated-0.7.81.json and krobus-parade-checks.json. Owned processes stopped after identity checks; Blender refreshed.

Workers continue Joja opening employees and independent reviews of shared/standalone NPC coverage. Full coverage is not yet proven.
## Previously installed and verified batch: 0.7.80 - Leo both outfits

Leo's 50 poses and eight portraits are installed and visually reviewed. Everyday art was cleaned up, yawns and rear gestures corrected, and winter retains the native orange hood. Winter sleep deliberately uses the same hoodless pose as everyday. Six native white placeholders remain exact.

Fresh isolated SMAPI passed 280/280 textures, 64 walking steps, 24 special-animation steps and four route transitions. Across both outfits, 24 actual ShowFrame commands and six actual Animate commands passed. Winter selection and spring restoration passed. No farm loaded; full saved-game events remain unverified.

NPC Modern 0.7.80 installed with 284 files hash-verified. Backup: artifacts/mod-backups/AbigailModern-20260905-162117. Ordinary SMAPI launched at 16:21:20 and passed 280/280 textures. Evidence: installed-0.7.80.json, isolated-0.7.80.json, leo-checks.json and both leo event-checks files. Owned processes stopped after identity checks; Blender refreshed.

Workers continue Joja opening employees, the remaining parade pig and shared-NPC reconciliation. Willy/Leo shared fishing investigation found no missing character art: existing double-height Willy frames already cover the events, while WillyWad and LeoWillyFishing reference props/effects. Detailed evidence is in work/WillySharedScenes/ARTWORK-HANDOFF.md.
## Previously installed and verified batch: 0.7.79 - Sasquatch sightings

All 16 Sasquatch frames are installed and visually reviewed. The native right-facing and front-facing eight-frame rows retain their dimensions and directions, with the bottom 32 transparent rows preserved exactly. Native sightings have no dialogue or portrait requirement.

Fresh isolated SMAPI passed 278/278 textures and 96 native temporary-animation transitions across 90, 100 and 120 ms timing, both rows and both flip settings. No actual random sighting was triggered; full scene playback remains unverified.

NPC Modern 0.7.79 installed with 282 files hash-verified. Backup: artifacts/mod-backups/AbigailModern-20260905-161854. Ordinary SMAPI launched at 16:18:57 and passed 278/278 textures. Evidence: installed-0.7.79.json, isolated-0.7.79.json and sasquatch-checks.json. Owned processes stopped after identity checks; Blender refreshed.

Leo reviewed for integration. Remaining work includes Joja opening employees, parade creatures and shared-NPC coverage reconciliation.
## Previously installed and verified batch: 0.7.78 - Clothing therapy costumes

All 24 costume frames for Shane, Abigail, Lewis, Clint and Robin are installed and visually reviewed. The shared sheet preserves the original event row offsets, raised visor, wink, dejected front and back-facing reactions. Clint's existing costume portraits remain the native fallback.

Fresh isolated SMAPI passed 277/277 textures, actual temporary actor creation, four native row-offset commands, 40 walking draws and six reaction draws. Each rendered offset frame was compared pixel-for-pixel with its expected atlas region. No farm loaded; full clothing-therapy event playback remains unverified.

NPC Modern 0.7.78 installed with 281 files hash-verified. Backup: artifacts/mod-backups/AbigailModern-20260905-161709. Ordinary SMAPI launched at 16:17:12 and passed 277/277 textures. Evidence: installed-0.7.78.json, isolated-0.7.78.json and clothes-therapy-checks.json. Owned processes stopped after identity checks; Blender refreshed.

Sasquatch is reviewed for integration. Workers continue Leo, special Willy/Leo fishing drawings and Joja opening employees. Non-character scenery, menu artwork and placeholders are explicitly identified in non-character-assets.json without counting them as modernized characters.
## Previously installed and verified batch: 0.7.77 - Jas both outfits

Jas's 52 poses and ten portraits are installed and visually reviewed. Jump rope, raised classroom hand, reading, gift reaction, airborne hug and sleep preserve native directions and positions. Four sprite placeholders and two portrait placeholders remain exact.

Fresh isolated SMAPI passed 276/276 textures, 64 walking steps, 48 special-animation steps, six route transitions, eight actual ShowFrame commands, ten actual Animate commands and 72 timed event steps. Winter appearance selection and return to everyday art passed. No farm loaded; complete saved-game events remain unverified.

NPC Modern 0.7.77 installed with 280 files hash-verified. Backup: artifacts/mod-backups/AbigailModern-20260905-161142. Ordinary SMAPI launched at 16:11:45 and passed 276/276 textures. Evidence: installed-0.7.77.json, isolated-0.7.77.json and jas-checks.json. Owned processes stopped after identity checks; Blender refreshed.

Three workers continue Leo, Sasquatch and clothing-therapy costumes. Shared artwork and special actors remain under review.
## Previously installed and verified batch: 0.7.76 - Vincent both outfits

Vincent's 50 poses and eight portraits are installed. Native main striped shirt, shorts and shoes are restored in the modern artwork; winter retains the earflap hat and scarf. Reading, classroom, seated toys and dance frames were visually reviewed. Six white placeholders remain exact.

Fresh isolated SMAPI passed 274/274 textures, 64 walking steps, 256 special-animation steps, eight route transitions, eight actual ShowFrame commands, 24 actual Animate commands, 136 timed event steps and 16 profile-animation steps. No farm loaded; full saved-game events remain unverified.

NPC Modern 0.7.76 installed with 278 files hash-verified. Backup: artifacts/mod-backups/AbigailModern-20260905-160946. Ordinary SMAPI launched at 16:09:48 and passed 274/274 textures. Evidence: installed-0.7.76.json, isolated-0.7.76.json and vincent-checks.json. Owned processes stopped after identity checks; Blender refreshed.

Jas reviewed and queued for integration. Workers continue Leo, Sasquatch and clothing-therapy costumes. Coverage roster now includes MarILDA explicitly.
## Previously installed and verified batch: 0.7.75 - Willy both outfits

Willy's 68 body poses and eight portraits are installed and visually reviewed. Twelve native fishing overlay cells and eight placeholders retain their original pixels. Fishing rods and lines align across the double-height event frames.

Fresh isolated SMAPI passed 272/272 textures, 22 actual ShowFrame commands, 12 actual Animate commands, 80 timed event steps, 64 walking steps, 12 fishing rectangles and two native fishing route transitions. No farm loaded; full saved-game event playback remains unverified.

NPC Modern 0.7.75 installed with 276 files hash-verified. Backup: artifacts/mod-backups/AbigailModern-20260905-160744. Ordinary SMAPI launched at 16:07:47 and passed 272/272 textures. Evidence: installed-0.7.75.json, isolated-0.7.75.json and willy-checks.json. Owned verification processes stopped after identity checks.

Vincent and Jas await integration. Workers continue Leo, Sasquatch and clothing-therapy costumes.
## Previously installed and verified batch: 0.7.74 - MarILDA robot

MarILDA's12main poses,4flight frames and6new portrait expressions are installed. Her portrait decorates the original eight messages in Maru's event, preserving their text and command flow. Three unrelated-message cases remain undecorated. The missing-portrait test failed before implementation and passed afterward. Main art, flight draws and native message panels were visually reviewed.

Fresh isolated SMAPI passed270/270textures,18actual ShowFrame commands,3actual Animate commands,15timed transitions, the native launch pose and4RobotBlastoff.draw frames. Source merge kept1586604unrelated shared-atlas pixels exact. No farm loaded; full heart event and other language playthroughs remain unverified.

NPC Modern0.7.74 installed with274files verified. Backup:artifacts/mod-backups/AbigailModern-20260905-155916. Ordinary SMAPI launched15:59:19 and passed270/270textures in the real Mods folder. Evidence:installed-0.7.74.json,isolated-0.7.74.json,robot-art-checks.json,robot-portrait-checks.json. Owned processes stopped after identity checks; package and Blender refreshed.

Willy and Vincent reviewed for next integration. Workers continue Leo,Jas and clothes-therapy event costumes.

## Previously installed and verified batch: 0.7.73 - Grandpa opening scene

Grandpa's opening body, talking faces and hand gestures are modernized and installed. Two room variants and four overlays retain native placement. Existing spectral sprites and portraits were reviewed again. The1x1character placeholder remains native. Source verification preserves956346pixels outside the body/overlay masks; unrelated room and office artwork remain unchanged.

Fresh isolated SMAPI passed268/268textures and six states through the actual GrandpaStory.draw method, comparing43350scene pixels. Temporary-content loading and native placeholder checks passed. The constructor was skipped to avoid moving a player/loading a farmhouse. No farm loaded; complete story playback remains unverified.

NPC Modern0.7.73 installed with272files verified. Backup:artifacts/mod-backups/AbigailModern-20260905-154104. Ordinary SMAPI launched15:41:06 and passed268/268textures in the real Mods folder. Evidence:installed-0.7.73.json,isolated-0.7.73.json,grandpa-story-checks.json. Owned processes stopped after identity checks; package and Blender refreshed.

Workers continue Willy,Jas,Vincent. Shared NPC sheets and remaining actors are being reconciled separately from UI, placeholders and effects.

## Previously installed and verified batch: 0.7.72 - Linus both outfits

Linus's54poses and12portraits are installed. Winter hood, bathing, rummaging, parrot call and legacy alternate cells retain native layouts. Two transparent placeholders remain exact. Accepted main walking0–3 and portraits0/2/3 preserved; other walking phases and bathing expressions corrected. Prepared and game-rendered previews reviewed.

Fresh isolated SMAPI passed267/267textures,64walking steps,12sleep steps,2routes,54actual ShowFrame commands,10actual Animate commands and40timed rummaging steps. Seasonal selection and everyday restoration passed. No farm loaded; full saved-game event playback remains unverified.

NPC Modern0.7.72 installed with271files verified. Backup:artifacts/mod-backups/AbigailModern-20260905-153903. Ordinary SMAPI launched15:39:05 and passed267/267textures in the real Mods folder. Evidence:installed-0.7.72.json,isolated-0.7.72.json,linus-checks.json. Owned processes stopped after identity checks; package and Blender refreshed.

Workers continue Willy,Jas,Vincent. Grandpa opening-scene prepared previews reviewed; actual drawing checks are next.

## Previously installed and verified batch: 0.7.71 - Kent both outfits

Kent's 38 poses and 12 portraits are installed. Corrected side strides, grief, cheering and left-facing sleep retain native positions. Older main portrait4/5 had wrong expressions;4 now uses the existing shocked portrait and5 has closed troubled eyes. Main0–3 preserved exactly. Both orange placeholders remain exact. Prepared and game-rendered previews reviewed.

Fresh isolated SMAPI passed265/265 textures,64walking steps,12sleep steps,2routes,4actual ShowFrame commands,2actual Animate commands and4timed event steps. Grief profile metadata and seasonal restoration passed. No farm loaded; full saved-game scenes remain unverified.

NPC Modern0.7.71 installed with269files verified. Backup:artifacts/mod-backups/AbigailModern-20260905-153359. First ordinary launch exited before content loading; incomplete log retained. A fresh ordinary launch at15:35:42 passed265/265textures in the real Mods folder. Evidence:installed-0.7.71.json,isolated-0.7.71.json,kent-checks.json. Owned process stopped after identity check.

Workers continue Willy,Jas,Vincent. Linus reviewed and ready for integration. Grandpa opening scene prepared and under review.

## Previously installed and verified batch: 0.7.70 - Marnie all three outfits

Marnie's 88 poses and 15 portraits are installed. Drumming, drinking, jump rope, three chicks, sleep and event reactions retain their native frame positions; three portrait placeholders remain exact. Prepared and game-rendered previews reviewed.

Fresh isolated SMAPI passed 263/263 textures, 96 walking steps, 212 special steps, six routes, eight actual event ShowFrame commands and 16 chick profile animation steps. The test interval was corrected to match the native strict greater-than timer; the earlier failed test and log are retained. All outfit changes and everyday restoration passed. No farm loaded; full saved-game scene playback remains unverified.

NPC Modern 0.7.70 installed with 267 files verified. Backup: artifacts/mod-backups/AbigailModern-20260905-152833. Ordinary SMAPI launched September 5 at 15:28:35 and passed 263/263 textures in the real Mods folder. Evidence: installed-0.7.70.json, isolated-0.7.70.json and marnie-checks.json. Both owned test processes stopped after identity checks.

Workers continue Willy, Linus and Vincent. Kent's two incorrectly ordered older portraits are being corrected after review. Grandpa opening-scene artwork is in progress.

## Previously installed and verified batch: 0.7.69 - Pam all three outfits

Pam artwork is installed:82semantic poses and15portraits. Eight original long-prop continuation cells retain exact native partial alpha; six white sprite placeholders and three cream portrait placeholders are exact. Main accepted portraits preserved. Winter sleep30/31 reuses main on proven native equivalence. Walking opposite-arm corrections, crying, folding arms, drinking, sleep and omelet poses complete. Prepared and game-rendered previews reviewed.

Fresh isolated SMAPI passed259/259textures,82poses,15portraits,8tall atlas rectangles,96walking steps,100special steps,4routes,28actual ShowFrame commands,22actual Animate commands and72timed event-animation steps. All outfits/restoration passed; Clint checks passed again. No farm loaded; full saved-game scenes and embedded bus driver art remain separate verification scope.

NPC Modern0.7.69 installed with263files verified. Backup:artifacts/mod-backups/AbigailModern-20260905-152249. Ordinary SMAPI launched15:22:52 September5,2026 passed259/259textures in real Mods folder. Evidence:installed-0.7.69.json,isolated-0.7.69.json,pam-checks.json. Owned processes stopped after identity checks; package and Blender refreshed.

Workers continue Willy,Linus,Vincent. Marnie reviewed and Kent handed off for review. Coordinator has begun Grandpa opening-scene asset inventory. Remaining NPCs and full saved-game scene checks stay in scope.

## Previously installed and verified batch: 0.7.68 - Clint all three outfits

Clint artwork is installed:70semantic poses,20portraits and4exact transparent native cells. Full32x32hammer and32x48geode poses retain native rectangles; winter work-clothes exceptions and everyday geode menu asset are retained. Winter soda portrait5 was corrected to smile; other7cells remained exact. Prepared and game-rendered previews reviewed.

Fresh isolated SMAPI passed255/255textures,70poses,20portraits,96walking steps,64native hammer steps,4sleep steps,4routes,10timed steps through actual GeodeMenu.startGeodeCrack and2actual advertisement ShowFrame commands. A disposable player supplied the geode test and was restored; no farm was loaded. Earlier audit-only failures (intro-to-loop index, disposable money initialization and initial balance assumption) were corrected against native source; failed sessions/logs retained. Full saved-game scenes remain unverified.

NPC Modern0.7.68 installed with259files verified. Backup:artifacts/mod-backups/AbigailModern-20260905-151945. Ordinary SMAPI launched15:19:47 September5,2026 passed255/255textures in real Mods folder. Evidence:installed-0.7.68.json,isolated-0.7.68.json,clint-checks.json. Owned processes stopped after identity checks; package and Blender refreshed.

Workers continue Willy,Linus,Kent. Pam and Marnie are reviewed and awaiting runtime integration. Remaining NPCs and full saved-game scene checks stay in scope.

## Previously installed and verified batch: 0.7.67 - Lewis all three outfits

Lewis artwork is installed: 74 occupied poses, six everyday and six winter portraits, and six exact native white/transparent placeholder cells. Beach retains the native everyday portrait fallback. Gardening, door reach, glass drinking, reactions, sleep and omelet retain native frame positions. Accepted everyday portraits remain unchanged. Prepared and game-rendered previews were reviewed.

Fresh isolated SMAPI passed 251/251 textures, 74 poses, 18 selected portrait slots including beach fallback, 96 walking steps, 276 special steps, six native routes, 32 actual ShowFrame commands and two actual Animate commands. All outfits and restoration passed. No farm was loaded; full saved-game scenes remain unverified.

NPC Modern 0.7.67 is installed with all 255 files verified. Backup: artifacts/mod-backups/AbigailModern-20260905-151201. Ordinary SMAPI launched at 15:12:04 on September5,2026 passed 251/251 textures from the real Mods folder. Evidence: installed-0.7.67.json, isolated-0.7.67.json and lewis-checks.json. Owned processes stopped after identity checks. Package and Blender refreshed.

Workers continue Marnie, Linus and Kent. Clint winter portrait5 correction is reviewed; coordinator is preparing Clint runtime integration and Pam final sheets. Remaining NPCs and full saved-game scene checks remain in scope.

## Previously installed and verified batch: 0.7.66 - Jodi all three outfits

Jodi artwork is installed: 67 occupied poses, 14 portraits, five exact native brown sprite placeholders and two exact blue portrait placeholders. Everyday walking was re-cropped through clean gutters to remove detached pixels, with opposing rear and side phases corrected; lower poses, accepted main portraits and seasonal art remained unchanged during that correction. Dishes, exercise, sitting and sleep retain native frame geometry. Prepared and game-rendered previews were reviewed.

Fresh isolated SMAPI passed 248/248 textures, 67 poses, 14 portraits, 96 walking steps, 196 special-animation steps and eight native route transitions. Winter/beach selection and everyday restoration passed. No farm was loaded; full saved-game event scenes remain unverified.

NPC Modern 0.7.66 is installed with all 252 files verified. Backup: artifacts/mod-backups/AbigailModern-20260905-150043. Ordinary SMAPI launched at 15:00:45 on September5,2026 passed 248/248 textures from the real Mods folder. Evidence: installed-0.7.66.json, isolated-0.7.66.json and jodi-checks.json. Owned verification processes were stopped after identity checks. Package and Blender are refreshed.

Workers continue Marnie, Lewis and Clint. Coordinator is preparing Pam. Remaining NPCs and full saved-game scene checks remain in scope.

## Previously installed and verified batch: 0.7.65 - Gus everyday and winter

Gus artwork is installed: 54 occupied poses, eight portraits and two preserved native blank cells. Violin, glass polishing, sitting, cheering, marinara pot and sleep follow native frame positions. Accepted everyday portraits are preserved. Prepared and game-rendered previews were reviewed.

Fresh isolated SMAPI passed 244/244 textures, 54 poses, eight portraits, 64 walking steps, 64 special steps, six routes, 12 actual event Animate commands and 36 timed violin/cooking/cheer steps. Winter switching and everyday restoration passed. Robin checks passed again. No farm was loaded; full saved-game event scenes remain unverified.

NPC Modern 0.7.65 is installed with all 248 files verified. Backup: artifacts/mod-backups/AbigailModern-20260905-145826. Ordinary SMAPI launched at 14:58:29 on September5,2026 passed 244/244 textures from the real Mods folder. Evidence: installed-0.7.65.json, isolated-0.7.65.json and gus-checks.json. Owned verification processes were stopped after identity checks. Package and Blender are refreshed.

Workers continue Marnie, Lewis and Clint. Jodi walking correction is reviewed and ready for integration; coordinator has started Pam. Remaining NPCs and full saved-game scene checks remain in scope.

## Previously installed and verified batch: 0.7.64 - Pierre all three outfits

Pierre artwork is installed: 66 occupied poses, 16 portraits and six native brown placeholder cells. Accepted main portraits0-4 remain unchanged; slot5 adds a thoughtful/questioning expression. Winter and beach costumes, overhead gesture, boxing actions, sunglasses and sleep follow native sheet geometry. Prepared and game-rendered previews were reviewed.

Fresh isolated SMAPI passed 242/242 textures, 66 poses, 16 portraits, 96 walking steps, 12 sleep steps, two routes, 40 actual ShowFrame commands, ten actual Animate commands and 40 timed boxing-guard steps. All three appearances and restoration passed. An earlier run exposed a Robin audit-only assumption: entering hammer frame27 immediately calls a native random rest selection which may choose frame23. The test now accepts both legal states and the fresh Robin checks passed. Failed session is retained. No farm was loaded; full Community Center fight placement and other saved-game scenes remain unverified.

NPC Modern 0.7.64 is installed with all 246 files verified. Backup: artifacts/mod-backups/AbigailModern-20260905-145350. Ordinary SMAPI launched at 14:53:52 on September5,2026 passed 242/242 textures from the real Mods folder. Evidence: installed-0.7.64.json, isolated-0.7.64.json, pierre-checks.json and fresh robin-checks.json. Owned verification processes were stopped after identity checks. Package and Blender are refreshed.

Workers continue Marnie, Lewis and Clint. Coordinator has Gus ready for runtime integration and Jodi prepared with a narrow Base walking cleanup requested. Remaining NPCs and full saved-game scene checks remain in scope.

## Previously installed and verified batch: 0.7.63 - Shane all four outfits

Shane artwork is installed: 137 occupied poses, 48 portraits and 19 exact native placeholder/blank cells across everyday, winter, beach and Joja outfits. Accepted everyday portraits0-10 remain unchanged. Winter worried-with-chicken portrait9 was corrected after coordinator review; the other11 winter cells remain byte-identical. Winter sleep has bare feet; everyday sleep retains shoes. Base/Winter alternate outfits and Joja casual portrait exceptions follow native assets. Prepared and game-rendered previews were reviewed.

Fresh isolated SMAPI passed 238/238 registered textures, 137 poses, 48 portrait slots, 128 walking steps, 348 special-animation steps, six routes, 82 actual ShowFrame commands and 300 native event frame references. Work attire wins over winter in Joja, and everyday restoration passed. An initial audit assertion incorrectly required animation-only frames32/33 to appear in ShowFrame commands; native event evidence corrected that assertion. Failed audit session is retained. No farm was loaded; full saved-game scenes and separately drawn event overlays remain unverified.

NPC Modern 0.7.63 is installed with all 242 files verified. Backup: artifacts/mod-backups/AbigailModern-20260905-144700. Ordinary SMAPI launched at 14:47:02 on September5,2026 from the real Mods folder passed 238/238 textures. Evidence: installed-0.7.63.json, isolated-0.7.63.json and versioned logs/previews. Owned verification processes were stopped after identity checks. Package and Blender are refreshed.

All twelve marriage candidates now have their main, seasonal and native work artwork completed and installed. This does not establish full saved-game event playback. Workers continue Jodi, Pierre and Clint; coordinator has started Gus. Remaining NPCs and full scene checks remain in scope.

## Previously installed and verified batch: 0.7.62 - Robin all three outfits

Robin artwork is installed: 92 occupied poses, 24 portraits and four exact native brown placeholder cells across everyday, winter and beach outfits. Carpentry, dance, exercise, mug, laughter, sleep and omelet poses follow native geometry. Native portrait expressions0-6 and raised-palm gesture are retained; slot7 is filled with surprise. Prepared and game-rendered previews were reviewed.

Fresh isolated SMAPI at 14:41:36 local on September 5, 2026 passed 232/232 registered textures, 92 poses, 24 portrait slots, 96 walking steps, 76 special-animation steps, six native routes, 20 actual ShowFrame commands, six timed hammer steps and 16 native variable-rest callbacks. All three appearances and everyday restoration passed. No farm was loaded; full construction placement and saved-game scenes remain unverified.

NPC Modern 0.7.62 is installed with all 236 files verified. Backup: artifacts/mod-backups/AbigailModern-20260905-144214. Ordinary SMAPI launched at 14:42:16 from the real Mods folder passed 232/232 textures. Evidence: installed-0.7.62.json, isolated-0.7.62.json and versioned logs/previews. Both owned verification processes were stopped after identity checks. Package and Blender are refreshed.

Workers are on Jodi, Pierre and Clint; Shane Winter portrait9 is receiving a narrow expression correction before integration. Remaining NPCs and full saved-game scene checks remain in scope.

## Previously installed and verified batch: 0.7.61 - Demetrius all three outfits

Demetrius artwork is installed: 88 occupied poses across everyday, winter and beach, eight exact native placeholder cells, eight everyday and eight winter portraits. Beach retains the native everyday portrait fallback. Accepted everyday portraits are unchanged; winter adds a thoughtful expression in slot7. Ten beach special poses reuse Base only after exact native matching. Reading, notes, dancing, sleep, tomato groceries and everyday hazmat suit are complete.

Fresh isolated SMAPI on September 5, 2026 passed 228/228 registered textures, 88 poses, 24 selected portrait slots across outfits, 96 walking steps, 402 special-animation steps, 13 native route transitions and 20 actual ShowFrame commands from native ScienceHouse events. All three appearances and restoration passed. Prepared and game-rendered previews were reviewed. No farm was loaded; full saved-game scenes remain unverified.

NPC Modern 0.7.61 is installed with all 232 files verified. Backup: artifacts/mod-backups/AbigailModern-20260905-143951. Ordinary SMAPI launched at 14:40:16 from the real Mods folder passed 228/228 textures. Evidence: installed-0.7.61.json, isolated-0.7.61.json and versioned logs/previews. Both owned verification processes were stopped after identity checks. Package and Blender are refreshed.

Workers are on Jodi, Pierre and Clint. Robin and Shane artwork is prepared for coordinator integration. Remaining NPCs and full saved-game scene checks remain in scope.

## Previously installed and verified batch: 0.7.60 - George everyday and winter

George artwork is installed: 44 wheelchair poses and eight portraits, with four exact native placeholder cells. Accepted complete everyday artwork is preserved. Winter retains the brown cap, charcoal coat, olive turtleneck, green trousers and blue wheelchair. Sleep frames preserve the native moving Z glyphs. Prepared and game-rendered previews were reviewed.

Fresh isolated SMAPI at 14:36:19 local on September 5, 2026 passed 225/225 registered textures, 44 poses, eight portrait slots, 64 movement steps, 12 sleep-animation steps and two native route transitions. Winter selection and everyday restoration passed. No farm was loaded; full saved-game scenes remain unverified.

NPC Modern 0.7.60 is installed with all 229 files verified. Backup: artifacts/mod-backups/AbigailModern-20260905-143637. Ordinary SMAPI launched at 14:36:41 from the real Mods folder passed 225/225 textures. Evidence: installed-0.7.60.json, isolated-0.7.60.json and versioned logs/previews. Both owned verification processes were stopped after identity checks. Package and Blender are refreshed.

Workers are on Jodi, Pierre and Shane, with Clint after Shane. Robin is prepared for coordinator review. Coordinator is finishing Demetrius. Remaining NPCs and full saved-game scene checks remain in scope.

## Previously installed and verified batch: 0.7.59 - Caroline all three outfits

Caroline everyday, winter and beach artwork is installed: 66 occupied poses, 12 portraits and six exact native placeholder cells. Accepted everyday portraits are preserved. Exercise, reading, tea, sleep and meditation poses were visually reviewed in prepared and game-rendered sheets.

Fresh isolated SMAPI at 14:29:20 local on September 5, 2026 passed 223/223 registered textures, 66 poses, 12 portrait slots, 96 walking steps, 64 special-animation steps, six native route transitions and 20 actual tea-event ShowFrame commands. All three outfit selections and everyday restoration passed. No farm was loaded; full tea-event playback remains unverified.

NPC Modern 0.7.59 is installed with all 227 files verified. Backup: artifacts/mod-backups/AbigailModern-20260905-143059. Ordinary SMAPI launched at 14:31:18 from the real Mods folder passed 223/223 textures. Evidence: installed-0.7.59.json, isolated-0.7.59.json and versioned logs/previews. Both owned verification processes were stopped after identity checks. Package and Blender are refreshed.

Workers are on Jodi, Robin and Shane, with Clint after Shane. George is prepared for coordinator review. Coordinator is finishing Demetrius. The remaining NPCs and full saved-game scene checks remain in scope.

## Previously installed and verified batch: 0.7.58 - Sebastian all three outfits

Sebastian everyday, winter and beach artwork is installed. Everyday and winter each have 57 modern poses and ten portraits; beach has 24 poses and eight portraits. All six native brown placeholder cells are preserved. Winter retains the scarf and native hoodie exceptions for umbrella and sleep frames. Workshop pieces retain the native cropped torso shape. The accepted everyday portraits are unchanged. Source prompts, mapping, QA and handoff are in work/SebastianBase, SebastianWinter and SebastianBeach.

Fresh isolated SMAPI at 14:23:18 local on September 5, 2026 passed 219/219 textures. Sebastian passed all three outfits, 138 poses, 28 portrait slots, 96 walking steps, 620 special-animation steps, ten native route transitions, 34 actual ShowFrame commands and 268 native event frame references. Prepared and game-rendered previews were reviewed. Seasonal and beach restoration passed. No farm was loaded; full garage overlay and saved-game event scenes remain unverified.

NPC Modern 0.7.58 is installed with all 223 files verified. The installer backed up 0.7.57 to artifacts/mod-backups/AbigailModern-20260905-142429. Fresh ordinary SMAPI at 14:24:50 loaded 219/219 textures from the real Mods folder, with installed files matching isolated package hashes. Evidence: installed-0.7.58.json, isolated-0.7.58.json, versioned logs and sebastian-checks/runtime-preview files. Production build has zero warnings/errors; audit build retains the unrelated Krobus warning. Package and Blender are refreshed. Only owned verification processes were stopped after identity checks.

Worker queues are now Caroline completed -> George, Sebastian completed -> Robin, and Shane portraits -> Clint. Coordinator has Caroline handoff for review and is generating Demetrius sprites. Of the twelve marriage candidates, only Shane still has unfinished artwork. Full saved-game scene checks remain a separate requirement for overall completion.

## Previously installed and verified batch: 0.7.57 - Evelyn everyday and winter

Evelyn has 21 modern poses and four portraits in each of her everyday and winter outfits. Three pale native placeholder cells per outfit remain byte-identical. Everyday sprites have clean cell boundaries, opposite side strides, distinct raised gardening tools, a seated pose and a baked pie. The accepted four everyday portraits are unchanged. Winter retains her mauve patterned headscarf, ochre coat, silver fringe and elderly facial features. Generated source images, prompts, corrections and export mappings are saved in work/EvelynBase and work/EvelynWinter.

Fresh isolated SMAPI at 14:18:23 local on September 5, 2026 passed 215/215 textures. Evelyn passed both outfits, 42 poses, eight dialogue portrait slots, six exact native placeholders, 64 walking steps, 108 special-animation steps, four route transitions, ten actual ShowFrame commands, the native community-center gardening command and both seasonal transitions. Prepared and game-rendered previews were reviewed. No farm was loaded; full saved-game scenes remain unverified.

NPC Modern 0.7.57 is installed with all 219 files verified. The guarded installer backed up 0.7.56 to artifacts/mod-backups/AbigailModern-20260905-141905. A fresh ordinary launch passed 215/215 textures from the real Mods folder; installed files match the isolated package. Evidence: installed-0.7.57.json, isolated-0.7.57.json, versioned logs, evelyn-checks.json and both runtime previews. Production build has zero warnings/errors; audit build retains the existing unrelated Krobus warning. Package and Blender are refreshed. Only owned verification processes were stopped after identity checks.

The team continues: Caroline -> George, Sebastian -> Robin, Shane -> Clint. Coordinator finished Evelyn and has inventoried Demetrius; Sebastian artwork has just arrived for review. Remaining NPC variants, special poses, expressions and full scene checks remain in scope.

## Previously installed and verified batch: 0.7.56 - Sam everyday and Joja

Sam now has complete modern everyday, winter, beach and Joja sprite and portrait sheets. Everyday and Joja each contain55 occupied poses plus the exact white placeholder55 and12 portraits. Everyday adds the native sad side glance and folded arms while preserving the other ten portrait cells. Joja has22 unique sprites and nine unique portraits, with33 sprites and three portraits reused only after exact native matching. Winter work poses40-42 now include blue headphones and the correct light-blue work uniform. Prepared and game-rendered previews were reviewed.

Fresh isolated SMAPI at14:13:08 local on September5,2026 passed213/213 textures. Sam everyday passed55 poses,12 portraits,96 walking steps across outfits,364 timed special-animation steps,six route transitions,50 native event frame references,20 actual ShowFrame commands and five skate callbacks. Joja passed55 poses,12 portraits,32 walking steps,20 timed work steps,the work-route transition,and work outfit selection in Joja and the museum during spring and winter. Winter and beach checks passed again. An initial audit-only museum location setup error was traced to NetLocationRef resolving an unregistered temporary location; setting the game location before assigning the NPC location fixed it. Failed runs remain in runtime-audits for traceability. No saved farm or full event scene was played.

NPC Modern0.7.56 is installed. The guarded installer backed up0.7.55 to artifacts/mod-backups/AbigailModern-20260905-141347 and verified217 files. Fresh ordinary SMAPI at14:14:14 loaded213/213 textures from the real Mods folder, with all217 installed files matching the isolated package hashes. Evidence: isolated-0.7.56.json, installed-0.7.56.json, versioned loader logs, sam-base and sam-jojamart checks/previews. Only owned verification processes were stopped after identity checks. Production build has zero warnings/errors; audit build retains the existing Krobus warning. Package and Blender are refreshed.

Coverage is213 registered and213 verified installed assets across66 roster entries and284 records. Pending/partial texture records are not unfinished-character counts. Sam artwork is complete; Sebastian and Shane remain among the marriage candidates. Full saved-game scene validation remains separate.

The user authorized one worker per NPC, sprites then portraits, and keeping as many workers active as available. Three workers plus the coordinator fill the four-agent platform limit. Worker queues: Sam completed -> Caroline; Sebastian -> Robin; Shane -> Clint. Coordinator owns Evelyn and shared integration/runtime/install. Remaining NPC variants, special poses, expressions and full scene checks remain in scope.

## Previously installed and verified batch: 0.7.55 - Sam beach

Sam beach now has21 modern poses and twelve portraits, including curious6 and thoughtful11 in previously blank portrait slots. Three native blank sprite cells21–23 remain byte-identical. Bare torso, pink-purple swim shorts, bare feet and uncovered tall spiky blond hair follow the beach appearance. New idle views support alternating walking steps; four compact acoustic guitar poses replace the overly wide first draft. Prepared and game-rendered sprites and portraits were visually reviewed. Prompts, source IDs and corrections are documented in artifacts/npc-modern/work/SamBeach.

Fresh isolated SMAPI at13:50:10 local on September5,2026 passed211/211 textures. Sam beach passed21 poses, three preserved blanks, twelve dialogue portrait indices, native island outfit selection and normal restoration,32 walking steps,24 timed guitar animation steps and one actual route intro-to-loop transition. Sam winter passed again. Full island/event scene playback remains unverified; no farm loaded. Evidence: isolated-0.7.55.json, loader-isolated-0.7.55.txt, sam-beach-checks.json and sam-beach-runtime-preview.png.

NPC Modern0.7.55 is installed. The guarded installer backed up0.7.54 to artifacts/mod-backups/AbigailModern-20260905-135103 and verified215 files. Fresh ordinary SMAPI at13:51:39 loaded211/211 textures from the real Mods folder; installed artwork, metadata and DLL match source. Evidence: installed-0.7.55.json, loader-installed-0.7.55.txt and registered-loader-check.txt. Only owned isolated46856 and ordinary10632 processes were stopped after identity checks. Production build passed without warnings/errors; audit build passed with the existing unrelated Krobus warning. Package and Blender are refreshed.

Coverage is211 registered and211 verified installed assets across66 roster entries and284 records, with73 pending records and25 partial entries. Shared-atlas partial entries include intentionally untouched unrelated game art, so this is not a count of unfinished NPCs. Full overhaul completion remains unproven. Among the twelve marriage candidates, Sam, Sebastian and Shane still have artwork work remaining; the six bachelorettes, Alex, Elliott and Harvey have their main/winter/beach sheets completed, with full scene validation still separate.

Next: Sam everyday full sprite sheet and any missing native portrait details. Native inventory, references and exact winter reuse comparisons are in work/SamBase. Ten work/special/formal poses are identical to winter;45 everyday poses remain to be generated and the walking-only patch must be replaced. Existing portrait11 lacks the native folded arms. Remaining NPC variants, special poses, expressions and full scene checks stay in scope.

## Previously installed and verified batch: 0.7.54 - Sam winter

Sam winter now has55 modern occupied poses, twelve portraits and the exact native white placeholder55. Beanie/brown jacket, handheld console, red electric guitar, green skateboard, blue sweeping uniform, formal suit, composer letters and acoustic guitar follow native roles. Portrait6 retains sheet music,9 bare spiky hair,10 hand behind head; formerly blank11 adds a thoughtful expression. Uneven walking-source columns were corrected using verified empty gutters; opposite strides and native facing directions were checked in prepared and game-rendered previews. Prompts, source IDs and export mapping are in work/SamWinter.

Fresh isolated SMAPI at13:41:33 local on September5,2026 passed209/209 registered textures. Sam passed55 poses, placeholder preservation, twelve dialogue portrait slots, native winter/spring appearance selection and restoration,364 timed special animation steps, six route intro-to-loop transitions,32 walking steps,50 native event frame references,20 actual ShowFrame commands and five native skate callbacks. Full scene playback remains unverified and no farm was loaded. Evidence: isolated-0.7.54.json, loader-isolated-0.7.54.txt, sam-winter checks/native-events/runtime-preview files.

NPC Modern0.7.54 is installed. The other game closed before the guarded installer ran;0.7.53 was backed up to artifacts/mod-backups/AbigailModern-20260905-134338. All213 files match source. Fresh ordinary SMAPI at13:43:50 loaded209/209 textures from the real Mods folder. Evidence: installed-0.7.54.json, loader-installed-0.7.54.txt and registered-loader-check.txt. Only owned isolated process59024 and ordinary verification process42528 were stopped after identity checks.

Production build passed with zero warnings/errors; audit build passed with the existing unrelated Krobus warning. NpcModern-0.7.54.zip contains213 verified files and Blender is refreshed. Coverage remains an artwork inventory, not proof that full NPC scenes or the overhaul are complete.

Next: Sam beach. Native references show21 occupied poses and three blank cells; the extra poses play acoustic guitar despite the native route name sam_beach_towel. Ten native portraits plus two new expressions are planned. Initial images are generating and a dedicated native beach audit is being prepared. Remaining NPC variants, special poses, expressions and full scene validation stay in scope.

## Previously installed and verified batch: 0.7.53 - Harvey everyday, winter and beach

Harvey everyday now replaces the full native sprite sheet, removing the previous walking-only patch. There are48 newly generated everyday poses and seven exact matching modern winter poses44–50, for55 complete occupied poses. The native512-pixel black placeholder55 is preserved. Green jacket, red tie, brown trousers, blue reading book, radio headphones, medical stethoscope, exercise headband/undershirt/dumbbells and dining props follow native roles. Eleven accepted portraits are byte-preserved; medical portrait3 now includes the native stethoscope. Prepared and game-rendered artwork was visually reviewed. All prompts, source IDs, targeted corrections and assembly mappings are documented in artifacts/npc-modern/work/HarveyBase/design.md.

Fresh isolated SMAPI at13:24:38 local on September5,2026 passed207/207 registered textures. Harvey everyday passed55 poses, exact placeholder, seven retained-frame comparisons, twelve dialogue portrait indices, everyday/winter/beach selection and restoration,96 native walking steps,212 special animation steps, five native route intro-to-loop callbacks,66 event frame references and49 actual ShowFrame commands. The latter begin on a different frame to verify that each command changes the pose. Winter and beach audits also passed in the same run. Full events/island scene playback remains unverified; no farm was loaded. Evidence: isolated-0.7.53.json, loader-isolated-0.7.53.txt and harvey-base checks/native-events/runtime-preview files.

NPC Modern0.7.53 is installed, including the earlier pending Harvey winter and beach batches. The other game had closed before installation. The guarded installer backed up0.7.50 to artifacts/mod-backups/AbigailModern-20260905-132517 and verified211 installed files. Fresh ordinary SMAPI at13:25:42 loaded207/207 textures from the real Mods folder; all installed artwork, metadata and DLL hashes match source. Evidence: installed-0.7.53.json, loader-installed-0.7.53.txt and ordinary registered-loader-check.txt. Only owned isolated process3952 and ordinary verification process14560 were stopped after identity checks. No other running game was stopped.

Production build passed with zero warnings/errors; audit build passed with the existing unrelated Krobus warning. Package NpcModern-0.7.53.zip contains211 checksum-verified files; Blender refreshed. Coverage is207 registered and207 verified installed assets across66 roster entries and284 asset records. This is asset coverage, not proof of complete NPC scenes or of the entire overhaul.

Next: Sam winter. Native references, cell inventory and animation descriptions are saved in work/SamWinter. It has55 poses plus solid white placeholder55, eleven portraits and empty slot11. Preserve beanie/clothing, handheld game console, guitars, skateboard, work/broom uniform, sheet music portrait6, bare-haired portrait9 and hand-behind-head portrait10. Confirm remaining event roles before generation. Remaining NPC seasonal/special sheets, missing expressions and full scene validation stay in scope.
## Previously prepared and isolated-tested batch: 0.7.52 - Harvey beach and winter

Harvey beach now has sixteen modern walking poses and ten portraits, including a new thoughtful expression in the old solid mauve slot9. Straw sunhat/green band, glasses, moustache, bare shoulders, green swim briefs and bare feet follow native clothing. Four fourth-column stride corrections provide opposite front/back feet and rearward side arms. Native-size prepared sprites/portraits and the game-rendered atlas were visually inspected. Exact prompts, source IDs and assembly mapping are in artifacts/npc-modern/work/HarveyBeach/design.md.

Fresh isolated SMAPI at 13:14:17 local on September 5, 2026 passed all 207 registered textures. Harvey beach passed sixteen sprite cells, ten non-placeholder dialogue portraits, 32 native walking steps, native island attire selection and restoration to ordinary clothing. Harvey winter passed again in the same run. All 211 extracted package files match source checksums. Evidence: isolated-0.7.52.json, loader-isolated-0.7.52.txt, harvey-beach-checks.json and harvey-beach-runtime-preview.png. No farm was loaded; full island/event playback remains unverified.

Production build passed without warnings/errors; audit build passed with the existing unrelated Krobus net-field warning. NpcModern-0.7.52.zip contains 211 verified files and includes both Harvey seasonal updates. Blender refreshed. Owned isolated process9132 was stopped after checking its identity.

Installation remains deferred. Another game process26492 started at 13:14:57, so the guarded installer refused changes. It was left running. Installed manifest remains0.7.50: 203 verified installed assets versus207 prepared. The ordinary registered-loader-check.txt still holds genuine0.7.50 installed proof. Earlier0.7.51 and current0.7.52 are packaged and isolated-tested, not installed. Full NPC overhaul remains incomplete.

Next: Harvey everyday full sprite sheet. Its native inventory is saved in work/HarveyBase; exact native comparisons permit reuse of winter poses44–50, while other poses and final cell55 require their own review. Remaining NPC variants, special animations, missing expressions and full scene checks stay in scope.
## Previously prepared and isolated-tested batch: 0.7.51 - Harvey winter

Harvey winter now has 55 modern poses, the exact native black placeholder at 55, and twelve portraits. Blue coat/red tie, glasses/moustache, reading books, radio headphones, medical stethoscope, gray dumbbells, balloon reactions, dining utensils, green formal suit and special blue outfit follow native artwork and event roles. Walking arm phases, seated radio poses, kiss accessories and special pose proportions received targeted corrections. The final sprites, portraits and game-rendered atlas were visually reviewed. Sources and exact prompts are retained in artifacts/npc-modern/work/HarveyWinter.

Fresh isolated SMAPI at 13:09:25 local on September 5, 2026 passed all 205 registered textures. Harvey winter passed 55 poses, one preserved placeholder, twelve portrait indices, winter selection/spring restoration, 212 timed animation steps, five actual route intro-to-loop transitions, 32 native walking steps, 66 native event frame references and 49 actual ShowFrame commands. The initial audit incorrectly expected zero sleep offset; native NPC.playSleepingAnimation applies (0,-4) to unmarried Harvey after the intro callback. The audit now checks that offset and the sleeping flag, and a fresh run passed. No farm was loaded; full events remain unverified.

Production build passed with zero warnings/errors; audit build passed with the existing unrelated Krobus warning. Package NpcModern-0.7.51.zip contains 209 checksum-verified files. Blender refreshed. Evidence: isolated-0.7.51.json, loader-isolated-0.7.51.txt and harvey-winter checks/native-events/runtime-preview files under artifacts/npc-modern/evidence. Both owned isolated processes (19560 failed audit, 53208 successful rerun) were stopped after identity checks.

Installation is deferred: user game process 41840 started at 13:10:10 before the guarded installer ran. No installed files were changed and the user process was left running. Version 0.7.50 remains installed, with 203 installed assets verified; 205 assets are now prepared. The ordinary registered-loader-check.txt remains the genuine installed 0.7.50 proof. The full NPC overhaul is still incomplete.

Next: Harvey beach generation is underway (16 walking poses and ten portraits, adding thoughtful expression 9), then Harvey everyday lower poses and the remaining NPC variants, special animations, missing expressions and full scene checks.
## Previously completed, still installed batch: 0.7.50 - Elliott everyday full sheet

Elliott everyday now replaces the full sprite sheet, removing the old walking-only patch. There are 49 complete modern poses across 51 occupied cells, with blank23 preserved. New everyday artwork covers 44 occupied cells, while seven matching modern winter cells44-50 are reused after exact native RGBA comparisons. Red coat, green tie/trousers, blue reading book, silver mug, invitation letter and wide fishing composites follow native roles. Eight chair-seated replacements preserve raised hips and visible legs. Fishing lines remain visible at native scale. Existing nine portraits are byte-preserved and a new thoughtful portrait fills slot9. All source prompts and mappings are recorded in artifacts/npc-modern/work/ElliottBase/design.md; prepared and game-rendered atlases were visually reviewed.

Fresh isolated SMAPI test at 12:49:24 local on 2026-09-05 passed 203/203 registered textures. Elliott everyday passed 51 occupied cells, one blank, ten portrait slots, seven exact retained-frame comparisons, everyday selection and winter transition, 106 reading/sitting/drinking/sleep steps, 96 walking steps across everyday/winter/beach, and 31 native event frame references. Eight fishing wrap checks and actual festival extend/reset commands passed for the everyday texture. Full event/festival playback remains unverified; no farm loaded. Evidence: isolated-0.7.50.json, loader-isolated-0.7.50.txt and elliott-base checks/native-events/runtime-preview under artifacts/npc-modern/evidence.

NPC Modern 0.7.50 is installed. The guarded installer backed up 0.7.49 to artifacts/mod-backups/AbigailModern-20260905-125026 and verified all 207 installed files. Fresh ordinary SMAPI launch at 12:50:36 passed 203/203 textures from the real game Mods folder. Installed assets, metadata and DLL match source. Proof: installed-0.7.50.json, loader-installed-0.7.50.txt and ordinary registered-loader-check.txt. Only our isolated process10064 and ordinary verification process17184 were stopped; no user game was open.

Production build passed without warnings/errors. Audit build passed with the existing unrelated Krobus net-field warning. ZIP NpcModern-0.7.50.zip contains207 checksum-verified files. Blender refreshed. Elliott everyday, winter and beach artwork are now installed; full scenes remain to be validated. Full NPC overhaul remains incomplete.

Next: Harvey winter, then remaining NPC seasonal/special sheets, missing expressions and full scene validation. Native Harvey winter references, inventory and animation descriptions are saved in work/HarveyWinter. It has56 occupied cells but final55 is a black placeholder (496 opaque black pixels plus16 transparent pixels), so preserve it rather than inventing a pose. Twelve portraits include stethoscope/headphone variants. Special prop roles still need local native-event confirmation before generation. No modern Harvey winter images generated yet.

## Previously installed and verified batch: 0.7.49 - Elliott beach

Elliott beach now has 20 modern poses and ten bare-shouldered portraits, including a new thoughtful expression. Dark green swim shorts, copper hair and bare feet follow native artwork. All four directions walk with alternating steps; side rows were corrected to native facing directions. Four silver-can poses retain the native drinking progression. Exact prompts, sources and correction mappings are in artifacts/npc-modern/work/ElliottBeach/design.md. Prepared and game-rendered atlases were visually reviewed.

Fresh isolated SMAPI test at 12:41:07 local on 2026-09-05 passed 203/203 registered textures. Elliott beach passed 20 occupied sprite cells, ten portrait slots, beach selection and normal restoration, 100 native drinking steps and 32 walking steps. The actual native route-end drinking behavior entered its intro and transitioned into its loop, retaining standing/shadow/offset flags. Full island scene playback remains unverified; no farm was loaded. Evidence: isolated-0.7.49.json, loader-isolated-0.7.49.txt, elliott-beach-checks.json and elliott-beach-runtime-preview.png under artifacts/npc-modern/evidence.

NPC Modern 0.7.49 is installed. The guarded installer backed up 0.7.48 to artifacts/mod-backups/AbigailModern-20260905-124149 and verified all 207 installed files. Fresh ordinary SMAPI launch at 12:42:00 passed 203/203 textures from the actual game Mods folder. Installed assets, metadata and DLL match source. Ordinary loader evidence is in installed-0.7.49.json, loader-installed-0.7.49.txt and registered-loader-check.txt. Only our isolated process 41068 and ordinary process 14592 were stopped; no user game was open.

Production build passed with no warnings/errors; audit build passed with the existing unrelated Krobus net-field warning. ZIP NpcModern-0.7.49.zip contains 207 checksum-verified files. Blender has been refreshed. Full NPC overhaul remains incomplete.

Next: Elliott everyday full sheet and missing portrait9. Fresh native comparisons found only seven occupied base/winter cells identical in color and alpha:44-50. Other base poses need their red coat, green trousers/tie and BLUE reading book. Native inventory and enlarged references are saved in work/ElliottBase. Remaining NPC seasonal/special sheets, missing expressions and full scene validation remain in scope.

## Previously installed and verified batch: 0.7.48 - Elliott winter and Alex everyday

Elliott winter now has 49 complete modern poses across 51 occupied cells, one preserved blank, and ten portraits including a new thoughtful expression. His brown coat, red scarf, copper hair, book, piano seating, invitation letter, formal outfit and ice-fishing props follow the native roles. Two fishing poses span 32 pixels each. A targeted correction preserved the fishing line at game size and gave the rod a visible alternating position. All prepared and game-rendered artwork was visually reviewed. Exact sources and prompts are in artifacts/npc-modern/work/ElliottWinter/design.md.

Fresh isolated SMAPI test at 12:35:13 local on 2026-09-05 verified 201/201 registered textures. Elliott checks passed all 51 occupied cells, one blank, ten portrait slots, winter selection and spring restoration, 106 native reading/sitting/drinking/sleep animation steps, 32 walking steps, and 31 frame references from native reading, piano and book-tour invitation events. Actual festival rectangle extend/reset commands passed; eight frame-wrap checks reproduce the native fishing geometry. These checks do not prove full event or festival playback. No farm was loaded.

NPC Modern 0.7.48 is installed, including the pending Alex everyday update from 0.7.47. The prior user game had closed. The guarded installer backed up 0.7.46 to artifacts/mod-backups/AbigailModern-20260905-123546 and verified all 205 installed files. A fresh ordinary game launch at 12:36:05 verified 201/201 registered textures from the real Mods folder. All installed assets, metadata and DLL match source. Only our isolated process 3720 and ordinary verification process 39752 were stopped; no user-owned process was stopped.

Production build passed without warnings/errors; audit build passed with the existing unrelated Krobus net-field warning. ZIP NpcModern-0.7.48.zip contains 205 checksum-verified files, and Blender has been refreshed. Evidence: isolated-0.7.48.json, installed-0.7.48.json, both versioned loader logs, and Elliott winter checks/native-events/runtime-preview files under artifacts/npc-modern/evidence. The ordinary loader proof is saved in registered-loader-check.txt.

Next: Elliott beach, then the remaining NPC seasonal/special sheets and missing expressions, plus full scene validation. Elliott beach native inventory and enlarged references are prepared: 20 occupied sprite cells, nine existing portraits and one empty portrait slot. Full NPC overhaul remains incomplete.

## Previously prepared and isolated-tested batch: 0.7.47 - Alex everyday full sheet

Alex's everyday sprite now replaces the full native51-pose sheet instead of the earlier walking-only patch.36 new poses cover walking, football, red music box, sitting and kissing;15 modern winter weightlifting/blue outfit/formal/sleep poses are reused after exact native color/alpha comparisons. Blank43 remains transparent. The green varsity jacket with cream sleeves and yellow trim follows the accepted modern portrait. The front stride, seated arms and kiss silhouette received targeted corrections. First ten portraits remain byte-identical; portrait10 now eats from a fork and new11 is thoughtful/hopeful. Exact prompts,sources and assembly decisions are in artifacts/npc-modern/work/AlexBase/design.md.

Fresh separate SMAPI title-screen test at12:22:15 local2026-09-05 passed199/199 registered textures,all51 occupied poses/one blank,twelve portrait indices,180 native football/weightlifting/sitting/sleep steps,96 walking steps across everyday/winter/beach,and10 frame references from the native music-box event. Everyday and winter appearance selection passed. Full game-rendered atlas reviewed. Full event playback remains unverified; no farm loaded.

Production build passed with zero warnings/errors. Audit build passed with the existing unrelated Krobus net-field warning. ZIP NpcModern-0.7.47.zip contains203 checksum-verified files; Blender refreshed. Source assets,metadata and DLL match the tested package. Evidence: artifacts/npc-modern/evidence/isolated-0.7.47.json,loader-isolated-0.7.47.txt,alex-base checks/native-event/runtime preview. Only the owned audit process12868 was stopped.

NOT installed yet: the user's game reopened at12:19:42 (process13828), retaining installed0.7.46. The install guard was respected. Normal installed coverage reports197 matching verified assets because the two Alex everyday textures have changed in source. Install the latest prepared version after the game closes. No user-owned process was stopped. Full NPC overhaul remains active.

Next: Elliott winter,then remaining NPC seasonal/special sheets and missing emotions,plus full scene verification. ElliottWinter native references,52-cell inventory and animation descriptions are prepared. Its occupied cells include two32px-wide ice-fishing composites40+41/42+43,which require grouped assembly; native iceFishing update logic confirms horizontal frame toggling. Piano event423502 uses24..30. Reading/book and paper pose references still need local confirmation before generation.

## Previously installed and verified batch: 0.7.46 - Alex winter and beach, accumulated artwork update

NPC Modern0.7.46 is installed. The user's earlier0.7.31 game session had closed before installation. All203 installed files match source/package, and a fresh ordinary SMAPI launch at12:14:20 local2026-09-05 verified199/199 registered textures from the real game Mods folder. No NPC Modern errors were found; no farm was loaded. Evidence: artifacts/npc-modern/evidence/installed-0.7.46.json and loader-installed-0.7.46.txt. registered-loader-check.txt now contains this ordinary installed run, and coverage reports199 registered/199 verified installed assets across the66-entry roster and284 asset records. Full NPC coverage remains incomplete.

This installs the accumulated work after0.7.31: Emily/seasonal bachelorette full sheets, Maru hospital, Penny everyday/winter/beach, Leah's missing expression, Alex winter and Alex beach, plus retained earlier artwork including Abigail's adventure outfit. Existing installed files were backed up before each copy. Ordinary verification process56340 was stopped after checking; no user-owned process was stopped.

Alex beach adds19 occupied poses/blank19 and ten shirtless portrait expressions, including playful expression3. Blue swim shorts and bare feet follow native art; sunbathing poses were regenerated to match the compact walking proportions. Two shirtless expressions6/8 retain matching modern Winter artwork after native identity checks. Sources and exact prompts are in artifacts/npc-modern/work/AlexBeach/design.md. Alex winter's51 poses/twelve portraits were verified in0.7.44 and are included unchanged.

Corrected separate SMAPI test at12:13:14 local2026-09-05 passed199/199 textures,19 beach poses/one blank,ten portrait indices,ten timed towel steps,32 walking steps,actual laying_down behavior with hidden shadow and zero offset, beach selection and normal outfit restoration. Runtime portrait pixels exactly match the corrected atlas; full game-rendered preview was reviewed. Evidence: isolated-0.7.46.json,loader-isolated-0.7.46.txt,alex-beach checks/runtime preview/loaded portrait. Full reclining and event scenes remain unverified.

0.7.45 passed narrow loader/layout checks but failed visual review. A generic preparation branch overwrote the correct ten portraits using a five-row assumption on an eight-bust source. The source was corrected, both preparation and runtime checks now enforce retained6/8 pixel identity, and a regression check rejects the old atlas and accepts the fixed one.0.7.45 was briefly installed and then replaced by0.7.46. Diagnostic sessions0.7.45-20260905-120932-606 and0.7.45-20260905-121118-343 are retained; all owned test processes15660,30136,50664 were stopped. Its evidence report is marked failed visual review.

Production build passed with zero warnings/errors; audit build passed with the existing unrelated Krobus net-field warning. ZIP NpcModern-0.7.46.zip has203 checksum-verified files and the Blender collection was refreshed. Source assets,metadata and DLL match the tested/installed package. Full overhaul remains active: next Alex everyday special poses and corrected fork/eating portrait plus missing expression11, then remaining NPC variants/special poses/emotions and full scene validation. AlexBase native references and15-pose Winter reuse evidence are prepared; no new AlexBase artwork generated yet.

## Previously prepared and isolated-tested batch: 0.7.44 - Alex winter

Alex winter now contains all 51 artwork poses and twelve portraits, including the new thoughtful expression in spare cell11. Blank sprite43 remains transparent. The red hoodie, football sequence, shirtless green-shorts weightlifting, mother's green music box, seated/kiss poses, blue rear-facing outfit, formal suit and sleepwear retain their native roles. Corrected alternating side strides, standing kiss, closed-eye formal/sleep poses and two source crop boundaries. Exact prompts, sources and assembly decisions are in artifacts/npc-modern/work/AlexWinter/design.md.

Fresh separate SMAPI title-screen session at12:05:14 local2026-09-05 passed197/197 registered textures. Alex passed51 occupied poses/one blank,12 nonempty portrait indices,180 native football/weights/sitting/sleep animation steps,32 timed walking steps,10 frame references in his native music-box event, winter selection and spring restoration. Game-rendered sprite and portrait atlas reviewed. Full event playback remains unverified; no farm loaded.

Production build passed with zero warnings/errors. Audit build passed with the existing unrelated Krobus net-field warning. ZIP NpcModern-0.7.44.zip contains201 checksum-verified files; Blender collection refreshed. Source assets, metadata and DLL match the tested package. Evidence: artifacts/npc-modern/evidence/isolated-0.7.44.json, loader-isolated-0.7.44.txt and alex-winter checks/runtime preview. Steam achievement service was unavailable; no artwork-mod errors. Only the owned test process59140 was stopped.

NOT installed into the user game: its existing0.7.31 session remains open. Normal installed coverage remains170 matching verified textures. Install the latest prepared package after the user closes the game. Full NPC overhaul remains incomplete. Next: Alex beach and everyday special poses, including correcting the everyday eating portrait that currently has a phone; then remaining NPC variants/special poses/emotions and full scene validation.

## Previously prepared and isolated-tested batch: 0.7.43 - Leah missing expression

Leah's unused everyday portrait9 now contains a thoughtful hand-under-chin expression matching her copper braid, green overshirt and cream shirt. The existing nine portraits remain byte-identical. Source and prompt are saved in work/LeahBase/extra-portrait-source.png and extra-portrait-prompt.txt. scripts/prepare-leah-extra-portrait.mjs crops excess lower torso to match the existing framing, fills only slot9 and verifies preservation. LeahBaseAudit now requires every portrait cell to be nonempty.

Fresh separate SMAPI title-screen session at11:49:10 local2026-09-05 passed195/195 registered textures. Leah passed ten nonempty portrait expressions/indices,50 occupied poses/two blanks,166 sculpt/draw/sleep steps,96 walking steps across three outfits and everyday/winter appearance switching. Game-rendered sheet reviewed. Full event/seasonal scenes remain unverified; no farm loaded.

Production build passed with no warnings/errors; audit build passed with the existing unrelated Krobus net-field warning. ZIP NpcModern-0.7.43.zip contains199 checksum-verified files; Blender refreshed with195 textures. Source assets,metadata and DLL match the tested isolated package. Evidence: artifacts/npc-modern/evidence/isolated-0.7.43.json,loader-isolated-0.7.43.txt and refreshed leah-base checks/runtime preview. Steam achievement service was unavailable; no artwork-mod errors. Only the owned audit process12780 was stopped.

NOT installed into the user game: its existing0.7.31 session remains open. Normal installed coverage now records170 matching verified textures because Leah's portrait changed. Install the latest package after the user closes the game. Full NPC overhaul remains incomplete. Next: Alex winter, then remaining NPC variants/special poses/emotions and full scene validation. Alex winter native references and a51-pose layout inventory are prepared in work/AlexWinter/design.md; identify the green handheld prop before generating those cells. No Alex winter artwork has been generated yet.

## Previously prepared and isolated-tested batch: 0.7.42 - Penny everyday

Penny everyday now replaces the full49-pose sheet instead of the earlier walking-only patch. It has34 new everyday poses and15 matching modern winter poses reused after native silhouette checks. Three solid brown placeholder cells49-51 remain original. Fourteen portraits include ten new yellow-blouse expressions and four corrected winter swimsuit portraits; the old portrait8 blouse error and missing13 are resolved. Closed-book poses16/17 were regenerated after the special draft omitted the books;20 has a dedicated left-profile wave, and39 has the native everyday hug costumes. Sources and exact mappings are in work/PennyBase/design.md.

Fresh separate SMAPI title-screen session at11:43:21 local2026-09-05 passed195/195 registered textures. Penny everyday passed all52 cell layouts,14 nonempty expressions/indices,72 native dishwashing/reading/sitting/waving/sleeping steps,96 timed walking steps across everyday/winter/beach, spring selection and winter transition. All15 reused sprites and four reused portraits match their finished winter cells byte-for-byte. Game-rendered sheet reviewed. Full event/seasonal scenes remain unverified; no farm loaded.

Production build passed with no warnings/errors; audit build passed with the existing unrelated Krobus net-field warning. ZIP NpcModern-0.7.42.zip contains199 checksum-verified files; Blender refreshed with195 textures. Source assets,metadata and DLL match the tested isolated package. Evidence: artifacts/npc-modern/evidence/isolated-0.7.42.json,loader-isolated-0.7.42.txt and penny-base checks/runtime preview. Steam achievement service was unavailable; no artwork-mod errors.

NOT installed into the user game: its existing0.7.31 session remains open. Only the owned audit process38172 was stopped. Normal installed coverage now records171 matching verified textures because both Penny everyday assets changed. Install the latest prepared package after the user closes the game. Full NPC overhaul remains incomplete. Penny's three native outfits are prepared and isolated-tested; full scenes remain pending. Next: Leah's missing everyday portrait9, then remaining NPC seasonal/special poses/emotions and full scene validation.

## Previously prepared and isolated-tested batch: 0.7.41 - Penny beach

Penny beach adds all20 occupied poses and14 full portrait expressions, including five previously empty expression slots. Her green swimsuit with pale-green trim, copper hair, closed-book transitions and prone reading poses are retained. All four directions have distinct alternating strides. The14 portrait cells are nonempty and distinct. Exact prompts and sources are in artifacts/npc-modern/work/PennyBeach/design.md.

Fresh separate SMAPI title-screen session at11:35:30 local2026-09-05 passed195/195 registered textures. Penny beach passed20 frame layouts,14 nonempty portrait expressions/indices,48 native towel animation steps,32 timed walking steps, the actual native towel behavior with layingDown/HideShadow true and zero drawOffset, beach selection and normal outfit restoration. Game-rendered sheet reviewed. Full reclining/island/event scenes remain unverified; no farm loaded.

The first test exposed an audit assumption copied from Maru's offset0 16. Native Penny towel metadata has no offset, so the audit now checks zero and rejects unexpected offset metadata. The artwork did not change. The failed session0.7.41-20260905-113401-148 is retained; final passing session is0.7.41-20260905-113517-840. Both owned test processes20348 and33340 were stopped.

Production build passed with no warnings/errors; audit build passed with the existing unrelated Krobus net-field warning. ZIP NpcModern-0.7.41.zip contains199 checksum-verified files; Blender refreshed with the195-texture registry. Source assets,metadata and DLL match the final tested package. Evidence: artifacts/npc-modern/evidence/isolated-0.7.41.json,loader-isolated-0.7.41.txt and penny-beach checks/runtime preview. Steam achievement service was unavailable; no artwork-mod errors in the final run.

NOT installed into the user's game: its existing0.7.31 session remains open. Normal installed coverage remains173 matching verified textures. Install the latest prepared package after the user closes the game. Full overhaul remains incomplete. Next: Penny everyday full sheet and corrected14-expression portrait atlas; its exact15-pose winter reuse plan and native hug reference are recorded in work/PennyBase/design.md. Then remaining NPC variants/special poses/emotions and full scene validation. Leah everyday portrait9 remains pending.

## Previously prepared and isolated-tested batch: 0.7.40 - Penny winter

Penny winter adds all 49 artwork poses and fourteen complete portrait expressions. The three solid brown placeholder cells49-51 remain byte-identical to the native sheet. Green coat, lavender leggings and copper hair are retained, along with book, dishwashing stool/splashes, mug, swimsuit, headless dress body, wedding/dance costumes, composite hug and yellow sleepwear. Corrected pose directions, restored the green dress sash and regenerated the final four portrait busts. Source prompts and exact assembly decisions are in artifacts/npc-modern/work/PennyWinter/design.md.

Fresh separate SMAPI title-screen session at11:27:14 local2026-09-05 passed193/193 registered textures. Penny passed all52 sprite cell layouts, fourteen nonempty portrait expressions/indices,72 timed native dishwashing/reading/sitting/waving/sleeping steps,32 timed walking steps, winter selection and spring restoration. Game-rendered sheet reviewed. Native brown placeholders match byte-for-byte and all four directions have distinct alternating walking poses. Full event/seasonal scenes remain unverified; no farm loaded.

Production build passed with no warnings/errors; audit build passed with the existing unrelated Krobus net-field warning. ZIP NpcModern-0.7.40.zip contains197 checksum-verified files. Blender refreshed with the current193-texture registry. Source assets,metadata and DLL match the tested isolated package. Evidence: artifacts/npc-modern/evidence/isolated-0.7.40.json,loader-isolated-0.7.40.txt and penny-winter checks/runtime preview. Steam achievement service was unavailable; no artwork-mod errors.

NOT installed into the user game: its existing0.7.31 session remains open. Only the owned test process60396 was stopped. Normal installed coverage remains173 matching verified textures. Install the latest prepared package after the user closes the game. Full NPC overhaul remains incomplete. Next: Penny beach and full everyday sheet, including the everyday swimsuit portrait8 costume error and missing portrait13; then remaining NPC variants/special poses/emotions and full scene validation. Leah everyday unused portrait9 also remains pending.

## Previously prepared and isolated-tested batch: 0.7.39 — Maru everyday

Maru everyday now has all 45 occupied poses modernized: 33 refreshed walking/special poses and 12 matching modern formal/sleepwear poses retained from winter after native silhouette checks. Blank cells 34, 35 and 39 remain blank. Her lavender shirt, blue overalls, glasses and existing ten-expression portrait sheet are retained. Sources and stride remappings are in artifacts/npc-modern/work/MaruBase/design.md. This replaces the earlier walking-only patch with a full sprite sheet.

Fresh separate SMAPI title-screen session at 11:13:53 local 2026-09-05 passed 191/191 textures. Maru everyday passed all 48 cell layouts, ten nonempty portrait expressions/indices, 80 native tinkering/sitting/sleeping steps, 128 timed walking steps across all four outfits, spring selection and winter transition. Game-rendered sheet reviewed. No farm loaded; full events, schedules and seasonal scene playback remain unverified.

Production build passed with no warnings/errors. Audit build passed with the existing unrelated Krobus net-field warning. ZIP NpcModern-0.7.39.zip contains 195 checksum-verified files; Blender refreshed with 191 textures. Source assets, metadata and DLL match the tested isolated package. Evidence: artifacts/npc-modern/evidence/isolated-0.7.39.json, loader-isolated-0.7.39.txt and maru-base checks/runtime preview. Steam achievement service was unavailable; no artwork mod errors.

NOT installed into the user's game: the existing 0.7.31 session remains open. Only the owned test process was stopped. Normal installed coverage now records 173 matching verified textures because Maru's base sheet changed. Install the latest package after the user closes the game. Full NPC overhaul remains incomplete. Penny's winter, beach and full everyday sheet are next, followed by remaining NPC outfits/special poses/emotions and full scene validation. Leah's unused everyday portrait slot 9 also remains pending.

## Previously prepared and isolated-tested batch: 0.7.38 — Maru beach

Maru beach adds all20 occupied poses and ten full portrait expressions. Purple star swimsuit,bare feet and sunglasses in the appropriate reclining/portrait slots preserve her beach identity. Corrected a baked checkerboard background,repeated walking poses,unwanted ordinary glasses and four incomplete portrait crops. Frame1 uses a flipped front stride; minor mirrored hair/star details still need full walking-scene review. Sources,prompts and exact remapping/cropping decisions are in artifacts/npc-modern/work/MaruBeach/design.md.

Fresh separate SMAPI title-screen session at11:07:44 local2026-09-05 passed191/191 textures. Maru beach passed twenty frame layouts,ten nonempty portrait expressions/indices,32 timed walking steps,14 native towel animation steps and the real native towel behavior's layingDown/HideShadow flags and drawOffset(0,16). Beach selection and normal outfit restoration passed. Game-rendered sheet reviewed. Full reclining/island/event scene playback remains unverified; no farm loaded.

Production build passed with no warnings/errors. Audit build passed with the existing unrelated Krobus net-field warning. ZIP NpcModern-0.7.38.zip contains195 checksum-verified files; Blender refreshed with191 textures. Source assets,metadata and DLL match the tested isolated package. Evidence: artifacts/npc-modern/evidence/isolated-0.7.38.json,loader-isolated-0.7.38.txt and maru-beach checks/runtime preview. Steam achievement service was unavailable; no artwork mod errors.

NOT installed into the user's game: its existing0.7.31 session remains open. Only the owned test process was stopped. Normal installed coverage remains174 matching verified textures. Install the latest package after the user closes the game. Full NPC overhaul remains incomplete. Maru's full everyday sheet is next,followed by Penny and remaining NPC outfits/special poses/emotions. Leah's unused everyday portrait slot9 also remains pending.

## Previously prepared and isolated-tested batch: 0.7.37 — Maru clinic

Maru clinic adds all20 occupied sprite poses and six matching portrait expressions. Native blank cells19-27 and29-31 remain blank. White tunic, lavender details, purple trousers, flat shoes and white nurse cap preserve the original uniform; her approved brown skin, auburn bob and purple glasses remain. Corrected repeated side strides and retained a consistent front idle for frame16. Sources/prompts/corrections are in artifacts/npc-modern/work/MaruHospital/design.md.

Fresh separate SMAPI title-screen session at10:59:02 local2026-09-05 passed189/189 registered textures. Maru clinic passed all32 frame rectangles,20 occupied/12 blank checks,six nonempty expressions and dialogue indices,32 timed walking steps and eight native hospital/town appearance cases covering all four seasons. The game's exported Hospital event contains six showFrame Maru commands using frames18,17,0,16; each refers to an occupied updated pose. Game-rendered sheet reviewed. Full event, kissing and schedule scene playback remain unverified; no farm loaded.

Production build passed with no warnings/errors. Audit build passed with the existing unrelated Krobus net-field warning. ZIP NpcModern-0.7.37.zip has193 checksum-verified files; Blender refreshed with189 textures. Source assets,metadata and DLL match the tested isolated package. Evidence: artifacts/npc-modern/evidence/isolated-0.7.37.json,loader-isolated-0.7.37.txt,maru-hospital checks/runtime preview/native event export and event-frame-checks. Steam achievement service was unavailable; no artwork mod errors.

NOT installed into the user's game: its existing0.7.31 session remains open. Only the owned test process was stopped. Normal installed coverage remains174 matching verified textures; isolated checks do not count as installation. Install the latest package after the user closes the game. Full NPC overhaul remains incomplete. Next: Maru beach and full everyday poses,then Penny and remaining NPC outfits/specials/emotions; Leah's unused everyday portrait slot9 remains pending.

## Previously prepared and isolated-tested batch: 0.7.36 — Maru winter

Maru winter adds all 45 occupied sprite poses and ten portrait expressions. Three native blanks (34,35,39) are preserved. Brown jacket/gray hoodie/blue jeans, purple glasses and auburn bob follow the approved character and native outfit. Includes wrench-tinkering, sparks, seated poses, sleeping, wedding and Flower Dance. Corrected duplicate strides and floor-sitting drafts; reworked the front stride to retain her head proportions. Generation sources, prompts and corrections are recorded in artifacts/npc-modern/work/MaruWinter/design.md.

Fresh separate SMAPI title-screen session at 10:53:05 local 2026-09-05 passed 187/187 registered textures. Maru's audit passed all 48 frame layouts, ten nonempty portrait expressions/indices, 80 native tinker/sit/sleep animation steps, 32 timed walking steps, winter selection and spring restoration. Game-rendered sheet reviewed. No farm loaded; full event, clinic and seasonal scene playback remain unverified.

Production build passed with no warnings/errors. Audit build passed with the existing unrelated Krobus net-field warning. ZIP NpcModern-0.7.36.zip has 191 checksum-verified files; Blender refreshed with 187 textures. All source assets, metadata and the DLL match the tested isolated package. Evidence: artifacts/npc-modern/evidence/isolated-0.7.36.json, loader-isolated-0.7.36.txt and maru-winter checks/rendered preview. Steam achievement service was unavailable; no artwork mod errors.

NOT installed into the user's game: its existing 0.7.31 session remains open. Only the owned test process was stopped. The normal installed ledger still records 174 matching verified textures; isolated verification is separate from installation. Install the latest prepared package after the user's game closes. Full NPC overhaul remains incomplete. Next: Maru's clinic, beach and remaining everyday poses, then Penny and the other unfinished NPC sheets/emotions; Leah's unused everyday portrait slot9 also remains pending.

## Previously prepared and isolated-tested batch: 0.7.35 — Leah everyday and beach

Leah everyday now has all 50 occupied poses modernized, with native blanks 39/50 preserved. Thirty-eight new walking/special/painting poses join twelve matching modern formal/sleepwear poses retained after checking native silhouettes. Her accepted green overshirt, cream shirt and blue jeans remain. The existing everyday portrait sheet has nine occupied expressions and one unused blank slot (9); the extra expression remains pending. Leah beach adds all 20 poses and eight matching portrait expressions, including the four coconut-drink poses. Winter remains the previously verified 50-pose/ten-expression set.

Fresh separate SMAPI title-screen session at 10:46:25 local 2026-09-05 passed 185/185 registered textures. Leah everyday passed 166 sculpt/draw/sleep animation steps, 96 timed walking steps across all three outfits, all 52 frame rectangles, ten portrait indices, spring selection and winter transition. Leah beach passed 130 native drink animation steps, twenty frame layouts, eight portrait indices, native beach selection and normal outfit restoration. Game-rendered sheets visually reviewed; final exports exactly match the reviewed images. No farm loaded; full event/island/quest scenes remain unverified.

Production build passed with no warnings/errors; audit build passed with the existing unrelated Krobus net-field warning. ZIP NpcModern-0.7.35.zip has 189 checksum-verified files, and Blender was refreshed with 185 textures. Evidence: artifacts/npc-modern/evidence/isolated-0.7.35.json, loader-isolated-0.7.35.txt and leah-base/leah-beach checks and runtime previews. Source and package assets/metadata/DLL match the final isolated session. Steam achievement service was unavailable; no artwork mod errors.

NOT installed into the user's game: the existing session remains on 0.7.31. Abigail's adventure sprite and portrait were freshly checked and both match their installed files; her nine recorded native outfit-selection cases passed, while Living Memory still owns the quest trigger integration. Both owned 0.7.35 test processes were stopped. Normal installed coverage now reports 174 matching verified textures; isolated checks do not count as installation proof. Install the latest package after the user's game closes. Full NPC overhaul remains incomplete: Maru/Penny full and seasonal sheets, other NPC variants/special poses, missing portrait slots and full scene review remain.

## Previously prepared and isolated-tested batch: 0.7.34 — Emily everyday, Haley full/seasonal, Leah winter

Leah winter adds 50 occupied poses and ten portrait expressions, including sculpting, sketching, painting, wedding/dance and sleeping. Two native blank cells preserved. Haley everyday is now a complete 50-pose sheet with two blanks: 34 new walking/reaction/camera poses and 16 matching modern seasonal poses reused after checking native silhouettes. These updates include the earlier prepared Emily everyday (56 poses), Haley winter (47 poses/14 portraits), and Haley beach (23 poses/14 matching retained portraits).

Fresh separate SMAPI title-screen session at 10:32:30 local 2026-09-05 passed 183/183 registered textures. Leah winter passed 166 native sculpt/draw/sleep animation steps, ten portrait indices and winter/spring appearance selection. The same tested package includes Emily's 66 exercise/sleep steps; Haley everyday's 210 photo/sleep/desert steps and 96 walking steps across all three outfits; Haley winter's 180 photo/sleep steps; Haley beach's 66 towel steps and two real native towel behavior/offset checks. Complete game-rendered sheets reviewed. Corrected the initially clipped test exports and stray hair fragments in the last winter portrait rows before the final checks.

Production build passed with no warnings/errors. Audit build passed with the existing unrelated Krobus net-field warning. ZIP NpcModern-0.7.34.zip has 187 checksum-verified files; Blender refreshed with 183 textures. Evidence: artifacts/npc-modern/evidence/isolated-0.7.34.json and loader-isolated-0.7.34.txt, plus the per-character checks/rendered PNGs. Isolated 0.7.33 history and both earlier review iterations are retained under runtime-audits. No artwork mod errors; Steam achievement service was unavailable.

NOT installed into the user's game yet: its session remains open on 0.7.31. The separate test used scripts/start-npc-art-audit.ps1 with a workspace mod folder. All owned test processes were stopped; the user's existing session was left running. No farm loaded. Full events, island/desert positioning, sleeping/towel scenes and the adventure quest integration remain unverified. The normal installed coverage ledger intentionally reports only 175 matching verified assets; the 183 isolated checks are not counted as installation proof. Install the latest package after the game closes, then verify the ordinary installed loader. Full NPC coverage remains incomplete; Leah everyday/beach and many other seasonal sheets remain.

## Last installed verified batch: 0.7.31 — Abigail mining/adventure outfit

Added the requested quest outfit with 42 new adventuring poses and ten portrait expressions. Twelve modern formal poses and two blank cells retained. Plum tunic, teal scarf, cream sleeves, dark leggings, boots, satchel and brass headlamp. The host quest flag selects native normal/island appearance entries; removing it restores the appropriate seasonal outfit. Quest integration instructions: docs/npc-art/abigail-adventure-outfit.md. Actual quest boundaries remain owned by Living Memory.

Fresh SMAPI 09:54:08 local 2026-09-05 passed 177/177 textures. The new audit first failed before implementation, then passed nine appearance selection/restoration cases, 54 occupied cell layouts, two blanks and ten portrait indices. Game-rendered sheet reviewed. No farm loaded; full quest and overnight save playback remain unverified. Production build passed; installed files and ZIP 181 files checksum verified; Blender refreshed. Steam achievement service unavailable; no artwork mod errors. Owned process stopped and audit archived. Full NPC overhaul remains incomplete; Emily's everyday special poses are still next.
## Previously verified batch: 0.7.30 — Emily winter and beach

Installed 56 winter and 21 beach poses, with eight portrait expressions for each outfit. Corrected hovering proportions, missing beach idle poses, and pose directions. Preserved one native blank cell and two solid-white unused beach placeholders. Added a joyful beach portrait in the unused eighth slot.

Fresh SMAPI 09:42:04 local 2026-09-05 passed 175/175 registered textures. Native winter/beach selection and normal outfit restoration passed. Actual exercise/sleep sequences ran for 66 native animation steps, and beach dance for 56 steps. Game-rendered sheets reviewed. No farm loaded; full event and island scene playback remain unverified. Production build passed; installed files and ZIP 179 files checksum verified; Blender refreshed. Steam achievement service unavailable; no artwork mod errors. Owned process stopped and audit archived. Full NPC overhaul remains incomplete; Emily's everyday special poses are next.
## Previously verified batch: 0.7.29 — Abigail everyday full sheet

Installed all 54 occupied everyday poses, including refreshed walking, flute playing, sitting, sword practice, wedding and dance; two blank cells retained. Corrected the first drafts to match her accepted purple skirt and boots. Existing ten-expression portrait retained.

Fresh SMAPI 09:32:13 local 2026-09-05 passed 171/171 registered textures. Native audit passed 56 cell layouts, ten portrait indices, 96 timed walking steps across all three outfits, and winter/beach appearance restoration. Game-rendered sheet reviewed. No farm loaded; full event playback remains unverified. Production build passed and 175 installed files checksum verified. Owned process stopped; audit archived. ZIP 175 files checksum verified; Blender board refreshed. Full NPC overhaul remains incomplete.
## Previously verified batch: 0.7.28 — Abigail beach

Installed all 19 occupied beach sprite poses and 10 portrait slots, retaining one blank cell. Native beach outfit selection, dialogue portrait indices, frame layouts, and normal outfit restoration passed. Game-rendered sheet reviewed.

Fresh SMAPI 09:27:12 local 2026-09-05 passed 171/171 registered textures. Production build passed; 175 installed files checksum verified. No farm loaded; full animation and island scenes remain unverified. Steam achievement service unavailable; no artwork mod errors. Owned process stopped and audit archived. Full NPC overhaul remains incomplete.
## Previously verified batch: 0.7.27 — Abigail winter

Installed all54occupied winter sprite poses and10portrait slots, including flute, seated, sword, wedding and dance poses. Two blankcells retained. Corrected adjacent dancer arm fragment after first runtime review.

FreshfinalSMAPI08:55:04local2026-09-05 passed169/169registered textures. Nativewinter selection, portraitpixels/indices, allframe layouts andspringrestoration passed; finalgame-rendered sheet reviewed. Fullanimation/eventplayback remains unverified. No farmloaded. Steam reported achievement service unavailable becauseSteamnotloaded; no artworkmoderrors. Productionbuild passed; ZIP173files verified; Blenderrefreshed. Ownedtestprocesses stopped and auditarchived. FullNPCoverhaul remains incomplete.
## Previously verified batch: 0.7.26 — Summer fishing visitors

Installed11summer figures and10six-expression portrait sets. Forest Trout Derby visitors now use summer portraits and native localized fishing reactions. Winter portrait routing retained.

Fresh SMAPI07:46:03 local2026-09-05 passed167/167registered textures. Native audit passed10summer actor layouts/70reactions and12winter actor layouts/84reactions, including seasonal portrait selection. Both game-rendered sheets reviewed. No farm loaded. Full festival playback and HUD proximity/placement remain unverified. Production build passed; ZIP171files verified; Blender refreshed. Owned test process stopped and temporary audit archived. Full NPC overhaul remains incomplete.
## Previously verified batch: 0.7.25 — Winter fishing visitors

Installed13winter figures across12native actors and12six-expression portrait sets. New nearby portrait panel follows native localized fishing reactions without changing contest mechanics. Summer visitors remain in progress.

Fresh SMAPI07:30:55 local2026-09-05 passed156/156registered textures. Native actor audit passed12sprite layouts and84reactions; runtime contact sheet reviewed. No farm loaded. Full festival playback and HUD proximity/placement remain unverified. Build passed; ZIP160files verified; Blender refreshed. Owned test process stopped and temporary audit archived. Full NPC overhaul remains incomplete.
# NPC artwork overhaul progress

## Previously verified batch: 0.7.24 — Player children

Installed six appearance sprite sheets and six new portrait sheets: Baby/Baby_dark and four Toddler gender/skin variants. Eachbaby39occupiedposes plusblankcell, eachtoddler24poses:174totalposes and36portraitexpressions. Baby mixed22x16/22x32 atlas layout preserved; toddlerbody positioned using nativeframebounds to retainhead/foot baseline. Generatedsprite/portraitsources and preparationcorrections in Children/design.md. Nativeposeorder retained except documentedremappings/flips. Boy sleepingportrait tightcrop removesZmarks; fullhat/cribquality review stillneeded.

ChildPortraits shows nearestvisiblechildwithin4tiles, no menu/worldready, name fromchilddisplayName. Appearancefollowsage/gender/skin; sleeping5,toss4,emote1,crawlerblockplay3,toddlerarms4,neutral0. Concernedpreparedbutunused. Originalinteractionmethods remain unchanged.

ChildAudit passed16nativeconstructor/reloadgrowthcases forages0..3,bothgenders/bothskins; nativeassetpaths andframe22x16/22x32/16x32 sizes, portraitlayout, chosennames, sleeping/tossflag/blockplay/heartemote/armstate mappings. No farm loaded oractualtoss/hattransactionperformed. Game-renderedcontact andrealportraitpanelwithlongname reviewed. Initialtestcontactstretchedsprites; correctedtoequal2xscale andrerun. Fullsceneanimation, crib/tossalignment, hatclipping, HUDproximity andfestivalview remain unverified.

Productionbuild zero warnings/errors; audit existingunrelatedKrobus advisory. Version0.7.24 installedwithbackup and147files checksumverified. Freshfinal loader06:59:24local2026-09-05 passed143/143textures withno runtimewarnings/errors. ZIP147files checksumverified; Blenderrefreshed. OwnedtestPIDs56848and59768stopped; audit archivedoutsideMods at audit-children-20260905-0700. FullNPCoverhaul remainsincomplete.


## Previously verified batch: 0.7.23 — Junimos

Updated all48bodyframes in Characters/Junimo top128x96 for Community Center and harvesting Junimos. Neutral grayscale permits native color multiplication. Lower4096pixels (bundle/star props) retained. Added six64pxportrait expressions. Corrected generator seven-column output, baked checkerboard and extra arms; final magenta-key portraits and connected-component body extraction reviewed at native resolution. Allframes use constant0.095scale to avoid per-frame shrinkage from stray alpha pixels.

JunimoPortraits shows nearest visible Junimo or JunimoHarvester within5tiles, no menu and worldready, bottom-left160pxpanel. Uses native tint and opacity; excludes opacity<=0.1. Community Center states: shy2,neutral0,speech1,bundle3,star4,farewell5. Harvester: neutral0,harvesting3,carrying44..47expression4. No speech/reward/harvest mechanics modified.

JunimoAudit passed6expressions and3harvesterstates using detached instances, native tint access forbothclasses, neutralgrayscale body verification, and game-rendered48frames plus6coloredportraits. Preview inspected. No farm loaded. Full animation playback, HUD placement/proximity gating and reward/harvest scenes remain unverified.

Version0.7.23 installed with backup;135files checksum verified. Production build zero warnings/errors; audit retains unrelated Krobus name-field advisory. Fresh loader06:32:35local2026-09-05 passed131/131textures without runtime warnings/errors. ZIP135files checksum verified; Blender refreshed. OwnedPID31112stopped; audit archived outsideMods at audit-junimo-20260905-0634. Full NPC overhaul remains incomplete.


## Previously verified batch: 0.7.22 — Upgrade parrots

Installed 55 native 24px animation cells in the upper 264x120 region of LooseSprites/parrots: green and blue large/small rows and golden row. All 44,352 lower pixels remain identical to the original. Added green/golden six-expression portraits. Prepared sprite contact sheet and portrait sheets visually inspected; green native dialogue render inspected.

IslandParrotPortraits chooses matching green/gold art for native CheckAction and golden AnswerQuestion replies. Thoughtful for questions, concerned for insufficient funds, satisfied for Tonight text. Native insufficient-money branch opens dialogue while returning false, so answer hook detects menu replacement independently.

Fresh game audit verified green/gold portrait pixels, regular 0/10-walnut questions, golden initial and confirmation questions, insufficient-money dialogue, native text and Yes/No keys, unchanged balances and no construction/purchase. No farm loaded. Successful purchase/Tonight branch and full island animation playback remain unverified.

Production build zero warnings/errors; audit has existing unrelated Krobus name-field advisory. Version0.7.22 installed with backup and133files checksum verified. Fresh loader06:17:41 local2026-09-05 passed129/129 textures, no runtime warnings/errors. ZIP133files checksum verified; Blender refreshed. Owned testPID50780 stopped; audit archived outside Mods at audit-upgrade-parrots-20260905-0619. Full NPC overhaul remains incomplete.
## Previously verified batch: 0.7.21 — Island parrot character sheet and perch dialogue

Updated Characters/IslandParrot18native32px poses (two blank cells retained) and six portrait expressions. Actual event usage of this character sheet remains unresolved: literal scan of all Data/Events XNB strings found none. It is distinct from the still-original LooseSprites/parrots used by upgrade/construction birds and golden parrot. Do not count those sprites complete.

IslandParrotPortraits decorates newly opened non-golden native ParrotUpgradePerch.CheckAction dialogue. Thoughtful3 for upgrade question, concerned2 for insufficient walnuts. Audit invoked native Trader perch action with0and10walnuts, verified native localized text, Yes/No response keys and question routing key, correct expressions, unchanged walnut balances and no construction. Test state restored without farm load. First runtime preview exposed neighboring-row feather artifacts; explicit portrait crop bounds fixed them. Reinstalled and reran; corrected0/10walnut screenshots inspected.

Version0.7.21 installed with backup. Shipped build zero warnings/errors; audit retains unrelated Krobus name-field advisory. Fresh final loader05:56:26 local2026-09-05 passed126/126 textures without runtime warnings/errors. ZIP130files checksum verified, Blender126packed textures. Both owned test processes stopped; audit archived outside Mods. Full scene animation, other perch types and construction transactions remain unverified. Full NPC overhaul remains incomplete.
## Previously verified batch: 0.7.20 — Mermaid artwork and island portrait

Updated seven island frames, nine Night Market frames, two large singing frames and two swimming body/hair layers in shared temporary_sprites_1. Added six portraits. Night Market dance frames0/1 now reuse distinct island dance poses; other seven night poses retain their generated sheet. Prepared previews inspected, including corrected swimming hair composite. 302,548 pixels outside Mermaid regions preserved; DesertTrader regeneration also preserved shared atlas checksum after mermaid patches. See Mermaid/design.md for generation/cropping details and remaining quality concerns.

MermaidPortrait displays a small portrait panel only in IslandSouthEast when MermaidIsHere and the farmer is in the nearby southeast area (X>=25,Y>=26), world ready and no menu. Native idle/wave/reward/dance map to expressions0/1/4/5. No dialogue or puzzle mechanics changed. Two additional expressions are prepared but unused. Night Market portrait display has not been added.

MermaidAudit verified expression mapping against native animation array references and unchanged reward flag, then rendered runtime island/night frames and four portraits. It does not prove actual HUD positioning, weather/proximity gating, full animation playback, swimming tint colors, or puzzle completion. No farm loaded. Reviewed mermaid-runtime-preview.png; full island and Night Market scenes remain unverified.

Version0.7.20 installed with backup. Shipped build zero warnings/errors; audit has existing unrelated Krobus name-field advisory. Fresh loader05:44:02 local2026-09-05 passed124/124 textures without runtime warnings/errors. ZIP128 files checksum verified; Blender124 packed textures. Owned process stopped; audit archived outside Mods. Full NPC overhaul remains incomplete.
## Previously verified batch: 0.7.19 — Island Trader

Updated all 18 unique 16x16 bird tiles referenced by the native Maps/Island_N_Trader Front animation. Indices confirmed by runtime map export: 2001-2004, 2032-2037, 2065-2072. Patched only these cells in Maps/island_tilesheet_1; all 527,872 outside pixels preserved. Native map animation order unchanged. Six portrait expressions prepared, neutral used by IslandTrade shop. Prepared full frame contact sheet and actual 1920x1080 shop render inspected. Generated sprite source exec-3127d2e5-acf8-4c19-ad37-022ece3196e3 used original full 18-frame contact and new portrait as references, requested exact 6x3 pose order, magenta background and unchanged bird identity.

IslandTraderAudit opened native IslandTrade shop in a detached IslandNorth. Verified portrait pixels, unchanged stock count/greeting/money. No purchase or farm load. Full map rendering, animation playback and interaction tile remain unverified; stock count is not individual trade-price verification. Extra portrait expressions have no added dialogue routes. Evidence: island-trader-checks.json and island-trader-shop-preview.png.

Version 0.7.19 installed with backup. Shipped build zero warnings/errors; temporary audit retains existing unrelated Krobus name-field advisory. Fresh loader 05:30:13 local on 2026-09-05 passed 123/123 textures without runtime warnings/errors. ZIP 127 files checksum verified; Blender refreshed with 123 packed textures. Owned process stopped, audit archived outside Mods. Full NPC overhaul remains incomplete.
## Previously verified batch: 0.7.18 — Desert Trader

Updated eight 20x26 merchant frames in LooseSprites/temporary_sprites_1 at (0,614,160,26). Added six portrait expressions, with neutral cell 0 displayed in DesertTrade. Prepared sprite strip and actual shop render inspected. Background brown/sand reconstructed within merchant cells to cover the native idle sprite beneath temporary animation overlays. All 323,520 pixels outside merchant strip preserved. Full stall animation and festival/Night Market appearances remain unverified; extra expressions are prepared without additional dialogue routes.

DesertTraderAudit called native Desert.OnDesertTrader, then verified portrait pixels, unchanged inventory count, greeting and money. Actual 1920x1080 shop render inspected. No transaction or farm load. This verifies the native shop handler, not its map interaction tile or individual trade prices. Evidence: desert-trader-checks.json and desert-trader-shop-preview.png. Audit log mistakenly labels its shop as Traveler; actual code and result use DesertTrade.

Version 0.7.18 installed with backup. Shipped build zero warnings/errors; audit has existing unrelated Krobus parade name-field advisory. Fresh loader 05:22:32 local on 2026-09-05 passed 121/121 textures with no runtime warnings/errors. ZIP 125 files checksum verified; Blender refreshed with 121 packed textures. Owned process stopped; temporary audit archived outside Mods. Full NPC overhaul remains incomplete.
## Previously verified batch: 0.7.17 — Traveling Cart merchant

Updated the merchant's 16x14 cart-window appearance and three native 6x3 blink overlays in Cursors. The preparation check preserved 1,587,946 pixels outside these regions. Added six portrait expressions; the shared Traveler shop uses neutral cell 0. Other expressions are prepared but currently have no dialogue routes. Reviewed prepared cart, blink sequence, portrait sheet and actual 1920x1080 shop render.

TravelingMerchantAudit opened the native Traveler shop in a detached Forest and verified portrait pixels, unchanged stock count and greeting, and unchanged player money. It did not test the Forest interaction tile, individual prices, transactions, full cart animation, or festival appearances. No farm loaded. Evidence: traveling-merchant-checks.json and traveling-merchant-shop-preview.png.

Version 0.7.17 installed with backup. Shipped build zero warnings/errors; temporary audit retains its existing Krobus parade name-field advisory. Fresh loader at 05:16:02 local on 2026-09-05 passed 119/119 textures with no runtime warnings/errors. ZIP 123 files checksum verified; Blender project refreshed with 119 packed textures. Owned game process stopped; audit archived outside Mods. Full NPC overhaul remains incomplete.
## Previously verified batch: 0.7.16 — Hat Mouse

Updated Hat Mouse's13x10 shop-window head in Cursors(632,1969), preserving sign/building/counter and1,588,094 outside atlas pixels. Created six64px portrait expressions. Native HatMouse shop displays neutral cell0 through MenuChanged; additional expressions are prepared but have no assigned dialogue routes. Shared-atlas regeneration with Grandpa preserved the complete checksum. Prepared shop crop and portrait sheet inspected.

HatMouseAudit invoked native Forest.checkAction on Buildings tile1972 with a temporary achievement, then verified portrait pixels, unchanged inventory/greeting and money. Rendered and inspected actual shop at1920x1080 with merchant portraits enabled: hat-mouse-shop-preview.png. Initial detached Forest lacked its map; audit now loads Maps/Forest before locating the interaction tile. Test state restored; no farm loaded or purchase made. Actual Forest scene, purchases and narrower shop layouts remain unverified.

Version0.7.16 installed with backup. Shipped mod build zero warnings/errors; temporary audit retains its documented unrelated name-field advisory. Fresh loader at05:03:25 local on2026-09-05 passed118/118 textures without runtime warnings/errors. ZIP122 files checksum verified; Blender118 packed images. Owned process stopped; temporary audit archived outside Mods to audit-hat-mouse-20260905-0504. Full NPC overhaul remains incomplete.

## Previously verified batch: 0.7.15 — Sea-monster beach-event sheet

Modernized all24 native32x32 frames in Characters/SeaMonsterKrobus, including surfacing, tentacle extension and front/right/left Krobus riding poses. Explicit row crops share a fixed logical height so partial surfacing forms are not enlarged. Prepared atlas inspected. Original Beach event extracted to work/SeaMonsterKrobus/event-source.json. No creature dialogue appears in that script; existing Krobus portraits remain in use.

SeaMonsterAudit passed native temporary actor creation, complete texture pixel comparison and all five native explicit animation sequences. The detached event is marked terminal solely to prevent automatic Event.Update from progressing into uninitialized scene systems after each command; actual native animation setup and frame advancement are checked. This does not prove full beach-event execution or directional movement. No farm loaded. Evidence: sea-monster-checks.json; design notes document test-setup corrections.

Version0.7.15 installed with backup. Shipped build zero warnings/errors; temporary audit retains its documented unrelated AvoidNetField advisory. Fresh loader at04:54:36 local on2026-09-05 passed117/117 textures without runtime warnings/errors. ZIP121 files checksum verified; Blender117 packed images. Owned process stopped; temporary audit archived outside Mods to audit-sea-monster-20260905-0455. Full NPC overhaul remains incomplete.

## Previously verified batch: 0.7.14 — Flying parade character frames

Updated fourteen character frames in the mixed Characters/KrobusRaven sheet: five raven carrying Krobus, five cauldron-rider and four carpet-witch poses. Patched native32x32/32x39 regions; all10,368 outside pixels preserved. Broad magenta removal initially erased carpet colors; narrowed the key and inspected corrected prepared-parade.png before installation. Lower banners, planes, pig/vehicle and separate portrait coverage remain unfinished; this is not full-sheet or full-event completion.

KrobusParadeAudit invokes native SpecificTemporarySprite krobusraven, verifies exact runtime texture pixels and native frame lengths/rectangles/delays/intervals, then advances each isolated animation through all14 frames and verifies leftward motion. Passed with a mapless detached location after correcting title-screen map-season context errors. No farm loaded. Full timed scene remains unverified. Evidence: krobus-parade-checks.json; preparation and design notes in work/KrobusRaven.

Version0.7.14 installed with backup. Shipped mod build zero warnings/errors; temporary audit has one documented advisory about assigning its detached location's read-only-name backing field. Fresh loader at04:46:11 local on2026-09-05 passed116/116 textures without runtime errors/warnings. ZIP120 files checksum verified; Blender116 packed images. Owned process stopped; temporary audit archived outside Mods to audit-krobus-parade-20260905-0447. Full NPC overhaul remains incomplete.

## Previously verified batch: 0.7.13 — Krobus trench-coat disguise

Modernized all sixteen native16x24 directional walking frames in Characters/Krobus_Trenchcoat. Reused the approved modern base Krobus portrait expression8 for the standalone64x64 Portraits/Krobus_Trenchcoat asset, preserving exact matching pixels. Native appearance data and dialogue selection are unchanged. Prepared sprite atlas and rendered native movie dialogue inspected. Other Krobus appearances, including Raven/SeaMonster, remain unfinished.

KrobusDisguiseAudit passed native ChooseAppearance in MovieTheater, sprite/portrait pixel checks, actual localized movie dialogue expression8 and normal sprite restoration in Sewer. Initial test assumptions about raw localized tokens and detached location resolution were corrected; details in work/Krobus_Trenchcoat/design-notes.md. Evidence: krobus-disguise-checks.json and krobus-disguise-preview.png. No farm loaded. Full screening and live walking animation remain unverified.

Version0.7.13 installed with backup; build zero warnings/errors. Fresh loader at04:37:05 local on2026-09-05 passed115/115 textures with no runtime warnings/errors. ZIP119 files checksum verified; Blender115 packed images. Owned verification process stopped; temporary audit archived outside Mods to audit-krobus-disguise-20260905-0438. Full NPC overhaul remains incomplete.

## Previously verified batch: 0.7.12 — Winter mystery portrait

Native-source inspection corrected the initial assumption that the winter mystery uses the Data/Characters ??? entry. BusStop event520702 explicitly uses Krobus; Town.initiateMagnifyingGlassGet and mgThief_speech also use Characters/Krobus with16x24 cells. Existing modern Krobus art already covers those sprites. Added an apologetic expression2 portrait to the native mgThief_speech dialogue, retaining the hidden name ???. Reward callback and dialogue text are unchanged. Source evidence: mystery-event-source.json plus Town.cs. The separate ??? / Monsters\\Shadow_Brute alias remains unresolved and must not be marked complete from this work.

WinterMysteryAudit invoked the native speech handler and verified original text, hidden name, exact portrait pixels, expression and unchanged magnifying-glass reward callback. It did not invoke the reward, load a farm or alter quest ownership. Actual dialogue panel rendered and inspected: winter-mystery-preview.png. Full bush-jump, reward and departure sequence remains unverified.

Version0.7.12 installed with backup; build zero warnings/errors. Fresh loader at04:27:32 local on2026-09-05 passed113/113 textures without runtime errors/warnings; ZIP117 files checksum verified. Artwork did not change, so existing113-image Blender file remains current. Verification process stopped; temporary audit archived outside Mods at audit-winter-mystery-20260905-0428. Full NPC overhaul remains incomplete.

## Previously verified batch: 0.7.11 — Welwick

Updated all five Welwick TV frames: two speaking frames, neutral fortune pose and both extreme-luck poses. Created a six-expression portrait and attached it to native fortune-teller openings and forecasts. Uses original localized forecast strings for expression selection; native dialogue callbacks, luck calculations, animated symbols and TV controls are preserved. Both shared atlases retain Grandpa/Bookseller artwork, and rerunning those preparation scripts preserved complete atlas checksums. Prepared artwork and three rendered dialogue/screen previews inspected.

WelwickAudit passed eight native fortune flows, verifying opening/forecast expression, portrait pixels, callbacks and shutoff; weather excluded. Evidence: welwick-interaction-checks.json and welwick-*-preview.png. No farm loaded. Full furnished-room placement, animation timing and non-English runtime verification remain outstanding.

Version0.7.11 installed with backup; build zero warnings/errors; fresh loader at04:23:17 local on2026-09-05 passed113/113 textures with no runtime warnings/errors. ZIP contains117 checksum-verified files; Blender has113 packed textures. Verification process stopped; temporary audit moved outside Mods to audit-welwick-20260905-0425. Full NPC objective remains incomplete, including other missing actors/portraits, seasonal/special appearances and scene review.

## Previously verified batch: 0.7.10 — Trash Bear cleanup-event poses

Updated the two 46x56 closed/open leaf-umbrella poses used by native trashBearUmbrella1 and trashBearTown events in LooseSprites/Cursors2. Merged with Grandpa's existing thumbs-up region; all 76,768 pixels outside the new region were preserved. Grandpa preparation now preserves other registered shared-atlas regions; regenerating Grandpa left the complete merged atlas checksum unchanged. Prepared two-pose preview inspected after correcting a padding/resizing operation-order error. Sources and details: work/TrashBear/flying-design.md and flying-validation.json.

Version 0.7.10 installed with backup. Build zero warnings/errors; fresh SMAPI loader at 04:15:16 local on 2026-09-05 passed 112/112 textures with no runtime warnings/errors. ZIP contains 116 checksum-verified files; Blender artwork file regenerated with 112 packed textures. Evidence: loader-0.7.10.txt. Owned verification process stopped without loading a farm. Full cleanup-event progression and animation remain unverified. Full NPC objective remains incomplete, including missing actors/portraits, seasonal appearances, special poses and scene review.

## Previously verified batch: 0.7.9 — Trash Bear

Updated all fourteen occupied 32x32 TrashBear cells, including eating and pan-flute poses; cells 10/11 remain unused. Created a six-expression portrait. Main-artwork count is 55 actor/variant entries; 112 textures registered. TrashBear has no normal dialogue. A brief portrait panel appears at lower left during native food requests and eating, with eager/happy/grateful expressions; it hides on leaving the location and during menus. The existing wanted-item bubble and food handling remain native.

TrashBearAudit invoked the native empty-hand request and eating animation handler without submitting food. Verified eager, eating and satisfied expression selection and hiding after leaving. Rendered and inspected the eager portrait panel at 1280x720. Evidence: trash-bear-interaction-checks.json and trash-bear-portrait-preview.png. Full-world HUD overlap, actual food submission, timed animation and cleanup-event progression remain unverified. Separate umbrella/flying cleanup artwork remains original and needs updating; main sprite completion does not complete Trash Bear's appearances.

Version 0.7.9 installed with previous-version backup. Build zero warnings/errors; fresh loader at 04:05:29 local on 2026-09-05 passed 112/112 textures. ZIP contains 116 checksum-verified files; Blender packs 112 images. Evidence: loader-0.7.9.txt. Verification process stopped without loading a farm; temporary audit moved outside Mods to audit-trash-bear-20260905-0406. Full NPC objective remains incomplete, including remaining actors/missing portraits, seasonal/special appearances and scene review.

## Previously verified batch: 0.7.8 — Alternate raccoon family sheet and shop verification

Updated all 64 cells of Characters/mrs_raccoon. Original-byte matching identified 47 cells identical to frames of the shared raccoon sheet; their modern counterparts are reused exactly. Seventeen unique cells were generated, including eight couple poses. Extra front-facing poses initially came back brown; corrected through image generation to Mrs. Raccoon's lavender-gray before installation. Prepared atlas inspected. Event.cs uses the alternate texture for family scenes. Extracted raccoon_bundle_menu and inspected it: it contains interface decoration, not NPC artwork, so it remains unchanged.

RaccoonShopAudit invoked the real female Raccoon.activate, waited for MenuChanged, and verified native ShopId Raccoon plus exact MrsRaccoon portrait pixels. Passed; no transactions or farm loading. Shop wide-screen rendering, purchases, request submissions and full family-event animation remain unverified. Evidence: artifacts/npc-modern/evidence/raccoon-shop-checks.json. Main actor/variant count remains 54; registered textures now 110.

Version 0.7.8 installed with previous-version backup. Build zero warnings/errors; fresh loader at 03:59:07 local on 2026-09-05 passed 110/110 textures. ZIP contains 114 checksum-verified files; Blender packs 110 images. Evidence: loader-0.7.8.txt. Verification process stopped; temporary audit moved outside Mods to audit-raccoon-shop-20260905-0400. Full NPC objective remains incomplete, including remaining actors/missing portraits, seasonal/special appearances and live scene verification.

## Previously verified batch: 0.7.7 — Shared raccoon sheet and new portraits

Updated all 64 cells of Characters/raccoon, which the live Raccoon subclass uses for both adults and includes family poses. Created separate six-expression portraits for MrRaccoon and MrsRaccoon. Main-artwork count is now 54 actor/variant entries; 109 registered textures. Four 4x4 generation groups are packed into the original 8x8 native 32px frame layout. Prepared review caught magenta residue near several feet; broader saturated-magenta key removal corrected it before installation. Source references, generated art, prompts and validation are in work/Raccoons.

RaccoonPortraits attaches Mr. Raccoon's portrait to matching current-language Raccoon dialogue strings in Forest and supplies Mrs. Raccoon's portrait to native ShopMenu with ShopId Raccoon. Native text, request submission and shop logic are preserved. Temporary audit passed attachment for 15 current-language lines and invoked the real male Raccoon.activate greeting. Evidence: raccoon-interaction-checks.json. Shop attachment/display, transactions, bundle submissions, expression-by-expression rendering and live motion remain unverified. The separate Characters/mrs_raccoon sheet and any embedded raccoon_bundle_menu artwork remain original and need inspection/update.

Version 0.7.7 installed with previous-version backup; build zero warnings/errors. Fresh loader at 03:52:37 local on 2026-09-05 passed 109/109 textures. ZIP contains 113 checksum-verified files; Blender packs 109 images. Evidence: loader-0.7.7.txt. Verification process stopped without loading a farm; temporary audit moved outside Mods to audit-raccoon-20260905-0353. Full NPC objective remains incomplete, including remaining actors/missing portraits, alternate/seasonal/special appearances and scene review.

## Previously verified batch: 0.7.6 — Gil rocking animation and dialogue review

Live map export proved Gil has four animated tiles with eleven animation steps using three unique 32x32 poses: (176,656), (176,624), and (208,656). The previous batch updated only the first. All three are now modernized through separate PatchAreas on Maps/townInterior. Verified 553,984 unrelated pixels unchanged. Source animation frames are in evidence/guild-animation-frames.json; generation prompts and contact sheet are in work/Gil. Actor count remains 52 and texture count 106.

GilAudit verified the native Guild actor uses the exact updated portrait and opened both ComeBackLater and Snoring dialogue routes. Captured and inspected both actual native dialogue boxes at 1280x720 with complete text: modern portrait fits and text is readable. First captures preceded text reveal; audit corrected to reveal text before capture. Evidence: gil-interaction-checks.json, gil-ComeBackLater-preview.png and gil-Snoring-preview.png. Reward flow, startled-expression dialogue and full Guild animation rendering remain unverified.

Version 0.7.6 installed with previous-version backup. Build zero warnings/errors; final loader passed 106/106 textures and Gil audit. ZIP contains 110 checksum-verified files; Blender packs 106 images. Evidence: loader-0.7.6.txt. Final audit previews generated 03:43:48 local on 2026-09-05. Verification process stopped without loading a farm; temporary audit moved outside Mods to audit-gil-20260905-0344. Full NPC objective remains incomplete, including remaining actors/missing portraits, seasonal/special appearances and scene review.

## Previously verified batch: 0.7.5 — Gil

Updated Gil's two native portrait slots (asleep and startled) and verified seated Guild map figure. Main-artwork count is 52 actor/variant entries, with 106 registered textures. The 32x32 region at Maps/townInterior (176,656) maps to tiles 1323,1324,1355,1356 used at Guild coordinates 11/12,11/12 in the exported map. Preparation independently verified all 556,032 pixels outside this region unchanged. No new dialogue routing is needed: AdventureGuild creates Gil with Portraits/Gil and uses native NPC dialogue.

Other Gil/chair poses visible nearby in the shared sheet remain original; their actual usage and any animation need further inspection. Only the confirmed seated region is updated. Prepared sprite and portrait previews inspected; actual Guild rendering, snoring dialogue, reward expressions and menu behavior remain unverified. Source references, prompts and validation are in work/Gil; preparation is scripts/prepare-gil-art.mjs.

Version 0.7.5 installed with previous-version backup. Build zero warnings/errors; fresh loader at 03:37:09 local on 2026-09-05 passed 106/106 textures. ZIP contains 110 checksum-verified files; Blender packs 106 images. Evidence: artifacts/npc-modern/evidence/loader-0.7.5.txt. Verification process stopped without loading a farm. Full NPC goal remains incomplete, including remaining actors/missing portraits, seasonal and special appearances, Gil's additional map poses and live scene review.

## Previously verified batch: 0.7.4 — Gourmand Frog

Added Gourmand's four native 32x32 poses in a 2x2 sheet and new six-expression 128x192 portrait. Main-artwork count is 51 actor/variant entries, with 104 registered textures. Prepared atlas previews inspected. General preparation now supports two-column native sheets; re-preparing Bear and female Kel left both existing sprite files unchanged.

GourmandPortraits matches localized Gourmand dialogue strings, decorating plain dialogue and questions in IslandFarmCave or the crop-inspection event. It refreshes expression selection when dialogue advances. Neutral, delighted, disappointed, eager, surprised and grateful portraits are authored; surprised has no current native line mapping. It uses its own portrait panel despite the native hidden NPC object's original SafariGuy portrait reference. No native text, crop checks, rewards or callback code changed. Other languages use their localized source strings but were not independently run.

Temporary audit passed attachment for 32 current-language lines, excluded unrelated text, called native TalkToGourmand and invoked its question callback; Yes/No keys and callback clearing remained correct. Evidence: artifacts/npc-modern/evidence/gourmand-interaction-checks.json. Full scene rendering, per-expression visual review, actual crop validation, event progression and rewards remain unverified.

Version 0.7.4 installed with previous-version backup. Build zero warnings/errors; fresh loader 03:32:22 local on 2026-09-05 passed 104/104 textures. ZIP has 108 checksum-verified files; Blender packs 104 images. Evidence: loader-0.7.4.txt. Verification process stopped without loading a farm; temporary audit moved outside Mods to audit-gourmand-20260905-0333. Full NPC overhaul remains incomplete, including remaining actors, missing portraits, seasonal/special appearances and live scene review.

## Previously verified batch: 0.7.3 — Both Kel variants

Added LeahExMale and LeahExFemale: each has eight native 16x32 sprite poses and a new six-expression 128x192 portrait. Main-artwork count is now 50 actor/variant entries; 102 textures are registered. Preserved front-facing walk, crossed arms, falling and fallen poses. Per-pose height support keeps the final fallen frame shorter (18px), rather than stretching it to a standing height. Prepared atlas previews were inspected.

Native Event source creates an actor named LeahEx with the corresponding sprite variant. Kel speaks through plain message commands in Data/Events/Forest, not NPC speak commands. KelPortraits decorates newly opened KEL-prefixed messages with the matching variant and neutral, pleased, sad or angry expression. Original message text and choices remain unchanged. Six expressions are authored; surprised and embarrassed are available but not currently routed because the native Kel message lines do not require them. English routing is covered; other languages remain unverified.

Temporary KelAudit invoked real Event.DefaultCommands.Message for both variants, four routed emotions and unrelated messages: all ten cases passed, original text preserved and portrait pixels matched the correct variant. This verifies attachment, not rendered full-event appearance. Evidence: artifacts/npc-modern/evidence/kel-interaction-checks.json. Full picnic branches, art-show motion and visual dialogue layout remain unverified.

Version 0.7.3 installed with a previous-version backup. Build passed with zero warnings/errors. Fresh loader at 03:26:28 local on 2026-09-05 passed 102/102 textures. ZIP has 106 checksum-verified files; Blender packs 102 images. Evidence: loader-0.7.3.txt. Verification process stopped without loading a farm; temporary audit mod moved outside Mods to audit-kel-20260905-0327. Full NPC goal remains incomplete: remaining actors/missing portraits, seasonal/special appearances and scene review.

## Previously verified batch: 0.7.2 — Forest Bear

Added modern Forest Bear artwork: 48 actors have main artwork, with 98 registered textures. Updated all eighteen occupied 32x32 sprite cells and four portrait slots. Independently verified the two unused cells are byte-identical to the original. The Woods event creates Bear with explicit 32x32 dimensions and uses talking frames 16/17; the default 16x32 Data/Characters size does not apply to that event actor. Source evidence, prompts and prepared previews are in work/Bear.

Preparation supports native frameWidth 32. Marcello and Dwarf regression preparation left all four output images unchanged. Version 0.7.2 installed with a previous-version backup; build passed with zero warnings/errors. Fresh loader at 03:19:13 local on 2026-09-05 passed 98/98 textures. ZIP contains 102 checksum-verified files; Blender packs 98 images. Evidence: artifacts/npc-modern/evidence/loader-0.7.2.txt. Verification process stopped without loading a farm.

Bear's actual forest events, dialogue and motion remain unverified. Full NPC goal remains incomplete, including remaining actors and missing portraits, seasonal and special appearances, Bookseller live animation/shops, Birdie style consistency, Vincent sleep correction and scene review.

## Previously verified batch: 0.7.1 — Bookseller stall

Updated the Bookseller's embedded town-stall body, blink and four hand poses in LooseSprites/Cursors_1_6. The bottom strip is hand animation, not mouth animation as earlier notes suggested. The mod now supports several separate patch rectangles in one shared texture. Preparation verifies all 261,056 pixels outside the three regions are unchanged and preserves the original opacity stencil. Reviewed the stall preview and eight pose combinations; actual town animation remains unverified.

Version 0.7.1 installed with a backup of the previous mod. Build passed with zero warnings/errors. Fresh game loader at 03:14:39 local on 2026-09-05 passed 96/96 registered textures, including all three stall patches. ZIP has 100 checksum-verified files; Blender packs 96 images. Evidence: artifacts/npc-modern/evidence/loader-0.7.1.txt and work/Marcello/stall-validation.json. Verification process closed without loading a farm. There are still 47 actors with main artwork; this batch improves one existing actor.

Full NPC goal remains incomplete. Remaining work includes other actors and missing portraits, seasonal and special appearances, Bookseller live animation and wide-screen shop/transaction checks, Birdie style consistency, Vincent sleep correction, and scene review.

## Additional verification: Bookseller integration, 0.7.0

Added a temporary BooksellerAudit fixture. The real GameLocation Bookseller action displayed the happy portrait and preserved Buy/Trade/Leave response keys. The real Bookseller and BooksellerTrade menus received exact Marcello portrait pixels through the existing MenuChanged handler. Initial object-identity comparison was invalid across content-loader instances; replaced it with a pixel comparison. An experimental constructor patch was removed and the original integration re-tested successfully. No runtime behavior change was needed.

Rendered and inspected the greeting at 1280x720: portrait and text/choices are visible without overlap. Both shop previews at that width omit the native merchant portrait area due available space; attachment is verified but wide-screen shop rendering is still unverified. No purchases/trades were performed and no farm was loaded. Original title menu, day, location, read_a_book flag and movement/dialogue state restored. Mariner interaction regression also passed.

Evidence: artifacts/npc-modern/evidence/bookseller-interaction-checks.json, bookseller-greeting-preview.png, bookseller-buy-preview.png, bookseller-trade-preview.png and bookseller-audit-0.7.0.txt. Final audit launch at 03:02 local; its process was stopped. Temporary audit mod moved outside Mods to artifacts/npc-modern/audit-bookseller-20260905-0303. Release ZIP 0.7.0 rebuilt and all 99 files checksum-verified. Full NPC goal remains incomplete, including Bookseller shared-stall artwork and transaction/wide-screen checks.

## Previously verified batch: 0.7.0

Added Bookseller/Marcello: 47 actors have main artwork, with 95 registered textures. Created a new six-expression 128x192 portrait and updated eighteen occupied cells of Characters/Marcello, preserving two unused cells. Measured sprite column boundaries [0,315,557,762,1122] prevent splitting broad gestures. Built-in artwork, prompts and route notes are in work/Marcello.

PortraitPanel now adds the happy portrait after the Bookseller action creates its native buy/trade/leave question, and supplies the neutral portrait to Bookseller and BooksellerTrade ShopMenu instances. It retains native text, choices and transactions and uses the normal merchant portrait setting. This integration compiled and initialized without logged errors, but actual greeting, purchase/trade and settings behavior remain unverified; run those before declaring this route complete.

Version 0.7.0 installed, build zero warnings/errors. Actual loader passed 95/95 comparisons at 02:55:40 local on 2026-09-05. ZIP has 99 checksum-verified files; Blender packs 95 images. Evidence: artifacts/npc-modern/evidence/loader-0.7.0.txt. Release: dist/NpcModern-0.7.0.zip. Title-screen verification process closed without loading a farm.

Bookseller's visible town stall is still original shared art in LooseSprites/Cursors_1_6, including blink/mouth overlays. Texture extracted and exact draw rectangles documented in work/Marcello/design-notes.md. Updating Characters/Marcello does not update that stall. Full NPC goal remains incomplete, including this route, remaining actors/missing portraits, seasonal/special appearances and actual scene checks.

## Previously verified batch: 0.6.9

Added Henchman and Professor Snail (SafariGuy): 46 actors now have main artwork, with 93 registered textures. Henchman has twelve native 16x32 poses and three portraits; SafariGuy has twenty native poses including extra gestures and three portraits. Their unused portrait cells are preserved. SafariGuy's generated columns were uneven; prepared review caught shrunken middle figures. New optional spriteColumnEdges plus measured boundaries [0,331,563,791,1122] correct the splits. Corrected artwork was reinstalled before launch. Source art, built-in prompts and notes are in work/Henchman and work/SafariGuy.

Version 0.6.9 installed, build zero warnings/errors. Actual game loader passed 93/93 comparisons at 02:49:15 local on 2026-09-05. ZIP has 97 checksum-verified files; Blender packs 93 images. Evidence: artifacts/npc-modern/evidence/loader-0.6.9.txt. Release: dist/NpcModern-0.6.9.zip. Title-screen verification process closed without loading a farm. Swamp gift/departure and Professor Snail rescue/tent/museum scenes remain unverified visually.

Full goal remains incomplete: remaining actors and missing-portrait routes including Bookseller, embedded Gil/Welwick and traders; seasonal/special appearances; Krobus variants/events; Grandpa intro; Vincent sleep correction; Birdie style consistency; actual scene/motion review. SafariGuy's placeholder use by Gourmand Frog does not satisfy the frog's need for its own portrait.

## Previously verified batch: 0.6.8

Added Krobus main artwork: 44 actors now have main artwork, with 89 registered textures. All nine occupied portrait slots and all twenty-four 16x24 sprite cells are updated; the unused tenth portrait cell is preserved. Initial generation missed dark sprite frame 23; enlarged inspection identified the startled arms-raised pose and it was added. A baked checkerboard from that edit was caught in prepared-image review and corrected before installation. Source references, built-in prompts and corrections are saved in work/Krobus.

Version 0.6.8 installed, build zero warnings/errors. Actual game loader passed 89/89 comparisons at 02:40:53 local on 2026-09-05. ZIP contains 93 checksum-verified files; Blender packs 89 images. Evidence: artifacts/npc-modern/evidence/loader-0.6.8.txt. Release: dist/NpcModern-0.6.8.zip. Title-screen verification process closed without loading a farm.

Krobus is not fully complete: separate Trenchcoat sprite/portrait, KrobusRaven and SeaMonsterKrobus event sheets remain original, along with actual scene/motion review. Next initialized references are Henchman and SafariGuy. Full goal still includes all other remaining actors, missing portraits, seasonal/special appearances, Grandpa intro, Vincent sleep correction and Birdie style consistency.

## Previously verified batch: 0.6.7

Added Dwarf: 43 actors now have main artwork, with 87 registered textures. Dwarf's original Data/Characters Size is 16x24, and the 64x120 sheet has twenty frames. All four directional rows and the fifth gesture/sparkle row are updated, along with the native single 64x64 portrait. Preparation now supports frameHeight=24 and arbitrary complete native rows, keeping Dwarf's short proportions. Regression preparation of Fizz, Birdie, Governor and MrQi left all eight artwork files byte-identical.

Version 0.6.7 installed, build zero warnings/errors. Actual game loader passed 87/87 comparisons at 02:32:47 local on 2026-09-05. ZIP contains 91 checksum-verified files; Blender packs 87 images. Evidence: artifacts/npc-modern/evidence/loader-0.6.7.txt. Release: dist/NpcModern-0.6.7.zip. Title-screen verification process closed without loading a farm. Built-in generated artwork and prompts are in work/Dwarf. Walking/gesture and shop/dialogue scene checks remain outstanding.

Next initialized references: Krobus, Henchman, SafariGuy. Marcello requires a new portrait and dialogue-route investigation. Full goal remains incomplete, including remaining actors, missing portraits, seasonal/special appearances, Grandpa intro, Vincent sleep correction, Birdie style consistency and actual scene/motion review.

## Previously verified batch: 0.6.6

Added Mr. Qi (actor Mister Qi, texture MrQi): 42 actors now have main artwork, with 85 registered textures. Both portraits and all seven occupied sprite cells (0-4,8,12) are updated. The native sparse pose layout preserves front gestures, right/back/left views and nine unused cells; an independent byte comparison verified all nine unused cells match the original. References, built-in generation prompts and notes are in work/MrQi. Casino/Walnut Room and event visual review remain outstanding.

Version 0.6.6 installed, build zero warnings/errors. Actual game loader passed 85/85 comparisons at 02:28:48 local on 2026-09-05. ZIP has 89 checksum-verified files; Blender packs 85 textures. Evidence: artifacts/npc-modern/evidence/loader-0.6.6.txt. Release: dist/NpcModern-0.6.6.zip. Title-screen verification process closed without loading a farm.

Next references initialized: Dwarf, Krobus, Henchman, SafariGuy (Professor Snail). Marcello (Bookseller) initialization stopped because there is no original portrait; its work folder is partial and needs a newly created portrait plus dialogue-route investigation. Full NPC goal remains incomplete, including remaining actors, missing-portrait routes, seasonal/special appearances, Grandpa opening story, Vincent sleep correction, Birdie style consistency and actual scene review.

## Previously verified batch: 0.6.5

Added Grandpa's two portrait expressions and actual spirit/ thumbs-up sprites, bringing main artwork coverage to 41 actors and 83 registered textures. Characters/Grandpa is only a 1x1 placeholder and remains untouched. The visible spirit uses shared LooseSprites/Cursors region (555,1956,18,35), while thumbs-up uses Cursors2 (186,265,22,34), confirmed in Event.cs. scripts/prepare-grandpa-art.mjs patches these regions and checks every other source pixel is unchanged (1,587,594 and 81,172 pixels respectively). Shared sheets are registered with limited PatchArea, not replaced wholesale. Future other-NPC edits on these sheets must merge patches explicitly.

Version 0.6.5 installed; build zero warnings/errors. Actual game loader passed 83/83 comparisons at 02:24:39 local on 2026-09-05, including both shared regions and Grandpa portraits. ZIP contains 87 checksum-verified files; Blender packs 83 images. Evidence: artifacts/npc-modern/evidence/loader-0.6.5.txt. Release: dist/NpcModern-0.6.5.zip. Title-screen verification process closed without loading a farm. Built-in image generation originals, prompts and prepared region previews are in artifacts/npc-modern/work/Grandpa.

Grandpa is NOT fully complete: opening-story Minigames/jojacorps body and animated face/hand regions remain original, and evaluation/intro scenes need actual visual review. The original opening sheet is extracted and relevant source rectangles documented in work/Grandpa/design-notes.md. Other remaining actors, missing portraits, seasonal/special appearances, Vincent sleep correction and Birdie special-style refinement remain part of the full active goal.

## Previously verified batch: 0.6.4

Added Governor: 40 characters now have main artwork, with 80 registered images. The 64x128 sprite is a native event-pose sheet, not directional walking. All twelve occupied cells (0-4,8-14) are updated, including soup tasting and green sick/surprise reactions; unused cells 5,6,7,15 remain original. All four 64x64 portrait expression slots are updated. New spriteMode=native maps a 4x4 sheet directly without mirroring. Regression preparation of Fizz, Birdie and Marlon left their six artwork files byte-identical.

Version 0.6.4 installed, build zero warnings/errors. Real game loader passed 80/80 exact image comparisons at 02:18:15 local on 2026-09-05. Release ZIP has 84 checksum-verified files and Blender packs 80 images. Evidence: artifacts/npc-modern/evidence/loader-0.6.4.txt. Release: dist/NpcModern-0.6.4.zip. Title-screen verification process closed without loading a farm. Governor artwork, built-in generation prompts and layout notes are in artifacts/npc-modern/work/Governor. Actual Luau scene checks remain outstanding.

Next initialized actor: Grandpa. Full goal remains incomplete: remaining actors and missing portraits, special/seasonal appearances, Vincent sleep correction, Birdie special-style alignment, and runtime scene/motion review.

## Previously verified batch: 0.6.3

Added Birdie: 39 characters now have main artwork and 78 registered images. Three native portrait expressions and all 21 occupied 16-pixel base-sheet cells have updated pixels; the unused portrait slot and three unused sprite cells are preserved. Fishing uses two 32x32 frames (confirmed by NPC.cs birdie_fish setting SpriteWidth=32 and animationDescriptions frames 8/9). New specialWideRows support processes rods and figures together, retaining their connection. Existing eight special sheets remained byte-identical in a regression comparison.

Birdie's fishing and clasped-hand art is provisional: it is closer to the original chunky style than her new walking sheet. The image service rejected the requested style correction and produced no correction output. Style consistency and actual island fishing/dialogue review remain outstanding. Do not treat baseSpriteComplete as visual approval. References, built-in image-generation prompts and design notes are in artifacts/npc-modern/work/Birdie.

Version 0.6.3 installed; build zero warnings/errors. Actual game loader passed 78/78 pixel comparisons at 02:13:45 local on 2026-09-05. ZIP contains 82 checksum-verified files, Blender packs 78 images. Evidence: artifacts/npc-modern/evidence/loader-0.6.3.txt. Release: dist/NpcModern-0.6.3.zip. Verification title-screen process closed without loading a farm; audit remains outside Mods.

Governor and Grandpa remain initialized with original references only. Full NPC goal remains incomplete, including remaining actors, missing-portrait routes, special/seasonal appearances, Vincent sleep-frame correction, Birdie special-art style alignment and actual scene review.

## Previously verified batch: 0.6.2

Added Fizz: 38 characters now have main artwork, with 76 registered images. Fizz has his complete single 16x32 static sprite and all four native portrait cells: neutral, grin, worried and two-finger salute. Enlarged original references clarified his glasses and expression slots; rejected draft variants were replaced before installation. Builtin-imagegen artwork and final prompts are saved in artifacts/npc-modern/work/Fizz. His native dialogue route was checked in decompiled NPC.cs, but actual island interaction and visual review remain outstanding.

Version 0.6.2 is installed. Build succeeded with zero warnings/errors. The actual game loader passed 76/76 exact image comparisons at 02:06:55 local on 2026-09-05, including both complete Fizz textures. ZIP contains 80 checksum-verified files; Blender packs 76 images. Evidence: artifacts/npc-modern/evidence/loader-0.6.2.txt. Release: dist/NpcModern-0.6.2.zip. The verification title-screen process was closed without loading a farm. Audit mod remains outside Mods.

Birdie, Governor and Grandpa remain initialized with original references only. Continue remaining actors, missing-portrait routes, special/seasonal appearances, Vincent sleep-frame correction and actual scene review. Full NPC completion remains unproven.

## Previously verified batch: 0.6.1

Added Gunther, Morris and Bouncer. Main artwork now covers 37 characters and 74 registered images. New base sprite coverage: Gunther 7 occupied cells with all 9 unused cells preserved byte-for-byte; Morris all 24 cells including raised blue-paper and confrontation poses; Bouncer one static 16x32 image. Their native portrait layouts are preserved: Gunther/Bouncer single 64x64, Morris four expressions in 128x128. Gunther's sparse layout is handled by the new preserveUnusedSpriteCells preparation setting.

Corrected the generated Bouncer sprite to remove an invented beard and match his clean-shaven original/portrait. Corrected Morris's special sheet background before preparation. Builtin-imagegen outputs, original references and prompts are saved in work/{Gunther,Morris,Bouncer}. Gunther unused-cell equality was checked independently after preparation. Base-sheet coverage does not prove motion/event fidelity; actual scene checks remain outstanding.

Version 0.6.1 is installed. Build succeeded with zero warnings/errors. Actual game loader passed all 74 pixel comparisons at 01:56 local on 2026-09-05. ZIP contains 78 checksum-verified files and Blender contains 74 packed images. Evidence: artifacts/npc-modern/evidence/loader-0.6.1.txt. Release: dist/NpcModern-0.6.1.zip. Verification used the title screen only; its process was closed without loading a farm. The temporary audit mod remains outside Mods.

Next initialized visitor folders: Birdie, Fizz, Governor, Grandpa. They contain original references only. Continue all remaining actors and missing-portrait routes, unfinished special/seasonal appearances, Vincent's sleep-frame correction, and runtime visual/motion review. The full NPC goal remains incomplete.
## Previously verified batch: 0.6.0

Added Sandy, Wizard and Marlon: 34 characters now have main portraits and movement artwork. All three new characters have complete base-sheet artwork: Sandy 18 occupied cells plus 2 preserved unused cells, Wizard 23 plus 1 unused, Marlon 16. Sandy's extra poses preserve the wave sequence at frames 16-17, confirmed in animationDescriptions. Wizard's gesture, eyes-closed, downward-looking and surprised cells follow the original layout. Actual movement/activity scene checks and separate appearances remain outstanding.

Preparation now accepts native 64-pixel-wide single-column portraits as well as the existing 128-pixel sheets, preserving Marlon's single 64x64 portrait. A repeat preparation of Jas confirmed existing two-column portrait and full sprite outputs stayed byte-identical. A 128-pixel-high base sprite is now registered as a complete texture directly. Generated standing and step cells were mapped into the expected movement order. Artwork and builtin-imagegen prompts are in work/{Sandy,Wizard,Marlon}.

Version 0.6.0 is installed. Build: zero warnings/errors. Real game loader: 68/68 exact image comparisons passed at 01:44 local on 2026-09-05, including full texture checks for all three new characters. Release ZIP: 72 checksum-verified files. Blender: 68 packed images. Evidence: artifacts/npc-modern/evidence/loader-0.6.0.txt. The verification title-screen process was closed, with no farm loaded or save changed; audit mod remains outside Mods.

Next visitor work folders initialized with original references: Gunther, Morris, Bouncer, Birdie, Fizz, Governor and Grandpa. Those are only initialized, not generated or installed. The full objective remains incomplete, including remaining actors, missing-portrait dialogue routes, seasonal/special appearances and runtime visual/motion review. Vincent sleep-frame correction remains outstanding.
## Previously verified batch: 0.5.2

Finished the remaining base-sheet artwork for Jas (26 occupied cells, 2 preserved unused cells) and Leo/ParrotBoy (25 occupied cells, 3 preserved unused cells). Jas includes four jump-rope phases, back-facing gestures, reading, sitting, surprise and sleep. Leo includes seated, gesture, sleeping and back-facing sitting poses. Source images and builtin-imagegen prompts are saved in each work folder. Their winter variants and actual activity/motion scene review remain unfinished, so full character completion is not claimed.

Special-frame preparation now respects the smaller child heights, accepts explicit row boundaries, and supports a per-sheet alpha threshold. Leo's uneven generated row spacing was explicitly mapped. Jas uses threshold 64 to keep the thin rope visible after native-size reduction. Existing adult special outputs were confirmed byte-identical after the height change. Main movement pixels and original unused cells are asserted unchanged during special preparation.

Version 0.5.2 is installed. The build passed with zero warnings/errors; the real game loader passed all 62 image comparisons at 01:32 local on 2026-09-05, including full-sheet comparisons for Jas and Leo. The ZIP has 66 checksum-verified files and Blender has 62 packed images. The title-screen process was closed without loading a farm. Evidence: artifacts/npc-modern/evidence/loader-0.5.2.txt. Release: dist/NpcModern-0.5.2.zip.

The full objective remains active. Next work includes Vincent's remaining poses and sleep route, other visitors/residents/talking creatures, missing portraits and their dialogue integration, special/seasonal appearances, and runtime visual/motion checks. NPC.cs playSleepingAnimation reads the first frame number from animationDescriptions; Vincent currently points to 12, shared with left movement, and needs a targeted decision before complete animation fidelity can be claimed.
## Previously verified batch: 0.5.1

Added main portraits and movement sheets for Jas, Vincent and Leo (asset name ParrotBoy). There are now 31 characters and 62 registered images, all hash-verified through the actual game loader at 01:26 local on 2026-09-05. The build passed with zero warnings/errors, the ZIP contains 66 checksum-verified files, and Blender contains 62 packed images. No farm was loaded; the title-screen verification process was closed afterwards. The temporary audit mod remains outside Mods.

Preserved smaller child sprite heights: Jas and Leo 25 pixels, Vincent 23. Preparation now supports explicit sprite-cell ordering and searches clear gaps between generated rows to avoid neighboring hair/bow fragments. Jas and Leo generated direction columns were remapped to the game's direction rows; Vincent's side-facing images were flipped. Existing portrait slots and blank cells are preserved. Source images and builtin-imagegen prompts are in artifacts/npc-modern/work/{Jas,Vincent,ParrotBoy}.

Special poses and winter variants remain unfinished. Original special reference sheets were extracted for all three children. Data/animationDescriptions was extracted to artifacts/npc-modern/animation-descriptions.json: Jas jump rope uses 16-19, reading 22, sleeping 25; Vincent reading 24, playing 22-23, beach 18-19; Leo sleeping 23 and sitting up 24. Vincent's sleep entry names frame 12, so review that route against the generic left-facing row before claiming complete animation fidelity. Generated walking steps still need motion refinement and scene review.

Current release: dist/NpcModern-0.5.1.zip. Evidence: artifacts/npc-modern/evidence/loader-0.5.1.txt. The full NPC objective remains incomplete: other residents/visitors/talking creatures, missing portrait routes, all special/seasonal appearances and full runtime visual checks remain required.
## Previously verified batch: 0.5.0

Added Marnie, Jodi, Kent, Lewis, Linus, Pam, Pierre, Robin and Willy. Main portraits and all sixteen directional movement frames are now installed for 28 characters. All 56 registered images passed the real game loader pixel comparison at 01:15 local on 2026-09-05, with exact artwork SHA256 hashes. The build passed with zero warnings and errors. The ZIP contains 60 checksum-verified files; Blender contains 56 packed images.

Kent's entire base sprite sheet is updated, including three special poses and one preserved unused cell. Evelyn, George and the Old Mariner also have complete base sheets. Most characters still need special animations and separate seasonal/outfit variants. Generated walking motion still needs scene review and refinement; loader checks prove correct image loading only.

Corrected Linus's generated portrait background before installation. Corrected Willy's missing fourth portrait: the original bottom-right cell contains his pipe-smoking expression and is not blank. Corrected Pierre and Willy's side-facing orientation during preparation. The temporary audit again passed the Old Mariner's real interaction and purchase-question checks without loading a farm, and was moved outside Mods after verification. No saves or original game files were changed.

Current artifacts: dist/NpcModern-0.5.0.zip; artifacts/npc-modern/npc-modern.blend; artifacts/npc-modern/evidence/loader-0.5.0.txt. Images and prompts are in artifacts/npc-modern/work/{Name}. Coverage is tracked in coverage.json: 66 actor entries, 229 direct/shared asset records, 56 currently registered and verified.

Shared actor research is recorded in special-actor-routes.md. Gil's visible default map region is confirmed; Welwick and Gourmand Frog need targeted artwork and actual dialogue integration. Next: children, visitors, remaining talking creatures, then continue special/seasonal coverage and motion review. The full NPC goal is not complete.
## Previously verified batch: 0.4.0

Added Evelyn, George and Gus, bringing main portrait and movement coverage to 19 characters. All 38 registered assets passed the real game loader's pixel comparison on 2026-09-05 at 00:48 local time. Verification now records each artwork file's SHA256, and the coverage ledger requires the exact current hash in the log. The build passed with zero warnings/errors; the 0.4.0 ZIP contains 42 checksum-verified files. No farm was loaded and the temporary audit mod remained outside Mods.

Corrected the earlier three-direction movement limitation across every prepared humanoid: AnimatedSprite.AnimateLeft explicitly uses frames 12–15, so the fourth row now mirrors each corresponding modern right-facing frame. All sixteen movement frames are updated. The old references below to twelve frames or 96 pixels describe the previous batch and are superseded by this correction.

Evelyn and George also have fully updated base sprite sheets: Evelyn has 21 occupied cells plus 3 preserved unused cells; George has 22 occupied cells plus 2 preserved unused cells. This includes gardening and pie poses for Evelyn, and sleeping and leek-gift poses for George. George's leek was confirmed from the game's JoshHouse event data (`showFrame George 21` beside the leek dialogue) and corrected from the first generated plant interpretation. George remains seated in his wheelchair in every frame. His portrait was regenerated with head-and-shoulders framing. Winter/other separate appearances remain unfinished.

Current artifacts: dist/NpcModern-0.4.0.zip, artifacts/npc-modern/npc-modern.blend (38 packed images), artifacts/npc-modern/evidence/loader-0.4.0.txt. scripts/prepare-npc-art.mjs now prepares 16 directional frames and automatically applies available special sheets. scripts/prepare-npc-special-art.mjs now supports 64-pixel-wide base sheets of height 160 through 224; other shapes still need deliberate support. The left-facing helper preserves every pixel outside its fourth row.

Next: continue town residents (Marnie, Jodi, Kent, Lewis, Linus, Pam, Pierre, Robin, Willy), then visitors and missing-portrait actors. Gus's remaining special poses and all separately stored winter variants remain pending. The full NPC goal is not complete.

## Verified batch: 2026-09-05

NPC Modern 0.3.0 is installed under Mods/AbigailModern. Its stable ID remains David.AbigailModern. All 32 registered artwork assets passed the game's pixel comparison checks. The source DLL built with zero warnings and errors, and the release ZIP contains 36 checksum-verified files.

Prepared and installed main portraits and first twelve walking frames: Abigail, Alex, Caroline, Clint, Demetrius, Elliott, Emily, Haley, Harvey, Leah, Maru, Penny, Sam, Sebastian, Shane. The Old Mariner has a complete one-frame static sprite and a new six-expression portrait sheet. This is 16 characters with artwork, not 16 characters with every appearance finished.

The Old Mariner's actual Beach.checkAction path was exercised using temporary in-memory actors without loading a farm. Both normal dialogue and the pendant purchase question received portraits. Original text, response keys and native question handling were preserved. The purchase screen was rendered through the game's own drawing code and inspected; the portrait fits above the dialogue at 1280x720. Other window sizes and languages still need visual coverage. Buying a pendant against an actual saved farm was not exercised.

Evidence:

- artifacts/npc-modern/evidence/registered-loader-check.txt
- artifacts/npc-modern/evidence/mariner-interaction-checks.json
- artifacts/npc-modern/evidence/mariner-purchase-preview.png
- artifacts/npc-modern/work/{Name}/validation.json
- dist/NpcModern-0.3.0.zip
- artifacts/npc-modern/npc-modern.blend (32 packed image references)

The temporary NPC Art Audit mod is excluded from the release and should remain outside the installed Mods folder between verification sessions. Its initial integration fixture ran too early and disposed the title menu; the corrected fixture detaches the menu without disposing it, waits for the title screen update, and restores temporary state.

## Outstanding work

The persistent full-NPC goal remains active. coverage.json inventories 66 actor entries and 227 direct or newly created texture records, with unresolved embedded/shared actors explicitly listed. Do not infer full completion from the registered asset check count.

- Finish town residents and families, children and elders; then visitors, quest actors, merchants and talking creatures.
- Finish special animation frames and separate winter/beach/outfit textures. Current humanoid movement patches affect the top 128 pixels of each sprite atlas; Evelyn, George, Kent and Mariner have complete base sheets.
- Inspect walking motion in actual scenes; generated steps currently have less motion variation than the originals.
- Create needed missing portraits and connect them to actual dialogue/shop routes. The Mariner is the first completed route.
- Reconcile shared/embedded sprites and uncertain aliases in coverage.json.
- The optional scope question about pets, farm animals and ordinary monsters has no submitted answer. Continue people and talking creatures without treating the default choice as authorization.

Useful next standard residents: Evelyn, George, Gus, Marnie, Jodi, Kent, Lewis, Linus, Pam, Pierre, Robin, Willy. Inspect original layouts before generation; George uses a wheelchair and cannot use generic walking anatomy prompts.











Release 0.7.28 package: 175 ZIP files checksum verified; Blender asset board refreshed with 171 textures.


























