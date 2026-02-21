# Scene Capture Baselines

This directory contains approved baseline PNG files used by `.github/workflows/ci-scene-capture.yml`.

Expected file names:
- `00_Boot.png`
- `01_Cinematic.png`
- `02_MainMenu.png`
- `03_Harbor.png`
- `04_Fishing.png`

CI compares fresh captures from `artifacts/scene-captures` against these files using `scripts/ci/compare-scene-captures.py`.

When scene visuals are intentionally updated:
1. Run the scene capture workflow manually.
2. Review `scene-capture-diff-<sha>` artifacts.
3. Replace baseline PNGs in this directory with approved captures.
4. Commit baseline updates with a short rationale.
