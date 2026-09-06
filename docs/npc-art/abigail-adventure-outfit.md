# Abigail mining/adventurer outfit — quest integration

This is the outfit requested for the quest being developed in **Living Memory**. NPC Modern supplies the artwork and native appearance entries. The quest remains responsible for deciding when Abigail sets out and returns.

## Outfit assets

- Sprite: `Characters/Abigail_Adventure` — native 64x448 sheet.
- Portrait: `Portraits/Abigail_Adventure` — ten native 64px expression cells.
- Clothing: plum tunic over dark leggings, cream sleeves, teal scarf, sturdy brown boots, leather satchel, and a small brass headlamp on a green headband. Purple hair stays visible.
- Forty-two adventuring poses cover ordinary movement and special actions. Twelve existing modern wedding/dance/formal poses remain available at their native indices. Two blank cells remain blank.

## Equip and restore from the quest

NPC appearance is shared, so the host quest controls this flag. Set it when an actual outing begins; do not treat ordinary conversation or delivering an Iron Bar as an automatic outfit change.

```csharp
const string outfitKey = "David.AbigailModern/AbigailAdventureOutfit";

// Called by the host quest when the outing begins.
Game1.MasterPlayer.modData[outfitKey] = "true";
Game1.getCharacterFromName("Abigail")?.ChooseAppearance();

// Called by the host quest when the outing ends or is cancelled.
Game1.MasterPlayer.modData.Remove(outfitKey);
Game1.getCharacterFromName("Abigail")?.ChooseAppearance();
```

NPC Modern adds two conditional Data/Characters appearances, covering normal and island attire. With the flag enabled, native appearance selection uses the adventure sprite and portrait. Removing it restores the appropriate ordinary, winter, or beach outfit. Native map-specific appearance overrides still follow the game's normal rules. The flag is saved with the host farmer; appearance is refreshed on save load. The quest should restore or retain it according to its saved outing state.

No quest stage, inventory, friendship, schedule, combat behavior, or promise outcome is changed by this artwork feature. The current quest task still needs to call the equip/restore lines at its chosen outing boundaries. Full quest scene and overnight save playback need to be checked there.

Verified in NPC Modern 0.7.31: all 177 registered assets load; nine outfit selection/restoration cases, 54 occupied sprite slots and ten portrait indices pass. Full quest scene and overnight save playback remain pending.

Installation rechecked on September 5, 2026 in NPC Modern 0.7.50: both adventure textures, the appearance mod DLL, artwork registry, and manifest match the installed game files exactly. The recorded ordinary game launch loaded 203 registered textures. The quest equip/restore calls are still absent from SolaceWeather, so the other quest task needs to connect them before the outfit appears during that quest.
