# Review of unchanged detailed sprite cells

The current registered full sheets that are 64 pixels wide and use 32-pixel rows were compared against native PNGs. This is a pixel triage pass, not proof of correct poses or complete NPC coverage. The raw findings are in `unchanged-detailed-cell-audit.json`.

Only three character groups contain unchanged cells with more than three visible colors:

| Group | Unchanged cells | Meaning |
|---|---|---|
| Pam everyday/winter | 20–23 | Lower continuations of tall prop poses. The preparation handoff explicitly preserves these, including native partial alpha. Modern bodies occupy the adjoining upper cells. |
| Willy everyday/winter | 20–23, 37–38 | Fishing rods, lines and bobbers below the modern bodies. The independent WillySharedScenes comparison verifies all 3,072 lower-overlay pixels and 156 upper rod-join pixels. |
| Fishing contestants, regular/winter | 4–7, 12–13, 17–18, 21, 23–24 | The native sheet uses mixed 16×64, 32×64 and 32×32 actor rectangles. A 16×32 scan splits those rectangles through the retained fishing equipment. Native actor mapping and preparation preserve rods while replacing the bodies. |

Evidence read for classification:

- `artifacts/npc-modern/work/PamBase/ARTWORK-HANDOFF.md`
- `artifacts/npc-modern/work/WillySharedScenes/ARTWORK-HANDOFF.md` and `verification.json`
- `artifacts/npc-modern/work/FishingContestants/design.md`
- `src/NpcArtAudit/FishingContestantAudit.cs`, which uses the native mixed-size actor mapping rather than assuming regular cells.

No additional artwork change follows from these unchanged cells. This does not establish visual correctness of every changed cell, does not cover other sheet widths or shared atlases, and does not replace the independent NPC reviews still underway.
