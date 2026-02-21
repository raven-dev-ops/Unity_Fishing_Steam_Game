# 1.0 UX/Accessibility Signoff

## Scope
- Issue: `#210`
- Focus areas:
  - tutorial clarity and anti-frustration behavior,
  - readability/accessibility defaults and persistence,
  - final QA gates for 1.0 readiness.
- Current operating constraint: Unity execution enforcement is intentionally deferred (`UNITY_EXECUTION_ENFORCE=false`).

## Focused Findings and Tuning

### Tutorial Clarity
- Runtime prompts are explicit for cast/hook/reel sequence and use rebound action labels from `InputRebindingService`.
- Recovery path prevents soft-lock by auto-completing after repeated failures (`FishingLoopTutorialController`).
- No further tutorial copy blockers found for 1.0 baseline.

### Anti-Frustration Tuning (Applied)
- `CatchResolver` and `FishingAssistSettings` launch defaults were tuned:
  - `NoBitePityThresholdCasts`: `3 -> 2`
  - `PityBiteDelayScale`: `0.55 -> 0.50`
  - `AdaptiveFailureThreshold`: `3 -> 2`
  - `AdaptiveHookWindowBonusSeconds`: `0.35 -> 0.40`
- Rationale:
  - Reduce early-session churn for new players.
  - Activate assistance one failure/cast earlier while preserving mastery ceiling.
  - Keep cooldown (`2` catches) unchanged to avoid over-assist loops.

### Readability Defaults (Applied)
- `UserSettingsService` subtitle background opacity default tuned:
  - `0.65 -> 0.72`
- Rationale:
  - Improves subtitle legibility against bright fishing water/sky backgrounds.
  - Retains user control via persisted accessibility settings.

## Default Rationale (1.0)

| Setting | 1.0 Default | Rationale |
|---|---|---|
| Subtitles enabled | `true` | Safe baseline for dialogue comprehension and noisy play environments. |
| Subtitle scale | `1.0` | Neutral baseline with user-adjustable guardrails (`0.8` to `1.5`). |
| Subtitle background opacity | `0.72` | Better readability without fully opaque subtitle plate. |
| Readability boost | `false` | Strong styling remains opt-in to preserve default art presentation. |
| Reduced motion | `false` | Baseline presentation retained, opt-in available for sensitivity needs. |
| Reel input toggle | `false` | Hold-to-reel remains baseline, toggle available as accessibility option. |

## QA Signoff Gates (1.0)
- [x] Accessibility smoke path defined in `docs/QA_SMOKE_TEST.md`.
- [x] Conformance mapping updated in `docs/ACCESSIBILITY_CONFORMANCE.md`.
- [x] Anti-frustration defaults and rationale documented.
- [x] Readability defaults and rationale documented.
- [x] Known non-blocking exceptions recorded below.

## Known Non-Blocking Exceptions
- No external third-party accessibility certification has been performed.
- Platform-level text-to-speech/speech-to-text integration is not implemented in current baseline.
- Unity execution-backed test evidence is deferred under current repository execution mode; deterministic non-Unity checks remain in place.

## Evidence
- Runtime defaults:
  - `Assets/Scripts/Fishing/CatchResolver.cs`
  - `Assets/Scripts/Fishing/FishingAssistService.cs`
  - `Assets/Scripts/Core/UserSettingsService.cs`
- EditMode coverage updates:
  - `Assets/Tests/EditMode/FishingAssistServiceTests.cs`
  - `Assets/Tests/EditMode/UserSettingsAccessibilityTests.cs`
- QA/accessibility docs:
  - `docs/QA_SMOKE_TEST.md`
  - `docs/ACCESSIBILITY_BASELINE.md`
  - `docs/ACCESSIBILITY_CONFORMANCE.md`
