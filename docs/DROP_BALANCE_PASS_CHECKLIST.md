# Balance Pass Checklist and Validator Gates

## Balance Pass
- Review fish base values by rarity and depth.
- Review ship/hook price curves.
- Validate progression pacing for first 30 minutes.
- Distance-tier sell multiplier policy:
  - Tier 1 = `1.00x` baseline
  - Formula = `1 + (tier - 1) * distanceTierStep`
  - Tier values below 1 are treated as tier 1.

## Validator Gates
- No duplicate IDs.
- No invalid depth/distance ranges.
- No missing icon references in content definitions.

## Sign-off
- Regression smoke test complete.
- Save/load continuity confirmed after balance update.
