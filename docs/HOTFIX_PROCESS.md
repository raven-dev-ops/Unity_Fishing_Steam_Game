# Hotfix Process and Branch Strategy

## Branch Model
- `main`: stable integration branch.
- `release/<version>`: release hardening.
- `hotfix/<ticket>`: urgent production patch.

## Hotfix Flow
1. Branch from current release tag.
2. Implement minimal fix.
3. Validate smoke + regression set.
4. Tag patch release (`v1.0.1`, etc).
5. Merge back to `main`.

## Risk Controls
- Keep fixes tightly scoped.
- Preserve save compatibility.
- Document migration impacts in release notes.
