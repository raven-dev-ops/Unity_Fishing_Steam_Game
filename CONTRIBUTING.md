# Contributing

## Branching Model
- `main`: protected integration branch.
- Feature branches: `feature/<short-name>`.
- Fix branches: `fix/<short-name>`.
- Docs branches: `docs/<short-name>`.

## Pull Request Rules
1. Link issue(s) in PR description (`Closes #<id>`).
2. Complete the PR validation checklist.
3. Keep PR scope focused and reviewable.
4. Update docs with behavior/pipeline changes.

## Required Validation
- Run content validation (`Raven > Validate Content Catalog` or batch mode).
- Run affected smoke scenarios from `docs/QA_SMOKE_TEST.md`.
- Run build command path for build/pipeline changes.
- Add or update tests for behavior changes when test harness exists.

## Code Ownership and Reviews
- CODEOWNERS applies required review for critical paths:
  - `Assets/Scripts/Core/`
  - `Assets/Scripts/Save/`
  - `Assets/Scripts/Fishing/`
  - `.github/workflows/`
  - `docs/`

## Issue Filing
- Use issue templates in `.github/ISSUE_TEMPLATE/`.
- Use `Bug Report` for defects.
- Use `Content Drop` for fish/ship/hook or data-only additions.

## Commit Guidelines
- Use concise, descriptive commit messages.
- Keep one logical change per commit when possible.
- Include issue references in commit body when helpful.

## Security and Secrets
- Never commit credentials, tokens, or key files.
- CI secrets must be stored in GitHub Actions secrets/environments.
- Follow `docs/SECURITY_RELEASE_WORKFLOW.md` for release-related secrets and incident response.
