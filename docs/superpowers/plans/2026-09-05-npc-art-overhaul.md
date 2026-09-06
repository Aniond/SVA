# Full NPC Art Overhaul Implementation Plan

> For agentic workers: execute the tasks in this plan in order. Keep the persistent user goal active until the complete roster is covered and verified.

**Goal:** Update every existing NPC's sprites and portraits in the installed Stardew Valley game, creating appropriate expression portraits for NPCs who need them.

**Architecture:** Extend the existing reversible SMAPI visual mod, retaining its stable identity so upgrades do not introduce competing mods. Use the installed game's character data, asset files, and relevant actor implementations as the roster authority. Generate art from each original character's own references, prepare native-size transparent textures, and verify the assets through the real game loader and visual checks.

**Tech Stack:** Stardew Valley 1.6.15, SMAPI 4.5.2, .NET 6 mod, built-in image_gen, Node/sharp for technical atlas preparation, Blender for art inspection.

**Spec:** User's active thread goal: "Proceed to do the rest of the characters in the game the same way. Work until all existing NPC in the game has updated sprites and potraits. IF any charecers need a potrait create it and give it the needed emotions."

## Global constraints

- Preserve each NPC's recognizable identity, age, skin color, hairstyle, species and characteristic clothing.
- Preserve mechanics, saves, dialogue text and relationship rules. Portrait expressions must use the game's expected expression slots.
- Scope includes villagers, non-social NPCs, shopkeepers, visitors and special/event NPCs. Audit actors outside Data/Characters instead of assuming the social roster is exhaustive.
- Track alternate appearances, special sprite frames, embedded/shared-sheet actors and missing portraits explicitly. Do not claim full coverage from only the six bachelorettes or only a subset of the roster.
- Never close a player's active farm session without their authorization. Installation requires the game to be closed; generation, preparation and build work can continue independently.
- Keep originals, exact generation prompts, preparation parameters and validation evidence. Do not update user memory unless asked.

## Task 1: Authoritative roster and completion ledger

- [x] Extract installed Characters and Portraits texture inventory with sizes and hashes (226 direct textures).
- [x] Run a temporary audit mod to export Data/Characters, appearance mappings and NPC subclass names (48 definitions).
- [ ] Reconcile direct assets, aliases and special actors in a coverage manifest. Record unresolved scope instead of silently excluding actors.
- [ ] Identify portrait-less NPCs and their actual dialogue/drawing paths.

## Task 2: Generalized visual mod

- [x] Replace hard-coded six-character table with a validated asset manifest.
- [ ] Support nonstandard sprite dimensions and selected atlas regions; never assume every actor is 16x32.
- [ ] Add missing portrait loading/display only where needed, using the NPC's actual dialogue path.
- [ ] Keep an audit that distinguishes generated, prepared, installed, loader-verified and visually-verified coverage.

## Task 3: Complete the character artwork

- [x] Six bachelorettes: finish and verify installed main portraits and walking batch; alternate/special appearances remain pending below.
- [x] Six bachelors: Alex, Elliott, Harvey, Sam, Sebastian, Shane main portraits and twelve walking frames installed and loader verified; alternate/special appearances remain pending below.
- [ ] Town residents and families, including children and elders.
- [ ] Shopkeepers, visitors and special/quest NPCs.
- [ ] Remaining variants and shared/embedded actors from Task 1.
- [ ] New expression sheets for NPCs lacking portraits, with neutral/happy/sad/custom/blush-or-appropriate-alternative/angry slots as applicable.

For every sheet: inspect references; generate; inspect the output; repair clipping, direction errors and misplaced expressions; prepare transparent native atlas; verify frame count, dimensions, alignment, blank slots and coverage; retain evidence.

## Task 4: Installation and final audit

- [ ] Build with no warnings/errors, package all registered assets, and validate the package against the roster.
- [ ] Back up and install with file-hash verification while the game is closed.
- [ ] Verify registered textures through the real game content loader.
- [ ] Verify new portraits actually appear through their intended dialogue routes.
- [ ] Inspect representative walking directions and all expression sheets; resolve missing/cropped/incorrect frames.
- [ ] Reconcile every NPC and appearance in the coverage ledger. Mark the goal complete only when its full scope is proven complete.
