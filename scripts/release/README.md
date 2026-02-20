# Release Scripts

## SteamPipe Upload

Script: `scripts/release/steam_upload.sh`

Required environment variables:
- `STEAM_APP_ID`
- `STEAM_DEPOT_WINDOWS_ID`
- `STEAM_USERNAME`
- `STEAM_CONFIG_VDF` (either raw VDF contents or path to a local file)

Optional environment variables:
- `STEAMCMD_PATH` (default: `steamcmd`)
- `STEAM_BUILD_OUTPUT` (default: `Builds/Windows`)
- `STEAM_BUILD_LOGS` (default: `Builds/SteamPipeLogs`)
- `STEAM_BETA_BRANCH` (default: `beta`)
- `DRY_RUN` (`true` or `false`)

Example dry run:

```bash
DRY_RUN=true \
STEAM_APP_ID=123456 \
STEAM_DEPOT_WINDOWS_ID=123457 \
STEAM_USERNAME=build-bot \
STEAM_CONFIG_VDF=/tmp/config.vdf \
scripts/release/steam_upload.sh v0.1.0-rc1
```
