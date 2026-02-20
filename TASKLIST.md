# TASKLIST

Updated from open GitHub issues on 2026-02-20 after closing #115, #118, #124, #129, #130, #131.

## Completed This Pass
- [x] #115 REVIEW-005 - Add command-line Windows build entrypoint and metadata injection
- [x] #118 REVIEW-008 - Expand content validator rules and enforce headless CI gate
- [x] #124 REVIEW-014 - Add CI/CD workflows, code quality gates, and reproducible release pipeline
- [x] #129 REVIEW-019 - Improve developer onboarding docs and contribution workflow
- [x] #130 REVIEW-020 - Add LICENSE and third-party notices for compliance
- [x] #131 REVIEW-021 - Enforce CI secret hygiene and protected release/upload workflows

## Next Focus
- [ ] #116 REVIEW-006 - Introduce assembly definitions for runtime/editor/test boundaries
- [ ] #117 REVIEW-007 - Implement Unity Test Framework suites (EditMode and PlayMode)
- [ ] #119 REVIEW-009 - Replace pervasive FindObjectOfType usage with explicit wiring
- [ ] #120 REVIEW-010 - Harden game flow state/scene mapping and transition rules
- [ ] #133 BACKLOG-001 - Structured logging and in-game debug console (dev builds)

## Open Issues

### M0 - Foundation (Repo + Build + Baseline Play) (5)
- [ ] #116 REVIEW-006 - Introduce assembly definitions for runtime/editor/test boundaries (P1-high)
- [ ] #117 REVIEW-007 - Implement Unity Test Framework suites (EditMode and PlayMode) (P1-high)
- [ ] #119 REVIEW-009 - Replace pervasive FindObjectOfType usage with explicit wiring (P1-high)
- [ ] #120 REVIEW-010 - Harden game flow state/scene mapping and transition rules (P2-medium)
- [ ] #133 BACKLOG-001 - Structured logging and in-game debug console (dev builds) (P0-blocker)

### M1 - Vertical Slice (Core Fishing Loop) (10)
- [ ] #112 REVIEW-002 - Unify input architecture on Input System action maps (P0-blocker)
- [ ] #113 REVIEW-003 - Eliminate avoidable GC allocations in FishSpawner roll path (P0-blocker)
- [ ] #114 REVIEW-004 - Harden save system initialization and atomic write path (P0-blocker)
- [ ] #122 REVIEW-012 - Bring audio pipeline to production readiness (P2-medium)
- [ ] #135 BACKLOG-003 - Fishing controller and camera polish with controller baseline (P0-blocker)
- [ ] #136 BACKLOG-004 - Fish behavior model depth (bite windows, stamina, escape logic) (P0-blocker)
- [ ] #137 BACKLOG-005 - Tension and line feedback system with readable fail states (P1-high)
- [ ] #138 BACKLOG-006 - Catch log and inventory UI baseline (P1-high)
- [ ] #139 BACKLOG-007 - Single polished fishing environment slice (P1-high)
- [ ] #140 BACKLOG-008 - Fishing loop tutorialization (cast/hook/reel) with recovery (P1-high)

### M2 - Progression + Content (9)
- [ ] #121 REVIEW-011 - Clarify distance-tier economy multiplier policy and add tests (P2-medium)
- [ ] #125 REVIEW-015 - Establish performance profiling baseline and regression budget (P1-high)
- [ ] #134 BACKLOG-002 - Settings system completeness (graphics/audio/input persistence) (P1-high)
- [ ] #141 BACKLOG-009 - Progression system baseline (XP, levels, unlocks) (P1-high)
- [ ] #142 BACKLOG-010 - Time-of-day and weather modifiers for fish behavior (P2-medium)
- [ ] #143 BACKLOG-011 - In-game objectives system (non-Steam) (P2-medium)
- [ ] #147 BACKLOG-015 - UI architecture decoupling and lifecycle safety (P1-high)
- [ ] #148 BACKLOG-016 - Accessibility baseline pass (subtitles, UI scale, readable cues) (P2-medium)
- [ ] #149 BACKLOG-017 - Build configuration separation for Dev, QA, and Release (P1-high)

### M3 - Steam Release Candidate (7)
- [ ] #110 REVIEW-000 - Deep research recommendations backlog import (P1-high)
- [ ] #111 REVIEW-001 - Steamworks initialization and runtime safety (P0-blocker)
- [ ] #127 REVIEW-017 - Implement Steam achievements and stats MVP (P1-high)
- [ ] #128 REVIEW-018 - Rehearse SteamPipe beta uploads with secure deployment scripts (P1-high)
- [ ] #144 BACKLOG-012 - Steam Cloud saves integration and conflict strategy (P1-high)
- [ ] #145 BACKLOG-013 - Steam Rich Presence integration (optional) (P2-medium)
- [ ] #146 BACKLOG-014 - Crash reporting strategy and privacy disclosure (P1-high)

### M4 - Post-launch (5)
- [ ] #123 REVIEW-013 - Evaluate and pilot Addressables for scalable content loading (P2-medium)
- [ ] #126 REVIEW-016 - Define asset import optimization standards for textures and audio (P2-medium)
- [ ] #132 REVIEW-100 - Post-1.0 deep research backlog (P2-medium)
- [ ] #150 BACKLOG-018 - Photo mode and screenshot tooling for marketing (P2-medium)
- [ ] #151 BACKLOG-019 - Mod support strategy definition (workshop or manual) (P2-medium)
