using System;
using System.Collections.Generic;
using RavenDevOps.Fishing.Core;
using UnityEngine;

namespace RavenDevOps.Fishing.Fishing
{
    [DisallowMultipleComponent]
    public sealed class FishingDepthBackdropFishController : MonoBehaviour
    {
        private sealed class BackdropFishTrack
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public int LayerIndex;
            public float Speed;
            public float Direction;
            public float BobAmplitude;
            public float BobFrequency;
            public float Phase;
            public float BaseY;
        }

        [SerializeField] private Camera _targetCamera;
        [SerializeField] private Transform _ship;
        [SerializeField] private Transform _hook;
        [SerializeField] private HookMovementController _hookMovement;
        [SerializeField] private bool _allowInBatchMode = false;
        [SerializeField] private bool _searchInactive = true;
        [SerializeField] private string _fishNameToken = "FishingFish";
        [SerializeField] private int _totalBackdropFish = 6;
        [SerializeField] private float _overlayDepthFromCamera = 10f;
        [SerializeField] private int _baseSortingOrder = -30;
        [SerializeField] private float _horizontalPadding = 2.6f;
        [SerializeField] private float _minimumDepthMeters = 30f;
        [SerializeField] private Vector2 _swimSpeedRange = new Vector2(0.08f, 0.32f);
        [SerializeField] private Vector2 _scaleRange = new Vector2(0.45f, 0.95f);
        [SerializeField] private float _depthParallaxScale = 1.35f;
        [SerializeField] private float _shipTravelParallax = 0.45f;
        [SerializeField] private Vector3 _layerAlpha = new Vector3(0.6f, 0.35f, 0.12f);

        private readonly List<Sprite> _spriteLibrary = new List<Sprite>(16);
        private readonly List<BackdropFishTrack> _tracks = new List<BackdropFishTrack>(16);
        private GameObject _visualRoot;
        private bool _initialized;
        private bool _hasLastShipX;
        private float _lastShipX;
        private float _shipDeltaX;

        public void Configure(Camera targetCamera, Transform ship, Transform hook)
        {
            _targetCamera = targetCamera;
            _ship = ship;
            _hook = hook;
            _hookMovement = hook != null ? hook.GetComponent<HookMovementController>() : _hookMovement;
            _initialized = false;
            TryInitialize();
        }

        public void SetTotalBackdropFish(int totalFish)
        {
            var clamped = Mathf.Clamp(totalFish, 3, 24);
            if (_totalBackdropFish == clamped && _initialized)
            {
                return;
            }

            _totalBackdropFish = clamped;
            _initialized = false;
            RebuildTracks();
            TryInitialize();
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _hookMovement, this, warnIfMissing: false);
            TryInitialize();
        }

        private void OnEnable()
        {
            if (Application.isBatchMode && !_allowInBatchMode)
            {
                enabled = false;
                return;
            }

            TryInitialize();
        }

        private void LateUpdate()
        {
            TryInitialize();
            if (!_initialized || _tracks.Count == 0)
            {
                return;
            }

            AnchorToCamera();
            UpdateShipDeltaX();
            TickTracks();
        }

        private void OnDestroy()
        {
            if (_visualRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_visualRoot);
            }
            else
            {
                DestroyImmediate(_visualRoot);
            }
        }

        private void TryInitialize()
        {
            ResolveReferences();
            if (_targetCamera == null)
            {
                return;
            }

            if (_initialized)
            {
                AnchorToCamera();
                return;
            }

            RebuildSpriteLibrary();
            if (_spriteLibrary.Count == 0)
            {
                return;
            }

            EnsureVisualRoot();
            BuildTracks();
            _initialized = _tracks.Count > 0;
        }

        private void ResolveReferences()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_targetCamera == null)
            {
                _targetCamera = FindAnyObjectByType<Camera>(FindObjectsInactive.Exclude);
            }

            if (_hookMovement == null)
            {
                RuntimeServiceRegistry.Resolve(ref _hookMovement, this, warnIfMissing: false);
            }

            if (_hook == null && _hookMovement != null)
            {
                _hook = _hookMovement.transform;
            }

            if (_ship == null && _hookMovement != null)
            {
                _ship = _hookMovement.ShipTransform;
            }
        }

        private void RebuildSpriteLibrary()
        {
            _spriteLibrary.Clear();
            var seen = new HashSet<Sprite>();
            var renderers = FindObjectsByType<SpriteRenderer>(
                _searchInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || renderer.sprite == null || renderer.gameObject.scene != gameObject.scene)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(_fishNameToken)
                    && renderer.gameObject.name.IndexOf(_fishNameToken, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (seen.Add(renderer.sprite))
                {
                    _spriteLibrary.Add(renderer.sprite);
                }
            }
        }

        private void EnsureVisualRoot()
        {
            if (_visualRoot == null)
            {
                _visualRoot = new GameObject("FishingDepthBackdropFishRoot");
            }

            if (_targetCamera != null && _visualRoot.transform.parent != _targetCamera.transform)
            {
                _visualRoot.transform.SetParent(_targetCamera.transform, worldPositionStays: false);
            }

            _visualRoot.transform.localPosition = new Vector3(0f, 0f, Mathf.Abs(_overlayDepthFromCamera));
            _visualRoot.transform.localRotation = Quaternion.identity;
        }

        private void BuildTracks()
        {
            RebuildTracks();
            if (_visualRoot == null)
            {
                return;
            }

            var layerCount = 3;
            var layerCounts = BuildLayerCounts(Mathf.Clamp(_totalBackdropFish, 3, 24), layerCount);
            for (var layer = 0; layer < layerCount; layer++)
            {
                for (var i = 0; i < layerCounts[layer]; i++)
                {
                    var go = new GameObject($"DepthBackdropFish_L{layer}_{i}");
                    go.transform.SetParent(_visualRoot.transform, worldPositionStays: false);
                    var renderer = go.AddComponent<SpriteRenderer>();
                    renderer.sprite = _spriteLibrary[UnityEngine.Random.Range(0, _spriteLibrary.Count)];
                    renderer.sortingOrder = _baseSortingOrder + layer;
                    renderer.flipX = UnityEngine.Random.value > 0.5f;
                    var alpha = ResolveLayerAlpha(layer);
                    renderer.color = new Color(0.9f, 0.96f, 1f, alpha);
                    var scale = UnityEngine.Random.Range(_scaleRange.x, _scaleRange.y);
                    go.transform.localScale = new Vector3(scale, scale, 1f);

                    var track = new BackdropFishTrack
                    {
                        Transform = go.transform,
                        Renderer = renderer,
                        LayerIndex = layer,
                        Speed = UnityEngine.Random.Range(_swimSpeedRange.x, _swimSpeedRange.y) * (1f - (layer * 0.18f)),
                        Direction = UnityEngine.Random.value > 0.5f ? 1f : -1f,
                        BobAmplitude = UnityEngine.Random.Range(0.02f, 0.11f) * (1f + (layer * 0.22f)),
                        BobFrequency = UnityEngine.Random.Range(0.16f, 0.5f),
                        Phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                        BaseY = ResolveLayerBaseY(layer)
                    };

                    track.Transform.localPosition = new Vector3(
                        UnityEngine.Random.Range(-8f, 8f),
                        track.BaseY,
                        0f);
                    track.Renderer.flipX = track.Direction < 0f;
                    _tracks.Add(track);
                }
            }
        }

        private void AnchorToCamera()
        {
            if (_targetCamera == null || _visualRoot == null)
            {
                return;
            }

            if (_visualRoot.transform.parent != _targetCamera.transform)
            {
                _visualRoot.transform.SetParent(_targetCamera.transform, worldPositionStays: false);
            }

            _visualRoot.transform.localPosition = new Vector3(0f, 0f, Mathf.Abs(_overlayDepthFromCamera));
            _visualRoot.transform.localRotation = Quaternion.identity;
        }

        private void TickTracks()
        {
            ResolveViewportHalfSize(out var halfWidth, out var halfHeight);
            var wrapPadding = Mathf.Max(1f, _horizontalPadding);
            var minimumDepthLocalY = ResolveMinimumDepthLocalY();

            for (var i = 0; i < _tracks.Count; i++)
            {
                var track = _tracks[i];
                if (track == null || track.Transform == null || track.Renderer == null)
                {
                    continue;
                }

                track.Renderer.enabled = true;

                var p = track.Transform.localPosition;
                p.x -= _shipDeltaX * Mathf.Max(0f, _shipTravelParallax * (1f - (track.LayerIndex * 0.14f)));
                p.x += track.Speed * track.Direction * Time.unscaledDeltaTime;
                var bob = Mathf.Sin((Time.unscaledTime * track.BobFrequency) + track.Phase) * track.BobAmplitude;
                p.y = track.BaseY + bob;

                // Camera depth can shift rapidly between demo scenes; re-seed fish that are now fully outside view.
                var verticalReseedPadding = 1.25f;
                if (p.y < -(halfHeight + verticalReseedPadding) || p.y > halfHeight + verticalReseedPadding)
                {
                    track.BaseY = ResolveLayerBaseY(track.LayerIndex);
                    p.y = track.BaseY;
                }

                var wrapped = false;
                if (p.x > halfWidth + wrapPadding)
                {
                    p.x = -(halfWidth + wrapPadding);
                    wrapped = true;
                }
                else if (p.x < -(halfWidth + wrapPadding))
                {
                    p.x = halfWidth + wrapPadding;
                    wrapped = true;
                }

                if (wrapped)
                {
                    track.BaseY = ResolveLayerBaseY(track.LayerIndex);
                    track.Speed = Mathf.Max(0.02f, UnityEngine.Random.Range(_swimSpeedRange.x, _swimSpeedRange.y) * (1f - (track.LayerIndex * 0.18f)));
                    track.Direction = UnityEngine.Random.value > 0.5f ? 1f : -1f;
                    track.Renderer.flipX = track.Direction < 0f;
                    if (_spriteLibrary.Count > 0)
                    {
                        track.Renderer.sprite = _spriteLibrary[UnityEngine.Random.Range(0, _spriteLibrary.Count)];
                    }

                    p.y = track.BaseY;
                }

                p.y = Mathf.Min(p.y, minimumDepthLocalY - (track.LayerIndex * 0.12f));
                track.Transform.localPosition = p;
            }
        }

        private float ResolveLayerBaseY(int layerIndex)
        {
            ResolveViewportHalfSize(out _, out var halfHeight);
            var clampedLayer = Mathf.Clamp(layerIndex, 0, 2);
            var layerRatio = (clampedLayer + 1f) / 4f;
            var top = halfHeight * 0.12f;
            var bottom = -halfHeight * 0.86f;
            var y = Mathf.Lerp(top, bottom, layerRatio);
            var depthMeters = ResolveCurrentDepthMeters();
            var depthRatio = Mathf.Clamp01(depthMeters / 5000f);
            y -= depthRatio * Mathf.Max(0f, _depthParallaxScale) * (0.4f + (clampedLayer * 0.35f));
            var minimumDepthLocalY = ResolveMinimumDepthLocalY();
            y = Mathf.Min(y, minimumDepthLocalY - (clampedLayer * 0.12f));
            y += UnityEngine.Random.Range(-0.35f, 0.35f);
            return y;
        }

        private float ResolveCurrentDepthMeters()
        {
            if (_hookMovement != null)
            {
                return Mathf.Max(0f, _hookMovement.CurrentDepth);
            }

            if (_ship != null && _hook != null)
            {
                return Mathf.Max(0f, _ship.position.y - _hook.position.y);
            }

            return 0f;
        }

        private float ResolveMinimumDepthLocalY()
        {
            if (_ship == null || _targetCamera == null)
            {
                ResolveViewportHalfSize(out _, out var fallbackHalfHeight);
                return -(fallbackHalfHeight * 0.55f);
            }

            var minDepthWorldY = _ship.position.y - Mathf.Max(0f, _minimumDepthMeters);
            return minDepthWorldY - _targetCamera.transform.position.y;
        }

        private void ResolveViewportHalfSize(out float halfWidth, out float halfHeight)
        {
            if (_targetCamera != null && _targetCamera.orthographic)
            {
                halfHeight = Mathf.Max(0.5f, _targetCamera.orthographicSize);
                halfWidth = Mathf.Max(0.5f, halfHeight * Mathf.Max(0.1f, _targetCamera.aspect));
                return;
            }

            halfWidth = 10f;
            halfHeight = 6f;
        }

        private float ResolveLayerAlpha(int layerIndex)
        {
            switch (Mathf.Clamp(layerIndex, 0, 2))
            {
                case 0:
                    return Mathf.Clamp01(_layerAlpha.x);
                case 1:
                    return Mathf.Clamp01(_layerAlpha.y);
                default:
                    return Mathf.Clamp01(_layerAlpha.z);
            }
        }

        private void RebuildTracks()
        {
            _tracks.Clear();
            if (_visualRoot == null)
            {
                return;
            }

            for (var i = _visualRoot.transform.childCount - 1; i >= 0; i--)
            {
                var child = _visualRoot.transform.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private int[] BuildLayerCounts(int totalFish, int layerCount)
        {
            var counts = new int[layerCount];
            var remaining = Mathf.Max(layerCount, totalFish);
            for (var i = 0; i < layerCount; i++)
            {
                counts[i] = 1;
            }

            remaining -= layerCount;
            while (remaining > 0)
            {
                var index = UnityEngine.Random.Range(0, layerCount);
                counts[index]++;
                remaining--;
            }

            return counts;
        }

        private void UpdateShipDeltaX()
        {
            _shipDeltaX = 0f;
            if (_ship == null)
            {
                _hasLastShipX = false;
                return;
            }

            var shipX = _ship.position.x;
            if (!_hasLastShipX)
            {
                _hasLastShipX = true;
                _lastShipX = shipX;
                return;
            }

            _shipDeltaX = shipX - _lastShipX;
            _lastShipX = shipX;
        }
    }
}
