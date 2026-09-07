# Disposable native-farm persistence harness

Build: `dotnet build tests/SvaPersistenceAudit/SvaPersistenceAudit.csproj -c Release`.

Copy the Release DLL and manifest to the dedicated test mod folder. Requires installed David.SolaceWeather and David.AbigailModern. Write an absolute profile-root.txt in the test mod folder naming a child directory beneath C:/Users/david/SDV/artifacts/new-game-persistence. Optional audit-seed.txt overrides the default 202609060713. Use a different empty profile and seed for the cross-save control. The harness patches native app-data/local-data and SMAPI data/save getters before enabling commands. It verifies every path and rejects directory links. It neither scans nor loads normal saves.

Requests: write one allowlisted word to request.txt in the test mod folder. It is consumed once, every 15 ticks. No new farm is created automatically.

- status: capture real production service readiness, memory/farm/weather state, and sourced conversation context. InitialMemoryBlank and CrossSaveMarkersAbsent distinguish a clean farm from null/uninitialized memory.
- new: requires title and empty isolated Saves. Runs native resetPlayer, CharacterCustomization(NewGame), sets SvaAudit farmer/farm and seed, then TitleMenu.createdNewCharacter(true). Native new-day/save flow remains in charge.
- stage: requires ready production services; does not repair or initialize them. Calls RememberExchange, Personal.Apply, Promise Offer/Accept/Complete, Tree.Observe, Experiences.Record, and RomanceRules Talk/RecordIncident/LearnIncident. Adds a marked protected quartz stack and native guildMember progression flag. These are deliberate fixture actions, not evidence of actual quest gameplay or an adventure.
- sleep: requires owned farmer in their farmhouse; invokes native Sleep_Yes action. Do not issue during transitions.
- load: loads only SvaAudit_<seed> inside the isolated profile, requiring ownership.json plus both native save files. Exits to title first if needed.
- journal: opens the actual romance journal for Abigail and requests a screenshot.
- capture: takes a bounded backbuffer screenshot after Game1.Draw (maximum 16 MP).
- art: invokes the installed art mod's own CheckAsset method once per tick for all registry entries; expects 547 and writes art-checks.json.
- quit: takes status then requests native quit.

Outputs: test mod events.json/status.json/error.json/art-checks.json and isolated profile snapshots, expected.json, ownership.json, created-save.json, capture PNGs, native Saves. Events retain actual SaveLoaded/DayStarted/SaveCreated/Saving/Saved ordering. Reload checks verify completed promise, tree unlock, experience, incident and witness knowledge, item protection and flag, plus the preference in PersistentDetails from the real conversation-context API. No model/AI requests are made by this harness. The launcher must configure the installed test mods to disable unsolicited AI requests.

The harness sets pauseWhenOutOfFocus=false for its disposable process. Core DLL version must match installed SolaceWeather. It intentionally fails stage when service initialization is missing, preserving the initial baseline instead of masking a production bug. The save hook rejects writes by another farmer/seed. No game has been launched by the harness implementation agent.

Additional staging: sets the real AbigailAdventureOutfit player modData flag and calls native ChooseAppearance, then verifies the selected adventure appearance after reload. Calls actual WeatherRuntime.RequestEnabled(true); same-day checks allow pending, later-day checks require enabled with no pending. Cross-save control requires both outfit and weather opt-in absent.
