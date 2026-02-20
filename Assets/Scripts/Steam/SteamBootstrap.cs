using UnityEngine;

namespace RavenDevOps.Fishing.Steam
{
    public sealed class SteamBootstrap : MonoBehaviour
    {
        [SerializeField] private uint _steamAppId = 480;

        private void Awake()
        {
#if STEAMWORKS_NET
            // Steamworks.NET initialization hook should run here when package is installed.
            Debug.Log($"SteamBootstrap: STEAMWORKS_NET enabled, AppId={_steamAppId}");
#else
            Debug.Log("SteamBootstrap: STEAMWORKS_NET not defined. Running in non-Steam fallback mode.");
#endif
        }
    }
}
