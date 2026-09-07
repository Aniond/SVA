# Tools, weapons and items — installed 0.20

Version 0.20 is installed and its native audit and normal-startup checks passed, alongside Solace 0.4.0. The package contains 732 verified files and the artwork registry contains 699 entries.

The batch covers seven core sheets: tools, weapons, bobbers, springobjects, Objects_2, debris and Projectiles. Six localized springobjects routes bring the total to thirteen changed routes and eleven new registrations. It does not replace all game artwork or every effects atlas.

The base sheets contain 64,570 newly changed pixels: 60,055 in the five root-owned outputs, 2,605 in weapons and 1,910 in bobbers. All 165 previously approved regions, covering 47,616 pixels, remain exact. Integration preserves 720 prior source asset PNGs; build-directory copies are not counted as additional artwork.

Written-image checks preserve native dimensions, alpha, hidden RGB, silhouettes, protected dark/white pixels, tint relationships and declared duplicate cells. New RGB changes are bounded to 24 levels on the root sheets, 12 on debris and 18 on weapons/bobbers. Bobbers retain complete 16 × 32 cells; weapons retain 16 × 16 cells. Material detail is constrained to the original geometry, so item identity and native alignment remain intact.

The final native audit passed 977 item checks and 103 production player-HD checks. Evidence is under `artifacts/npc-modern/runtime-audits/0.20.0-20260906-195754-813`. Static contracts, exact prompts, generation provenance, before/after sheets and independent validation are under `artifacts/items-modern`. These checks cover the selected native rendering and item routes, not exhaustive gameplay, multiplayer or every animation.

Independent semantic review resolves all 911 native definitions (37 tools, 67 weapons, 807 objects) through the prepared routes, with zero missing textures or source-rectangle mismatches. Fifteen object definitions use a next-cell color overlay; those masks remain on their prepared sheets. Evidence is in artifacts/items-modern/semantic-coverage.md. Normal installation passed all 699 texture registrations and 732 package files. Evidence is under artifacts/npc-modern/installed-audits/0.20.0-20260906-200121-305. Remaining art review areas include tilled soil and floor patterns, minigames, uncovered map/interface regions and other dedicated effect or item sheets. Technical masks and intentionally preserved original pixels are not unfinished art by default.
