# Hidden outfit evaluation

User wants each clothing piece to have a hidden fashion value, combined into an outfit value that each NPC judges against their own standards and preferences. This applies beyond Haley. Initial effects are comments/context only: no automatic friendship, romance, prices or gameplay-stat changes, and no score UI.

## Native grounding and shared metadata

The native Clothing type exposes item identity, shirt/pants type, dyeability, dye RGB, prismatic state and sprite index. It does not supply a fashion-quality stat. Read equipped shirt, pants, hat and footwear identities; exclude rings, tools and combat bonuses from fashion value. Character-creator/default clothing must be represented honestly even when no inventory clothing item is equipped.

Assets owns new clothing and its metadata. Proposed schemaVersion 1 entries keyed by stable qualified item ID: slot, fashionValue 0–100, styleTags, original paletteTags, formality/practicality/statement 0–3. Unknown pieces receive neutral value 50 and unknown styling; price is not a quality proxy. Runtime dye information overrides original palette descriptions where relevant. Do not invent visible detail from the name or a cached creation portrait.

The deterministic outfit evaluator aggregates slot values and style attributes, then applies bounded NPC-specific taste adjustments. Haley can value presentation strongly, while practical or artistic characters weigh different details or simply care less. Standards, interests and style preferences are authored data. Gemini receives an assessment and actual visible facts, never permission to calculate its own score. Raw numeric values remain hidden from player-facing replies.

## Observation and repetition

An outfit fingerprint includes equipped identities and actual dye/prismatic state. Each NPC tracks their own last seen fingerprint and last comment day within the farm save. A changed outfit can invite one comment subject to cooldown and interest; an unchanged outfit is not repeatedly criticized or praised. Personal preferences and player corrections are still attributed conversation memory. Clothing does not establish wealth, character, intelligence or relationship consent.

In-person dialogue can inspect current clothing. Remote phone conversation cannot magically see a fresh outfit; it may recall only a dated outfit that NPC actually saw. Public NPC-to-NPC chatter should not reveal another character's private clothing conversation or become an outfit surveillance feed.

## Coverage

Current full AI conversations exist for the twelve relationship candidates. They can consume the shared fashion snapshot. To provide reactions for other native villagers, add a small conditional native-dialogue comment adapter with authored standards and voice, preserving native dialogue and special events. This does not pretend those villagers have the full memory/romance AI. Unknown/modded NPCs use neutral evaluation and no invented authored voice.

The other-villager adapter is approved. The verified combined source baseline was imported as `5f6b685`. Assets supplies schema version 1 as a JSON string at `David.AbigailModern/FashionCatalog`; the reader also accepts optional local `assets/fashion.json`. Unknown clothing remains neutral, including when an older artwork pack has no catalog. Twelve full AI candidates and 27 additional authored native profiles are currently covered; unknown/modded NPCs have no invented voice.

Core checks cover deterministic scoring, actual dye fingerprints, neutral unknowns, NPC differences, nonrepetition, phone visibility and farm identity. Native rendering, dialogue preservation and save/restart checks remain required before integration.
