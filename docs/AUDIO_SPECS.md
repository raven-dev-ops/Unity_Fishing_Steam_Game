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
- Music ducks to a reduced multiplier while VO source is playing.
- In the absence of a mixer, source-volume fallback is applied directly.
- Fishing runtime can map dedicated clips to:
  - tension warning
  - tension critical
  - line snap fail
  - fish escaped fail
  - missed hook fail
