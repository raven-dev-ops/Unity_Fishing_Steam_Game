# Performance Sanity Checklist

## Test Cases
- Harbor idle for 2 minutes.
- Fishing loop repeated cast/hook/catch for 5+ minutes.
- Rapid menu open/close while changing focus.

## Tooling
- Runtime FPS sampling: `Assets/Scripts/Performance/PerfSanityRunner.cs`.
- Log sample every configured frame window.

## Metrics
- Stable average FPS samples across each test case.
- No runaway frame-time spikes from UI update loops.
- No abnormal memory growth over 10+ minutes.
- Fish roll hot path should show no avoidable per-roll managed allocations in steady state.

## Failure Logging
Capture scene, timestamp, action sequence, and relevant Console output.
