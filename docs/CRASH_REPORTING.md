# Crash Reporting and Privacy

## Decision
- MVP path uses local crash diagnostics artifacts (no third-party telemetry upload).
- This satisfies supportability and privacy baseline without external data processors.

## Runtime Component
- `Assets/Scripts/Core/CrashDiagnosticsService.cs`

## Artifacts
- Crash/error artifact file:
  - `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/last_crash_report.json`
- Crash/error history artifacts (rolling retention):
  - `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/crash_report_yyyyMMdd_HHmmss_fff.json`
- Unity player log:
  - `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/Player.log`
- Structured runtime log (dev/debug oriented):
  - `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/raven_runtime.log`

## Captured Fields
- UTC timestamp
- log type
- message
- stack trace
- unity version
- runtime platform
- event category (`exception`, `assert`, `error`)
- session id
- active scene path
- frame count
- time scale

## Retention Policy
- `last_crash_report.json` is always updated to the latest captured event.
- History files are retained with rolling cap (`CrashDiagnosticsService._maxArtifactHistory`, default `10`).
- Oldest history files are pruned automatically when cap is exceeded.

## Privacy Disclosure
- No automatic network upload is performed by crash diagnostics.
- Crash artifact and logs remain on local disk until the player shares them manually for support.
- No personal profile data is transmitted by this feature.

## Validation
1. Trigger a controlled exception in dev build.
2. Confirm a new timestamped `crash_report_*.json` file is written.
3. Confirm `last_crash_report.json` updates.
4. Confirm old history files are pruned once retention cap is exceeded.
5. Confirm support docs point to correct log/artifact paths.
