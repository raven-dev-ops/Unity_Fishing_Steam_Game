# GitHub Issues Backlog (Client: [REDACTED] • Game: [REDACTED])

Last updated: **2026-02-19**

This document is designed to be **dropped into the repo** and used as a complete backlog for GitHub Issues + Milestones + (optional) GitHub Projects.  
It is **keyboard-first** and aligned to the current project spec (Menu → Harbor → Fishing → Harbor → Sell/Upgrade → repeat).

---

## How to use (recommended workflow)

1) Create the labels in **Labels taxonomy** (one-time).  
2) Create the milestones in **Milestones** (one-time).  
3) Create the **EPIC issues first**, then create the linked **sub-issues** underneath (copy/paste).  
4) Use task lists in Epic descriptions to track progress.  
5) Keep acceptance criteria tight; keep MVP scope tight.

---

## Labels taxonomy (recommended)

Priority:
- `P0-blocker` — cannot ship / blocks core loop
- `P1-high` — must-have for MVP 1.0
- `P2-medium` — important polish / tooling / non-core
- `P3-low` — optional / post-1.0

Type:
- `type:feature`
- `type:bug`
- `type:chore`
- `type:docs`
- `type:qa`
- `type:build`
- `type:art` (requests/needs; implementation may be external)
- `type:audio` (requests/needs; implementation may be external)

System:
- `system:core` — boot, state machine, scene loading, services
- `system:ui` — menus, navigation, aura selection, dialogue bubbles
- `system:harbor`
- `system:fishing`
- `system:economy` — shops, inventory, pricing, currency
- `system:save` — profile, persistence, day counter
- `system:audio`
- `system:tools` — debug panel, editor validation, data pipeline
- `system:steam` — Steamworks integration, packaging, publishing

Scope:
- `scope:mvp` — required for MVP 1.0
- `scope:post-1.0` — later (not required for MVP)
- `scope:content-drop` — monthly content pipeline (fish/ships/hooks)

Status (optional):
- `status:ready`
- `status:blocked`
- `status:in-progress`
- `status:review`
- `status:done`

Risk (optional):
- `risk:save-migration`
- `risk:platform`
- `risk:perf`
- `risk:ux`

---

## Milestones (recommended)

**M0 — Project Setup**: Unity version, repo hygiene, scene stubs, build settings.  
**M1 — Core Services**: Game state machine, scene transitions, input maps, save skeleton.  
**M2 — UI Foundation**: Menu/Profile/Settings/Pause + aura selection; dialogue bubble UI.  
**M3 — Harbor Slice**: Harbor movement + interactables + shops (stubs ok) + sail flow.  
**M4 — Fishing Slice**: Ship/hook movement + waves + fish spawn + catch + inventory add.  
**M5 — MVP Loop Complete**: Sell fish + copecs + upgrades + tutorial flags + persistence.  
**M6 — Content Pipeline & Tools**: ScriptableObject catalogs, validator, tuning config, debug panel.  
**M7 — Audio & Narrative Pass**: SFX triggers + VO/subtitles + mixing.  
**M8 — QA & Steam RC**: Test checklist, perf sanity, Steamworks baseline, SteamPipe upload test.  
**M9 — Steam Launch 1.0**: Store assets checklist + release tagging + launch playbook.  
**M10 — Monthly Content Drop Template** (post-launch).

---

## Project board (optional but helpful)

If you use GitHub Projects, create columns:
- Backlog
- Ready
- In Progress
- Review
- QA
- Done

Rules of thumb:
- Only `status:ready` items go to Ready.
- Limit “In Progress” to what the team can actually finish this week.

---

## Global definitions

Definition of Ready:
- Clear description + acceptance criteria
- Dependencies listed
- Test steps included (even basic)

Definition of Done:
- Feature works end-to-end in target scene(s)
- Keyboard-only inputs verified (WASD/Arrows, Enter, Space, Esc)
- No console errors during normal play
- Save/load verified if data touched
- Documentation updated if workflow/tooling changed

---

# EPICS & ISSUES (copy/paste into GitHub)

> Naming convention used below:  
> **CORE-xxx**, **UI-xxx**, **HARBOR-xxx**, **FISH-xxx**, **ECON-xxx**, **SAVE-xxx**, **AUDIO-xxx**, **TOOLS-xxx**, **STEAM-xxx**, **QA-xxx**, **BUILD-xxx**, **DOCS-xxx**

---

## M0 — Project Setup

### EPIC: CORE-000 — Project foundation & repo hygiene
Labels: `P0-blocker`, `type:chore`, `system:core`, `scope:mvp`  
Milestone: M0 — Project Setup

Sub-issues:
- [ ] CORE-001 — Choose Unity LTS + pipeline (URP/Built-in) and document
- [ ] CORE-002 — Repo hygiene: .gitignore + LFS + meta file rules
- [ ] CORE-003 — Package baseline (TMP/Input/Timeline/etc.)
- [ ] CORE-004 — Scene stubs + Build Settings (Boot/Cinematic/Menu/Harbor/Fishing)
- [ ] DOCS-001 — Add `docs/` structure + spec alignment docs

---

#### Issue: CORE-001 — Choose Unity LTS + pipeline and document
Labels: `P0-blocker`, `type:chore`, `system:core`, `scope:mvp`  
Milestone: M0

Acceptance criteria:
- Unity version recorded in README.
- Rendering pipeline recorded (and project created with it).

Test steps:
1) Clone repo on a clean machine/environment.
2) Open project in selected Unity version; no prompts/errors beyond expected imports.

---

#### Issue: CORE-002 — Repo hygiene: .gitignore + LFS + meta file rules
Labels: `P0-blocker`, `type:chore`, `system:core`, `scope:mvp`  
Milestone: M0

Acceptance criteria:
- `.gitignore` appropriate for Unity.
- Git LFS tracks large binaries (audio/video/textures).
- `.meta` files are committed; asset replacement preserves GUIDs.

Test steps:
1) Add a placeholder texture/audio file and confirm LFS tracking.
2) Replace a texture in-place; confirm references remain.

---

#### Issue: CORE-003 — Package baseline install
Labels: `P0-blocker`, `type:chore`, `system:core`, `scope:mvp`  
Milestone: M0

Acceptance criteria:
- TextMeshPro, Input System, Timeline installed.
- Project compiles cleanly after package import.

---

#### Issue: CORE-004 — Scene stubs + Build Settings
Labels: `P0-blocker`, `type:chore`, `system:core`, `scope:mvp`  
Milestone: M0

Acceptance criteria:
- Scenes exist:
  - `00_Boot`
  - `01_Cinematic`
  - `02_MainMenu`
  - `03_Harbor`
  - `04_Fishing`
- Scenes added to Build Settings in correct order.

---

#### Issue: DOCS-001 — Add docs structure + baseline standards
Labels: `P1-high`, `type:docs`, `system:tools`, `scope:mvp`  
Milestone: M0

Acceptance criteria:
- Add:
  - `docs/ART_SPECS.md`
  - `docs/AUDIO_SPECS.md`
  - `docs/CONTENT_PIPELINE.md`
  - `docs/INPUT_MAP.md`
- Each doc includes “MVP rules” + file naming + pivot guidance.

---

## M1 — Core Services

### EPIC: CORE-010 — GameFlow services (boot → state → scenes)
Labels: `P0-blocker`, `type:feature`, `system:core`, `scope:mvp`  
Milestone: M1 — Core Services

Sub-issues:
- [ ] CORE-011 — GameFlowManager state machine
- [ ] CORE-012 — SceneLoader + fade transitions
- [ ] INPUT-011 — Input action maps + context switching
- [ ] SAVE-011 — SaveManager v1 + versioned data model
- [ ] AUDIO-011 — AudioManager + AudioMixer groups

---

#### Issue: CORE-011 — GameFlowManager (state machine)
Labels: `P0-blocker`, `type:feature`, `system:core`, `scope:mvp`  
Milestone: M1

States:
- Cinematic
- MainMenu
- Harbor
- Fishing
- Pause (overlay in Harbor/Fishing)

Acceptance criteria:
- All state transitions go through GameFlowManager.
- Esc opens Pause in Harbor/Fishing, closes Pause when pressed again.
- “Return to Town Harbor” works from Fishing pause menu.

Test steps:
1) Start game → Harbor → Fishing → Pause → Return to Harbor.
2) Validate there are no duplicate persistent managers (DontDestroyOnLoad).

---

#### Issue: CORE-012 — SceneLoader + transitions
Labels: `P1-high`, `type:feature`, `system:core`, `scope:mvp`  
Milestone: M1

Acceptance criteria:
- Fade out → load → fade in.
- Input disabled during transitions.
- Handles quitting and returning to menu cleanly.

---

#### Issue: INPUT-011 — Input System action maps (UI/Harbor/Fishing)
Labels: `P0-blocker`, `type:feature`, `system:core`, `scope:mvp`  
Milestone: M1

Acceptance criteria:
- UI: Navigate, Submit(Enter), Cancel(Esc).
- Harbor: Move, Interact(Enter), Pause(Esc).
- Fishing: MoveShip(L/R), MoveHook(U/D), Action(Space), Pause(Esc).
- Only correct map active at a time.

---

#### Issue: SAVE-011 — SaveManager v1 (versioned)
Labels: `P0-blocker`, `type:feature`, `system:save`, `scope:mvp`, `risk:save-migration`  
Milestone: M1

Save fields (minimum):
- saveVersion
- copecs
- equippedShipId / equippedHookId
- ownedShips / ownedHooks
- fishInventory: list of (fishId, distanceTier, count)
- tutorial flags
- careerStartLocalDate + lastLoginLocalDate
- stats: totalFishCaught, farthestDistanceTier, totalTrips

Acceptance criteria:
- Save created on first run and loaded on relaunch.
- Non-breaking save version field included.

---

#### Issue: AUDIO-011 — AudioManager + AudioMixer scaffolding
Labels: `P1-high`, `type:feature`, `system:audio`, `scope:mvp`  
Milestone: M1

Acceptance criteria:
- AudioMixer groups: Music/SFX/VO.
- Simple playback API supports UI + fishing triggers.
- Settings sliders will be able to control the groups later.

---

## M2 — UI Foundation

### EPIC: UI-010 — Keyboard-first UI + aura selection everywhere
Labels: `P0-blocker`, `type:feature`, `system:ui`, `scope:mvp`  
Milestone: M2 — UI Foundation

Sub-issues:
- [ ] UI-011 — Main Menu (Start/Profile/Settings/Exit)
- [ ] UI-012 — Aura highlight for UI selection (EventSystem)
- [ ] UI-013 — Settings (audio sliders + display basics)
- [ ] UI-014 — Profile (career stats from save)
- [ ] UI-015 — Pause Menu (Resume/Town Harbor/Settings/Exit)
- [ ] UI-016 — Dialogue bubbles UI component (for tutorial)

---

#### Issue: UI-011 — Main Menu
Labels: `P0-blocker`, `type:feature`, `system:ui`, `scope:mvp`  
Milestone: M2

Acceptance criteria:
- Keyboard nav works; aura highlights selection.
- Start routes into Harbor through GameFlowManager.

---

#### Issue: UI-012 — Aura highlight for selected UI elements
Labels: `P0-blocker`, `type:feature`, `system:ui`, `scope:mvp`  
Milestone: M2

Acceptance criteria:
- Aura follows current selected UI object.
- Works in: Main Menu, Pause, Settings, Profile, Shops.
- No jitter when holding arrow keys.

---

#### Issue: UI-013 — Settings menu
Labels: `P1-high`, `type:feature`, `system:ui`, `system:audio`, `scope:mvp`  
Milestone: M2

Acceptance criteria:
- Music/SFX/VO sliders impact mixer.
- At least one display option implemented or explicitly deferred in UI text.

---

#### Issue: UI-014 — Profile menu
Labels: `P1-high`, `type:feature`, `system:ui`, `system:save`, `scope:mvp`  
Milestone: M2

Acceptance criteria:
- Shows: Day #, Copecs, Total Fish Caught, Farthest Distance Tier.
- Optional: “Reset Profile” with confirm prompt (or deferred with placeholder).

---

#### Issue: UI-015 — Pause Menu
Labels: `P0-blocker`, `type:feature`, `system:ui`, `scope:mvp`  
Milestone: M2

Acceptance criteria:
- Esc toggles pause.
- “Town Harbor” exits fishing and loads Harbor (inventory preserved).
- “Exit Game” quits build cleanly.

---

#### Issue: UI-016 — Dialogue bubbles UI (tutorial)
Labels: `P1-high`, `type:feature`, `system:ui`, `scope:mvp`  
Milestone: M2

Acceptance criteria:
- Text bubbles support multiple lines with Enter to advance.
- Optional VO reference per line supported (even placeholders).

---

## M3 — Harbor Slice

### EPIC: HARBOR-010 — Harbor scene core loop
Labels: `P0-blocker`, `type:feature`, `system:harbor`, `scope:mvp`  
Milestone: M3 — Harbor Slice

Sub-issues:
- [ ] HARBOR-011 — Player movement and boundaries
- [ ] HARBOR-012 — Interactables + world aura (shops + sail)
- [ ] SHOP-010 — Hook Shop UI (buy/equip)
- [ ] SHOP-011 — Boat Shop UI (buy/equip)
- [ ] SHOP-012 — Fish Shop UI (sell all)
- [ ] NARR-010 — Mermaid tutorial trigger (first-time only)

---

#### Issue: HARBOR-011 — Player movement and boundaries
Labels: `P0-blocker`, `type:feature`, `system:harbor`, `scope:mvp`  
Milestone: M3

Acceptance criteria:
- WASD/Arrows move player.
- Player cannot leave the walkable area.

---

#### Issue: HARBOR-012 — World interactables + aura selection
Labels: `P0-blocker`, `type:feature`, `system:harbor`, `system:ui`, `scope:mvp`  
Milestone: M3

Acceptance criteria:
- Enter interacts with nearest/in-range interactable.
- Aura indicates active interactable.
- Works for: Hook Shop, Boat Shop, Fish Shop, Sail arrow.

---

#### Issue: SHOP-010 — Hook Shop UI (Lv1–Lv3)
Labels: `P0-blocker`, `type:feature`, `system:economy`, `system:ui`, `scope:mvp`  
Milestone: M3

Acceptance criteria:
- Lists hooks with price; shows owned/equipped state.
- Purchase checks copecs and updates save.
- Equip updates Fishing behavior (max depth / stats).

---

#### Issue: SHOP-011 — Boat Shop UI (Lv1–Lv3)
Labels: `P0-blocker`, `type:feature`, `system:economy`, `system:ui`, `scope:mvp`  
Milestone: M3

Acceptance criteria:
- Lists boats with price; shows owned/equipped state.
- Equip updates Fishing trip distance tier cap.

---

#### Issue: SHOP-012 — Fish Shop UI (Sell All)
Labels: `P0-blocker`, `type:feature`, `system:economy`, `system:ui`, `scope:mvp`  
Milestone: M3

Acceptance criteria:
- Single button sells all fish.
- Value uses distance tier caught multiplier.
- Inventory clears and save updates.

---

#### Issue: NARR-010 — Mermaid tutorial (first-time only)
Labels: `P1-high`, `type:feature`, `system:harbor`, `system:ui`, `system:save`, `scope:mvp`  
Milestone: M3

Acceptance criteria:
- Plays only if `tutorialSeen == false`.
- Blocks other interactions during tutorial.
- Sets flag and saves when done/skip.

---

## M4 — Fishing Slice

### EPIC: FISH-010 — Fishing mode playable loop
Labels: `P0-blocker`, `type:feature`, `system:fishing`, `scope:mvp`  
Milestone: M4 — Fishing Slice

Sub-issues:
- [ ] FISH-011 — Wave animation system (2-layer)
- [ ] FISH-012 — Ship movement (left/right) with bounds
- [ ] FISH-013 — Hook movement (up/down) with bounds
- [ ] FISH-014 — Fishing action state machine (Space)
- [ ] FISH-015 — Fish spawner (filter by distance + depth)
- [ ] FISH-016 — Catch → inventory add + audio triggers
- [ ] FISH-017 — Pause/exit to Town Harbor

---

#### Issue: FISH-011 — Waves (2-layer animation)
Labels: `P1-high`, `type:feature`, `system:fishing`, `scope:mvp`  
Milestone: M4

Acceptance criteria:
- Two wave layers animate continuously.
- Speed tunable via config (TuningConfig).

---

#### Issue: FISH-012 — Ship movement (horizontal)
Labels: `P0-blocker`, `type:feature`, `system:fishing`, `scope:mvp`  
Milestone: M4

Acceptance criteria:
- L/R arrows move ship along a horizontal line.
- Speed comes from ShipDefinition (or ship stats config).

---

#### Issue: FISH-013 — Hook movement (vertical)
Labels: `P0-blocker`, `type:feature`, `system:fishing`, `scope:mvp`  
Milestone: M4

Acceptance criteria:
- U/D arrows move hook depth.
- Max depth comes from HookDefinition.
- Hook stays aligned to ship (line optional).

---

#### Issue: FISH-014 — Space key state machine (Cast/Reel/Resolve)
Labels: `P0-blocker`, `type:feature`, `system:fishing`, `scope:mvp`, `risk:ux`  
Milestone: M4

Acceptance criteria:
- Explicit states implemented (Cast → InWater → Hooked → Reel → Resolve).
- UI/audio feedback triggers on each transition.

---

#### Issue: FISH-015 — Fish spawner (distance + depth filters)
Labels: `P0-blocker`, `type:feature`, `system:fishing`, `system:tools`, `scope:mvp`  
Milestone: M4

Acceptance criteria:
- FishDefinitions filtered by:
  - tripDistanceTier in [minDistanceTier, maxDistanceTier]
  - currentDepth in [minDepth, maxDepth] (or within a band)
- Weighted random supported (rarityWeight field).

---

#### Issue: FISH-016 — Catch resolver + inventory add
Labels: `P0-blocker`, `type:feature`, `system:fishing`, `system:economy`, `system:audio`, `scope:mvp`  
Milestone: M4

Acceptance criteria:
- Fish collision triggers Hooked and Resolve.
- Adds inventory stack using distanceTierCaught.
- Plays cast/hook/catch SFX (placeholders ok).

---

#### Issue: FISH-017 — Pause/Exit flow to Harbor
Labels: `P0-blocker`, `type:feature`, `system:fishing`, `system:ui`, `scope:mvp`  
Milestone: M4

Acceptance criteria:
- Esc opens Pause Menu.
- Town Harbor returns to Harbor scene with inventory intact.

---

## M5 — MVP Loop Complete

### EPIC: LOOP-010 — End-to-end MVP loop with progression + persistence
Labels: `P0-blocker`, `type:feature`, `system:core`, `scope:mvp`  
Milestone: M5 — MVP Loop Complete

Sub-issues:
- [ ] ECON-020 — Pricing model (distance multiplier) + sell total summary
- [ ] SAVE-020 — Day counter (local date) + display in Profile/HUD
- [ ] UI-020 — HUD overlay (copecs/day/distance/depth)
- [ ] DATA-010 — ScriptableObject content definitions (fish/ships/hooks)
- [ ] DATA-011 — Catalog loading + references used everywhere
- [ ] NARR-020 — Tutorial skip + replay placeholder (optional)

---

#### Issue: ECON-020 — Distance-based sell formula + summary
Labels: `P1-high`, `type:feature`, `system:economy`, `scope:mvp`  
Milestone: M5

Acceptance criteria:
- Total sell value computed correctly for each stack (fishId + distanceTierCaught).
- UI shows breakdown: item count, per-item value, total earned (at least total earned).

---

#### Issue: SAVE-020 — Day counter based on local date
Labels: `P1-high`, `type:feature`, `system:save`, `scope:mvp`, `risk:ux`  
Milestone: M5

Acceptance criteria:
- Day # computed from careerStartLocalDate.
- Displayed in Profile and HUD.
- Limitation documented: system clock changes affect the day count (unless online time later).

---

#### Issue: UI-020 — HUD overlay (Harbor + Fishing)
Labels: `P1-high`, `type:feature`, `system:ui`, `scope:mvp`  
Milestone: M5

Acceptance criteria:
- Harbor HUD: copecs + day.
- Fishing HUD: distance tier + depth indicator; (optional) copecs.
- Does not obstruct gameplay.

---

#### Issue: DATA-010 — ScriptableObject definitions (Fish/Ship/Hook)
Labels: `P0-blocker`, `type:feature`, `system:tools`, `scope:mvp`  
Milestone: M5

Acceptance criteria:
- Stable string IDs for all items.
- Code does not hardcode fish/ship/hook lists.

---

#### Issue: DATA-011 — Catalog loading (GameConfig)
Labels: `P1-high`, `type:feature`, `system:tools`, `scope:mvp`  
Milestone: M5

Acceptance criteria:
- Single GameConfig references catalogs.
- Spawner + shops query catalogs.
- Missing IDs/assets surface as clear errors.

---

## M6 — Content Pipeline & Tools

### EPIC: TOOLS-010 — Data-driven scaling and validation
Labels: `P1-high`, `type:feature`, `system:tools`, `scope:mvp`  
Milestone: M6 — Content Pipeline & Tools

Sub-issues:
- [ ] TOOLS-011 — TuningConfig ScriptableObject
- [ ] TOOLS-012 — DEV debug panel (F1) for tuning and spawn tests
- [ ] TOOLS-013 — Editor validator (duplicate IDs, missing assets, invalid ranges)
- [ ] DOCS-010 — “How to add a fish/ship/hook” step-by-step

---

#### Issue: TOOLS-011 — TuningConfig (all key variables)
Labels: `P1-high`, `type:feature`, `system:tools`, `scope:mvp`  
Milestone: M6

Acceptance criteria:
- Wave speeds, move multipliers, spawn rate, and sell multiplier step are editable without code.
- Values applied at runtime.

---

#### Issue: TOOLS-012 — Debug panel (DEV only)
Labels: `P2-medium`, `type:feature`, `system:tools`, `scope:mvp`  
Milestone: M6

Acceptance criteria:
- Toggle with F1.
- Controls for wave speed/spawn rate; buttons for add copecs/unlock items/spawn fish/clear inventory.
- Not included in release builds.

---

#### Issue: TOOLS-013 — Editor validator
Labels: `P2-medium`, `type:feature`, `system:tools`, `scope:mvp`  
Milestone: M6

Acceptance criteria:
- Detects duplicate IDs, missing sprites, invalid depth/distance ranges.
- Outputs a clear report in Console (or editor window).

---

#### Issue: DOCS-010 — Content pipeline guide
Labels: `P2-medium`, `type:docs`, `system:tools`, `scope:mvp`  
Milestone: M6

Acceptance criteria:
- A non-programmer can add a new fish by following the doc.
- Includes naming + pivot + how to register in catalogs.

---

## M7 — Audio & Narrative Pass

### EPIC: AUDIO-010 — Audio triggers + mixing + tutorial VO
Labels: `P1-high`, `type:feature`, `system:audio`, `scope:mvp`  
Milestone: M7 — Audio & Narrative Pass

Sub-issues:
- [ ] AUDIO-011 — Implement required SFX trigger map
- [ ] AUDIO-012 — Music + ambience loops per scene
- [ ] AUDIO-013 — VO playback + subtitle sync in tutorial

---

#### Issue: AUDIO-011 — SFX trigger map
Labels: `P1-high`, `type:feature`, `system:audio`, `scope:mvp`  
Milestone: M7

Acceptance criteria:
- Triggers: UI navigate/select/cancel; cast/hook/catch; sell; purchase; depart/return.
- No duplicate firing on single action.

---

#### Issue: AUDIO-012 — Music + ambience loops
Labels: `P2-medium`, `type:audio`, `system:audio`, `scope:mvp`  
Milestone: M7

Acceptance criteria:
- Menu/Harbor/Fishing loops play and respect settings sliders.

---

#### Issue: AUDIO-013 — VO + subtitles in tutorial
Labels: `P2-medium`, `type:feature`, `system:audio`, `system:ui`, `scope:mvp`  
Milestone: M7

Acceptance criteria:
- Dialogue lines can reference VO clips (optional).
- Subtitles remain readable; skip behavior correct.

---

## M8 — QA & Steam RC

### EPIC: QA-000 — Release candidate hardening
Labels: `P0-blocker`, `type:qa`, `system:core`, `scope:mvp`  
Milestone: M8 — QA & Steam RC

Sub-issues:
- [ ] QA-010 — Smoke test checklist + regression pass
- [ ] QA-011 — Keyboard-only UX audit (menus/shops/fishing/pause)
- [ ] PERF-010 — Performance sanity (spawn + UI)
- [ ] BUILD-010 — Build pipeline (Windows) + release settings
- [ ] STEAM-010 — Steamworks integration baseline (overlay/launch)
- [ ] STEAM-011 — SteamPipe upload test (beta branch)

---

#### Issue: QA-010 — Smoke test checklist
Labels: `P1-high`, `type:qa`, `system:core`, `scope:mvp`  
Milestone: M8

Acceptance criteria:
- `docs/QA_SMOKE_TEST.md` exists and is runnable end-to-end.

---

#### Issue: BUILD-010 — Windows build pipeline
Labels: `P0-blocker`, `type:build`, `system:core`, `scope:mvp`  
Milestone: M8

Acceptance criteria:
- Clean Windows build produced with correct scenes.
- Save path verified and documented.

---

#### Issue: STEAM-010 — Steamworks.NET baseline integration
Labels: `P1-high`, `type:feature`, `system:steam`, `scope:mvp`, `risk:platform`  
Milestone: M8

Acceptance criteria:
- Launch via Steam; overlay functional.
- App ID handling documented for dev vs release.

---

#### Issue: STEAM-011 — SteamPipe upload test
Labels: `P1-high`, `type:build`, `system:steam`, `scope:mvp`  
Milestone: M8

Acceptance criteria:
- Upload to beta branch succeeds.
- Install/update verified on a clean install.

---

## M9 — Steam Launch 1.0

### EPIC: STEAM-020 — Launch readiness checklist
Labels: `P0-blocker`, `type:chore`, `system:steam`, `scope:mvp`  
Milestone: M9 — Steam Launch 1.0

Sub-issues:
- [ ] STEAM-021 — Store page assets checklist (capsules/screens/trailer)
- [ ] STEAM-022 — Release tagging (`v1.0.0`) + build upload
- [ ] DOCS-020 — Post-launch hotfix process + branch strategy

---

# Post-1.0 (optional backlog)

### EPIC: DROP-000 — Monthly content drops (fish/ships/hooks)
Labels: `P2-medium`, `type:chore`, `system:tools`, `scope:post-1.0`  
Milestone: M10 — Monthly Content Drop Template

Sub-issues:
- [ ] DROP-001 — “Add a fish” checklist (no code changes)
- [ ] DROP-002 — “Add a ship/hook” checklist (no code changes)
- [ ] DROP-003 — Balance pass checklist + validator gates
- [ ] DROP-004 — Save migration policy (version bumps + tests)

---

## Issue template (copy/paste)

### Issue: <ID> — <Title>
Labels: `P?`, `type:?`, `system:?`, `scope:?`  
Milestone: M?

Description:
<What and why>

Dependencies:
- <None / list>

Tasks:
- [ ] …
- [ ] …

Acceptance criteria:
- …
- …

Test steps:
1) …
2) …

---

## PR checklist (copy/paste into PR description)

- [ ] Matches keyboard-only input requirements
- [ ] No new console errors/warnings (or explained)
- [ ] Save/load verified if data touched
- [ ] Updated docs if workflow changed
- [ ] Added/updated tests or manual test steps

