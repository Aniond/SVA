# Stardew Valley Alive (SVA)

Solace gameplay systems and the NPC Modern artwork overhaul for Stardew Valley. The current artwork release is 0.13.0, including the approved translucent blue UI and interface icons. See [UI preview and validation](docs/terrain-art/ui-progress.md), [artwork progress](docs/terrain-art/progress.md), and [artwork mod instructions](src/AbigailModern/README.md).

Source, artwork and tests are included. Game binaries, local configuration, saves, build outputs and generated audit evidence are excluded. Building the game-facing projects requires a local Stardew Valley and SMAPI installation.

# Solace Weather

A single-player SMAPI mod for Stardew Valley **1.6.15**, requiring **SMAPI 4.5.2**. Weather runs locally without an AI service or server.

## Start playing

1. Close Stardew Valley before installing or updating the mod.
2. Extract the release ZIP into the game's `Mods` folder. The result should be `Mods/SolaceWeather/manifest.json` alongside the mod DLLs.
3. Launch the game through SMAPI and load **Dave's Solace** test farm.
4. Press **F7**, or click the **W** weather button beside the clock.
5. Choose **Enable next morning**, then sleep to save. Every other farm stays disabled unless you enable it there too.

The journal shows a labeled simulation preview while disabled. Disabling also takes effect next morning. If you quit without saving, pending settings are discarded with the rest of that unsaved day.

## What changes

### Global relationships (0.2.0)

All twelve native romanceable villagers now share explicit friendship and courtship choices, scheduled activities, persistent memories, exclusive relationships, witnessed town news and repair agreements. Press **F6** for the character journal, or **Bond** while talking for explicit actions. Abigail retains her own tree. See [the relationship guide](docs/global-romance.md) for controls, progression, marriage and boundaries.

### Craft from nearby storage (0.1.11)

- Open the normal crafting menu near ordinary player chests. Ingredients within **six tiles on each axis, in the same location**, join your backpack ingredients. Backpack materials are consumed first, followed by the nearest chests. Ordinary big chests work too.
- F9-protected items and quest items are excluded from both counts and consumption. Recipe tooltips show combined available ingredient totals. Protection follows the item, so it also applies to protected stacks already in a chest.
- Each craft rechecks nearby sources and ingredient quantities. Insufficient ingredients, a full backpack with no space for the result, or an incompatible item on the cursor leave ingredients untouched. Make room first even if crafting would itself free an ingredient slot. Normal cursor-held crafting output and batch-click controls remain.
- Fridges, shipping bins, linked/special storage, locked chests, and other locations are excluded. This feature applies to crafting, not cooking. It does not require weather to be enabled. Set `EnableNearbyCrafting` to `false` and restart to disable it.
- Nearby candidates are found when the menu needs its contents and rescanned on each craft; the mod does not scan every farm continuously. Containers added while a menu is open are picked up on crafting or reopening the menu.

### Machine status indicators (0.1.10)

- Machines show a small **green !** when output is ready, **gold ~** while working, or **gray –** when idle. Hover for the output name or the remaining processing time in game minutes/hours.
- Idle input-driven machines say “waiting for ingredients”; automatic producers say “waiting to start.” This is a state display, not a recipe or missing-ingredient calculator. It does not load machines, collect items, or advance timers.
- Uses the game's machine data. Chests and ordinary resources have no production indicator. Set `EnableMachineIndicators` to `false` to hide the markers and machine status text; `EnableClickFeedback` controls the hover panel.

### Crop protection (0.1.9)

- Living planted crops survive accidental axe and pickaxe impacts. The guard applies at the crop itself, covering mouse, keyboard, and queued tool uses in single-player. The tool can still swing and consume normal stamina.
- To deliberately remove a living crop, **hold Shift through the tool impact**. For a distant right-click, hold Shift while clicking to keep the selected tool, and keep it held until the swing finishes.
- Watering, hoe actions, normal scythe harvesting, dead-crop clearing, and removing empty tilled soil remain native. Forage crops and environmental damage (such as bombs or lightning) are outside this protection.
- A brief message explains a blocked impact. Set `EnableCropProtection` to `false` in `config.json` and restart to disable it. Weather need not be enabled.

### Quick-stack (0.1.8)

- Press **F8** during normal play to store matching backpack items in ordinary player chests within six tiles in the current location, nearest first. A chest must already contain a compatible item; normal quality, capacity, and stack limits apply. Excess items stay in your backpack.
- Tools, non-stackable equipment, quest items, your selected slot, and protected items stay with you. Select a tool before stacking if you want all eligible resource stacks to move.
- Select an item and press **F9** to toggle quick-stack protection. A message confirms its status. Protection is attached to that item and is saved by the game's normal save process.
- Shipping bins, fridges, linked/special storage, and locked chests are excluded. Ordinary big chests are supported. Multiplayer, menus, cutscenes, and active tool animations block quick-stack. Weather does not need to be enabled.
- Settings: `EnableQuickStack`, `QuickStackKey`, `ProtectItemKey`, and `QuickStackRadius` (clamped to 1–12 tiles). Radius measures tiles on each axis and does not search other locations or require walking to each chest.

### Object-only hover feedback (0.1.7)

- Hover outlines and labels appear on interaction targets and specific tool targets. Empty ground and ordinary walking no longer show hover feedback. Generic “Use selected tool” and “Use held item” labels are removed. The blue destination marker still appears during a click walk.

### Click feedback and standing up (0.1.6)

- Hover over the world to see the left/right-click actions. Green outlines indicate left-click targets; gold outlines indicate tool targets. A blue square marks the stopping point during a click walk.
- Feedback recognizes chests, doors, machines, furniture, villagers, and common tool targets. It reports missing matching tools and already-watered soil. Labels describe the action, not a guarantee that a route, shop hours, or machine ingredients will allow it.
- While seated, left-click your occupied chair to stand up, or click elsewhere to stand and approach that target. Uses the normal standing animation. Menus and other actions can cancel the pending approach.
- Feedback hides over the toolbar/weather button and during menus, cutscenes, and tool use. Set `EnableClickFeedback` to `false` in `config.json` to hide it.

### Object and furniture actions (0.1.5)

- Left-click a chest, door, machine, or furniture to walk into range and perform its normal game action. Chest lids and the upper artwork of tall furniture can be clicked too.
- Chests open; machines accept the selected input or release finished output. Normal inventory, ingredient, and shop-hour rules apply. Chairs use the game's sitting action, TVs use their normal menu, and fireplaces toggle. Decorative furniture without a native action remains decorative; lamps do not gain a new toggle.
- Larger furniture is approached from an accessible edge. Removing or moving furniture cancels its pending action. Use movement keys to stand up or cancel walking. Right-click remains tool use.

### Walk to use tools (0.1.4)

- With an axe, pickaxe, hoe, or watering can selected, right-click a distant tile to walk within reach and use a tool once at that spot. Smart selection happens on arrival; Shift + right-click keeps your selected tool.
- The destination stays where you clicked even if the pointer moves. A movement key, new click, menu, changed selected item, or removed/replaced target cancels the pending tool use. Blocked destinations show a message.
- Nearby right-clicks keep normal tool hold/release behavior. Distant clicks perform a single basic action. Fishing rods, weapons, and selected seeds/placeable items retain their existing controls.

### Smart tool selection (0.1.2)

- Right-click a nearby breakable rock to select your carried pickaxe, a tree or loose twig to select your axe, or dry tilled soil to select your watering can. Normal game reach, tool upgrades, stamina costs, and tool actions still apply.
- Keep tools in your backpack. An already-correct tool stays selected; otherwise the first matching carried tool is selected. Missing tools are never created, and the click is cancelled if the required tool is missing.
- Hold **Shift + right-click** to keep your manually chosen tool. Selected seeds, fertilizer, food, and placeable items retain their normal actions. Already-watered soil ignores an ordinary smart-tool click; use Shift to override when intentionally removing soil/crops.
- Selection happens once at the start of a right-click, never during an active tool animation. It works while weather is disabled. Set `EnableSmartToolSelection` to `false` in the mod's `config.json` and restart to turn it off.
- This first pass covers ordinary rocks, trees, twigs, and tilled soil. Large resource clumps, weeds, animals, and automatic hoe selection are not included.

### Movement and activation (0.1.3)

- Arrow keys move the farmer alongside existing WASD bindings.
- Left-click clear ground to walk there, following a path around obstacles.
- **Right-click uses the selected tool**, including the native hold/release behavior. It also cancels any current walk. The action key (**X** by default) remains available for talking, opening doors, and other interactions; **C** still uses tools.
- Press a movement key to cancel walking. A new ground click chooses a new destination.
- Left-click an actionable object, a villager's feet, or a door to approach and activate it using the game's normal interaction rules. Click an exit tile to walk through it. A moving villager is followed for up to three route attempts; a removed target cancels the action.
- Left-click on the world never uses a tool while click-to-move and right-click tools are enabled. Menus, the toolbar, and the weather button retain their clicks. Shift + left-click pauses click navigation; it no longer swings a tool. Hold Shift + right-click for manual tool selection.
- Movement keys, another click, menus, warps, and cutscenes cancel an approach. These controls are for ordinary on-foot single-player play; fishing, horse riding, and festival minigames retain native controls. Holding a gift or machine input retains the game's normal item interaction rules.
- Set `EnableClickToInteract` to `false` to disable automatic interactions.
- These controls work even with weather disabled. Set `EnableArrowKeys`, `EnableClickToMove`, or `RightClickUsesTool` to `false` in the mod's `config.json` and restart to disable the corresponding feature. If the game has saved the added arrow bindings, remove those in the game's Controls menu after disabling `EnableArrowKeys`.

### Weather

- Connected weather patterns across the valley, with separate desert and island climates.
- Gradual changes in clouds, temperature, wind, rain intensity, storms, and snow using native effects.
- Current conditions, a daily timeline, regional three-day forecasts, and expanded TV reports. Tomorrow's broad conditions are reliable; later outlooks are less certain.
- Fahrenheit by default; change to Celsius in the journal.
- Rain waters exposed tilled soil only after enough rainfall accumulates: initially 60 normal-rain-equivalent game minutes. Heavy rain counts faster. Snow doesn't water crops. Sleeping early includes the remainder of the simulated day.
- Fishing uses local current rain. Villager schedules are chosen from the day's broad conditions. Normal watering, sprinklers, lightning rods, and lightning consequences remain.
- The Egg Festival gains striped canopies around two gathering areas and weather-aware dialogue. Decorations disappear when the hunt starts, preserving visibility, routes, eggs, rewards, and timing.
- Original special-weather events and other festivals retain their required conditions. Early-game scripted weather is also respected, so your first few days may look familiar.

This release does **not** add persistent soil moisture, crop quality/growth bonuses, drainage, AI conversations, multiplayer, or Android support. Canopies are temporary visual shelter, not new collision or farming structures.

## Build and package

Install a .NET SDK (8 or newer), the game, and SMAPI. The mod targets the game's .NET 6 runtime. The test suite uses .NET 8.

```powershell
dotnet restore SolaceWeather.sln
dotnet build SolaceWeather.sln -c Release
dotnet test tests/SolaceWeather.Core.Tests -c Release
./scripts/package.ps1
```

The build helper detects the local game installation. If needed, provide `-p:GamePath="C:\path\to\Stardew Valley"` to build. Game assemblies are never included in the package. Builds do not automatically install the mod.

Climate values live in `assets/climate.json`, separate from UI text in `i18n/default.json`. Invalid climate values fall back to defaults with a warning. `config.json` is created by SMAPI and contains `JournalKey`, `UseFahrenheit`, and `DeveloperMode`.

## Development controls

With `DeveloperMode: true`, restart and use the SMAPI console:

```text
solace_weather status
solace_weather journal
solace_weather enable true
solace_weather force Farm Rain
solace_weather force Town Storm
solace_weather force Island Snow
solace_weather force Farm auto
```

Overrides are temporary, affect the selected region for the current day, and expire next morning. They do not override protected events. They replace the test day's rainfall calculation, so they are for testing, not ordinary play.

The optional `tests/SolaceWeather.GameTests` mod is a local integration harness. It is never shipped in release ZIPs. Its fixed `request.txt` commands load only the Solace test save and run controlled checks; see its source for the supported commands.

## Verification

See [testing.md](docs/testing.md) for results and the remaining hands-on checklist. A successful compile is not evidence that every festival interaction or game configuration has been tested.
# Abigail personal memory (0.1.18)

Abigail now keeps up to 64 lasting personal details and plans beyond the recent conversation log. She can bring up an eligible plan on a later game day, at most one offered follow-up per day. Corrections can replace a matching older detail. Recorded quotes retain their complete source message, and everything you tell her stays labeled as your statement rather than a proven event.

The game separately records entering mine floors (including Skull Cavern). Abigail can use dated records to question a conflicting claim; an unrecorded or partially observed day stays unknown. Entering a mine proves only a visit, not ore mined, fights or rewards. This release does not detect lies about every game activity or infer intent, and it does not change friendship points. **F6** opens the relationship tree, with shared history and promise details in tabs.

## Abigail AI conversations

Click Abigail to open a message box automatically during ordinary conversation. Type your message and press **Enter**. Gemini answers using her personality and recent memories. Press **Space** on her reply to type again. **Escape** exits completely, including while typing or waiting. On connection failure, ordinary dialogue is restored. Story scenes, dialogue choices and dialogue carrying game actions keep their native behavior.

The space beneath chat is reserved for explicit quest choices. Starter questions are removed. Open-ended answers aim for 4–6 sentences (around 80–140 words), with shorter answers for simple greetings.

Requires a saved `GEMINI_API_KEY` environment variable. The key is not included in the mod. `EnableAbigailAi` defaults to true, `AbigailTalkKey` to Space, and `GeminiModel` to `gemini-3.8-flash`. Your message and Abigail's game context go to Google only when you send a message. The latest 32 completed exchanges save with the farm. AI chat awards no extra friendship points and does not run during story events or question prompts.

## Abigail memory foundation (0.1.12)

Press **F6** during ordinary play to inspect Abigail's tree and shared-history tabs. This first step records daily conversation credit, daily gift counts, and up to eight ordinary Abigail dialogue pages displayed per day. It keeps the latest 112 interaction days separately for each farm and saves with normal game saving. No past interactions are reconstructed beyond the current day's native flags.

An authored personality asset preserves her independence, curiosity, playfulness, music and adventure interests, with explicit limits against invented experiences, spoilers and assumed romance. Gemini now consumes this context through Space. Ordinary dialogue and friendship rules remain the game's own. Typed AI exchanges and explicit promise outcomes are recorded separately; town gossip and full gift identity tracking are later work.

`EnableAbigailMemory` defaults to true; `AbigailMemoryKey` defaults to `F6`. Change configuration and restart to disable or rebind it. Memory is single-player only and works independently of the weather opt-in. Disabling preserves its existing save data. Unsupported or malformed memory is preserved and pauses the feature with a warning. Developer mode adds `solace_abigail` to inspect the context locally.



## Abigail: Trust and Follow-Through (0.1.21)

Abigail now has three authored favors: any fish, one Quartz and one Iron Bar. Materials unlock after you carry them; the Iron Bar also needs a previous completed favor. Explicit choices below chat accept, decline, extend or hand over a request. Fish has no deadline; Quartz and Iron Bar let you choose tomorrow or three days. The normal quest journal shows the agreed date.

Kept promises, missed deadlines and later repairs shape a separate remembered sense of reliability. F6 explains her outlook through meaningful moments. Hearts, gifts, dating and heart events keep their normal rules. Gemini reacts to verified game outcomes; apologies and conversation alone cannot award trust or deliver items. Quest item names remain purple, and delivery uses your backpack.

Old fish quests and memories migrate automatically. The Solace-only test Sardine arrives after accepting the fish favor and leaving conversation. See [promise rules and test instructions](docs/abigail-delivery-quest.md).

AI replies now show Abigail's portrait with a matching expression, using your installed AbigailModern artwork. Neutral, delighted, concerned, stern, thoughtful, serious, surprised and warm expressions use the sheet's authored positions. Portraits shrink for compact UI views, and unknown reaction tags use neutral.

## Abigail relationship tree (0.1.22)

F6 and Abigail’s Social-page entry open her branching relationship tree. Talk beside her and select **Perks** to study minerals, exchange spare studied minerals, review mine preparations, request supplies or take a flute break as those milestones unlock. Native hearts remain underneath. See [the tree guide](docs/abigail-relationship-tree.md) for requirements, cooling off, approach changes and installation.

## Shared experiences (0.1.23)

Meaningful tree actions now become lasting shared memories. Gemini can connect studies, preparations, supplies, flute breaks, promises and repairs with later conversations. The event stays verified; related conversation stays attributed speech. F6 → History includes these memories. Current perk availability and cooling off still control actions, and recalling a memory awards no trust or items.

