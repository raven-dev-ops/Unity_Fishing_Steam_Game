# Build Profiles

The Windows build entrypoint supports explicit runtime profiles:

- `Dev`
- `QA`
- `Release`

## Selection
- Unity batch arg: `-buildProfile=Dev|QA|Release`
- Project wrapper: `scripts/unity-cli.ps1 -Task build -BuildProfile QA`

If omitted, editor menu builds default to `Dev`.

## Profile Matrix
| Profile | Build options | Defines | Diagnostics expectation | Phase-two audio fallback policy |
|---|---|---|---|---|
| `Dev` | `Development`, `AllowDebugging`, `ConnectWithProfiler` | `RAVEN_BUILD_PROFILE_DEV` | Full diagnostics enabled for local iteration | Synthetic fallback generation allowed for missing required keys |
| `QA` | `Development`, `ConnectWithProfiler` | `RAVEN_BUILD_PROFILE_QA` | Diagnostics enabled for test runs and triage | Synthetic fallback generation allowed for missing required keys |
| `Release` | none (`BuildOptions.None`) | `RAVEN_BUILD_PROFILE_RELEASE` | Dev overlays/debug surfaces disabled by default | Synthetic fallback generation disabled; release build validates required keys and fails on missing/non-release-qualified clips |

## Notes
- Profile define symbols are set during build and restored after build completion.
- `Release` profile also reduces structured runtime log verbosity by default.
- `Release` profile invokes `ReleaseAudioContentValidator` from `BuildCommandLine` before build.
- CI Windows build workflow uses `QA` profile.
