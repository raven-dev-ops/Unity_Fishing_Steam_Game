# Unity CI License and Trusted-Context Policy

## Source of Truth
- This document defines Unity license behavior for CI workflows in this repository.
- Write-automation token policy is defined separately in `docs/CI_AUTOMATION_TOKEN_POLICY.md`.
- Related workflows:
  - `.github/workflows/ci-build.yml`
  - `.github/workflows/ci-tests.yml`
  - `.github/workflows/ci-content-validator.yml`
  - `.github/workflows/ci-scene-capture.yml`
  - `.github/workflows/release-steampipe.yml`

## Activation Strategy
- Unity jobs use `UNITY_LICENSE` secret for GameCI build/test actions.
- Unity workflows classify execution context as:
  - `trusted`: protected refs (`github.ref_protected == true`) or `workflow_dispatch`.
  - `untrusted`: all other contexts.
- Policy outcome:
  - missing `UNITY_LICENSE` in any context: warn and skip Unity-dependent steps.
  - if repository variable `UNITY_EXECUTION_ENFORCE=true`, trusted contexts fail when Unity execution is skipped.
  - release build/upload workflow still fails when required release secrets are missing.

## Current Enforcement Posture (2026-02-21)
- Repository variable `UNITY_EXECUTION_ENFORCE` is set to `false`.
- Effect in protected/trusted contexts:
  - Missing/invalid Unity license emits warning and skips Unity-dependent steps.
  - Unity execution is non-blocking until enforcement is re-enabled.
- Re-enable path:
  - Set `UNITY_EXECUTION_ENFORCE=true` once Unity license and trusted-context execution are ready to be hard-required.

## Required Secrets
- Repository secret:
  - `UNITY_LICENSE`: required for Unity build/test/validator/scene-capture/release-build jobs.
- Release environment secrets (`steam-release`) are additionally required for SteamPipe upload workflow.

## Context Matrix
| Context | Trusted? | Expected behavior with `UNITY_LICENSE` present | Expected behavior without `UNITY_LICENSE` |
|---|---|---|---|
| `push` to protected `main` | Yes | Unity jobs run | Unity jobs fail when enforcement is enabled |
| Protected tag push (`v*`) release flow | Yes (release policy) | Release build + upload path runs | Release build fails on missing license |
| Manual dispatch (`workflow_dispatch`) | Yes | Unity jobs run | Unity jobs fail when enforcement is enabled |
| Internal PR (same repo) | Usually No | Unity jobs run if secret is exposed to PR context | Unity jobs warn+skip |
| Fork PR | No | Secrets typically unavailable; Unity jobs generally cannot run | Unity jobs warn+skip |
| Dependabot PR | No | If secrets unavailable, Unity jobs skip | Unity jobs warn+skip |

## Execution Enforcement Toggle
- Repository variable: `UNITY_EXECUTION_ENFORCE`.
- Default behavior (unset/false):
  - Unity workflows warn+skip when `UNITY_LICENSE` is missing.
- Enforcement behavior (`true`):
  - In trusted contexts, workflows fail if Unity execution is skipped.
  - In untrusted contexts, warn+skip behavior remains.

## Why Write-Capable Steps Are Gated
- Unity license and release credentials are sensitive.
- Untrusted contexts can execute attacker-controlled branch content.
- Therefore write-capable or secret-dependent actions are restricted to trusted contexts or explicitly approved release environments.

## Troubleshooting
1. Unity steps skipped due missing license:
   - Check workflow logs for missing `UNITY_LICENSE` warning.
   - Add/update repository `UNITY_LICENSE` to re-enable Unity-dependent steps.
   - If trusted-context failures are expected, confirm `UNITY_EXECUTION_ENFORCE` value.
2. Unity steps skipped in PR:
   - For forks/dependabot, this is expected when secrets are unavailable.
3. Release workflow fails before Steam upload:
   - Confirm `UNITY_LICENSE` exists and release environment secrets are configured.
   - Confirm release was triggered by protected tag or approved manual dispatch.
4. Unexpected trusted/untrusted classification:
   - Verify `github.ref_protected` and event type in workflow run metadata.
5. Unity tests run but check publication is skipped:
   - Confirm `AUTOMATION_WRITE_TOKEN` is configured.
   - See `docs/CI_AUTOMATION_TOKEN_POLICY.md` for required scopes and fallback behavior.
