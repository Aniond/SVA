# NPC Modern — 0.13.0

Polished Stardew artwork for NPCs, interiors, outdoor locations, seasonal trees, crops, terrain and animated water, roads and bridges, all placeable path/fence families, craftable props and furniture. Version 0.13.0 contains 547 registered textures. Native frame positions and gameplay rules are preserved. This includes cosmetic runtime effects; full location-by-location gameplay has not been performed.

Interiors and remaining outdoor locations: 103 location atlases refreshed, including homes/shops, baths/sewer/witch locations, island rooms, mine/volcano variants, seasonal scenery and event backgrounds. All previous artwork regions, localized sign cells, native transparency and map geometry are preserved. Technical/UI/actor/shadow/text-only sheets and the black Maru foreground silhouette remain unchanged. The artwork loader now reuses raw pixels per sheet, avoiding repeated whole-image allocations for each small patch. Details: docs/terrain-art/locations-progress.md in the project.

Crops and special trees: all 50 crop definitions, five giant crops, crop harvest icons, wild-seed forage, spring onions and ginger; eight fruit-tree species across growth and seasonal states; 17 mahogany, palm, mushroom, mystic and green-rain tree sheets. Native tiny neutral seeds, tintable overlays, shadows, leaf particles and unused slots are preserved where required. Twenty new texture registrations and three narrowly extended shared sheets retain all prior artwork regions. Coverage and player checks: docs/terrain-art/crops-special-trees-progress.md in the project.

Local music preference: audio-preferences.json with MuteMusic true keeps music muted across startup and save loading, retaining sound effects and ambience settings. The preference is local and not included in the distributable. Set it false and restart to release the music override.

Farm buildings: all three farmhouse appearances, all seven cabin styles and their three upgrade appearances, Barn/Big Barn/Deluxe Barn, Coop/Big Coop/Deluxe Coop, Shed/Big Shed, and broken/repaired Greenhouse now have Polished Stardew surface detail. Native definitions use the same art across all four seasons. Doors, upgrade layouts, transparency and paint masks are preserved. Seventeen new textures are added; all345previous textures remain exact. Details and player checks: docs/terrain-art/farm-buildings-progress.md in the project.

Night sky update: a gentle blue outdoor moonlight tint fades in from 8 p.m. to 11 p.m. Rain and snow weaken it; storms suppress it. Clear-night stars twinkle in the visible Summit sky above the mountain artwork. Town/farm overhead views have no visible sky, so stars are not drawn over the ground. Use modern_effects moonlight 0.65 and modern_effects stars 0.65, or on/off. Existing saved settings gain these enabled defaults automatically.

Visual effects: soft lamp and window glow, sunlight-driven outdoor shadows, and drifting seasonal/weather mist are enabled by default. Shadows cover the reviewed Town buildings, placeable buildings, and growing trees. Effects remain below the interface and pause during cutscenes and map screenshots. Use the SMAPI console: modern_effects status; modern_effects off; modern_effects lighting 0.65; modern_effects shadows 0.55; modern_effects fog 0.5. Each effect also accepts on/off. Changes persist in visual-effects.json in this mod folder. These are stylized 2D effects; mist does not hide unexplored areas.

Seasonal civic buildings: the Community Center, Blacksmith, Museum/Library and JojaMart now match the completed Town houses and shops across all four seasons. Worn and restored Community Center, warehouse facade, abandoned JojaMart and both theater locations are included. Native snow, signs, doors, clocks, posters, lighting and progression remain intact. Details and player checks: docs/terrain-art/town-civic-progress.md in the project.

Seasonal town homes: Jodi's family home, Emily and Haley's house, George and Evelyn's house, Mayor Lewis's manor, and Pam and Penny's trailer now receive the same roof, wall and window polish across spring, summer, fall and winter. Pam's rebuilt house and separate trailer night window are included. Native shapes, seasonal snow and decorations, entrances and upgrade rules are preserved. Details and verification: docs/terrain-art/town-houses-progress.md in the project. Restart through SMAPI to see the update.

Town building upgrade: Pierre's General Store, Harvey's Clinic and the Stardrop Saloon have polished roof, wall and window detail across all four seasons. The Saloon's separate nighttime window is included. Original silhouettes, signs, seasonal decorations, doors and native lighting rules are preserved. Exact coverage and test limits: docs/terrain-art/town-buildings-progress.md in the project.

Weather upgrade: refreshed rain and green-rain splashes, snowfall, seasonal wind petals/leaves, snow flecks and lightning bolts. Occasional cosmetic hail appears during local thunderstorms, with four animation frames and a 24-particle cap. Hail causes no damage and changes no forecasts. Native snow transparency and lightning screen-flash settings still apply. Full scope and verification: docs/terrain-art/weather-progress.md in the project.

The Lost Items crow merchant now includes a modern 32-frame sprite strip and six-expression portrait sheet. Its native silent shop displays the neutral portrait, preserving the original inventory, prices and closing behavior. The NPC artwork release contained 285 registered textures; the current package contains 345 total. Targeted runtime checks are documented in the project; full scene-by-scene gameplay has not been performed.

Recent additions include complete everyday/winter artwork for Kent, Linus, Willy, Vincent and Jas; Grandpa opening-scene art; MarILDA main/flight sprites and message portraits; and all 24 clothing-therapy costume frames. Native event layouts, portrait expressions and seasonal selection are preserved. See docs/npc-art/progress.md in the project for verification scope.

Marnie now has complete everyday, winter and beach art:88sprite poses and15portraits. Drumming, drink, jump rope, three chicks, sleep, surprise and sitting retain native positions. Main portraits and native seasonal costume exceptions are preserved.

Pam now has complete everyday, winter and beach artwork:82semantic poses and15portraits. Native tall-prop continuations, sleep costume reuse, blank placeholders and accepted everyday portraits are preserved. Crying, drinking, folded arms, omelet and corrected walking poses follow native positions.

Clint now has complete everyday, winter and beach art: 70 semantic poses and 20 portraits. Full hammer32x32 and geode32x48 frames retain their native layout. Blue-soda and sleep poses, native blank cells and seasonal work-clothes exceptions are included. Winter soda portrait5 now smiles like the original.

Lewis now has complete everyday, winter and beach sprites: 74 occupied poses, six everyday and six winter portraits. Beach keeps the native everyday portrait fallback. Gardening, door gestures, drinking, reactions, sleep and omelet poses retain native geometry; white/transparent placeholder cells are preserved.

Jodi now has complete everyday, winter and beach artwork: 67 occupied poses and 14 portraits. Walking is cleaned up; dishes, exercise, sitting and sleep retain native frame positions. Native brown sprite and blue portrait placeholders are preserved.

Gus now has complete everyday and winter artwork: 54 occupied poses and eight portraits. Violin, glass polishing, sitting, cheering, marinara pot and sleep poses follow native frame positions. Native blank23 and accepted everyday portraits are preserved.

Pierre now has complete everyday, winter and beach artwork: 66 occupied poses and 16 portraits. Boxing, overhead gesture, sunglasses and sleep poses retain native frame positions; brown placeholder cells and accepted main expressions are preserved.

Shane now has complete everyday, winter, beach and Joja artwork: 137 occupied sprite poses and 48 portraits. Native blank cells, seasonal sleep footwear and work costume exceptions are retained. Winter worried-with-chicken portrait9 is corrected; other winter expressions are unchanged.

Robin now has complete everyday, winter and beach artwork: 92 occupied sprite poses and 24 portraits. Carpentry, dance, exercise, sleep, laughter and omelet poses are included. Native brown placeholder cells are preserved.

Demetrius now has complete everyday, winter and beach sprite sheets: 88 occupied poses, eight preserved native placeholders, and eight portraits in each everyday/winter sheet. Beach uses his native everyday portrait fallback. Newspapers, field notes, tomato groceries, dance and the everyday hazmat suit are included.

George now has complete everyday and winter artwork: 44 wheelchair poses and eight portraits. Winter adds his brown cap, charcoal coat and olive turtleneck. Native sleep glyphs and placeholder cells are preserved.

Caroline now has complete modern everyday, winter and beach artwork: 66 occupied poses and 12 portraits across three outfits. Exercise, reading, tea, sleeping and meditation poses are included. Native placeholder cells and accepted everyday portraits are preserved.

Sebastian now has complete modern everyday, winter and beach artwork: 138 occupied poses and 28 portraits across three outfits. Native brown placeholder cells are preserved. Garage body pieces retain the native cropped layout; full garage and other saved-game scenes remain to be checked.

Evelyn now has complete everyday and winter art: 21 poses and four expressions per outfit. The everyday walking sheet is cleaned up, and sitting, gardening and baking poses are refreshed. Native placeholder cells remain unchanged.

Sam everyday and Joja work outfits now include all 55 poses and 12 portraits. Everyday side-glance and folded-arm portraits are corrected. Work poses include the native headphones and light-blue uniform. Winter work poses receive the same costume correction. Full saved-game scenes remain to be checked.

Ongoing Stardew Valley NPC artwork overhaul. The authoritative installed asset list is artwork.json. The existing Mods/AbigailModern folder and David.AbigailModern ID are retained so this upgrades the previous Abigail/Bachelorettes Modern mod.

Portraits keep native 64x64 cells and original expression indices; added expressions use available cells. Movement sprites replace all sixteen 16x32 directional frames, including the separate left-facing row. Left-facing poses follow the native direction; some older movement sets use mirrored right-facing artwork. Special animation frames and winter/beach variants are tracked separately in docs/npc-art/coverage.json. The Old Mariner has a complete static sprite and a new six-expression portrait. A portrait panel appears above his normal dialogue and pendant question without replacing text, response keys or purchase handling. Currently neutral and happy expressions are used for his normal and purchase dialogue.

Generated artwork is reduced to native game resolution, so fine preview details are reduced. Source images, prompts and validation reports live in artifacts/npc-modern/work plus the earlier Abigail and bachelorette artifact folders. Run node scripts/prepare-npc-art.mjs followed by the character names to prepare newer sets.

Build with dotnet build src/AbigailModern/AbigailModern.csproj -c Release. With the game closed, run scripts/install-bachelorettes-modern.ps1; it reads artwork.json, backs up the installed mod, copies all registered artwork, and verifies file hashes. The script name is retained for compatibility. No save data or original game files are modified. Remove Mods/AbigailModern with the game closed to uninstall.

At launch the mod compares every registered texture or patched region against packaged pixels. This proves asset loading, not full NPC coverage or visual acceptance. In game, check ordinary dialogue expressions and walking in each direction, and talk to the Old Mariner in rain to check the portrait panel and normal pendant choices. The temporary NPC Art Audit mod is a development check and is not part of the release package.











Winter fishing visitors:13 figures and12 six-expression portrait sets. Nearby winter anglers show matching portraits and native fishing reactions. Summer Trout Derby counterparts also have11updated figures and10six-expression portrait sets.

Abigail winter: all54occupied sprite poses and10portrait slots updated; event playback validation remains in progress.

Abigail beach: all 19 occupied sprite poses and 10 portrait slots updated; full island scene playback remains to be checked.

Abigail everyday: all 54 occupied poses modernized, including flute, sitting, sword practice, wedding and dance. Her everyday, winter and beach sheets are prepared; full event playback remains to be checked.


Emily winter and beach: 56 winter poses and 21 beach poses, eight portraits per outfit. Winter hovering and beach idle poses corrected; native unused beach placeholders retained. Her everyday sheet now also has all 56 poses modernized.

Abigail mining/adventure outfit: plum tunic, teal scarf, boots, satchel and brass headlamp. Includes 42 adventure poses, 12 retained modern formal poses and ten portraits. A host-owned quest enables the appearance with player modData David.AbigailModern/AbigailAdventureOutfit=true and calls Abigail.ChooseAppearance(); removing the flag restores normal seasonal attire. Quest integration notes are in docs/npc-art/abigail-adventure-outfit.md in the source workspace.

Leah everyday and seasonal sets: 50 everyday poses, 50 winter poses and 20 beach poses. Everyday retains ten portrait slots (nine occupied expressions and one unused blank); winter has ten expressions and beach adds eight matching expressions. Native sculpting, sketching, painting, formal and sleeping frames keep their original positions. Full event and beach scene playback remain to be checked.

Maru winter: all 45 occupied poses and ten portrait expressions modernized. Three native blank cells remain blank. Includes tinkering, sitting, sleeping, wedding and dance poses. Full clinic/event scenes remain to be checked.

Maru clinic:20 occupied poses and six portrait expressions updated, with twelve unused sprite cells preserved. Native hospital appearance selection remains in charge of when she changes. Full clinic event and kissing scene playback remain to be checked.

Maru beach:20 poses and ten portrait expressions modernized, including reclining and sunglasses poses. Native beach selection and towel behavior remain in charge of positioning. Full island/reclining scenes remain to be checked.

Maru everyday:all45 occupied poses modernized, including tinkering,reactions,sitting,sleeping,wedding and dance;three native blank cells remain blank. Ten existing portrait expressions retained. Full events and seasonal scene playback remain to be checked.
















Terrain starter: spring map ground region, ordinary spring grass, Stone Floor in normal/winter versions, and a new ten-frame water loop. Other terrain regions and seasonal ground remain pending. NPC artwork is retained.


Vegetation batch: regular seasonal oak, maple and pine growth/stump sheets; ordinary seasonal bushes; two decorative plant positions across four seasons. Special trees, fruit trees, tea/walnut bushes and crop flowers remain separate work.


Outdoor props batch: seasonal paths, roads and bridges, four fence materials and gates, craftable surfaces and regular furnishings. Shapes and source-frame layouts are preserved. Exact coverage and gameplay verification limits: docs/terrain-art/roads-props-progress.md in the project.


Translucent blue UI and interface symbols: see docs/terrain-art/ui-progress.md in the source workspace. Native fonts, controls, prior artwork and music preference retained.

Translucent blue UI and interface symbols: see docs/terrain-art/ui-progress.md in the source workspace. Native fonts, controls, prior artwork and music preference retained.

