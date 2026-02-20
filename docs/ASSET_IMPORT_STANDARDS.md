# Asset Import Standards

## Goal
Define stable texture/audio import defaults to reduce memory and performance regressions as content scales.

## Texture Policies

### UI Sprites / Icons
- `Texture Type`: `Sprite (2D and UI)`
- `sRGB`: enabled
- `Alpha Is Transparency`: enabled
- `Mip Maps`: disabled
- Max size:
  - UI icon/portrait: `512` (or `1024` for high-detail profile cards)
- Compression:
  - PC default: `RGBA 32-bit` only when alpha quality is critical
  - otherwise use platform-compressed default equivalent

### Environment / Background Textures
- `Texture Type`: `Default`
- `Mip Maps`: enabled
- Max size:
  - background tiles/parallax: `1024` baseline
  - hero vistas: `2048` max (document exceptions)
- Compression:
  - use platform-compressed formats (`BC7` / equivalent)
- `Read/Write`: disabled unless runtime processing requires it

### Normal Maps
- `Texture Type`: `Normal map`
- `sRGB`: disabled
- `Mip Maps`: enabled

## Audio Policies

### Music
- `Load Type`: `Streaming`
- `Compression Format`: `Vorbis`
- `Quality`: `0.45` to `0.60`
- `Preload Audio Data`: enabled

### SFX
- Short clips (`< 4s`):
  - `Load Type`: `Decompress on Load`
  - `Compression`: `ADPCM` (or equivalent lightweight)
- Medium/long clips:
  - `Load Type`: `Compressed In Memory`
  - `Compression`: `Vorbis`

### VO / Dialogue
- `Load Type`: `Compressed In Memory`
- `Compression`: `Vorbis`
- `Quality`: `0.55` to `0.70`
- Ensure subtitles alignment remains valid after trimming silence.

## Prohibited Defaults
- Textures imported with `Read/Write` enabled by default.
- Audio clips left at engine defaults without category-specific review.
- UI sprites with mipmaps enabled unless explicitly justified.

## QA Compliance Checks
1. Spot-check new textures for type/mipmap/compression alignment.
2. Spot-check new audio clips for category-appropriate load type.
3. Record intentional exceptions in PR description.
4. Reject content drops that add avoidable import-memory overhead.

## Post-1.0 Automation Proposal
- Add editor audit utility to report non-compliant texture/audio import settings.
- Integrate report into CI as warning-first, fail-later policy.
