# Scene Capture Baseline

## Purpose
- Detect high-impact visual regressions in key scenes with deterministic CI checks.
- Keep approved reference captures versioned in-repo at `ci/scene-capture-baseline/`.

## Workflow
- Capture workflow: `.github/workflows/ci-scene-capture.yml` (manual dispatch).
- Capture source: `Assets/Tests/PlayMode/SceneCapturePlayModeTests.cs`.
- Diff script: `scripts/ci/compare-scene-captures.py`.
- Baseline folder: `ci/scene-capture-baseline/`.
- Diff artifacts:
  - `scene-capture-diff-<sha>` (includes summary JSON/Markdown and per-scene diff panels).

## Threshold Policy
- Warn threshold env: `SCENE_CAPTURE_WARN_THRESHOLD` (default `0.015`).
- Fail threshold env: `SCENE_CAPTURE_FAIL_THRESHOLD` (default `0.030`).
- Optional enforcement toggle:
  - Repository variable `SCENE_CAPTURE_DIFF_ENFORCE=true` fails workflow when severe diffs are detected (`fail`, `missing_capture`, `missing_baseline`, `dimension_mismatch`).
  - Default behavior is non-blocking diagnostics.

## Baseline Update Procedure
1. Manually dispatch `.github/workflows/ci-scene-capture.yml`.
2. Download and inspect:
   - `scene-captures-<sha>`
   - `scene-capture-diff-<sha>`
3. If visual changes are intentional, update files in `ci/scene-capture-baseline/` with approved capture PNGs.
4. Commit baseline updates and reference the visual-change issue/PR.
5. Re-run scene capture workflow and verify diff summary is within expected thresholds.
