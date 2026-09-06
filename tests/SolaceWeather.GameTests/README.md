# Temporary in-game checks

`smarttools` checks target-based selection against native rock/tree/soil fixtures, carried tools, Shift override, range, missing tools, watered soil, selected seeds, menus, and active tool animations. It restores inventory, selected slot, soil/objects, and temporary flags without saving or using tools on the real farm. The same test initially records the missing feature against 0.1.1.

`movementchecks` verifies added arrow/WASD bindings, real native farmer movement along a planned path, stopping/cancellation, crop-click preservation, menu/off-map rejection, and preservation of existing scripted movement. Test positions, temporary soil, and weather settings are restored without saving. It exercises the movement service and native controller; physical mouse/keyboard input at different UI scales remains a player smoke test.

This is a separate SMAPI mod for the disposable `Solace_448236644` test farm. Do not include it in a release. Build its project, then copy only `SolaceWeather.GameTests.dll` and this folder's manifest into a separate test-mod folder alongside the installed Solace Weather mod.

Write one fixed command into `request.txt` inside that installed harness folder. It is consumed every 30 update ticks and deleted before processing. The allowlist is `load`, `status`, `checks`, `journal`, `rain`, `festival`, `capture`, and `quit`. No command evaluates arbitrary code or saves the game.

`load` loads the named test farm. `checks` reports each assertion to SMAPI and `results.json`; it restores temporary changes in a finally block. It tests the actual native tilling method with the installed patch and the runtime's soil adjustment routine, without hoe animations or applying water to every crop. Forecast tests call the actual TV method with the mod enabled and disabled. Pending enable application uses the save state's morning application method; it does not simulate a complete overnight save/load cycle. Test dates also update and restore `DaysPlayed` to avoid the native first-four-days weather rule.

`rain` and `festival` deliberately leave temporary visual staging active in memory for screenshots. `festival` runs the game's native `festival spring13` debug command. Exit without saving when finished. `status` writes the current screen, location, weather, event, and actor information to `status.json`. Each consumed command records its outcome in `last-request.json`.

Successful compilation is not evidence that these checks passed in the running game. Read the generated results and SMAPI log after invoking `checks`.

Additional checks exercise the actual new-day weather method and its installed patches, pending enable/disable changes, early sleep rainfall, and preservation of manually watered soil. These do not save or reload the farm; all affected soil and weather state is restored after the synchronous checks.

`capture` queues a read of the game's graphics backbuffer after `Game1.Draw` completes, writing `capture.png` and `capture.json` after two frames. The test-only Harmony postfix captures the composited HUD and menus. Final backbuffer rows are saved unchanged; the earlier weather-layer capture had different orientation. `status.json` includes the draw count and pending capture state.

`journalcapture` opens the weather journal and queues a capture. `journalqa` additionally clicks the actual Desert region control and unit control through the menu's click handler, verifies the changes, toggles units back, and writes `journal-qa.json`. These are fixed semantic test actions, not operating-system input automation.

Regression checks also cover an unlocked Desert Festival, tomorrow's NPC wedding, and a forced storm during overnight processing after the date advances. Temporary mail, spouse, and friendship fixtures are restored immediately after those checks.

`festivalqa` requires the active initial Egg Festival gathering and writes `festival-qa.json`: two initialized shelters; original dialogue preserved; five remarks once per event; Lewis unchanged; and dialogue checks leaving actor positions, native props, scripts, and timer intact. It restores the controller's remark bookkeeping after testing. `festivalclear` and `festivalrain` change only Town's override and queue a capture. `festivalhunt` starts the native main event once; `festivaladvance` advances one native dialogue box; `huntqa` records native hunt sequence, timer, prop count, score, and the exact condition which hides decorations. These diagnostics do not prove a full hunt/reward completion.

`journalcompact` stages UI scale 150 percent and captures the journal. `uirestore` or `quit` restores the original UI scale. It does not write game preferences to disk.
