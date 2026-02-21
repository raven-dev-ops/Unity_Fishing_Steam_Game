# Accessibility Conformance Map

## Scope
- Internal baseline mapping against common game accessibility categories (input, readability, motion, subtitles, contrast).
- This is an engineering conformance map, not an external certification artifact.

## Gap Audit (Before -> After)
| Category | Before | After |
|---|---|---|
| Input mode flexibility | Hold-only reel interaction | Hold/toggle reel mode setting persisted |
| Motion sensitivity | No explicit motion toggle | Reduced motion toggle applied to wave/camera motion |
| Subtitle quality controls | Subtitles on/off only | Subtitles on/off + scale + background opacity |
| Readability controls | UI scale + high-contrast fishing cues | Added readability boost option plus existing controls |
| Persistence | Partial accessibility persistence | All listed accessibility settings persisted via `UserSettingsService` |

## Implemented Options by Category
| Category | Option | Runtime Surface | Persistence |
|---|---|---|---|
| Input | Reel input mode (`hold` / `toggle`) | `CatchResolver` + settings UI | PlayerPrefs |
| Motion | Reduced motion | `WaveAnimator`, `FishingCameraController` | PlayerPrefs |
| Subtitles | Enable/disable | `DialogueBubbleController` | PlayerPrefs |
| Subtitles | Subtitle scale | `DialogueBubbleController` | PlayerPrefs |
| Subtitles | Subtitle background opacity | `DialogueBubbleController` | PlayerPrefs |
| Readability | UI scale | `GlobalUiAccessibilityService` | PlayerPrefs |
| Readability | Readability boost | `GlobalUiAccessibilityService`, `DialogueBubbleController` | PlayerPrefs |
| Contrast | High-contrast fishing cues | `HudOverlayController` | PlayerPrefs |

## Verification Checklist
1. Execute accessibility steps in `docs/QA_SMOKE_TEST.md`.
2. Validate persistence by relaunching after each option change.
3. Record any known issues/exceptions in test run notes.
4. Complete 1.0 signoff artifact in `docs/UX_ACCESSIBILITY_SIGNOFF.md`.

## Known Exceptions
- No external third-party accessibility certification has been performed.
- Platform-level text-to-speech/speech-to-text integration is not implemented in current baseline.
- Unity execution-backed evidence is deferred while `UNITY_EXECUTION_ENFORCE=false`.
