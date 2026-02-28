using System;
using System.Collections.Generic;
using RavenDevOps.Fishing.Core;
using UnityEngine;

namespace RavenDevOps.Fishing.Fishing
{
    [DisallowMultipleComponent]
    public sealed class FishingDepthBackdropFishController : MonoBehaviour
    {
        private const string TutorialSpriteLibraryResourcePath = "Pilot/Tutorial/SO_TutorialSpriteLibrary";

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
            public float TargetBaseY;
            public float VerticalDriftLerpSpeed;
            public float TravelVerticalOffsetPerShipMeter;
            public float RetargetDelaySeconds;
            public float SpawnDelaySeconds;
            public bool PendingSpawn;
        }

        [SerializeField] private Camera _targetCamera;
        [SerializeField] private Transform _ship;
        [SerializeField] private Transform _hook;
        [SerializeField] private HookMovementController _hookMovement;
        [SerializeField] private bool _allowInBatchMode = false;
        [SerializeField] private bool _searchInactive = true;
        [SerializeField] private string _fishNameToken = "FishingFish";
        [SerializeField] private bool _allowGenericFishNameFallback = true;
        [SerializeField] private Sprite _fallbackFishSprite;
        [SerializeField] private int _totalBackdropFish = 6;
        [SerializeField] private float _overlayDepthFromCamera = 10f;
        [SerializeField] private int _baseSortingOrder = -30;
        [SerializeField] private float _horizontalPadding = 2.6f;
        [SerializeField] private float _minimumDepthMeters = 30f;
        [SerializeField] private bool _adaptiveMinimumDepthToViewport = true;
        [SerializeField] private float _adaptiveMinimumDepthFloorMeters = 2f;
        [SerializeField] [Range(0.1f, 1.25f)] private float _adaptiveMinimumDepthViewportCoverage = 0.82f;
        [SerializeField] private Vector2 _swimSpeedRange = new Vector2(0.65f, 1.55f);
        [SerializeField] private Vector2 _speedVarianceRange = new Vector2(0.72f, 1.38f);
        [SerializeField] private Vector2 _scaleRange = new Vector2(0.45f, 0.95f);
        [SerializeField] private Vector2 _nearLayerScaleRange = new Vector2(0.74f, 1.18f);
        [SerializeField] private Vector2 _midLayerScaleRange = new Vector2(0.56f, 0.92f);
        [SerializeField] private Vector2 _farLayerScaleRange = new Vector2(0.38f, 0.72f);
        [SerializeField] private float _depthParallaxScale = 1.35f;
        [SerializeField] private float _shipTravelParallax = 0.45f;
        [SerializeField] private Vector3 _layerAlpha = new Vector3(0.6f, 0.35f, 0.12f);
        [SerializeField] private Vector2 _offscreenSpawnOffsetRange = new Vector2(0.25f, 0.85f);
        [SerializeField] [Range(0f, 1f)] private float _initialAheadSpawnChance = 0.5f;
        [SerializeField] [Range(0f, 1f)] private float _wrapAheadSpawnChance = 0.75f;
        [SerializeField] [Range(0f, 0.45f)] private float _verticalBandJitterRatio = 0.18f;
        [SerializeField] private Vector2 _initialSpawnDelayRangeSeconds = new Vector2(0.02f, 0.3f);
        [SerializeField] private Vector2 _respawnDelayRangeSeconds = new Vector2(0.12f, 0.65f);
        [SerializeField] private Vector2 _verticalRetargetIntervalRangeSeconds = new Vector2(1.2f, 3.4f);
        [SerializeField] private Vector2 _verticalDriftLerpSpeedRange = new Vector2(0.45f, 1.3f);
        [SerializeField] private Vector2 _shipTravelVerticalOffsetPerMeterX = new Vector2(-0.18f, 0.18f);
        [SerializeField] private float _spawnDelayStaggerStepSeconds = 0.08f;
        [SerializeField] private int _spawnDelayStaggerCycle = 6;
        [SerializeField] private int _spawnYSpacingSampleAttempts = 7;
        [SerializeField] private float _spawnActivationOffscreenBuffer = 0.2f;
        [SerializeField] private int _minimumVisibleTracks = 2;
        [SerializeField] private float _visibleTrackRecoverySeconds = 0.45f;
        [SerializeField] private float _recoveryPendingSpawnMaxDelaySeconds = 0.18f;
        [SerializeField] [Range(0f, 0.2f)] private float _visibleTrackViewportPadding = 0.02f;

        private readonly List<Sprite> _spriteLibrary = new List<Sprite>(16);
        private readonly List<BackdropFishTrack> _tracks = new List<BackdropFishTrack>(16);
        private GameObject _visualRoot;
        private bool _initialized;
        private bool _hasLastShipX;
        private float _lastShipX;
        private float _shipDeltaX;
        private int _spawnSequenceIndex;
        private float _visibleTrackRecoveryElapsed;

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

            AlignVisualRootDepth();
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
                AlignVisualRootDepth();
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

                TryAddSpriteToLibrary(renderer.sprite, seen);
            }

            if (_spriteLibrary.Count == 0 && _allowGenericFishNameFallback)
            {
                for (var i = 0; i < renderers.Length; i++)
                {
                    var renderer = renderers[i];
                    if (renderer == null || renderer.sprite == null || renderer.gameObject.scene != gameObject.scene)
                    {
                        continue;
                    }

                    if (renderer.gameObject.name.IndexOf("Fish", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    TryAddSpriteToLibrary(renderer.sprite, seen);
                }
            }

            if (_spriteLibrary.Count > 0)
            {
                return;
            }

            if (_fallbackFishSprite != null)
            {
                TryAddSpriteToLibrary(_fallbackFishSprite, seen);
            }

            if (_spriteLibrary.Count > 0)
            {
                return;
            }

            var tutorialSpriteLibrary = Resources.Load<TutorialSpriteLibrary>(TutorialSpriteLibraryResourcePath);
            if (tutorialSpriteLibrary != null)
            {
                TryAddSpriteToLibrary(tutorialSpriteLibrary.FishSprite, seen);
            }
        }

        private void TryAddSpriteToLibrary(Sprite sprite, HashSet<Sprite> seen)
        {
            if (sprite == null || seen == null)
            {
                return;
            }

            if (!seen.Add(sprite))
            {
                return;
            }

            _spriteLibrary.Add(sprite);
        }

        private void EnsureVisualRoot()
        {
            if (_visualRoot == null)
            {
                _visualRoot = new GameObject("FishingDepthBackdropFishRoot");
            }

            if (_visualRoot.transform.parent != null)
            {
                _visualRoot.transform.SetParent(null, worldPositionStays: true);
            }

            _visualRoot.transform.localScale = Vector3.one;
            AlignVisualRootDepth();
        }

        private void BuildTracks()
        {
            RebuildTracks();
            if (_visualRoot == null)
            {
                return;
            }

            _visibleTrackRecoveryElapsed = 0f;
            ResolveViewportHalfSize(out var halfWidth, out _);
            var spawnPadding = Mathf.Max(1f, _horizontalPadding);
            _spawnSequenceIndex = 0;
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
                    // Layer 0 is closest (highest alpha/size), layer 2 is farthest.
                    renderer.sortingOrder = _baseSortingOrder + (2 - layer);
                    renderer.flipX = UnityEngine.Random.value > 0.5f;
                    var alpha = ResolveLayerAlpha(layer);
                    renderer.color = new Color(0.9f, 0.96f, 1f, alpha);
                    var scale = ResolveLayerScale(layer);
                    go.transform.localScale = new Vector3(scale, scale, 1f);

                    var track = new BackdropFishTrack
                    {
                        Transform = go.transform,
                        Renderer = renderer,
                        LayerIndex = layer,
                        Speed = ResolveTrackSpeed(layer),
                        Direction = UnityEngine.Random.value > 0.5f ? 1f : -1f,
                        BobAmplitude = UnityEngine.Random.Range(0.02f, 0.11f) * (1f + (layer * 0.22f)),
                        BobFrequency = UnityEngine.Random.Range(0.16f, 0.5f),
                        Phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                        BaseY = ResolveLayerBaseWorldY(layer, i, layerCounts[layer]),
                        TargetBaseY = 0f,
                        VerticalDriftLerpSpeed = ResolveVerticalDriftLerpSpeed(),
                        TravelVerticalOffsetPerShipMeter = ResolveTravelVerticalOffsetPerShipMeter(),
                        RetargetDelaySeconds = ResolveVerticalRetargetDelaySeconds(),
                        SpawnDelaySeconds = 0f,
                        PendingSpawn = false
                    };

                    QueueTrackSpawn(track, halfWidth, spawnPadding, preferAhead: false, initialSpawn: true);
                    _tracks.Add(track);
                }
            }
        }

        private void AlignVisualRootDepth()
        {
            if (_targetCamera == null || _visualRoot == null)
            {
                return;
            }

            var targetZ = _targetCamera.transform.position.z + Mathf.Abs(_overlayDepthFromCamera);
            var p = _visualRoot.transform.position;
            p.z = targetZ;
            _visualRoot.transform.position = p;
        }

        private void TickTracks()
        {
            ResolveViewportHalfSize(out var halfWidth, out var halfHeight);
            var wrapPadding = Mathf.Max(1f, _horizontalPadding);
            var minimumDepthWorldY = ResolveMinimumDepthWorldY();
            var cameraX = _targetCamera != null ? _targetCamera.transform.position.x : 0f;
            var cameraY = _targetCamera != null ? _targetCamera.transform.position.y : 0f;
            var leftBound = cameraX - (halfWidth + wrapPadding);
            var rightBound = cameraX + (halfWidth + wrapPadding);
            var topBound = cameraY + halfHeight + 1.25f;
            var bottomBound = cameraY - halfHeight - 1.25f;
            var visibleTracks = 0;

            for (var i = 0; i < _tracks.Count; i++)
            {
                var track = _tracks[i];
                if (track == null || track.Transform == null || track.Renderer == null)
                {
                    continue;
                }

                if (track.PendingSpawn)
                {
                    track.SpawnDelaySeconds -= Time.unscaledDeltaTime;
                    if (track.SpawnDelaySeconds > 0f)
                    {
                        continue;
                    }

                    if (!IsTrackOffscreenForActivation(track, cameraX, halfWidth))
                    {
                        SnapTrackToActivationOffscreenX(track, cameraX, halfWidth);
                        track.SpawnDelaySeconds = Mathf.Max(
                            0.05f,
                            Mathf.Min(0.35f, ResolveSpawnDelaySeconds(initialSpawn: false) * 0.2f));
                        continue;
                    }

                    track.PendingSpawn = false;
                    track.Renderer.enabled = true;
                }

                track.RetargetDelaySeconds -= Time.unscaledDeltaTime;
                if (track.RetargetDelaySeconds <= 0f)
                {
                    track.TargetBaseY = ResolveLayerBaseWorldYSpaced(track.LayerIndex, track);
                    track.RetargetDelaySeconds = ResolveVerticalRetargetDelaySeconds();
                }

                var travelOffset = (-_shipDeltaX) * track.TravelVerticalOffsetPerShipMeter;
                track.TargetBaseY = ClampLayerBaseWorldY(track.LayerIndex, track.TargetBaseY + travelOffset);
                var baseYBlend = 1f - Mathf.Exp(-Mathf.Max(0.1f, track.VerticalDriftLerpSpeed) * Time.unscaledDeltaTime);
                track.BaseY = Mathf.Lerp(track.BaseY, track.TargetBaseY, baseYBlend);

                var p = track.Transform.position;
                p.x += track.Speed * track.Direction * Time.unscaledDeltaTime;
                var bob = Mathf.Sin((Time.unscaledTime * track.BobFrequency) + track.Phase) * track.BobAmplitude;
                p.y = track.BaseY + bob;

                // Camera depth can shift rapidly between demo scenes; re-seed fish that are now fully outside view.
                if (p.y < bottomBound || p.y > topBound)
                {
                    track.BaseY = ResolveLayerBaseWorldYSpaced(track.LayerIndex, track);
                    track.TargetBaseY = track.BaseY;
                    p.y = track.BaseY;
                }

                if (p.x > rightBound)
                {
                    QueueTrackSpawn(track, halfWidth, wrapPadding, preferAhead: true, initialSpawn: false);
                    continue;
                }

                if (p.x < leftBound)
                {
                    QueueTrackSpawn(track, halfWidth, wrapPadding, preferAhead: true, initialSpawn: false);
                    continue;
                }

                p.y = Mathf.Min(p.y, minimumDepthWorldY - (track.LayerIndex * 0.12f));
                track.Transform.position = p;
                if (IsWorldPointVisibleInViewport(p, cameraX, cameraY, halfWidth, halfHeight))
                {
                    visibleTracks++;
                }
            }

            MaintainVisibleTrackCoverage(visibleTracks, cameraX, cameraY, halfWidth, halfHeight, wrapPadding);
        }

        private void MaintainVisibleTrackCoverage(
            int visibleTracks,
            float cameraX,
            float cameraY,
            float halfWidth,
            float halfHeight,
            float spawnPadding)
        {
            var requiredVisibleTracks = Mathf.Clamp(_minimumVisibleTracks, 0, Mathf.Max(0, _tracks.Count));
            if (requiredVisibleTracks <= 0 || visibleTracks >= requiredVisibleTracks)
            {
                _visibleTrackRecoveryElapsed = 0f;
                return;
            }

            _visibleTrackRecoveryElapsed += Time.unscaledDeltaTime;
            var recoverySeconds = Mathf.Max(0.2f, _visibleTrackRecoverySeconds);
            if (_visibleTrackRecoveryElapsed < recoverySeconds)
            {
                return;
            }

            _visibleTrackRecoveryElapsed = 0f;
            var pendingSpawnMaxDelay = Mathf.Clamp(_recoveryPendingSpawnMaxDelaySeconds, 0.02f, 0.5f);
            for (var i = 0; i < _tracks.Count; i++)
            {
                var pendingTrack = _tracks[i];
                if (pendingTrack == null || !pendingTrack.PendingSpawn)
                {
                    continue;
                }

                pendingTrack.SpawnDelaySeconds = Mathf.Min(pendingTrack.SpawnDelaySeconds, pendingSpawnMaxDelay);
            }

            var requiredRecoveryCount = requiredVisibleTracks - visibleTracks;
            for (var i = 0; i < _tracks.Count && requiredRecoveryCount > 0; i++)
            {
                var track = _tracks[i];
                if (track == null || track.Transform == null || track.Renderer == null || track.PendingSpawn)
                {
                    continue;
                }

                var p = track.Transform.position;
                if (IsWorldPointVisibleInViewport(p, cameraX, cameraY, halfWidth, halfHeight))
                {
                    continue;
                }

                QueueTrackSpawn(track, halfWidth, spawnPadding, preferAhead: true, initialSpawn: false);
                track.SpawnDelaySeconds = Mathf.Min(track.SpawnDelaySeconds, UnityEngine.Random.Range(0.04f, 0.16f));
                requiredRecoveryCount--;
            }
        }

        private bool IsWorldPointVisibleInViewport(Vector3 worldPosition, float cameraX, float cameraY, float halfWidth, float halfHeight)
        {
            var viewportPadding = Mathf.Clamp(_visibleTrackViewportPadding, 0f, 0.2f);
            var paddedHalfWidth = halfWidth * (1f + viewportPadding);
            var paddedHalfHeight = halfHeight * (1f + viewportPadding);
            return worldPosition.x >= cameraX - paddedHalfWidth
                && worldPosition.x <= cameraX + paddedHalfWidth
                && worldPosition.y >= cameraY - paddedHalfHeight
                && worldPosition.y <= cameraY + paddedHalfHeight;
        }

        private float ResolveLayerBaseWorldY(int layerIndex)
        {
            return ResolveLayerBaseWorldY(layerIndex, layerTrackIndex: -1, layerTrackCount: 0);
        }

        private float ResolveLayerBaseWorldY(int layerIndex, int layerTrackIndex, int layerTrackCount)
        {
            ResolveViewportHalfSize(out _, out var halfHeight);
            var cameraY = _targetCamera != null ? _targetCamera.transform.position.y : 0f;
            var clampedLayer = Mathf.Clamp(layerIndex, 0, 2);
            var top = cameraY + (halfHeight * 0.16f);
            var bottom = cameraY - (halfHeight * 1.08f);
            var totalBandHeight = Mathf.Max(0.5f, top - bottom);
            var layerBandHeight = totalBandHeight / 3f;
            var bandTop = top - (layerBandHeight * clampedLayer);
            var bandBottom = bandTop - layerBandHeight;

            float bandRatio;
            if (layerTrackCount > 1 && layerTrackIndex >= 0)
            {
                var slotCenterRatio = (layerTrackIndex + 0.5f) / layerTrackCount;
                bandRatio = Mathf.Clamp01(slotCenterRatio + UnityEngine.Random.Range(-0.24f, 0.24f));
            }
            else
            {
                bandRatio = UnityEngine.Random.Range(0.08f, 0.92f);
            }

            var y = Mathf.Lerp(bandTop, bandBottom, bandRatio);
            var jitterRange = halfHeight * Mathf.Clamp(_verticalBandJitterRatio, 0f, 0.45f);
            y += UnityEngine.Random.Range(-jitterRange, jitterRange);
            var depthMeters = ResolveCurrentDepthMeters();
            var depthRatio = Mathf.Clamp01(depthMeters / 5000f);
            y -= depthRatio * Mathf.Max(0f, _depthParallaxScale) * (0.4f + (clampedLayer * 0.35f));
            var minimumDepthWorldY = ResolveMinimumDepthWorldY();
            y = Mathf.Min(y, minimumDepthWorldY - (clampedLayer * 0.12f));
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

        private float ResolveMinimumDepthWorldY()
        {
            if (_ship == null || _targetCamera == null)
            {
                ResolveViewportHalfSize(out _, out var fallbackHalfHeight);
                var fallbackCameraY = _targetCamera != null ? _targetCamera.transform.position.y : 0f;
                return fallbackCameraY - (fallbackHalfHeight * 0.55f);
            }

            var minimumDepthMeters = Mathf.Max(0f, _minimumDepthMeters);
            if (_adaptiveMinimumDepthToViewport && _targetCamera.orthographic)
            {
                ResolveViewportHalfSize(out _, out var halfHeight);
                var cameraBottomY = _targetCamera.transform.position.y - halfHeight;
                var visibleDepthMeters = Mathf.Max(0f, _ship.position.y - cameraBottomY);
                if (visibleDepthMeters > 0.001f)
                {
                    var coverage = Mathf.Clamp(_adaptiveMinimumDepthViewportCoverage, 0.1f, 1.25f);
                    var adaptiveMinimumDepthMeters = Mathf.Max(
                        0f,
                        Mathf.Max(_adaptiveMinimumDepthFloorMeters, visibleDepthMeters * coverage));
                    minimumDepthMeters = Mathf.Min(minimumDepthMeters, adaptiveMinimumDepthMeters);
                }
                else
                {
                    // Surface framing can keep camera bottom at/above ship Y for short demo scenes.
                    // Keep backdrop fish near the lower viewport edge instead of pinning them far below.
                    minimumDepthMeters = Mathf.Min(
                        minimumDepthMeters,
                        Mathf.Max(0f, _adaptiveMinimumDepthFloorMeters));
                }
            }

            return _ship.position.y - minimumDepthMeters;
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

        private float ResolveSpawnWorldX(float halfWidth, float padding, bool preferAhead)
        {
            var offscreenOffsetMin = Mathf.Max(0.35f, Mathf.Min(_offscreenSpawnOffsetRange.x, _offscreenSpawnOffsetRange.y));
            var offscreenOffsetMax = Mathf.Max(offscreenOffsetMin + 0.05f, Mathf.Max(_offscreenSpawnOffsetRange.x, _offscreenSpawnOffsetRange.y));
            var offset = UnityEngine.Random.Range(offscreenOffsetMin, offscreenOffsetMax) + (Mathf.Max(0f, padding) * 0.2f);
            var aheadDirection = ResolveShipTravelDirection();
            var aheadChance = preferAhead
                ? Mathf.Clamp01(_wrapAheadSpawnChance)
                : Mathf.Clamp01(_initialAheadSpawnChance);

            if (_targetCamera == null)
            {
                var spawnLeftFallback = UnityEngine.Random.value < 0.5f;
                return spawnLeftFallback
                    ? -(halfWidth + offset)
                    : (halfWidth + offset);
            }

            var cameraX = _targetCamera.transform.position.x;
            var leftEdge = cameraX - halfWidth;
            var rightEdge = cameraX + halfWidth;
            var spawnLeft = UnityEngine.Random.value < aheadChance
                ? aheadDirection < 0f
                : UnityEngine.Random.value < 0.5f;

            return spawnLeft
                ? leftEdge - offset
                : rightEdge + offset;
        }

        private float ResolveShipTravelDirection()
        {
            if (_ship != null && Mathf.Abs(_shipDeltaX) > 0.0001f)
            {
                return Mathf.Sign(_shipDeltaX);
            }

            // Demo flow is leftward by design; default to left-side spawn-ahead.
            return -1f;
        }

        private float ResolveTrackSpeed(int layerIndex)
        {
            var minSpeed = Mathf.Min(_swimSpeedRange.x, _swimSpeedRange.y);
            var maxSpeed = Mathf.Max(_swimSpeedRange.x, _swimSpeedRange.y);
            var minVariance = Mathf.Max(0.2f, Mathf.Min(_speedVarianceRange.x, _speedVarianceRange.y));
            var maxVariance = Mathf.Max(minVariance + 0.01f, Mathf.Max(_speedVarianceRange.x, _speedVarianceRange.y));
            var layerScale = Mathf.Max(0.25f, 1f - (Mathf.Clamp(layerIndex, 0, 2) * 0.18f));
            var speedVariance = UnityEngine.Random.Range(minVariance, maxVariance);
            return Mathf.Max(0.02f, UnityEngine.Random.Range(minSpeed, maxSpeed) * layerScale * speedVariance);
        }

        private float ResolveLayerScale(int layerIndex)
        {
            Vector2 layerRange;
            switch (Mathf.Clamp(layerIndex, 0, 2))
            {
                case 0:
                    layerRange = _nearLayerScaleRange;
                    break;
                case 1:
                    layerRange = _midLayerScaleRange;
                    break;
                default:
                    layerRange = _farLayerScaleRange;
                    break;
            }

            var layerMin = Mathf.Max(0.05f, Mathf.Min(layerRange.x, layerRange.y));
            var layerMax = Mathf.Max(layerMin + 0.01f, Mathf.Max(layerRange.x, layerRange.y));
            if (layerMax <= 0.06f)
            {
                layerMin = Mathf.Max(0.05f, Mathf.Min(_scaleRange.x, _scaleRange.y));
                layerMax = Mathf.Max(layerMin + 0.01f, Mathf.Max(_scaleRange.x, _scaleRange.y));
            }

            return UnityEngine.Random.Range(layerMin, layerMax);
        }

        private void QueueTrackSpawn(BackdropFishTrack track, float halfWidth, float padding, bool preferAhead, bool initialSpawn)
        {
            if (track == null || track.Transform == null || track.Renderer == null)
            {
                return;
            }

            track.Speed = ResolveTrackSpeed(track.LayerIndex);
            var spawnX = ResolveSpawnWorldX(halfWidth, padding, preferAhead);
            track.Direction = ResolveSpawnDirectionForX(spawnX);
            var spawnY = ResolveLayerBaseWorldYSpaced(track.LayerIndex, track);
            track.BaseY = spawnY;
            track.TargetBaseY = spawnY;
            track.RetargetDelaySeconds = ResolveVerticalRetargetDelaySeconds();
            track.VerticalDriftLerpSpeed = ResolveVerticalDriftLerpSpeed();
            track.TravelVerticalOffsetPerShipMeter = ResolveTravelVerticalOffsetPerShipMeter();
            track.Renderer.flipX = track.Direction < 0f;
            if (_spriteLibrary.Count > 0)
            {
                track.Renderer.sprite = _spriteLibrary[UnityEngine.Random.Range(0, _spriteLibrary.Count)];
            }

            track.Transform.position = new Vector3(spawnX, spawnY, track.Transform.position.z);
            track.SpawnDelaySeconds = ResolveSpawnDelaySeconds(initialSpawn);
            var staggerCycle = Mathf.Max(1, _spawnDelayStaggerCycle);
            var staggerStep = Mathf.Max(0f, _spawnDelayStaggerStepSeconds);
            var staggerOffset = (_spawnSequenceIndex % staggerCycle) * staggerStep;
            _spawnSequenceIndex++;
            track.SpawnDelaySeconds = Mathf.Max(0.02f, track.SpawnDelaySeconds + staggerOffset);
            if (!initialSpawn)
            {
                // Scene transitions can push all tracks into respawn at once. Cap delay so
                // backdrop fish remain visible through short demo phases.
                track.SpawnDelaySeconds = Mathf.Min(track.SpawnDelaySeconds, 0.75f);
            }

            track.PendingSpawn = true;
            track.Renderer.enabled = false;
        }

        private float ResolveSpawnDelaySeconds(bool initialSpawn)
        {
            var range = initialSpawn ? _initialSpawnDelayRangeSeconds : _respawnDelayRangeSeconds;
            var min = Mathf.Max(0f, Mathf.Min(range.x, range.y));
            var max = Mathf.Max(min + 0.01f, Mathf.Max(range.x, range.y));
            return UnityEngine.Random.Range(min, max);
        }

        private bool IsTrackOffscreenForActivation(BackdropFishTrack track, float cameraX, float halfWidth)
        {
            if (track == null || track.Transform == null)
            {
                return true;
            }

            var x = track.Transform.position.x;
            var halfSpriteWidth = ResolveTrackHalfWidth(track) + Mathf.Max(0f, _spawnActivationOffscreenBuffer);
            var visibleLeft = cameraX - halfWidth;
            var visibleRight = cameraX + halfWidth;
            return (x + halfSpriteWidth) < visibleLeft || (x - halfSpriteWidth) > visibleRight;
        }

        private void SnapTrackToActivationOffscreenX(BackdropFishTrack track, float cameraX, float halfWidth)
        {
            if (track == null || track.Transform == null)
            {
                return;
            }

            var halfSpriteWidth = ResolveTrackHalfWidth(track) + Mathf.Max(0.05f, _spawnActivationOffscreenBuffer);
            var leftOffscreen = cameraX - halfWidth - halfSpriteWidth;
            var rightOffscreen = cameraX + halfWidth + halfSpriteWidth;
            var spawnOnLeft = track.Direction > 0f;
            var p = track.Transform.position;
            p.x = spawnOnLeft ? leftOffscreen : rightOffscreen;
            track.Transform.position = p;
        }

        private static float ResolveTrackHalfWidth(BackdropFishTrack track)
        {
            if (track == null || track.Transform == null)
            {
                return 0.35f;
            }

            var sprite = track.Renderer != null ? track.Renderer.sprite : null;
            if (sprite == null)
            {
                return 0.35f;
            }

            var scaleX = Mathf.Abs(track.Transform.lossyScale.x);
            var halfWidth = sprite.bounds.extents.x * Mathf.Max(0.01f, scaleX);
            return Mathf.Max(0.25f, halfWidth);
        }

        private float ResolveLayerBaseWorldYSpaced(int layerIndex, BackdropFishTrack targetTrack)
        {
            var attempts = Mathf.Max(2, _spawnYSpacingSampleAttempts);
            var bestY = ResolveLayerBaseWorldY(layerIndex);
            var bestScore = EvaluateSpawnYSpacingScore(bestY, targetTrack);

            for (var i = 1; i < attempts; i++)
            {
                var candidateY = ResolveLayerBaseWorldY(layerIndex);
                var score = EvaluateSpawnYSpacingScore(candidateY, targetTrack);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestY = candidateY;
                }
            }

            return bestY;
        }

        private float EvaluateSpawnYSpacingScore(float candidateY, BackdropFishTrack targetTrack)
        {
            var nearestDistance = float.MaxValue;
            var foundComparable = false;
            for (var i = 0; i < _tracks.Count; i++)
            {
                var track = _tracks[i];
                if (track == null || track == targetTrack || track.Transform == null || track.Renderer == null)
                {
                    continue;
                }

                foundComparable = true;
                var referenceY = track.PendingSpawn ? track.TargetBaseY : track.BaseY;
                var distance = Mathf.Abs(candidateY - referenceY);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                }
            }

            return foundComparable ? nearestDistance : float.MaxValue;
        }

        private float ResolveVerticalRetargetDelaySeconds()
        {
            var min = Mathf.Max(0.1f, Mathf.Min(_verticalRetargetIntervalRangeSeconds.x, _verticalRetargetIntervalRangeSeconds.y));
            var max = Mathf.Max(min + 0.05f, Mathf.Max(_verticalRetargetIntervalRangeSeconds.x, _verticalRetargetIntervalRangeSeconds.y));
            return UnityEngine.Random.Range(min, max);
        }

        private float ResolveVerticalDriftLerpSpeed()
        {
            var min = Mathf.Max(0.05f, Mathf.Min(_verticalDriftLerpSpeedRange.x, _verticalDriftLerpSpeedRange.y));
            var max = Mathf.Max(min + 0.01f, Mathf.Max(_verticalDriftLerpSpeedRange.x, _verticalDriftLerpSpeedRange.y));
            return UnityEngine.Random.Range(min, max);
        }

        private float ResolveTravelVerticalOffsetPerShipMeter()
        {
            var min = Mathf.Min(_shipTravelVerticalOffsetPerMeterX.x, _shipTravelVerticalOffsetPerMeterX.y);
            var max = Mathf.Max(_shipTravelVerticalOffsetPerMeterX.x, _shipTravelVerticalOffsetPerMeterX.y);
            return UnityEngine.Random.Range(min, max);
        }

        private float ClampLayerBaseWorldY(int layerIndex, float candidateY)
        {
            ResolveViewportHalfSize(out _, out var halfHeight);
            var cameraY = _targetCamera != null ? _targetCamera.transform.position.y : 0f;
            var top = cameraY + (halfHeight * 0.2f);
            var bottom = cameraY - (halfHeight * 1.12f);
            var minimumDepthWorldY = ResolveMinimumDepthWorldY();
            var depthFloor = minimumDepthWorldY - (Mathf.Clamp(layerIndex, 0, 2) * 0.12f);
            var clampedTop = Mathf.Min(top, depthFloor);
            var clampedBottom = Mathf.Min(bottom, clampedTop - 0.15f);
            return Mathf.Clamp(candidateY, clampedBottom, clampedTop);
        }

        private float ResolveSpawnDirectionForX(float spawnX)
        {
            if (_targetCamera == null)
            {
                return UnityEngine.Random.value > 0.5f ? 1f : -1f;
            }

            var cameraX = _targetCamera.transform.position.x;
            var towardCenter = Mathf.Sign(cameraX - spawnX);
            if (Mathf.Abs(towardCenter) < 0.01f)
            {
                towardCenter = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            }

            // Keep almost all fish entering toward camera center so short demo scenes
            // consistently show backdrop motion without long empty windows.
            if (UnityEngine.Random.value < 0.92f)
            {
                return towardCenter;
            }

            return -towardCenter;
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
            _visibleTrackRecoveryElapsed = 0f;
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
