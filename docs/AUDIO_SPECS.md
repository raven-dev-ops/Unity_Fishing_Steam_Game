# Audio Specs (MVP)

## MVP Rules
- Prioritize responsive gameplay feedback over cinematic layering.
- Keep SFX short and clear for keyboard-driven interactions.
- Avoid frequency conflicts with dialogue and tutorial VO.

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
