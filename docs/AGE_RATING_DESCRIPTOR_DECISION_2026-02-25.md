# Age Rating and Content Descriptor Decision (2026-02-25)

## Scope
- Issue: `#233`
- Release scope reviewed: Steam desktop launch path, English-only storefront metadata.

## Decision Record
- Formal third-party board rating artifact requirement for this scoped release: **Not required**.
- Steam content-descriptor coverage requirement: **Required and versioned per RC**.
- Baseline descriptor evidence record:
  - `release/steam_descriptors/rc-2026-02-25/descriptor_manifest.json`

## Descriptor Verification Matrix
| Descriptor | Baseline Value | Verification Source |
|---|---|---|
| Violence | `false` | gameplay/runtime review (scene capture + regression evidence) |
| Blood/Gore | `false` | gameplay/runtime review |
| Sexual Content/Nudity | `false` | gameplay/runtime review |
| Drugs/Alcohol | `false` | gameplay/runtime review |
| Gambling | `false` | gameplay/runtime review |
| User-Generated Content | `false` | gameplay/runtime review |
| Online Interactions Not Rated | `false` | no networked user interaction feature scope |

## Ownership and Review Cadence
- Owner: `@raven-dev-ops`
- Last reviewed: `2026-02-25`
- Next scheduled review: `2026-03-31`

## Reassessment Triggers
- Any release scope expansion into regions requiring formal board ratings.
- Any content update that introduces descriptor-relevant material.
- Steam descriptor/rating policy changes.
- New distribution channel beyond current Steam desktop scope.

## Escalation Rule
- If any trigger is hit, release status changes to **NO-GO** until descriptor/rating review is rerun and the manifest is updated for the target RC.
