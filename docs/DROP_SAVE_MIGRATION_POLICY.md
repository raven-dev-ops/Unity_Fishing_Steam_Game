# Save Migration Policy

## Versioning
- Increment `saveVersion` when data shape changes.
- Keep backward-compatible readers for prior known versions.

## Migration Rules
- Migrations must be deterministic and idempotent.
- Never delete unknown fields without fallback handling.

## Testing
- Keep fixture save files for every shipped version.
- Run migration tests on boot for all fixtures.
- Validate post-migration gameplay and economy integrity.
