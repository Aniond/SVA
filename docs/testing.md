# Verification record

Target: Stardew Valley 1.6.15 / SMAPI 4.5.2, Windows, solo play.

## Global romance 0.2.0

- Release and native harness builds succeeded with zero warnings/errors. All 164 core tests passed. The final native run passed 280 checks: global ownership/migration (34), native relationship transactions (16), date rules (22), actual scenes and twelve-character availability (31), witnesses/gossip (8), conversation ownership/portrait mappings (91), weather travel (2), delivered warnings (9), and existing Abigail promises/tree/memories (67). Evidence: `artifacts/romance-final-native-summary-0.2.0.json` and `artifacts/smapi-romance-final-checks-0.2.0.log`.
- Actual Solace overnight saves and a full restart preserved a booking, an eyewitness report forwarded by another NPC, and an acknowledged repair agreement. A separate temporary family fixture exercised native pendant acceptance, the wedding ceremony, another overnight/restart, an actual adoption arrival, deferred divorce while arrival was pending, and final divorce. The final check confirms the child, money, inventory/protected items and ownership remain; the spouse room is removed and Sam returns to SamHouse. Reports: `romance-overnight-restart-0.2.0.json`, `romance-marriage-native-0.2.0.json`, `romance-marriage-restart-0.2.0.json`, `romance-adoption-deferral-0.2.0.json`, and `romance-divorce-after-adoption-0.2.0.json` under `artifacts`.
- All four original Solace save files were restored byte-for-byte after the temporary family scenarios. Backup: `artifacts/backups/global-romance-fixture-original/Solace_448236644`. Restoration evidence: `artifacts/romance-original-save-restored-0.2.0.json`. No test marriage, child, year jump or separation remains on the original farm.
- Two batches of 72 live Gemini samples completed without provider errors. The final batch covers all twelve characters across friendship, friendship-only, dating, conflict, separation and marriage. Reviewed boundaries, source attribution, authored repair requirements and claimed game effects. See `artifacts/romance-ai-review-0.2.0.md`. Occasional minor household assumptions and repeated phrasing remain dialogue-quality limitations; Gemini cannot apply those claims as game actions.
- Journal presentation was reviewed at normal and 150% UI scale. Native scenes advanced the clock by sixty minutes and restored characters and controls on completion, early departure and interruption. Authored offline responses were checked. Long progression thresholds and cross-farm isolation use controlled fixtures, not months of ordinary play or a second player-owned farm.
- Fixed two issues found during actual overnight tests: the native rain buffer must retain at least seventy entries for travel/sleep, and divorce needs both native farmer cleanup and the NPC return-home call. Proposal dialogue cleanup was also verified through an actual wedding and sleep cycle.

Player checks still requested:

1. Talk to several villagers, use **Bond** to choose friendship or interest, and check that F6 shows separate histories. Abigail should retain her existing tree and art.
2. Book and finish each activity in ordinary play; inspect arrival prompts, physical scene placement, dialogue, and return to schedules. Check seasonal schedule combinations and UI scales on your setup.
3. Play through longer courtship pacing and varied couple conversations. Report repetitive wording, unsupported personal details, or a portrait that feels mismatched.
4. Opposite-sex pregnancy presentation, unusual family layouts and compatibility with other relationship mods need additional manual coverage. The shared pending-arrival guard and an actual adoption were verified; no destructive family scenario should be tried on an unbacked-up farm.

See `global-romance.md` for controls and rules. The final package excludes the test harness, generated reports, saves and credentials. Test sessions mute audio without changing saved volume settings.

## Nearby crafting 0.1.11

- Release/harness builds passed without warnings/errors; 29 core tests passed. Eight live checks passed through a native crafting page: nearby sources, protected/range/fridge/lock count filtering, actual crafting with backpack-first consumption across two chests, insufficient-material rejection, removed-source revalidation, full-output-space protection, cursor compatibility, and cooking exclusion.
- Actual inventories, location, crafting-recipe counters, and the menu were restored after temporary fixtures. No save was written. Results/log: `artifacts/nearby-crafting-results-0.1.11.json`, `artifacts/smapi-controls-0.1.11.log`.
- Player checks remain: open the ordinary crafting tab beside a stocked chest, inspect combined tooltip counts, craft once and with Shift, verify F9-protected materials, then move beyond six tiles and reopen. Check tooltip readability at your UI scale and the full save/reload cycle. Cooking and modified crafting-menu compatibility are not newly certified by this feature.

## Machine indicators 0.1.10

- Successful build with zero warnings/errors; 29 core tests passed. Eight live checks passed: idle input status, processing-time formatting, read-only status inspection, unfinished-output classification, ready item names, and chest/resource/hidden-object exclusion.
- Native framebuffer screenshot reviewed at 1920×1080: idle, working, and ready markers visible; the working hover text includes the remaining game time. Image: `artifacts/machine-indicators-0.1.10.png`. Results/log: `artifacts/machine-indicator-results-0.1.10.json`, `artifacts/smapi-controls-0.1.10.log`.
- Manually verify a full machine cycle, input/collection, markers at your UI scale, and automatic producers. Displays use native machine metadata and timer values; they do not evaluate a complete missing-ingredient list. Initial harness requests ran before load; the subsequent fresh session completed the checks. Temporary demonstration furnaces were not saved.

## Crop protection 0.1.9

- Release build passed without warnings/errors; 29 core tests passed. Ten live checks passed: native axe/pickaxe crop preservation, watering, scythe/hoe eligibility, native destruction with a simulated held Shift, dead-crop clearing, empty-soil removal, environmental damage, and disabled protection.
- The native impact tests use temporary crops and restore the original terrain. The Shift test changes SMAPI's input state only within the synchronous check, then restores it. Results: `artifacts/crop-protection-results-0.1.9.json`.
- Manual checks remain: a physical keyboard tool swing at a living crop, then Shift through impact on a disposable test crop; distant right-click with Shift held through arrival; normal crop harvesting. Smart selection can choose watering before an axe/pickaxe would hit, so the keyboard tool key is useful for testing the protection directly. A blocked impact does not refund native stamina costs.

## Quick-stack 0.1.8

- Release build passed with zero warnings/errors; 29 core tests passed.
- Twelve live checks passed: matching-only transfers and quantity conservation; selected, protected, and quest-item retention; different-quality rejection; partial/full-capacity overflow; shipping-bin, fridge, and locked-chest exclusion; nearby/distant chest filtering; and disabled behavior. Tests use temporary inventories and an isolated location for range checks, restoring the actual backpack without saving.
- Results: `artifacts/quick-stack-results-0.1.8.json`; fresh log: `artifacts/smapi-controls-0.1.8.log`.
- Player smoke test: put wood in a nearby chest, carry more wood and an unmatched resource, select a tool, and press F8. Confirm only matching wood moves. Protect a resource with F9, select another slot, and repeat. Check the F9 toggle and protection after your normal save/reload; physical hotkeys and save/reload persistence remain manual checks.

## Click feedback and standing 0.1.6

- Build passed without warnings/errors; 29 core tests passed. All 25 live interaction checks passed, including read-only chest/machine/chair descriptions, destination exposure/cleanup, native standing initiation on chair click, and a queued destination when clicking elsewhere from a chair.
- Hover labels and outlines visually checked at normal and 150% UI scale. The blue walking destination marker was captured and reviewed. Screenshots: `artifacts/feedback-destination.png`, `artifacts/feedback-150-percent.png`.
- Early test attempts encountered a seated/animating farmer; the test setup now normalizes this temporary state and restores the original seat afterward. Passing assertions were obtained in a fresh session.
- Hands-on checks remain: click to finish standing and walk to a target, same-chair stand without reseating, blocked seat exits, screen-edge labels, and labels over your actual crops/machines. Automated standing checks cover initiation and queued intent, not the full animation-to-walk sequence. Normal-game item and route restrictions still apply.

## Object and furniture actions 0.1.5

- Successful build, zero warnings/errors; 29 core tests passed. Seventeen live interaction/tool approach checks passed, including native chest opening, furnace input consumption, finished copper-bar collection, and approaching/sitting in a real chair. The original 0.1.4 build failed the new chest and idle-machine checks.
- The chest fix supplies the native action-click signal only during the requested interaction. Physical right-click tool mapping and left-click tool blocking remain covered by the movement/input checks.
- Physical playtest still required: chest lid/menu, chair and sofa sitting/standing, TV/menu, fireplace, large furniture near obstacles, machine inputs/outputs, locked/open doors, and UI scales. The chest harness validates the lock/opening request, not a full manual inventory transfer. No farm save is written.

## Walk to use tools 0.1.4

- Build passed with zero warnings/errors; 29 core tests passed. Ten live approach/interaction checks passed, including distant tool approach without a swing, cancellation, and native tool startup aimed at the stored target. The test restores the farmer and inventory before the tool can alter the saved farm.
- Physical playtest still needed: right-click a distant rock, tree, or dry soil while carrying farming tools; move the pointer during the approach; verify the final tool action, cancellation, and nearby hold/release. Distant clicks use one basic action; upgraded charge behavior remains for nearby held clicks.

## Activation and mouse fix 0.1.3

- Release build passed without warnings/errors; all 29 core tests passed.
- Seven live activation checks passed: installed feature, delayed approach, one native activation on arrival, cancellation, removed targets, immediate nearby activation, and empty-ground rejection. Uses a temporary interactive object through the native action method; does not save Solace.
- All 13 live movement/input checks passed, including the new assertion that physical world left-click cannot become native tool input. All 13 smart-selection checks passed again. Fresh log: `artifacts/smapi-controls-0.1.3.log`, no warning/error entries.
- Hands-on checks still needed: physically click/hold over crops and rocks (no tool swing), right-click to use tools, click a chest and a villager at their feet, enter/exit doors including a locked shop, cancel an approach, and test toolbar/dialogue/menu clicks at your preferred UI scale. NPC/door behavior uses normal game actions but has not yet been certified by a full manual playthrough.

## Smart tool update 0.1.2

- **13 live selection checks passed** on Solace: feature loaded, rock/pickaxe, existing correct tool retained, Shift override, range limit, tree/axe, dry soil/watering can, watered-soil protection, missing-tool protection, seed placement, menus, active animations, and unrecognized targets. The pre-implementation run failed as expected because the feature was absent. Results: `artifacts/smart-tool-results.json`.
- Tests use temporary native object/terrain fixtures and carried tools. Inventory, selected slot, terrain/objects, and temporary flags are restored without saving or swinging a tool on the real farm.
- Player smoke test: carry an axe, pickaxe, and watering can; select the wrong tool and right-click a nearby ordinary rock, tree, and dry tilled tile. Check selection and the actual tool action. Try Shift + right-click to keep your selected tool, and right-click with seeds selected to plant. Large resource clumps and weed/animal tools are outside this first pass.

## Movement update 0.1.1

- Core suite: **29 passed**, including four walking-path checks for obstacle avoidance, no diagonal corner cutting, unreachable/off-map destinations, large map coordinates, and bounded searches.
- **12 live movement/input checks passed** on Solace: arrow keys alongside WASD, unique bindings, weather-independent routes, native farmer walking and stopping, cancellation, tilled-soil click preservation, menus/off-map rejection, existing scripted movement preservation, right-click mapped to native tool input, tool release, and unmodified right-click input in menus. Results: `artifacts/movement-results.json`. The new right-click assertion failed on the previous build and passed with the mapping installed.
- Controls: arrows or WASD; left-click clear ground to walk; another ground click changes destination; movement keys cancel. Right-click uses the selected tool and cancels walking. X remains the normal action key. As of 0.1.3, world left-click is reserved for walking and activation; right-click uses tools. Click-to-walk is disabled during events/minigames, menus, swimming, and horse riding.
- Player smoke test: try arrows in all four directions, ground clicks around fences/trees, right-click tilling, normal crop watering, cancelling with a key, and the journal/toolbar at your chosen UI scale. The live harness invokes the movement service and native controller; it does not certify every physical input/UI-scale combination.

## Automated checks

- Release build: successful, zero warnings and errors.
- Core tests: **25 passed**. Covers deterministic seeds, region differences, seasonal transitions, actual/forecast agreement, rainfall completion after early sleep, zero watering from snow, save restoration, pending enable settings, climate validation, and game weather ID mapping.
- Independent source review completed. Regression cases were added for Desert Festival weather, scheduled wedding forecasts, and forced storms during overnight lightning processing.
- **18 live integration checks passed** on September 4, 2026, against the installed game. Confirmed native weather bypass when disabled, next-morning settings, regional rain/snow, native tilling, 30/60-minute watering thresholds, later tilling after the threshold, ordinary/special TV reports, overnight enable/disable, manual watering preservation, early-sleep rainfall, Desert Festival protection, wedding forecast protection, and previous-day storm handling. Local results: `artifacts/live-integration-results.json`.
- Journal framebuffer captures visually reviewed at 1920×1080 with normal and 150% UI scale: all sections and controls fit without clipping. The live menu's region and unit controls passed; original units and UI scale were restored. Local screenshots: `artifacts/journal-standard.png` and `artifacts/journal-150-percent.png`.
- **8 live Egg Festival checks passed**: both shelters initialized, five weather remarks appended once without replacing original dialogue, Lewis unchanged, and original scripts/props/actor positions/timer preserved. Native hunt startup reached `eggHunt` with its timer running and 32 original egg props; the added shelters were hidden. Local results: `artifacts/festival-integration-results.json`.
- Clear/rain Egg Festival lighting checks both passed after correcting the native event's frozen rain tint. Fresh clear and rainy framebuffer captures were visually reviewed, including both canopy placements. Screenshots: `artifacts/festival-clear.png` and `artifacts/festival-rain.png`. The weather button correctly disappears during the event.
- Fresh SMAPI logs confirm all nine integration patches loaded and Solace loaded successfully. The final festival session contains no warning/error-level entries from this mod or its harness. Local logs: `artifacts/smapi-weather-final.log` and `artifacts/smapi-festival-final.log`. Earlier harness staging failures remain in the weather log and are separate from the passing assertions.

Live tests use an isolated test-only SMAPI mod and load Dave's actual Solace farm. Date/weather changes are temporary in memory; the harness does not save the farm. The live overnight checks invoke the native weather setup and patched watering methods; they are not a replacement for sleeping through a full planted-crop day and reloading from disk.

## Hands-on acceptance checklist

- [ ] Open the journal with F7 and W; exercise every region (Desert selector and units already checked through the live menu).
- [ ] Check additional window sizes; exercise controller focus and mouse close (normal/150% UI scale already visually checked at 1920×1080).
- [ ] Enable for Solace, sleep, and reload. Confirm enabled persists and forecasts repeat.
- [ ] Compare the TV's regional report with the journal, including special-event days.
- [ ] Observe rain, clouds, wind, snow, sound transitions, interior travel, and paused game time.
- [ ] Check brief showers, cumulative watering, normal manual watering, sprinklers, and early sleep with planted crops.
- [ ] Check rain-dependent fishing and normal lightning-rod operation.
- [ ] Check daily NPC schedules and special-weather progression.
- [ ] Visit the Egg Festival in clear/rainy weather; inspect canopies and remarks, then complete the hunt and collect its reward.
- [ ] Disable and sleep; confirm native weather returns. Load another farm and confirm disabled by default.

Solace was backed up before testing under `artifacts/backups/before-testing/Solace_448236644`. Local backups, logs, decompiled inspection files, tools, and test harness output are excluded from Git and the release package.

After testing, the saved Solace main file still matches its original SHA-256: `04F4920AAEEF98CB107570229398FCCE0CB8052C5B0D1ED1177D8B93AD17DF39`. No test progress was saved. The installed mod starts disabled; enable it through F7 and sleep normally when beginning the player playtest.

Status: **playable test candidate, hands-on acceptance incomplete**. In particular, a full normal save/sleep/reload cycle, fishing catches, charged lightning rods, sprinkler/crop outcomes, sustained sound and travel checks, and completing/receiving rewards from both clear and rainy Egg Festivals remain user playtests. The automated checks do not mark these complete.

# Personal memory and activity evidence — 0.1.18

48 automated checks pass, including old-save defaults, exact-source quotes, retained qualifications, correction/reload, bounded retention, follow-up timing and daily limits, and unknown versus observed mine visits. Six live SMAPI checks pass for personal-detail storage, separate activity recording from a native MineShaft location, context fields, SMAPI JSON restoration and unchanged hearts. Live Gemini probes successfully extract a preference/plan, ask about it on a later simulated day and question a claim conflicting with a covered day's record. The first probe invented a witness; revised instructions were tested successfully without that invented explanation. Model interpretation remains subject to playtesting.

Remaining manual acceptance: real mine travel through entrances, contrasting truth/false claims over full game days, multi-day save/sleep/reload, correction phrasing, repeated follow-up behavior and F6 readability. The native-location fixture is not a complete physical travel test. No test request saves the farm; the user may save during the open test session.

## Opening-only suggestions — 0.1.17

Build passes with zero warnings/errors. Suggested starters are hidden and not clickable in subsequent text-entry screens after the first submitted message. Ending the conversation resets the flag for a new conversation. Player check: submit a starter or typed message, press Space on the reply, and confirm only text entry remains; Escape and click Abigail again to see starters return.

## Abigail longer replies and suggestions — 0.1.16

Build succeeded with zero warnings/errors; all 38 automated checks passed. In the live game, clicking the authored adventure prompt submitted the exact question, displayed a longer Gemini reply, recorded one exchange and left friendship points unchanged. Evidence: artifacts/ai-results-0.1.16.json and artifacts/smapi-ai-0.1.16.log. After correcting the test warp to release the newly loaded farmer's bed state, the production click interaction opened text entry directly. The suggested-button screen was visually inspected at 1920x1080; all three choices were readable. Evidence: artifacts/ai-first-click-0.1.16.json and artifacts/ai-suggestions-0.1.16.png. AI can also reopen through click-to-interact after the native daily conversation has been exhausted.

Player checks remaining: suggested buttons at other UI scales, longer reply readability, continuing with Space, exiting with Escape, and history after sleep/reload. The three starter prompts are authored and fixed. Long-term personality and recall quality still require playtesting.

## Abigail controls — 0.1.14

Space now opens the AI text entry during an ordinary Abigail conversation or on her AI reply. Space types normally inside text entry. Escape exits entirely from typing, waiting or reply display; waiting requests are cancelled. Build and all 38 automated tests pass. Physical Space/Escape behavior still needs a player check in the restarted test session. Installed and test configurations explicitly use Space.

## Abigail Gemini — 0.1.13

Successful build (zero warnings/errors) and 38 passing automated checks. A real Gemini request succeeded through the production client. In the restarted Solace test session, a full typed message submitted through the entry control produced a reply, displayed it and added exactly one memory exchange without changing friendship points. Fresh SMAPI log and result JSON are in artifacts/smapi-ai-0.1.13.log and artifacts/ai-results-0.1.13.json.

Still awaiting player checks: physical F10 during ordinary Abigail dialogue, continuing multiple exchanges, Escape/back behavior, saved AI history after sleeping/reloading, offline fallback in-game, UI scales, and personality quality over repeated play. Provider failure parsing is automated; a full simulated network outage in-game has not been tested. No full release-readiness claim for the broader weather/festival milestone.

## Abigail memory foundation — 0.1.12

Verified: successful build with zero warnings/errors; 34 automated tests, including bounded recall, duplicate prevention, JSON restoration and new-ledger isolation; six live Solace checks covering initialization, native observation, personality context, copy isolation, journal opening and unchanged friendship. The test harness does not save the farm.

Awaiting player verification: speak to Abigail and check F6; display several dialogue pages and confirm the page count; give a gift and confirm the count; sleep normally and reload; switch farms and confirm separate histories; disable/re-enable the feature; inspect journal readability at different UI scales. Ordinary dialogue capture, physical F6, the complete save/sleep/reload cycle and presentation have not been fully exercised by the live checks. Generated AI responses are not part of this release.

## Conversation delivery quest 0.1.19

Release build passed with zero warnings/errors; 52 automated checks passed. Thirteen native checks passed: native quest creation/XML restore, one-time deferred test item, full inventory, F9 protection, distance, claims versus actual delivery, backpack delivery with a tool selected, exactly-one consumption, completion storage, unchanged hearts/gift count, and other-farm isolation. Results: `artifacts/delivery-results-0.1.19.json`.

A live Gemini probe exposed an unsupported empty schema enum, which blocked conversation. Changed the wire format to `none`/`fish`, added a regression check, and verified real Gemini requests successfully. Delivery context explicitly distinguishes a just-completed handover from earlier history and does not claim the player caught the test fish.

Player checks: Escape after the request, see one Sardine added, return to Abigail, click the delivery button, confirm the AI acknowledgement and removal from the normal quest journal. Check F6's completion record and an overnight save/reload. Native quest XML and SMAPI memory roundtrips passed; full overnight gameplay persistence and all UI scales still require playtesting.

## Quest conversation layout 0.1.20

Build passed with zero warnings/errors. 54 automated checks passed, including whole-item matching, multiword names, overlaps, and excluding words such as fishing/selfish. Six native interface checks passed for removal of starter controls, row placement below entry/reply, one callback per click, and hidden-action rejection. Input and wrapped reply screenshots were reviewed at 1280 x 720; fish and Sardine are purple while unrelated words keep their ordinary color. A fixture initially omitted native box initialization; corrected the fixture before final review.

Manual checks: talk, turn in the fish using the row below the input, inspect purple names in her acknowledgement, and try your preferred UI scale. Native text animation and paging remain in charge. Other languages and all window/UI scale combinations have not been certified.

## Trust and portrait reactions 0.1.21

Release build succeeded with zero warnings/errors. All 107 core tests passed, including promise lifecycle, repeat clicks, deadline boundaries, extensions, late and abandonment repair, gates, legacy migration, JSON restoration and expression mapping. The final native trust suite passed all 26 checks: explicit choices versus discussion, dates in the ordinary journal, protected inventory, distance, actual consumption, no duplicate contribution, missed dates, repair, shared reminders, per-farm isolation and unchanged native hearts/gifts. Six interface checks passed at normal scale and at 150% scale in a 1280x720 window. Portraits, purple item names, dialogue paging and three quest choices were visually inspected. A compact typing overlap found during testing was fixed and rechecked.

Seven live Gemini scenarios passed through the production client: all three proposals, a delayed open-ended fish, an overdue Iron Bar, its completed repair and a Quartz extension. Reviewed expression choices were thoughtful, serious, warm or sad as appropriate. These samples demonstrate the selected behavior, not a guarantee about every generated reply.

A live Solace playthrough accepted the fish through a displayed quest choice, left conversation, received the configured test Sardine, returned and delivered it through the displayed action. The actual delivery and trust record survived a native overnight save and a full SMAPI restart. Solace advanced from Spring 4 to Spring 6 during the two overnight checks; the save from before testing is backed up under artifacts/backups/before-trust-overnight. The completed fish favor now belongs to the test farm's saved history and cannot be offered again.

Evidence: artifacts/trust-results-0.1.21.json, trust-compact-ui-0.1.21.json, trust-overnight-0.1.21.json, trust-gemini-probe.json, trust-compact-input-0.1.21.png, trust-handover-modern-0.1.21.png and smapi-trust-0.1.21.log. The final SMAPI session verified AbigailModern's portrait and sprite pixels through the content loader. Earlier harness failures were caused by an occupied warp destination or missing native bed/fade setup; the final test setup was corrected.

Remaining player checks: natural Quartz/Iron progression over multiple days, whether reminders and dialogue tone feel right, physical Space/Escape on your machine, other languages, and window/UI scale combinations beyond those tested. The separate-farm checks used isolated fixtures; a second real farm was not modified. Longer-term personality tuning and the broader weather/festival acceptance checklist remain separate work.

Final typed-message check also passed through the native text-entry submission: the reply displayed, one exchange was remembered, and friendship remained unchanged (artifacts/ai-results-0.1.21.json). Both installed artwork files matched src/AbigailModern byte-for-byte. The final fresh SMAPI session had no warning/error entries at handoff.

## Abigail relationship tree 0.1.22

Release build passed with zero warnings/errors. All 125 core tests passed (including 18 new tree cases). The native tree harness passed 35 checks covering protected/quest items, split stacks, full backpacks, whole rewards, stale/duplicate actions, daily studies, both supply kits, preparation and approach switching, both cooling periods, repair, abandonment, flute time/energy, serialization and unchanged friendship. The existing 26 native promise checks also passed. Future cooling boundaries use controlled game-state fixtures, not multiple real overnight waits.

Normal and 150% UI tree captures were reviewed at 1280x720, along with the visible Adventure fork, Social indicator and conversation Perks selector. A clipped Promises tab label was corrected and recaptured. UI checks confirmed read-only inspection, panel bounds and the actual native Social-row click opening the tree. Custom AbigailModern portraits and walking sprites were verified through the game content loader. A live Gemini preparation acknowledgement used the actual missing weapon, torch and Cherry Bomb findings; it was displayed and remembered without changing friendship. This one sample is not a guarantee of every generated response. Its TypedMessageSent field is false because this test sent a service action, not the older generic typing probe.

Solace was backed up under artifacts/backups/before-tree-0.1.22/save. Test controls deliberately staged all three completed favors, five mine-visit days, three mineral studies, safe supplies on cooldown, and an unfinished change to bold with one prepared visit. Personal memories were preserved. This test state survived a native overnight save and a full SMAPI restart, from Spring 6 to Spring 7 Year 1. These are explicit testing records, not claims that Dave organically completed those activities. The original save backup preserves his prior progression.

Evidence: artifacts/tree-results-0.1.22.json, tree-trust-regression-0.1.22.json, tree-overnight-0.1.22.json, tree-stage-0.1.22.json, tree-gemini-0.1.22.json, tree-normal-0.1.22.png, tree-compact-0.1.22.png, tree-fork-0.1.22.png, tree-social-0.1.22.png, tree-perks-0.1.22.png and smapi-tree-0.1.22.log. The final fresh log has no SolaceWeather or AbigailModern warnings/errors. The game reports Steam achievements unavailable because Steam is not running in this direct test launch. An earlier harness copy was stale; it was rebuilt in Release and all reported tree checks came from the corrected harness.

Remaining player checks: physical F6/Space/Escape and mouse controls, other resolutions/languages, flute audio preference, ordinary multi-day mineral collection and mine travel, actual story/dating events, and tone across more cooling-off and repair conversations. Separate-farm behavior uses isolated state fixtures; a second real farm was not altered. Authored offline replies are implemented and reviewed, but an actual provider outage was not forced. Native flute testing invokes real clock callbacks, so its fixtures restore their own state but do not claim to reverse all world effects.

## Shared experiences 0.1.23

Release build succeeded without warnings/errors. All 134 core tests passed, including nine new cases for deduplication, repeat experience days, relevance, conversation attribution, old-record migration, separate farms, restoration, recent-recall preference, and retrieval using later farmer statements. Six native experience checks passed, alongside all 35 tree regressions. An invented AI memory ID cannot create a verified event, and retaining a reflection changes neither the event nor native friendship.

The live Gemini probe recalled the recorded Amethyst, Earth Crystal and Quartz studies, connected them to Abigail’s curiosity, and attached the exchange to the Amethyst memory. These studies were existing Solace test records from 0.1.22. The probe's TypedMessageSent field is false because it uses a different test prompt from the old generic input probe. This is one sampled reply, not a guarantee about all generated dialogue.

A native overnight save and full restart preserved ten experience groups, including that attributed conversation, together with promises and tree progress. Solace advanced from Spring 7 to Spring 8 Year 1. Backup: artifacts/backups/before-experiences-0.1.23. The final build reloaded that save successfully and passed the experience/tree checks. The fresh SMAPI log confirms 0.1.23 and AbigailModern content; no mod warnings/errors occurred. The game-only Steam achievements message remains because this direct test launch did not start Steam.

Evidence: artifacts/experience-tests-0.1.23/*.trx, experience-results-0.1.23.json, experience-tree-regression-0.1.23.json, experience-gemini-0.1.23.json, experience-overnight-0.1.23.json, smapi-experiences-0.1.23.log.

Remaining player checks: whether memories feel naturally timed over longer play, repeated-topic behavior across several days, non-English retrieval, and preferred UI scales when reading longer history entries. Retrieval uses authored topics and matching words, not a semantic database. Events remain permanent within the authored experience groups; each group retains four recent related exchanges and counts distinct recorded days. No unrelated world-event tracking or ordinary gift-item tracking was added.
