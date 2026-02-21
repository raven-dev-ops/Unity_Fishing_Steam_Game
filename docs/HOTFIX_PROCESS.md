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
4. Open PR to `main` (PR review + required checks are mandatory).
5. Tag patch release (`v1.0.1`, etc).
6. Merge back to active release branch if applicable.

## Operator Outputs
- Branch creation output references the source release tag.
- Regression run captures test summary and failure triage notes.
- Patch tag is annotated and pushed with release notes delta.
- Audit record includes incident ID, approver, and UTC timestamps for each critical step.

## Emergency Admin Bypass (Exception Path)
- Use only for production-severity incidents where normal PR flow is unavailable.
- Required post-action audit record:
  - incident/ticket id
  - acting admin
  - UTC timestamp
  - bypass rationale
  - linked follow-up PR restoring policy-compliant state
- Publish audit note in internal ops log within 24h.

## Risk Controls
- Keep fixes tightly scoped.
- Preserve save compatibility.
- Document migration and player-impact notes in release notes.
- Track follow-up cleanup tickets for any temporary workaround.
