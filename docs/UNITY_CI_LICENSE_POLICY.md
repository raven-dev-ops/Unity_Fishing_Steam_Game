# Unity CI License and Trusted-Context Policy

## Source of Truth
- This document defines Unity license behavior for CI workflows in this repository.
- Write-automation token policy is defined separately in `docs/CI_AUTOMATION_TOKEN_POLICY.md`.
- Related workflows:
  - `.github/workflows/ci-build.yml`
  - `.github/workflows/ci-tests.yml`
  - `.github/workflows/ci-content-validator.yml`
  - `.github/workflows/ci-scene-capture.yml`
  - `.github/workflows/nightly-full-regression.yml`
  - `.github/workflows/release-steampipe.yml`

## Activation Strategy
- Unity jobs use `UNITY_LICENSE` secret for GameCI build/test actions.
- Unity workflows classify execution context as:
  - `trusted`: protected refs (`github.ref_protected == true`), `workflow_dispatch`, and nightly `schedule` triggers.
  - `untrusted`: all other contexts.
- Policy outcome:
  - trusted contexts: Unity execution is required; missing/invalid `UNITY_LICENSE` fails the workflow.
  - untrusted contexts: workflows warn and skip Unity-dependent steps.
  - release workflow additionally enforces RC validation bundle evidence and required Steam release secrets.

## Current Enforcement Posture (2026-02-24)
- Trusted-context Unity execution is enforced by workflow logic (not a repository toggle) across:
  - `ci-build`
  - `ci-tests`
  - `ci-content-validator`
  - `ci-scene-capture`
  - `nightly-full-regression`
  - `release-steampipe`
- Defer/skip behavior is scoped to untrusted contexts only.

## Required Secrets
- Repository secret:
  - `UNITY_LICENSE`: required for Unity build/test/validator/scene-capture/release-build jobs.
- Release environment secrets (`steam-release`) are additionally required for SteamPipe upload workflow.

## Context Matrix
| Context | Trusted? | Expected behavior with `UNITY_LICENSE` present | Expected behavior without `UNITY_LICENSE` |
|---|---|---|---|
| `push` to protected `main` | Yes | Unity jobs run | Unity jobs fail |
| Protected tag push (`v*`) release flow | Yes (release policy) | Release build + upload path runs | Release build fails on missing license |
| Manual dispatch (`workflow_dispatch`) | Yes | Unity jobs run | Unity jobs fail |
| Nightly schedule (`schedule`) | Yes | Nightly Unity jobs run | Nightly Unity jobs fail |
| Internal PR (same repo) | Usually No | Unity jobs run if secret is exposed to PR context | Unity jobs warn+skip |
| Fork PR | No | Secrets typically unavailable; Unity jobs generally cannot run | Unity jobs warn+skip |
| Dependabot PR | No | If secrets unavailable, Unity jobs skip | Unity jobs warn+skip |

## Execution Enforcement Rule
- There is no trusted-context runtime toggle for Unity execution enforcement.
- Any trusted-context relaxation requires workflow changes and an explicit approved waiver.
- `UNITY_LICENSE` remains optional only for untrusted contexts.

## Why Write-Capable Steps Are Gated
- Unity license and release credentials are sensitive.
- Untrusted contexts can execute attacker-controlled branch content.
- Therefore write-capable or secret-dependent actions are restricted to trusted contexts or explicitly approved release environments.

## Troubleshooting
1. Unity steps skipped due missing license:
   - In trusted contexts this is a workflow failure by design.
   - Check workflow logs for missing/invalid `UNITY_LICENSE` diagnostics.
   - Add/update repository `UNITY_LICENSE` to re-enable Unity-dependent steps.
2. Unity steps skipped in PR:
   - For forks/dependabot, this is expected when secrets are unavailable.
3. Release workflow fails before Steam upload:
   - Confirm `UNITY_LICENSE` exists and release environment secrets are configured.
   - Confirm `RC validation bundle gate` reports success for required workflows.
   - Confirm release was triggered by protected tag or approved manual dispatch.
4. Unexpected trusted/untrusted classification:
   - Verify `github.ref_protected` and event type in workflow run metadata.
5. Unity tests run but check publication is skipped:
   - Confirm `AUTOMATION_WRITE_TOKEN` is configured.
   - See `docs/CI_AUTOMATION_TOKEN_POLICY.md` for required scopes and fallback behavior.
