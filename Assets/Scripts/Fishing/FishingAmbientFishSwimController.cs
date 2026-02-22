using System;
using System.Collections.Generic;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Data;
using UnityEngine;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class FishingAmbientFishSwimController : MonoBehaviour
    {
        private sealed class SwimTrack
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public Vector3 baseScale;
            public Color baseColor;
            public string fishId;
            public float direction;
            public float speed;
            public float baseY;
            public float phase;
            public float spawnDelay;
            public float approachTimeoutAt;
            public float hookedOffsetSign;
            public bool active;
            public bool reserved;
            public bool hooked;
            public bool approaching;
            public float offscreenSeconds;
        }

        [SerializeField] private string _fishNameToken = "FishingFish";
        [SerializeField] private Vector2 _xBounds = new Vector2(-9.8f, 9.8f);
        [SerializeField] private Vector2 _yBounds = new Vector2(-3.2f, -1.55f);
        [SerializeField] private bool _dynamicWaterBand = true;
        [SerializeField] private bool _dynamicHorizontalBand = true;
        [SerializeField] private float _horizontalHalfSpan = 9.8f;
        [SerializeField] private float _bandTopOffsetBelowShip = 1.15f;
        [SerializeField] private float _minimumAmbientSpawnDepth = 20f;
        [SerializeField] private float _bandBottomOffsetBelowHook = 1.1f;
        [SerializeField] private float _minBandHeight = 3.4f;
        [SerializeField] private float _maxBandHeight = 90f;
        [SerializeField] private Vector2 _speedRange = new Vector2(1.15f, 2.45f);
        [SerializeField] private Vector2 _spawnIntervalRange = new Vector2(0.4f, 1.4f);
        [SerializeField] private Vector2 _scaleMultiplierRange = new Vector2(0.85f, 1.15f);
        [SerializeField] private float _edgeBuffer = 1.35f;
        [SerializeField] private float _offscreenDespawnSeconds = 2.8f;
        [SerializeField] private float _offscreenDespawnDistance = 3.25f;
        [SerializeField] private float _bobAmplitude = 0.14f;
        [SerializeField] private float _bobFrequency = 1.45f;
        [SerializeField] private int _maxConcurrentFish = 3;
        [SerializeField] private bool _searchInactive = true;
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private bool _linkSpawnCadenceToFishSpawner = true;
        [SerializeField] private FishSpawner _fishSpawner;
        [SerializeField] private CatalogService _catalogService;
        [SerializeField] private float _baselineSpawnRatePerMinute = 6f;
        [SerializeField] private float _spawnRateJitterRatio = 0.35f;
        [SerializeField] private float _reducedMotionSpeedScale = 0.55f;
        [SerializeField] private float _biteApproachSpeed = 1.45f;
        [SerializeField] private float _biteApproachStopDistance = 0.14f;
        [SerializeField] private float _biteApproachMaxDurationSeconds = 2.6f;
        [SerializeField] private Vector2 _biteApproachHookOffset = new Vector2(0.28f, 0.08f);
        [SerializeField] private float _hookedFollowLerp = 6f;
        [SerializeField] private float _hookedStruggleAmplitude = 0.08f;
        [SerializeField] private float _hookedStruggleFrequency = 6.8f;
        [SerializeField] private float _escapedFishSpeedMultiplier = 1.65f;
        [SerializeField] private Camera _runtimeCamera;
        [SerializeField] private Transform _ship;
        [SerializeField] private Transform _hook;

        private readonly List<SwimTrack> _tracks = new List<SwimTrack>(16);
        private readonly List<Sprite> _spriteLibrary = new List<Sprite>(16);
        private readonly Dictionary<string, List<Sprite>> _spritesByFishId = new Dictionary<string, List<Sprite>>(StringComparer.OrdinalIgnoreCase);
        private bool _initialized;
        private SwimTrack _boundTrack;
        private Transform _boundHookTransform;
        private bool _spawnIntervalDefaultsCaptured;
        private Vector2 _defaultSpawnIntervalRange;
        private bool _subscribedToFishSpawner;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _fishSpawner, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
            CaptureDefaultSpawnIntervalRange();
            RebuildCatalogSpriteLookup();
        }

        private void OnEnable()
        {
            SubscribeToFishSpawner();
            if (!_initialized)
            {
                InitializeTracks();
            }

            ApplySpawnRateFromFishSpawner();
        }

        private void OnDisable()
        {
            UnsubscribeFromFishSpawner();
        }

        private void OnDestroy()
        {
            UnsubscribeFromFishSpawner();
        }

        private void Update()
        {
            if (_linkSpawnCadenceToFishSpawner && !_subscribedToFishSpawner)
            {
                SubscribeToFishSpawner();
                if (_fishSpawner != null)
                {
                    ApplySpawnRateFromFishSpawner();
                }
            }

            if (!_initialized || _tracks.Count == 0)
            {
                return;
            }

            UpdateDynamicWaterBand();
            var allowAmbientPresence = IsAmbientPresenceAllowed();

            if (_boundTrack != null && (_boundTrack.transform == null || _boundTrack.renderer == null))
            {
                _boundTrack = null;
                _boundHookTransform = null;
            }

            var speedScale = _settingsService != null && _settingsService.ReducedMotion
                ? Mathf.Clamp(_reducedMotionSpeedScale, 0.1f, 1f)
                : 1f;

            var activeCount = 0;
            var now = Time.time;
            for (var i = 0; i < _tracks.Count; i++)
            {
                var track = _tracks[i];
                if (track == null || track.transform == null || track.renderer == null)
                {
                    continue;
                }

                if (track.active)
                {
                    if (!allowAmbientPresence && !track.reserved && !track.hooked && !track.approaching)
                    {
                        DespawnTrack(track);
                        continue;
                    }

                    activeCount++;
                    TickTrack(track, now, speedScale);
                }
                else if (track.approaching)
                {
                    TickApproachTrack(track);
                }
                else if (track.hooked)
                {
                    TickHookedTrack(track);
                }
                else
                {
                    if (!track.reserved)
                    {
                        track.spawnDelay -= Time.deltaTime;
                    }
                }
            }

            for (var i = 0; i < _tracks.Count; i++)
            {
                if (!allowAmbientPresence)
                {
                    break;
                }

                if (activeCount >= Mathf.Max(1, _maxConcurrentFish))
                {
                    break;
                }

                var track = _tracks[i];
                if (track == null || track.active || track.hooked || track.reserved || track.transform == null)
                {
                    continue;
                }

                if (track.spawnDelay <= 0f)
                {
                    SpawnTrack(track);
                    activeCount++;
                }
            }
        }

        private void InitializeTracks()
        {
            _tracks.Clear();
            _spriteLibrary.Clear();

            var renderers = FindObjectsByType<SpriteRenderer>(
                _searchInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || renderer.gameObject.scene != gameObject.scene)
                {
                    continue;
                }

                var go = renderer.gameObject;
                if (string.IsNullOrWhiteSpace(go.name) || go.name.IndexOf(_fishNameToken, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (renderer.sprite != null)
                {
                    _spriteLibrary.Add(renderer.sprite);
                    RegisterFishSpriteFromName(renderer.sprite);
                }

                var sway = go.GetComponent<SpriteSwayMotion2D>();
                if (sway != null)
                {
                    sway.enabled = false;
                }

                var track = new SwimTrack
                {
                    transform = go.transform,
                    renderer = renderer,
                    baseScale = go.transform.localScale,
                    baseColor = renderer.color,
                    fishId = ResolveFishIdForSprite(renderer.sprite),
                    active = false,
                    reserved = false,
                    hooked = false,
                    approaching = false,
                    approachTimeoutAt = 0f,
                    hookedOffsetSign = 1f,
                    spawnDelay = RandomSpawnDelay(),
                    phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                    offscreenSeconds = 0f
                };

                renderer.enabled = false;
                _tracks.Add(track);
            }

            _initialized = _tracks.Count > 0;
        }

        public bool TryBindFish(string fishId, out Transform fishTransform)
        {
            fishTransform = null;
            if (!_initialized)
            {
                InitializeTracks();
            }

            if (_tracks.Count == 0)
            {
                return false;
            }

            if (_boundTrack != null && _boundTrack.transform != null && _boundTrack.renderer != null)
            {
                fishTransform = _boundTrack.transform;
                return true;
            }

            var normalizedFishId = NormalizeFishId(fishId);
            var track = FindBindingCandidateTrack(normalizedFishId);
            if (track == null || track.transform == null || track.renderer == null)
            {
                return false;
            }

            if (!track.active)
            {
                SpawnTrack(track, normalizedFishId);
            }
            else if (!string.IsNullOrEmpty(normalizedFishId))
            {
                var fishSprite = ResolveSpriteForFishId(normalizedFishId);
                if (fishSprite != null)
                {
                    track.renderer.sprite = fishSprite;
                    track.fishId = normalizedFishId;
                }
            }

            track.reserved = true;
            track.hooked = false;
            track.approaching = false;
            track.renderer.color = Color.Lerp(track.baseColor, Color.white, 0.2f);
            track.renderer.enabled = true;
            track.hookedOffsetSign = track.direction >= 0f ? 1f : -1f;

            _boundTrack = track;
            _boundHookTransform = null;
            fishTransform = track.transform;
            return true;
        }

        public bool BeginBoundFishApproach(Transform hookTransform)
        {
            if (_boundTrack == null || _boundTrack.transform == null || _boundTrack.renderer == null || hookTransform == null)
            {
                return false;
            }

            _boundHookTransform = hookTransform;
            _boundTrack.reserved = true;
            _boundTrack.hooked = false;
            _boundTrack.active = false;
            _boundTrack.approaching = true;
            _boundTrack.approachTimeoutAt = Time.time + Mathf.Max(0.2f, _biteApproachMaxDurationSeconds);
            _boundTrack.hookedOffsetSign = ResolveHookSideSign(_boundTrack.transform.position.x, hookTransform.position.x);
            _boundTrack.renderer.enabled = true;
            _boundTrack.renderer.color = Color.Lerp(_boundTrack.baseColor, Color.white, 0.34f);
            return true;
        }

        public bool IsBoundFishApproachComplete()
        {
            if (_boundTrack == null || _boundTrack.transform == null || _boundTrack.renderer == null)
            {
                return true;
            }

            return !_boundTrack.approaching;
        }

        public void SetBoundFishHooked(Transform hookTransform)
        {
            if (_boundTrack == null || _boundTrack.transform == null || _boundTrack.renderer == null)
            {
                return;
            }

            _boundTrack.reserved = true;
            _boundTrack.hooked = true;
            _boundTrack.active = false;
            _boundTrack.approaching = false;
            _boundTrack.renderer.enabled = true;
            _boundHookTransform = hookTransform;
            if (hookTransform != null)
            {
                _boundTrack.hookedOffsetSign = ResolveHookSideSign(_boundTrack.transform.position.x, hookTransform.position.x);
            }
        }

        public void ResolveBoundFish(bool caught)
        {
            if (_boundTrack == null)
            {
                return;
            }

            var track = _boundTrack;
            var hookTransform = _boundHookTransform;
            _boundTrack = null;
            _boundHookTransform = null;

            track.hooked = false;
            track.reserved = false;
            track.approaching = false;
            if (track.renderer != null)
            {
                track.renderer.color = track.baseColor;
            }

            if (caught)
            {
                DespawnTrack(track);
                return;
            }

            if (track.transform == null || track.renderer == null)
            {
                return;
            }

            track.active = true;
            track.renderer.enabled = true;
            if (hookTransform != null)
            {
                track.direction = ResolveHookSideSign(track.transform.position.x, hookTransform.position.x);
            }
            else
            {
                track.direction = UnityEngine.Random.value < 0.5f ? 1f : -1f;
            }

            track.speed = UnityEngine.Random.Range(
                Mathf.Min(_speedRange.x, _speedRange.y),
                Mathf.Max(_speedRange.x, _speedRange.y)) * Mathf.Max(1f, _escapedFishSpeedMultiplier);
            track.baseY = Mathf.Clamp(
                track.transform.position.y,
                Mathf.Min(_yBounds.x, _yBounds.y),
                Mathf.Max(_yBounds.x, _yBounds.y));
            track.phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            track.spawnDelay = RandomSpawnDelay();
            track.renderer.flipX = track.direction < 0f;
        }

        private SwimTrack FindBindingCandidateTrack(string preferredFishId)
        {
            preferredFishId = NormalizeFishId(preferredFishId);
            SwimTrack matchingInactiveCandidate = null;
            SwimTrack inactiveCandidate = null;
            for (var i = 0; i < _tracks.Count; i++)
            {
                var track = _tracks[i];
                if (track == null || track.transform == null || track.renderer == null || track.reserved || track.hooked)
                {
                    continue;
                }

                var matchesPreferredId = !string.IsNullOrEmpty(preferredFishId)
                    && string.Equals(track.fishId, preferredFishId, StringComparison.OrdinalIgnoreCase);
                if (track.active && track.renderer.enabled)
                {
                    if (!string.IsNullOrEmpty(preferredFishId))
                    {
                        if (matchesPreferredId)
                        {
                            return track;
                        }

                        continue;
                    }

                    return track;
                }

                if (matchesPreferredId && matchingInactiveCandidate == null)
                {
                    matchingInactiveCandidate = track;
                }

                if (inactiveCandidate == null)
                {
                    inactiveCandidate = track;
                }
            }

            if (matchingInactiveCandidate != null)
            {
                return matchingInactiveCandidate;
            }

            return inactiveCandidate;
        }

        private void TickTrack(SwimTrack track, float now, float speedScale)
        {
            var p = track.transform.position;
            p.x += track.direction * track.speed * speedScale * Time.deltaTime;
            p.y = track.baseY + Mathf.Sin((now * _bobFrequency) + track.phase) * _bobAmplitude;
            track.transform.position = p;

            if (TryResolveDistanceOutsideViewport(p, out var outsideDistance))
            {
                track.offscreenSeconds += Time.deltaTime;
                var despawnByDistance = outsideDistance >= Mathf.Max(0.5f, _offscreenDespawnDistance);
                var despawnByTime = track.offscreenSeconds >= Mathf.Max(0.25f, _offscreenDespawnSeconds);
                if (despawnByDistance || despawnByTime)
                {
                    if (track.reserved)
                    {
                        SpawnTrack(track);
                        track.reserved = true;
                        track.hooked = false;
                        track.approaching = false;
                        track.offscreenSeconds = 0f;
                        track.renderer.color = Color.Lerp(track.baseColor, Color.white, 0.2f);
                        return;
                    }

                    DespawnTrack(track);
                    return;
                }
            }
            else
            {
                track.offscreenSeconds = 0f;
            }
        }

        private void TickApproachTrack(SwimTrack track)
        {
            if (track == null || track.transform == null || track.renderer == null)
            {
                return;
            }

            if (_boundHookTransform == null)
            {
                track.approaching = false;
                return;
            }

            var side = Mathf.Abs(track.hookedOffsetSign) > 0.01f
                ? Mathf.Sign(track.hookedOffsetSign)
                : ResolveHookSideSign(track.transform.position.x, _boundHookTransform.position.x);
            var targetPosition = _boundHookTransform.position + new Vector3(Mathf.Abs(_biteApproachHookOffset.x) * side, _biteApproachHookOffset.y, 0f);
            var currentPosition = track.transform.position;
            var nextPosition = Vector3.MoveTowards(
                currentPosition,
                targetPosition,
                Mathf.Max(0.1f, _biteApproachSpeed) * Time.deltaTime);

            var sway = Mathf.Sin((Time.time * (_hookedStruggleFrequency * 0.5f)) + track.phase) * (_hookedStruggleAmplitude * 0.35f);
            nextPosition.y = Mathf.Lerp(nextPosition.y, targetPosition.y + sway, 1f - Mathf.Exp(-5f * Time.deltaTime));
            track.transform.position = new Vector3(nextPosition.x, nextPosition.y, currentPosition.z);

            var xDelta = targetPosition.x - currentPosition.x;
            if (Mathf.Abs(xDelta) > 0.01f)
            {
                track.renderer.flipX = xDelta < 0f;
            }

            var stopDistance = Mathf.Max(0.02f, _biteApproachStopDistance);
            if ((track.transform.position - targetPosition).sqrMagnitude <= (stopDistance * stopDistance)
                || Time.time >= track.approachTimeoutAt)
            {
                track.approaching = false;
                track.active = false;
            }
        }

        private void TickHookedTrack(SwimTrack track)
        {
            if (track == null || track.transform == null || track.renderer == null)
            {
                return;
            }

            track.renderer.enabled = true;
            if (_boundHookTransform == null)
            {
                return;
            }

            var side = Mathf.Abs(track.hookedOffsetSign) > 0.01f ? Mathf.Sign(track.hookedOffsetSign) : (track.direction >= 0f ? 1f : -1f);
            var baseTarget = _boundHookTransform.position + new Vector3(Mathf.Abs(_biteApproachHookOffset.x) * side, _biteApproachHookOffset.y, 0f);
            var struggle = new Vector3(
                Mathf.Sin((Time.time * _hookedStruggleFrequency) + track.phase) * _hookedStruggleAmplitude,
                Mathf.Cos((Time.time * (_hookedStruggleFrequency * 0.7f)) + track.phase) * (_hookedStruggleAmplitude * 0.45f),
                0f);
            var targetPosition = baseTarget + struggle;
            var blend = 1f - Mathf.Exp(-Mathf.Max(0.2f, _hookedFollowLerp) * Time.deltaTime);
            track.transform.position = Vector3.Lerp(track.transform.position, targetPosition, blend);
            track.renderer.flipX = side < 0f;
        }

        private void SpawnTrack(SwimTrack track, string preferredFishId = null)
        {
            if (track.transform == null || track.renderer == null)
            {
                return;
            }

            EnsureAnchors();
            var left = Mathf.Min(_xBounds.x, _xBounds.y);
            var right = Mathf.Max(_xBounds.x, _xBounds.y);
            var top = Mathf.Max(_yBounds.x, _yBounds.y);
            var bottom = Mathf.Min(_yBounds.x, _yBounds.y);
            if (_ship != null)
            {
                var minimumDepthTopY = _ship.position.y - Mathf.Abs(_minimumAmbientSpawnDepth);
                top = Mathf.Min(top, minimumDepthTopY);
                bottom = Mathf.Min(bottom, top - 0.5f);
            }

            track.direction = UnityEngine.Random.value < 0.5f ? 1f : -1f;
            track.speed = UnityEngine.Random.Range(Mathf.Min(_speedRange.x, _speedRange.y), Mathf.Max(_speedRange.x, _speedRange.y));
            track.baseY = ResolveSpawnY(bottom, top);
            track.phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            track.spawnDelay = RandomSpawnDelay();
            track.approachTimeoutAt = 0f;
            track.approaching = false;
            track.hooked = false;
            track.reserved = false;
            track.offscreenSeconds = 0f;
            track.hookedOffsetSign = track.direction >= 0f ? 1f : -1f;

            var scaleMultiplier = UnityEngine.Random.Range(
                Mathf.Min(_scaleMultiplierRange.x, _scaleMultiplierRange.y),
                Mathf.Max(_scaleMultiplierRange.x, _scaleMultiplierRange.y));
            track.transform.localScale = track.baseScale * scaleMultiplier;

            var resolvedFishId = NormalizeFishId(preferredFishId);
            var preferredSprite = ResolveSpriteForFishId(resolvedFishId);
            if (preferredSprite != null)
            {
                track.renderer.sprite = preferredSprite;
                track.fishId = resolvedFishId;
            }
            else if (_spriteLibrary.Count > 0)
            {
                var randomSprite = _spriteLibrary[UnityEngine.Random.Range(0, _spriteLibrary.Count)];
                track.renderer.sprite = randomSprite;
                track.fishId = ResolveFishIdForSprite(randomSprite);
            }
            else
            {
                track.fishId = string.Empty;
            }

            track.renderer.color = track.baseColor;
            track.renderer.flipX = track.direction < 0f;

            if (!TryResolveCameraWorldBounds(out var cameraLeft, out var cameraRight, out _, out _))
            {
                track.active = false;
                track.renderer.enabled = false;
                track.spawnDelay = RandomSpawnDelay();
                return;
            }

            left = cameraLeft;
            right = cameraRight;

            var edgeBuffer = Mathf.Max(0.75f, Mathf.Abs(_edgeBuffer));
            var spawnX = track.direction > 0f
                ? left - edgeBuffer
                : right + edgeBuffer;
            track.transform.position = new Vector3(spawnX, track.baseY, track.transform.position.z);

            track.active = true;
            track.renderer.enabled = true;
        }

        private void DespawnTrack(SwimTrack track)
        {
            track.active = false;
            track.approaching = false;
            track.hooked = false;
            if (track.renderer != null)
            {
                track.renderer.enabled = false;
            }

            track.spawnDelay = RandomSpawnDelay();
            track.offscreenSeconds = 0f;
        }

        public void ApplySpawnRatePerMinute(float spawnRatePerMinute)
        {
            CaptureDefaultSpawnIntervalRange();
            if (spawnRatePerMinute <= 0.01f)
            {
                _spawnIntervalRange = new Vector2(999f, 999f);
                ResetInactiveTrackSpawnDelays();
                return;
            }

            var baselineSpawnRate = Mathf.Max(0.1f, _baselineSpawnRatePerMinute);
            var defaultAverageIntervalSeconds = (_defaultSpawnIntervalRange.x + _defaultSpawnIntervalRange.y) * 0.5f;
            defaultAverageIntervalSeconds = Mathf.Max(0.08f, defaultAverageIntervalSeconds);
            var scale = baselineSpawnRate / Mathf.Max(0.1f, spawnRatePerMinute);
            var averageIntervalSeconds = Mathf.Clamp(defaultAverageIntervalSeconds * scale, 0.08f, 30f);
            var jitterRatio = Mathf.Clamp(_spawnRateJitterRatio, 0f, 0.9f);
            var minIntervalSeconds = Mathf.Max(0.08f, averageIntervalSeconds * (1f - jitterRatio));
            var maxIntervalSeconds = Mathf.Max(minIntervalSeconds + 0.01f, averageIntervalSeconds * (1f + jitterRatio));
            _spawnIntervalRange = new Vector2(minIntervalSeconds, maxIntervalSeconds);
            ResetInactiveTrackSpawnDelays();
        }

        private void HandleSpawnerSpawnRateChanged(float spawnRatePerMinute)
        {
            if (!_linkSpawnCadenceToFishSpawner)
            {
                return;
            }

            ApplySpawnRatePerMinute(spawnRatePerMinute);
        }

        private void ApplySpawnRateFromFishSpawner()
        {
            CaptureDefaultSpawnIntervalRange();
            if (!_linkSpawnCadenceToFishSpawner)
            {
                _spawnIntervalRange = _defaultSpawnIntervalRange;
                return;
            }

            RuntimeServiceRegistry.Resolve(ref _fishSpawner, this, warnIfMissing: false);
            if (_fishSpawner == null)
            {
                _spawnIntervalRange = _defaultSpawnIntervalRange;
                return;
            }

            ApplySpawnRatePerMinute(_fishSpawner.SpawnRatePerMinute);
        }

        private void SubscribeToFishSpawner()
        {
            if (!_linkSpawnCadenceToFishSpawner || _subscribedToFishSpawner)
            {
                return;
            }

            RuntimeServiceRegistry.Resolve(ref _fishSpawner, this, warnIfMissing: false);
            if (_fishSpawner == null)
            {
                return;
            }

            _fishSpawner.SpawnRateChanged -= HandleSpawnerSpawnRateChanged;
            _fishSpawner.SpawnRateChanged += HandleSpawnerSpawnRateChanged;
            _subscribedToFishSpawner = true;
        }

        private void UnsubscribeFromFishSpawner()
        {
            if (!_subscribedToFishSpawner || _fishSpawner == null)
            {
                _subscribedToFishSpawner = false;
                return;
            }

            _fishSpawner.SpawnRateChanged -= HandleSpawnerSpawnRateChanged;
            _subscribedToFishSpawner = false;
        }

        private void CaptureDefaultSpawnIntervalRange()
        {
            if (_spawnIntervalDefaultsCaptured)
            {
                return;
            }

            _spawnIntervalRange = NormalizeRange(_spawnIntervalRange);
            _defaultSpawnIntervalRange = _spawnIntervalRange;
            _spawnIntervalDefaultsCaptured = true;
        }

        private static Vector2 NormalizeRange(Vector2 value)
        {
            var min = Mathf.Max(0.01f, Mathf.Min(value.x, value.y));
            var max = Mathf.Max(min + 0.01f, Mathf.Max(value.x, value.y));
            return new Vector2(min, max);
        }

        private float RandomSpawnDelay()
        {
            _spawnIntervalRange = NormalizeRange(_spawnIntervalRange);
            return UnityEngine.Random.Range(_spawnIntervalRange.x, _spawnIntervalRange.y);
        }

        private void ResetInactiveTrackSpawnDelays()
        {
            if (_tracks.Count == 0)
            {
                return;
            }

            for (var i = 0; i < _tracks.Count; i++)
            {
                var track = _tracks[i];
                if (track == null || track.active || track.reserved || track.hooked || track.approaching)
                {
                    continue;
                }

                track.spawnDelay = RandomSpawnDelay();
            }
        }

        private void RebuildCatalogSpriteLookup()
        {
            _spritesByFishId.Clear();
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
            if (_catalogService == null || _catalogService.FishById == null || _catalogService.FishById.Count == 0)
            {
                return;
            }

            foreach (var pair in _catalogService.FishById)
            {
                var fishId = NormalizeFishId(pair.Key);
                if (string.IsNullOrEmpty(fishId) && pair.Value != null)
                {
                    fishId = NormalizeFishId(pair.Value.id);
                }

                if (string.IsNullOrEmpty(fishId) || pair.Value == null)
                {
                    continue;
                }

                RegisterFishSprite(fishId, pair.Value.icon);
            }
        }

        private void RegisterFishSpriteFromName(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
            if (_catalogService == null || _catalogService.FishById == null || _catalogService.FishById.Count == 0)
            {
                return;
            }

            var tokenizedName = Tokenize(sprite.name);
            foreach (var pair in _catalogService.FishById)
            {
                var fishId = NormalizeFishId(pair.Key);
                if (string.IsNullOrEmpty(fishId) && pair.Value != null)
                {
                    fishId = NormalizeFishId(pair.Value.id);
                }

                if (string.IsNullOrEmpty(fishId))
                {
                    continue;
                }

                if (!TokenMatchesFishId(tokenizedName, fishId))
                {
                    continue;
                }

                RegisterFishSprite(fishId, sprite);
                break;
            }
        }

        private void RegisterFishSprite(string fishId, Sprite sprite)
        {
            fishId = NormalizeFishId(fishId);
            if (string.IsNullOrEmpty(fishId) || sprite == null)
            {
                return;
            }

            if (!_spritesByFishId.TryGetValue(fishId, out var sprites))
            {
                sprites = new List<Sprite>(4);
                _spritesByFishId[fishId] = sprites;
            }

            if (!sprites.Contains(sprite))
            {
                sprites.Add(sprite);
            }
        }

        private Sprite ResolveSpriteForFishId(string fishId)
        {
            fishId = NormalizeFishId(fishId);
            if (string.IsNullOrEmpty(fishId))
            {
                return null;
            }

            if (!_spritesByFishId.TryGetValue(fishId, out var sprites) || sprites == null || sprites.Count == 0)
            {
                return null;
            }

            return sprites[UnityEngine.Random.Range(0, sprites.Count)];
        }

        private string ResolveFishIdForSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return string.Empty;
            }

            var tokenizedName = Tokenize(sprite.name);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
            if (_catalogService == null || _catalogService.FishById == null || _catalogService.FishById.Count == 0)
            {
                return string.Empty;
            }

            foreach (var pair in _catalogService.FishById)
            {
                var fishId = NormalizeFishId(pair.Key);
                if (string.IsNullOrEmpty(fishId) && pair.Value != null)
                {
                    fishId = NormalizeFishId(pair.Value.id);
                }

                if (string.IsNullOrEmpty(fishId))
                {
                    continue;
                }

                if (TokenMatchesFishId(tokenizedName, fishId))
                {
                    RegisterFishSprite(fishId, sprite);
                    return fishId;
                }
            }

            return string.Empty;
        }

        private static string NormalizeFishId(string fishId)
        {
            return string.IsNullOrWhiteSpace(fishId)
                ? string.Empty
                : fishId.Trim().ToLowerInvariant();
        }

        private static string Tokenize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var chars = value.Trim().ToLowerInvariant().ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if ((chars[i] >= 'a' && chars[i] <= 'z') || (chars[i] >= '0' && chars[i] <= '9'))
                {
                    continue;
                }

                chars[i] = '_';
            }

            return new string(chars);
        }

        private static bool TokenMatchesFishId(string tokenizedName, string fishId)
        {
            if (string.IsNullOrEmpty(tokenizedName) || string.IsNullOrEmpty(fishId))
            {
                return false;
            }

            if (tokenizedName.Contains(fishId))
            {
                return true;
            }

            var shortId = fishId.StartsWith("fish_", StringComparison.Ordinal)
                ? fishId.Substring(5)
                : fishId;
            return !string.IsNullOrEmpty(shortId) && tokenizedName.Contains(shortId);
        }

        private void UpdateDynamicWaterBand()
        {
            if (!_dynamicWaterBand)
            {
                return;
            }

            EnsureAnchors();
            if (_ship == null)
            {
                return;
            }

            if (_dynamicHorizontalBand)
            {
                var horizontalHalfSpan = Mathf.Max(2f, Mathf.Abs(_horizontalHalfSpan));
                _xBounds = new Vector2(_ship.position.x - horizontalHalfSpan, _ship.position.x + horizontalHalfSpan);
            }

            var topOffset = Mathf.Max(Mathf.Abs(_bandTopOffsetBelowShip), Mathf.Abs(_minimumAmbientSpawnDepth));
            var top = _ship.position.y - topOffset;
            var minBand = Mathf.Max(1f, _minBandHeight);
            var maxBand = Mathf.Max(minBand, _maxBandHeight);
            var bottom = top - minBand;

            if (_hook != null)
            {
                bottom = Mathf.Min(bottom, _hook.position.y - Mathf.Abs(_bandBottomOffsetBelowHook));
            }

            if (top - bottom > maxBand)
            {
                bottom = top - maxBand;
            }

            _yBounds = new Vector2(bottom, top);
        }

        private float ResolveSpawnY(float bottom, float top)
        {
            var minY = Mathf.Min(bottom, top);
            var maxY = Mathf.Max(bottom, top);
            if (Mathf.Abs(maxY - minY) <= 0.001f)
            {
                return minY;
            }

            const int attempts = 7;
            var bestY = UnityEngine.Random.Range(minY, maxY);
            var bestSpacing = EvaluateSpawnYSpacing(bestY);
            for (var i = 0; i < attempts; i++)
            {
                var candidate = UnityEngine.Random.Range(minY, maxY);
                var spacing = EvaluateSpawnYSpacing(candidate);
                if (spacing > bestSpacing)
                {
                    bestSpacing = spacing;
                    bestY = candidate;
                }
            }

            return bestY;
        }

        private float EvaluateSpawnYSpacing(float candidateY)
        {
            var hasComparableTrack = false;
            var nearestDistance = float.MaxValue;
            for (var i = 0; i < _tracks.Count; i++)
            {
                var track = _tracks[i];
                if (track == null || !track.active || track.transform == null || track.renderer == null || !track.renderer.enabled)
                {
                    continue;
                }

                hasComparableTrack = true;
                var trackY = track.transform.position.y;
                var distance = Mathf.Abs(candidateY - trackY);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                }
            }

            return hasComparableTrack ? nearestDistance : float.MaxValue;
        }

        private bool IsAmbientPresenceAllowed()
        {
            EnsureAnchors();
            var thresholdDepth = Mathf.Max(0.1f, Mathf.Abs(_minimumAmbientSpawnDepth));
            if (_hook == null || _ship == null)
            {
                return false;
            }

            var currentDepth = Mathf.Max(0f, _ship.position.y - _hook.position.y);
            return currentDepth >= thresholdDepth;
        }

        private void EnsureAnchors()
        {
            if (_ship != null && _hook != null)
            {
                return;
            }

            if (RuntimeServiceRegistry.TryGet<HookMovementController>(out var hookController))
            {
                _hook ??= hookController.transform;
                _ship ??= hookController.ShipTransform;
            }
        }

        private bool TryResolveDistanceOutsideViewport(Vector3 worldPosition, out float outsideDistance)
        {
            outsideDistance = 0f;
            if (!TryResolveCameraWorldBounds(out var left, out var right, out var bottom, out var top))
            {
                return false;
            }

            var outsideX = 0f;
            if (worldPosition.x < left)
            {
                outsideX = left - worldPosition.x;
            }
            else if (worldPosition.x > right)
            {
                outsideX = worldPosition.x - right;
            }

            var outsideY = 0f;
            if (worldPosition.y < bottom)
            {
                outsideY = bottom - worldPosition.y;
            }
            else if (worldPosition.y > top)
            {
                outsideY = worldPosition.y - top;
            }

            outsideDistance = Mathf.Max(outsideX, outsideY);
            return outsideDistance > 0.001f;
        }

        private bool TryResolveCameraWorldBounds(out float left, out float right, out float bottom, out float top)
        {
            left = 0f;
            right = 0f;
            bottom = 0f;
            top = 0f;

            if (_runtimeCamera == null)
            {
                _runtimeCamera = Camera.main;
                if (_runtimeCamera == null)
                {
                    _runtimeCamera = FindAnyObjectByType<Camera>(FindObjectsInactive.Exclude);
                }
            }

            if (_runtimeCamera == null || !_runtimeCamera.orthographic)
            {
                return false;
            }

            var halfHeight = Mathf.Max(0.01f, _runtimeCamera.orthographicSize);
            var halfWidth = Mathf.Max(0.01f, halfHeight * Mathf.Max(0.01f, _runtimeCamera.aspect));
            var cameraPosition = _runtimeCamera.transform.position;

            left = cameraPosition.x - halfWidth;
            right = cameraPosition.x + halfWidth;
            bottom = cameraPosition.y - halfHeight;
            top = cameraPosition.y + halfHeight;
            return true;
        }

        private static float ResolveHookSideSign(float fishX, float hookX)
        {
            var delta = fishX - hookX;
            if (Mathf.Abs(delta) < 0.01f)
            {
                return UnityEngine.Random.value < 0.5f ? -1f : 1f;
            }

            return Mathf.Sign(delta);
        }
    }
}
