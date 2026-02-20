# Hotfix Process and Branch Strategy

## Branch Model
- `main`: stable integration branch.
- `release/<version>`: release hardening branch.
- `hotfix/<ticket>`: urgent production patch branch.

## Hotfix Flow
1. Branch from current release tag:
   - `git checkout -b hotfix/<ticket> <release-tag>`
2. Implement minimal, isolated fix.
3. Run smoke + targeted regression checks.
4. Tag patch release (`v1.0.1`, etc).
5. Merge back to `main` and active release branch if applicable.

## Risk Controls
- Keep fixes tightly scoped.
- Preserve save compatibility.
- Document migration and player-impact notes in release notes.
- Track follow-up cleanup tickets for any temporary workaround.
