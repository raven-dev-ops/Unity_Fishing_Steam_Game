using UnityEngine;

namespace RavenDevOps.Fishing.Core
{
    [DisallowMultipleComponent]
    public sealed class CinematicSceneContract : MonoBehaviour
    {
        [SerializeField] private GameObject _backdropFar;
        [SerializeField] private GameObject _backdropVeil;

        public GameObject BackdropFar => _backdropFar;
        public GameObject BackdropVeil => _backdropVeil;
    }

    [DisallowMultipleComponent]
    public sealed class HarborSceneContract : MonoBehaviour
    {
        [SerializeField] private GameObject _harborShipMain;
        [SerializeField] private GameObject _dockPlankZero;

        public GameObject HarborShipMain => _harborShipMain;
        public GameObject DockPlankZero => _dockPlankZero;
    }

    [DisallowMultipleComponent]
    public sealed class FishingSceneContract : MonoBehaviour
    {
        [SerializeField] private GameObject _fishingShip;
        [SerializeField] private GameObject _fishingHook;
        [SerializeField] private GameObject _fishingLine;
        [SerializeField] private GameObject _fishingDynamicLine;
        [SerializeField] private GameObject _backdropFar;
        [SerializeField] private GameObject _backdropVeil;

        public GameObject FishingShip => _fishingShip;
        public GameObject FishingHook => _fishingHook;
        public GameObject FishingLine => _fishingLine;
        public GameObject FishingDynamicLine => _fishingDynamicLine;
        public GameObject BackdropFar => _backdropFar;
        public GameObject BackdropVeil => _backdropVeil;
    }
}
