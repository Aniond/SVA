# Blue UI and interface icons implementation plan

User approved the translucent-blue concept and all interface icons. No further palette approval needed. Follow parallel task execution with root integrating and reviewing each deliverable.

**Goal:** Install the approved translucent-blue UI across native shared interfaces, refreshing UI symbols while preserving item/world art and input behavior.

**Architecture:** Selective native texture patches plus scoped light-text rendering. Existing raw-pixel artwork cache remains. Native dimensions, layout, fonts, controller navigation, saved data and music-only mute are preserved; panel alpha changes are intentional.

**Spec:** artifacts/ui-rework/PROPOSAL.md and concept-blue.png, with subsequent user approval including all UI icons.

**Tools:** C#, Harmony, SMAPI, native source inspection, generated artwork donors, Node image preparation, isolated runtime audit. No commits/worktrees in this unborn checkout.

- [x] Inventory every shared UI source and icon family/state; record exact regions, native callers, locale-sensitive text and prior protected patches under artifacts/ui-rework/icons.
- [x] Prepare blue panel artwork and icon donors; preserve native source geometry, semantic silhouettes, controller labels and previous registered regions. Write preservation/coverage checks before staging.
- [x] Implement src/AbigailModern/BlueUiText.cs with bounded draw scope, exception-safe restoration and readable semantic colors; test nesting, world exclusion and dark-text mapping.
- [x] Implement src/NpcArtAudit/BlueUiAudit.cs to render representative native UI, check alpha on bright/dark backgrounds and verify state restoration; add selected blue-ui check in audit ModEntry.
- [x] Stage verified artwork and initialize text handler; build production/audit and package next release. Review generated native-size panels and icons and actual GPU previews.
- [x] Run isolated memory-bounded blue-ui and visual-effects tests, all registered-art checks, then backup/install and verify fresh normal launch/music mute. Stop only owned tests.
- [x] Record complete interface coverage, remaining player checks and evidence in docs/terrain-art/ui-progress.md; send preview/results to originating task.

