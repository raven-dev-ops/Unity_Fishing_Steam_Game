# Audio Specs (MVP)

## MVP Rules
- Prioritize responsive gameplay feedback over cinematic layering.
- Keep SFX short and clear for keyboard-driven interactions.
- Avoid frequency conflicts with dialogue and tutorial VO.
- Route runtime audio into distinct mixer lanes where available:
  - `Master`
  - `Music`
  - `SFX`
  - `VO`
- Persist volume settings (`master/music/sfx/vo`) across sessions.
- Apply basic VO-over-music ducking when voice playback is active.
- Reserve short SFX cues for fishing tension transitions and fail outcomes.

## File Naming
- Format: `<type>_<event>_<variant>_<vNN>`
- Examples:
  - `sfx_ui_select_a_v01`
  - `sfx_fish_catch_b_v01`
  - `amb_harbor_loop_v01`

## Pivot Guidance (Timing/Sync)
- UI SFX start on input frame.
- Fishing action sounds should align to cast/hook/resolve transitions.
- VO subtitle timing should include slight lead-in (100-200ms).

## Loudness Targets
- SFX: around -14 LUFS integrated baseline.
- Music: around -18 LUFS integrated baseline.
- VO: around -16 LUFS integrated baseline.

## Runtime Behavior Baseline
- Audio settings are loaded from user settings on boot.
- Fresh profiles initialize non-zero audio defaults (`master 0.85`, `music 0.75`, `sfx 0.85`, `vo 0.85`) before first playback.
- Persisted user values are authoritative on subsequent boots, including explicit muted (`0`) channels.
- Music ducks to a reduced multiplier while VO source is playing.
- In the absence of a mixer, source-volume fallback is applied directly.
- Phase-two audio key contract uses required keys from `PhaseTwoAudioContract`.
- Dev/QA profiles may synthesize temporary phase-two fallback tones for missing required keys.
- Release profile disables synthesized phase-two fallback generation and relies on explicit release-audio validation (`ReleaseAudioContentValidator`) to fail fast on missing or non-release-qualified critical keys.
- Fishing runtime can map dedicated clips to:
  - tension warning
  - tension critical
  - line snap fail
  - fish escaped fail
  - missed hook fail

## Import Baseline
- Follow `docs/ASSET_IMPORT_STANDARDS.md` for clip import/load defaults.
- Category defaults:
  - Music -> streaming Vorbis.
  - SFX -> decompress-on-load for short clips, compressed-in-memory for longer clips.
  - VO -> compressed-in-memory Vorbis with subtitle timing validation.
