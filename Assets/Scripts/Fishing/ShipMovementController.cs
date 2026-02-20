using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Save;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class ShipMovementController : MonoBehaviour
    {
        [SerializeField] private float _fallbackSpeed = 6f;
        [SerializeField] private Vector2 _xBounds = new Vector2(-9f, 9f);
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private CatalogService _catalogService;
        [SerializeField] private float _speedMultiplier = 1f;

        public int DistanceTierCap { get; private set; } = 1;

        private void Awake()
        {
            RuntimeServiceRegistry.Register(this);
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
            RefreshShipStats();
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }

        public void RefreshShipStats()
        {
            DistanceTierCap = 1;
            if (_saveManager == null || _catalogService == null)
            {
                return;
            }

            var equippedId = _saveManager.Current.equippedShipId;
            if (_catalogService.TryGetShip(equippedId, out var shipDefinition))
            {
                DistanceTierCap = Mathf.Max(1, shipDefinition.maxDistanceTier);
            }
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0.1f, multiplier);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var speed = ResolveMoveSpeed();
            var axis = 0f;
            if (keyboard.leftArrowKey.isPressed) axis -= 1f;
            if (keyboard.rightArrowKey.isPressed) axis += 1f;

            var p = transform.position;
            p.x += axis * speed * _speedMultiplier * Time.deltaTime;
            p.x = Mathf.Clamp(p.x, Mathf.Min(_xBounds.x, _xBounds.y), Mathf.Max(_xBounds.x, _xBounds.y));
            transform.position = p;
        }

        private float ResolveMoveSpeed()
        {
            if (_saveManager == null || _catalogService == null)
            {
                return _fallbackSpeed;
            }

            var equippedId = _saveManager.Current.equippedShipId;
            if (_catalogService.TryGetShip(equippedId, out var shipDefinition))
            {
                return Mathf.Max(0.1f, shipDefinition.moveSpeed);
            }

            return _fallbackSpeed;
        }
    }
}
