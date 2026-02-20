# Steam Achievements and Stats MVP

## Runtime Component
- `Assets/Scripts/Steam/SteamStatsService.cs`

## Event Sources
- Catch events from `SaveManager.CatchRecorded`
- Purchase events from `SaveManager.PurchaseRecorded`
- Trip events from `SaveManager.TripCompleted`

## Stats Keys
- `STAT_TOTAL_CATCHES`
- `STAT_TOTAL_PURCHASES`
- `STAT_TOTAL_TRIPS`
- `STAT_CATCH_VALUE_COPECS`

## Achievement Keys
- `ACH_FIRST_CATCH`
- `ACH_FIRST_PURCHASE`
- `ACH_TRIP_5`

## Alignment Policy (Save vs Steam)
- Canonical source: local save (`SaveDataV1.stats`).
- Steam stats are a mirrored external projection of local canonical values.
- If Steam API is unavailable, gameplay continues; mirror sync retries when Steam is ready.

## Failure Handling
- Steam calls are guarded behind `SteamBootstrap.IsSteamInitialized`.
- Failed `RequestCurrentStats`/`StoreStats` logs warnings only.
- No gameplay state transition depends on Steam stats success.

## Validation
1. Launch via Steam and trigger first catch, first purchase, and trip milestones.
2. Verify achievements unlock in Steam overlay/backend.
3. Verify stat counters match save stats after relaunch.
4. Launch outside Steam and confirm no gameplay blocker occurs.
