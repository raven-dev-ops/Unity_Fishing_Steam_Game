# Crash Reporting and Privacy

## Decision
- MVP path uses local crash diagnostics artifacts (no third-party telemetry upload).
- This satisfies supportability and privacy baseline without external data processors.

## Runtime Component
- `Assets/Scripts/Core/CrashDiagnosticsService.cs`

## Artifacts
- Crash/error artifact file:
  - `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/last_crash_report.json`
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

## Privacy Disclosure
- No automatic network upload is performed by crash diagnostics.
- Crash artifact and logs remain on local disk until the player shares them manually for support.
- No personal profile data is transmitted by this feature.

## Validation
1. Trigger a controlled exception in dev build.
2. Confirm `last_crash_report.json` updates.
3. Confirm support docs point to correct log/artifact paths.
