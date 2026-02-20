using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Save;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class HookMovementController : MonoBehaviour
    {
        [SerializeField] private Transform _shipTransform;
        [SerializeField] private float _verticalSpeed = 4f;
        [SerializeField] private Vector2 _depthBounds = new Vector2(-8f, -1f);
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private CatalogService _catalogService;
        [SerializeField] private float _speedMultiplier = 1f;

        public float MaxDepth
        {
            get => Mathf.Abs(_depthBounds.x);
            set => _depthBounds.x = -Mathf.Abs(value);
        }

        public float CurrentDepth => Mathf.Abs(transform.position.y);

        private void Awake()
        {
            RuntimeServiceRegistry.Register(this);
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
            RefreshHookStats();
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }

        public void RefreshHookStats()
        {
            if (_saveManager == null || _catalogService == null)
            {
                return;
            }

            var equippedId = _saveManager.Current.equippedHookId;
            if (_catalogService.TryGetHook(equippedId, out var hookDefinition))
            {
                MaxDepth = hookDefinition.maxDepth;
            }
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0.1f, multiplier);
        }

        private void LateUpdate()
        {
            if (_shipTransform != null)
            {
                var p = transform.position;
                p.x = _shipTransform.position.x;
                transform.position = p;
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var axis = 0f;
            if (keyboard.downArrowKey.isPressed) axis -= 1f;
            if (keyboard.upArrowKey.isPressed) axis += 1f;

            var p = transform.position;
            p.y += axis * _verticalSpeed * _speedMultiplier * Time.deltaTime;
            p.y = Mathf.Clamp(p.y, Mathf.Min(_depthBounds.x, _depthBounds.y), Mathf.Max(_depthBounds.x, _depthBounds.y));
            transform.position = p;
        }
    }
}
