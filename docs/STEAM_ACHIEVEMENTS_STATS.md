# Steam Achievements and Stats MVP

## Runtime Component
- `Assets/Scripts/Steam/SteamStatsService.cs`

## Event Sources
- Catch events from `SaveManager.CatchRecorded`
- Purchase events from `SaveManager.PurchaseRecorded`
- Trip events from `SaveManager.TripCompleted`

## Stats Keys
| Key | Type | Runtime Field | Save Source |
|---|---|---|---|
| `STAT_TOTAL_CATCHES` | `INT` | `_statTotalCatches` | `SaveDataV1.stats.totalFishCaught` |
| `STAT_TOTAL_PURCHASES` | `INT` | `_statTotalPurchases` | `SaveDataV1.stats.totalPurchases` |
| `STAT_TOTAL_TRIPS` | `INT` | `_statTotalTrips` | `SaveDataV1.stats.totalTrips` |
| `STAT_CATCH_VALUE_COPECS` | `INT` | `_statCatchValueCopecs` | `SaveDataV1.stats.totalCatchValueCopecs` |

## Achievement Keys
| Key | Runtime Field | Unlock Condition |
|---|---|---|
| `ACH_FIRST_CATCH` | `_achievementFirstCatch` | `totalFishCaught > 0` |
| `ACH_FIRST_PURCHASE` | `_achievementFirstPurchase` | `totalPurchases > 0` |
| `ACH_TRIP_5` | `_achievementTripMilestone` | `totalTrips >= _tripMilestoneTarget` |

## Backend Contract
- Versioned backend contract file:
  - `release/steamworks/achievements_stats/backend_contract.json`
- Contract verifier:
  - `scripts/ci/verify-steamworks-achievements-stats.ps1`
- Contract checks:
  - runtime key parity (stats + achievements)
  - stat type contract (`INT`)
  - release app/depot mapping secret references (`STEAM_APP_ID`, `STEAM_DEPOT_WINDOWS_ID`)
  - publish metadata completeness for backend publish evidence

## Alignment Policy (Save vs Steam)
- Canonical source: local save (`SaveDataV1.stats`).
- Steam stats are a mirrored external projection of local canonical values.
- If Steam API is unavailable, gameplay continues; mirror sync retries when Steam is ready.

## Failure Handling
- Steam calls are guarded behind `SteamBootstrap.IsSteamInitialized`.
- `RequestCurrentStats` retries on a cooldown (`_requestStatsRetryIntervalSeconds`, default `5s`) to avoid frame-level request spam.
- `StoreStats` writes are gated by a minimum interval (`_storeStatsMinimumIntervalSeconds`, default `15s`).
- Failed writes apply exponential backoff (`2s` initial, `2.0x` multiplier, `60s` cap by default) while keeping sync pending.
- Callback failures from `UserStatsStored_t` requeue sync and enter backoff.
- Service diagnostics include attempts, successes, failures, throttled writes, pending flag, and next-attempt timing.
- No gameplay state transition depends on Steam stats success.

## Validation
1. Verify contract parity before Steamworks publish:
   - `powershell -ExecutionPolicy Bypass -File scripts/ci/verify-steamworks-achievements-stats.ps1`
2. Launch via Steam and trigger first catch, first purchase, and trip milestones.
3. Verify achievements unlock in Steam overlay/backend.
4. Verify stat counters match save stats after relaunch.
5. Launch outside Steam and confirm no gameplay blocker occurs.
6. Run automated cadence/backoff coverage:
   - `./scripts/unity-cli.ps1 -Task test-edit -LogFile issue-225-steam-stats-policy-editmode.log -ExtraArgs @('-testFilter','SteamStoreStatsSyncPolicyTests')`
7. Confirm non-Steam fallback PlayMode regression still passes:
   - `./scripts/unity-cli.ps1 -Task test-play -LogFile issue-225-launch-regression-playmode.log -ExtraArgs @('-testFilter','LaunchPathRegressionPlayModeTests')`
8. After Steamworks backend publish, update `backend_contract.json` publish metadata and run strict validation:
   - `powershell -ExecutionPolicy Bypass -File scripts/ci/verify-steamworks-achievements-stats.ps1 -RequirePublishedMetadata`
9. Current repository verification report (this pass):
   - `docs/STEAMWORKS_ACHIEVEMENTS_STATS_VERIFICATION_REPORT_2026-02-25.md`
