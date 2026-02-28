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
            public bool settled;
            public float offscreenSeconds;
            public float spawnFadeDuration;
            public float spawnFadeElapsed;
            public bool hasPreviousPosition;
            public Vector3 previousPosition;
        }

        [SerializeField] private string _fishNameToken = "FishingFish";
        [SerializeField] private Vector2 _xBounds = new Vector2(-9.8f, 9.8f);
        [SerializeField] private Vector2 _yBounds = new Vector2(-3.2f, -1.55f);
        [SerializeField] private bool _dynamicWaterBand = true;
        [SerializeField] private bool _dynamicHorizontalBand = true;
        [SerializeField] private float _horizontalHalfSpan = 9.8f;
        [SerializeField] private float _bandTopOffsetBelowShip = 1.15f;
        [SerializeField] private float _minimumAmbientSpawnDepth = 30f;
        [SerializeField] private float _bandBottomOffsetBelowHook = 1.1f;
        [SerializeField] private float _minBandHeight = 3.4f;
        [SerializeField] private float _maxBandHeight = 90f;
        [SerializeField] private bool _spawnAheadWhileDescending = true;
        [SerializeField] private float _descendingAheadCameraLengths = 2.5f;
        [SerializeField] private float _descendingAheadBandHeight = 22f;
        [SerializeField] private int _descendingAheadMinimumActiveFish = 2;
        [SerializeField] private float _descendingSpawnCadenceMultiplier = 2.5f;
        [SerializeField] private float _descendingDetectionMinMetersPerSecond = 0.08f;
        [SerializeField] private Vector2 _speedRange = new Vector2(1.15f, 2.45f);
        [SerializeField] private Vector2 _spawnIntervalRange = new Vector2(0.4f, 1.4f);
        [SerializeField] private Vector2 _scaleMultiplierRange = new Vector2(0.85f, 1.15f);
        [SerializeField] private float _edgeBuffer = 1.35f;
        [SerializeField] private float _offscreenDespawnSeconds = 2.8f;
        [SerializeField] private float _offscreenDespawnDistance = 3.25f;
        [SerializeField] private float _spawnFadeInSeconds = 0.5f;
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
        [SerializeField] private float _escapedFishSpeedMultiplier = 3f;
        [SerializeField] private bool _enforceCatchableSpawnRules = true;
        [SerializeField] private float _collisionMotionPadding = 0.08f;
        [SerializeField] private float _collisionSweepMaxStepDistance = 1.5f;
        [SerializeField] private Camera _runtimeCamera;
        [SerializeField] private Transform _ship;
        [SerializeField] private Transform _hook;
        [SerializeField] private ShipMovementController _shipMovement;

        private readonly List<SwimTrack> _tracks = new List<SwimTrack>(16);
        private readonly List<Sprite> _spriteLibrary = new List<Sprite>(16);
        private readonly Dictionary<string, List<Sprite>> _spritesByFishId = new Dictionary<string, List<Sprite>>(StringComparer.OrdinalIgnoreCase);
        private bool _initialized;
        private SwimTrack _boundTrack;
        private Transform _boundHookTransform;
        private bool _spawnIntervalDefaultsCaptured;
        private Vector2 _defaultSpawnIntervalRange;
        private bool _subscribedToFishSpawner;
        private bool _hasLastHookY;
        private float _lastHookY;
        private bool _isHookDescending;
        private bool _hasLastHookCollisionPosition;
        private Vector2 _lastHookCollisionPosition;
        private int _hookCollisionSampleFrame = -1;
        private Vector2 _hookCollisionSampleStart;
        private Vector2 _hookCollisionSampleEnd;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _fishSpawner, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _shipMovement, this, warnIfMissing: false);
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

            UpdateHookDescentState();
            UpdateDynamicWaterBand();
            var allowAmbientPresence = IsAmbientPresenceAllowed();
            var descendingSpawnCadenceMultiplier = ResolveDescendingSpawnCadenceMultiplier();

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
                        track.spawnDelay -= Time.deltaTime * descendingSpawnCadenceMultiplier;
                    }
                }
            }

            var activeAheadFishCount = CountActiveFishAheadOfHook();
            var minimumAheadFishWhileDescending = ResolveMinimumAheadFishWhileDescending(allowAmbientPresence);
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

                var forceDescendingSpawn = minimumAheadFishWhileDescending > 0
                    && activeAheadFishCount < minimumAheadFishWhileDescending;
                if (track.spawnDelay <= 0f || forceDescendingSpawn)
                {
                    SpawnTrack(track);
                    activeCount++;
                    if (_hook == null || track.baseY < _hook.position.y - 0.05f)
                    {
                        activeAheadFishCount++;
                    }
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
                    offscreenSeconds = 0f,
                    spawnFadeDuration = 0f,
                    spawnFadeElapsed = 0f,
                    hasPreviousPosition = true,
                    previousPosition = go.transform.position
                };

                renderer.enabled = false;
                _tracks.Add(track);
            }

            _initialized = _tracks.Count > 0;
        }

        internal void ConfigureAnchorsForTests(Transform ship, Transform hook)
        {
            _ship = ship;
            _hook = hook;
            _hasLastHookY = false;
            _isHookDescending = false;
            _hasLastHookCollisionPosition = false;
            _hookCollisionSampleFrame = -1;
        }

        public void SetMaxConcurrentFish(int maxConcurrentFish)
        {
            _maxConcurrentFish = Mathf.Clamp(maxConcurrentFish, 1, 12);
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
            CompleteSpawnFade(track);
            track.hookedOffsetSign = track.direction >= 0f ? 1f : -1f;

            _boundTrack = track;
            _boundHookTransform = null;
            fishTransform = track.transform;
            return true;
        }

        public bool PositionBoundFishForDemo(float verticalViewportRatio, bool spawnFromLeft, float speedMultiplier = 1f)
        {
            if (_boundTrack == null || _boundTrack.transform == null || _boundTrack.renderer == null)
            {
                return false;
            }

            if (!TryResolveCameraWorldBounds(out var cameraLeft, out var cameraRight, out var cameraBottom, out var cameraTop))
            {
                return false;
            }

            var edgeBuffer = Mathf.Max(0.75f, Mathf.Abs(_edgeBuffer));
            var clampedRatio = Mathf.Clamp01(verticalViewportRatio);
            var targetY = Mathf.Lerp(
                cameraBottom + (edgeBuffer * 0.35f),
                cameraTop - (edgeBuffer * 0.35f),
                clampedRatio);

            if (_ship != null)
            {
                var minimumDepthTopY = _ship.position.y - Mathf.Abs(_minimumAmbientSpawnDepth);
                targetY = Mathf.Min(targetY, minimumDepthTopY - 0.15f);
            }

            _boundTrack.baseY = targetY;
            _boundTrack.phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            _boundTrack.direction = spawnFromLeft ? 1f : -1f;
            var minSpeed = Mathf.Min(_speedRange.x, _speedRange.y);
            var maxSpeed = Mathf.Max(_speedRange.x, _speedRange.y);
            _boundTrack.speed = UnityEngine.Random.Range(minSpeed, maxSpeed) * Mathf.Max(0.8f, speedMultiplier);
            _boundTrack.active = true;
            _boundTrack.reserved = true;
            _boundTrack.hooked = false;
            _boundTrack.approaching = false;
            _boundTrack.settled = false;
            _boundTrack.offscreenSeconds = 0f;
            _boundTrack.spawnDelay = RandomSpawnDelay();
            _boundTrack.hookedOffsetSign = _boundTrack.direction >= 0f ? 1f : -1f;
            _boundTrack.renderer.enabled = true;
            _boundTrack.renderer.flipX = _boundTrack.direction < 0f;
            _boundTrack.renderer.color = Color.Lerp(_boundTrack.baseColor, Color.white, 0.22f);

            var spawnX = spawnFromLeft
                ? cameraLeft - edgeBuffer
                : cameraRight + edgeBuffer;
            _boundTrack.transform.position = new Vector3(spawnX, targetY, _boundTrack.transform.position.z);
            _boundTrack.previousPosition = _boundTrack.transform.position;
            _boundTrack.hasPreviousPosition = true;
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
            _boundTrack.settled = false;
            _boundTrack.approachTimeoutAt = Time.time + Mathf.Max(0.2f, _biteApproachMaxDurationSeconds);
            _boundTrack.hookedOffsetSign = ResolveHookSideSign(_boundTrack.transform.position.x, hookTransform.position.x);
            _boundTrack.renderer.enabled = true;
            _boundTrack.renderer.color = Color.Lerp(_boundTrack.baseColor, Color.white, 0.34f);
            CompleteSpawnFade(_boundTrack);
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
            _boundTrack.settled = false;
            _boundTrack.renderer.enabled = true;
            var hookedColor = Color.Lerp(_boundTrack.baseColor, Color.white, 0.35f);
            hookedColor.a = _boundTrack.baseColor.a;
            _boundTrack.renderer.color = hookedColor;
            CompleteSpawnFade(_boundTrack);
            _boundHookTransform = hookTransform;
            if (hookTransform != null)
            {
                _boundTrack.hookedOffsetSign = ResolveHookSideSign(_boundTrack.transform.position.x, hookTransform.position.x);
            }
        }

        public void SetBoundFishSettled(Transform hookTransform)
        {
            if (_boundTrack == null || _boundTrack.transform == null || _boundTrack.renderer == null)
            {
                return;
            }

            _boundTrack.reserved = true;
            _boundTrack.hooked = true;
            _boundTrack.active = false;
            _boundTrack.approaching = false;
            _boundTrack.settled = true;
            _boundTrack.renderer.enabled = true;
            var settledColor = Color.Lerp(_boundTrack.baseColor, Color.white, 0.35f);
            settledColor.a = _boundTrack.baseColor.a;
            _boundTrack.renderer.color = settledColor;
            CompleteSpawnFade(_boundTrack);
            if (hookTransform != null)
            {
                _boundHookTransform = hookTransform;
                _boundTrack.hookedOffsetSign = ResolveHookSideSign(_boundTrack.transform.position.x, hookTransform.position.x);
            }
        }

        public void SetBoundFishVisualFade(float normalizedFade)
        {
            if (_boundTrack == null || _boundTrack.renderer == null)
            {
                return;
            }

            var fade = Mathf.Clamp01(normalizedFade);
            var hookedColor = Color.Lerp(_boundTrack.baseColor, Color.white, 0.35f);
            hookedColor.a = _boundTrack.baseColor.a * fade;
            _boundTrack.renderer.color = hookedColor;
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
            track.settled = false;
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
            track.previousPosition = track.transform.position;
            track.hasPreviousPosition = true;
        }

        public bool IsBoundFishCollidingWithHook(Transform hookTransform, float hookRadius = 0.22f)
        {
            return IsTrackCollidingWithHook(
                _boundTrack,
                hookTransform,
                Mathf.Max(0.02f, hookRadius),
                out _);
        }

        public string GetBoundFishId()
        {
            return _boundTrack != null ? NormalizeFishId(_boundTrack.fishId) : string.Empty;
        }

        public bool TryBindCollidingFishToHook(Transform hookTransform, float hookRadius, out string fishId)
        {
            fishId = string.Empty;
            if (hookTransform == null)
            {
                return false;
            }

            var enforceCatchableRules = IsCatchableSpawnEnforcementActive();
            var collisionRadius = Mathf.Max(0.02f, hookRadius);
            SwimTrack collidedTrack = null;
            var bestDistanceSqr = float.MaxValue;

            if (IsTrackCollidingWithHook(_boundTrack, hookTransform, collisionRadius, out var boundDistanceSqr))
            {
                BackfillTrackFishIdForCollision(_boundTrack);
                if (!enforceCatchableRules || !string.IsNullOrWhiteSpace(_boundTrack.fishId))
                {
                    collidedTrack = _boundTrack;
                    bestDistanceSqr = boundDistanceSqr;
                }
            }

            for (var i = 0; i < _tracks.Count; i++)
            {
                var track = _tracks[i];
                if (track == null
                    || track.transform == null
                    || track.renderer == null
                    || track.hooked
                    || !track.renderer.enabled)
                {
                    continue;
                }

                if (!track.active && !track.approaching && !track.reserved)
                {
                    continue;
                }

                if (!IsTrackCollidingWithHook(track, hookTransform, collisionRadius, out var distanceSqr))
                {
                    continue;
                }

                BackfillTrackFishIdForCollision(track);
                if (enforceCatchableRules && string.IsNullOrWhiteSpace(track.fishId))
                {
                    continue;
                }

                if (distanceSqr >= bestDistanceSqr)
                {
                    continue;
                }

                collidedTrack = track;
                bestDistanceSqr = distanceSqr;
            }

            if (collidedTrack == null)
            {
                return false;
            }

            if (enforceCatchableRules && string.IsNullOrWhiteSpace(collidedTrack.fishId))
            {
                return false;
            }

            if (_boundTrack != null && _boundTrack != collidedTrack)
            {
                // Release the previously reserved fish back into active swimming to avoid abrupt visual despawn.
                ResolveBoundFish(caught: false);
            }

            _boundTrack = collidedTrack;
            _boundHookTransform = hookTransform;
            collidedTrack.reserved = true;
            collidedTrack.hooked = false;
            collidedTrack.active = false;
            collidedTrack.approaching = false;
            collidedTrack.settled = false;
            collidedTrack.renderer.enabled = true;
            collidedTrack.renderer.color = Color.Lerp(collidedTrack.baseColor, Color.white, 0.34f);
            CompleteSpawnFade(collidedTrack);
            collidedTrack.hookedOffsetSign = ResolveHookSideSign(collidedTrack.transform.position.x, hookTransform.position.x);
            fishId = NormalizeFishId(collidedTrack.fishId);
            return true;
        }

        private bool IsTrackCollidingWithHook(
            SwimTrack track,
            Transform hookTransform,
            float hookRadius,
            out float distanceSqr)
        {
            distanceSqr = float.PositiveInfinity;
            if (hookTransform == null
                || track == null
                || track.transform == null
                || track.renderer == null
                || !track.renderer.enabled
                || track.hooked)
            {
                return false;
            }

            var fishCurrent = new Vector2(track.transform.position.x, track.transform.position.y);
            var hookCurrent = new Vector2(hookTransform.position.x, hookTransform.position.y);
            var combinedRadius = Mathf.Max(0.02f, hookRadius)
                + ResolveTrackCollisionRadius(track)
                + Mathf.Max(0f, _collisionMotionPadding);
            var radiusSqr = combinedRadius * combinedRadius;

            var deltaCurrent = hookCurrent - fishCurrent;
            distanceSqr = deltaCurrent.sqrMagnitude;
            if (distanceSqr <= radiusSqr)
            {
                return true;
            }

            ResolveHookCollisionSample(hookTransform, hookCurrent, out var hookPrevious, out var hookSampleCurrent);
            var fishPrevious = track.hasPreviousPosition
                ? new Vector2(track.previousPosition.x, track.previousPosition.y)
                : fishCurrent;
            var maxSweepStepDistance = Mathf.Max(0.1f, _collisionSweepMaxStepDistance);
            if (Vector2.Distance(fishCurrent, fishPrevious) > maxSweepStepDistance
                || Vector2.Distance(hookSampleCurrent, hookPrevious) > maxSweepStepDistance)
            {
                return false;
            }

            var relativeStart = hookPrevious - fishPrevious;
            var relativeEnd = hookSampleCurrent - fishCurrent;
            var sweptDistanceSqr = ResolvePointToSegmentDistanceSqr(Vector2.zero, relativeStart, relativeEnd);
            distanceSqr = Mathf.Min(distanceSqr, sweptDistanceSqr);
            return sweptDistanceSqr <= radiusSqr;
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
            RecordTrackPreviousPosition(track);
            var p = track.transform.position;
            p.x += track.direction * track.speed * speedScale * Time.deltaTime;
            p.y = track.baseY + Mathf.Sin((now * _bobFrequency) + track.phase) * _bobAmplitude;
            track.transform.position = p;
            UpdateSpawnFade(track);

            if (TryResolveDistanceOutsideViewport(p, out var outsideDistance))
            {
                if (ShouldRetainOffscreenTrackWhileDescending(track, p, outsideDistance))
                {
                    track.offscreenSeconds = 0f;
                    return;
                }

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
                        CompleteSpawnFade(track);
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

            RecordTrackPreviousPosition(track);
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

            RecordTrackPreviousPosition(track);
            var side = Mathf.Abs(track.hookedOffsetSign) > 0.01f ? Mathf.Sign(track.hookedOffsetSign) : (track.direction >= 0f ? 1f : -1f);
            var baseTarget = _boundHookTransform.position + new Vector3(Mathf.Abs(_biteApproachHookOffset.x) * side, _biteApproachHookOffset.y, 0f);
            if (track.settled)
            {
                track.transform.position = new Vector3(baseTarget.x, baseTarget.y, track.transform.position.z);
                track.renderer.flipX = side < 0f;
                return;
            }

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
            track.settled = false;
            track.reserved = false;
            track.offscreenSeconds = 0f;
            track.hookedOffsetSign = track.direction >= 0f ? 1f : -1f;

            var scaleMultiplier = UnityEngine.Random.Range(
                Mathf.Min(_scaleMultiplierRange.x, _scaleMultiplierRange.y),
                Mathf.Max(_scaleMultiplierRange.x, _scaleMultiplierRange.y));
            track.transform.localScale = track.baseScale * scaleMultiplier;

            var enforceCatchableRules = IsCatchableSpawnEnforcementActive();
            var resolvedFishId = NormalizeFishId(preferredFishId);
            if (string.IsNullOrEmpty(resolvedFishId)
                && enforceCatchableRules
                && TryResolveCatchableFishIdForSpawn(track.baseY, out var catchableFishId))
            {
                resolvedFishId = catchableFishId;
            }

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
                var resolvedFromSprite = ResolveFishIdForSprite(randomSprite);
                if (!string.IsNullOrEmpty(resolvedFishId))
                {
                    track.fishId = resolvedFishId;
                }
                else
                {
                    track.fishId = resolvedFromSprite;
                }
            }
            else
            {
                track.fishId = string.Empty;
            }

            if (enforceCatchableRules && string.IsNullOrEmpty(track.fishId))
            {
                track.active = false;
                track.renderer.enabled = false;
                track.spawnDelay = RandomSpawnDelay();
                return;
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
            track.previousPosition = track.transform.position;
            track.hasPreviousPosition = true;

            track.active = true;
            track.renderer.enabled = true;
            BeginSpawnFade(track);
        }

        private void BeginSpawnFade(SwimTrack track)
        {
            if (track == null || track.renderer == null)
            {
                return;
            }

            var fadeDuration = Mathf.Max(0f, _spawnFadeInSeconds);
            track.spawnFadeElapsed = 0f;
            track.spawnFadeDuration = fadeDuration;
            if (fadeDuration <= 0.001f)
            {
                CompleteSpawnFade(track);
                return;
            }

            var color = track.baseColor;
            color.a = 0f;
            track.renderer.color = color;
        }

        private void UpdateSpawnFade(SwimTrack track)
        {
            if (track == null || track.renderer == null || track.spawnFadeDuration <= 0.001f)
            {
                return;
            }

            if (track.reserved || track.hooked || track.approaching)
            {
                CompleteSpawnFade(track);
                return;
            }

            track.spawnFadeElapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(track.spawnFadeElapsed / track.spawnFadeDuration);
            var color = track.renderer.color;
            color.a = Mathf.Lerp(0f, track.baseColor.a, progress);
            track.renderer.color = color;
            if (progress >= 0.999f)
            {
                CompleteSpawnFade(track);
            }
        }

        private static void CompleteSpawnFade(SwimTrack track)
        {
            if (track == null || track.renderer == null)
            {
                return;
            }

            track.spawnFadeElapsed = 0f;
            track.spawnFadeDuration = 0f;
            var color = track.renderer.color;
            color.a = track.baseColor.a;
            track.renderer.color = color;
        }

        private bool IsCatchableSpawnEnforcementActive()
        {
            if (!_enforceCatchableSpawnRules)
            {
                return false;
            }

            RuntimeServiceRegistry.Resolve(ref _fishSpawner, this, warnIfMissing: false);
            return _fishSpawner != null;
        }

        private void BackfillTrackFishIdForCollision(SwimTrack track)
        {
            if (track == null || !string.IsNullOrWhiteSpace(track.fishId))
            {
                return;
            }

            if (track.renderer != null)
            {
                var resolvedFromSprite = ResolveFishIdForSprite(track.renderer.sprite);
                if (!string.IsNullOrWhiteSpace(resolvedFromSprite))
                {
                    track.fishId = resolvedFromSprite;
                    return;
                }
            }

            if (track.transform != null
                && TryResolveCatchableFishIdForSpawn(track.transform.position.y, out var catchableFishId))
            {
                track.fishId = catchableFishId;
            }
        }

        private bool TryResolveCatchableFishIdForSpawn(float spawnWorldY, out string fishId)
        {
            fishId = string.Empty;
            if (!IsCatchableSpawnEnforcementActive())
            {
                return false;
            }

            EnsureAnchors();
            if (_ship == null)
            {
                return false;
            }

            if (_shipMovement == null)
            {
                _shipMovement = _ship.GetComponent<ShipMovementController>();
                if (_shipMovement == null)
                {
                    RuntimeServiceRegistry.Resolve(ref _shipMovement, this, warnIfMissing: false);
                }
            }

            var distanceTier = _shipMovement != null ? Mathf.Max(1, _shipMovement.CurrentDistanceTier) : 1;
            var depth = Mathf.Max(0f, _ship.position.y - spawnWorldY);
            if (_shipMovement != null && !_shipMovement.IsDepthWithinOperationalBand(depth))
            {
                return false;
            }

            var rolledFish = _fishSpawner.RollFish(distanceTier, depth);
            if (rolledFish == null || string.IsNullOrWhiteSpace(rolledFish.id))
            {
                return false;
            }

            fishId = NormalizeFishId(rolledFish.id);
            return !string.IsNullOrEmpty(fishId);
        }

        private void DespawnTrack(SwimTrack track)
        {
            track.active = false;
            track.approaching = false;
            track.hooked = false;
            track.settled = false;
            track.hasPreviousPosition = false;
            track.spawnFadeDuration = 0f;
            track.spawnFadeElapsed = 0f;
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

                if (TryResolveDescendingAheadSpawnBand(out var descendingTop, out var descendingBottom))
                {
                    top = Mathf.Min(top, descendingTop);
                    bottom = Mathf.Min(bottom, descendingBottom);
                }
            }

            if (top - bottom > maxBand)
            {
                bottom = top - maxBand;
            }

            _yBounds = new Vector2(bottom, top);
        }

        private bool TryResolveDescendingAheadSpawnBand(out float top, out float bottom)
        {
            top = 0f;
            bottom = 0f;
            if (!_spawnAheadWhileDescending || !_isHookDescending || _hook == null)
            {
                return false;
            }

            if (!TryResolveCameraWorldBounds(out _, out _, out var cameraBottom, out var cameraTop))
            {
                return false;
            }

            var cameraLength = Mathf.Max(0.5f, cameraTop - cameraBottom);
            var aheadCameraLengths = Mathf.Max(0.5f, _descendingAheadCameraLengths);
            var aheadDistance = cameraLength * aheadCameraLengths;
            var bandHeight = Mathf.Max(1f, _descendingAheadBandHeight);
            var maxBand = Mathf.Max(bandHeight, _maxBandHeight);
            aheadDistance = Mathf.Min(aheadDistance, maxBand);

            top = _hook.position.y - Mathf.Abs(_bandBottomOffsetBelowHook) - aheadDistance;
            bottom = top - bandHeight;
            return true;
        }

        private float ResolveDescendingSpawnCadenceMultiplier()
        {
            if (!_spawnAheadWhileDescending || !_isHookDescending)
            {
                return 1f;
            }

            return Mathf.Max(1f, _descendingSpawnCadenceMultiplier);
        }

        private int ResolveMinimumAheadFishWhileDescending(bool allowAmbientPresence)
        {
            if (!_spawnAheadWhileDescending
                || !_isHookDescending
                || !allowAmbientPresence
                || _hook == null)
            {
                return 0;
            }

            var maxConcurrent = Mathf.Max(1, _maxConcurrentFish);
            var minimumAhead = Mathf.Max(1, _descendingAheadMinimumActiveFish);
            return Mathf.Min(maxConcurrent, minimumAhead);
        }

        private int CountActiveFishAheadOfHook()
        {
            if (_hook == null)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < _tracks.Count; i++)
            {
                var track = _tracks[i];
                if (track == null
                    || !track.active
                    || track.transform == null
                    || track.renderer == null
                    || !track.renderer.enabled)
                {
                    continue;
                }

                if (track.transform.position.y < _hook.position.y - 0.05f)
                {
                    count++;
                }
            }

            return count;
        }

        private void UpdateHookDescentState()
        {
            EnsureAnchors();
            if (_hook == null)
            {
                _isHookDescending = false;
                _hasLastHookY = false;
                _lastHookY = 0f;
                return;
            }

            var currentHookY = _hook.position.y;
            if (!_hasLastHookY)
            {
                _hasLastHookY = true;
                _lastHookY = currentHookY;
                _isHookDescending = false;
                return;
            }

            var deltaTime = Mathf.Max(0.0001f, Time.deltaTime);
            var velocityY = (currentHookY - _lastHookY) / deltaTime;
            _lastHookY = currentHookY;
            _isHookDescending = velocityY <= -Mathf.Max(0.05f, _descendingDetectionMinMetersPerSecond);
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
            // Keep ambient fish active while an encounter is in progress so
            // non-hooked fish do not abruptly despawn the moment a hook occurs.
            if (_boundTrack != null && (_boundTrack.hooked || _boundTrack.approaching || _boundTrack.reserved))
            {
                return true;
            }

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

        private bool ShouldRetainOffscreenTrackWhileDescending(SwimTrack track, Vector3 worldPosition, float outsideDistance)
        {
            if (track == null
                || !_spawnAheadWhileDescending
                || !_isHookDescending
                || _hook == null)
            {
                return false;
            }

            if (worldPosition.y >= _hook.position.y - 0.1f)
            {
                return false;
            }

            if (!TryResolveCameraWorldBounds(out _, out _, out var cameraBottom, out var cameraTop))
            {
                return false;
            }

            var cameraLength = Mathf.Max(0.5f, cameraTop - cameraBottom);
            var protectedDistance = cameraLength * Mathf.Max(1f, _descendingAheadCameraLengths + 0.5f);
            return outsideDistance <= protectedDistance;
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

        private void RecordTrackPreviousPosition(SwimTrack track)
        {
            if (track == null || track.transform == null)
            {
                return;
            }

            track.previousPosition = track.transform.position;
            track.hasPreviousPosition = true;
        }

        private void ResolveHookCollisionSample(
            Transform hookTransform,
            Vector2 hookCurrent,
            out Vector2 hookPrevious,
            out Vector2 hookResolvedCurrent)
        {
            if (hookTransform == null)
            {
                hookPrevious = hookCurrent;
                hookResolvedCurrent = hookCurrent;
                return;
            }

            var frame = Time.frameCount;
            if (_hookCollisionSampleFrame != frame)
            {
                _hookCollisionSampleStart = _hasLastHookCollisionPosition
                    ? _lastHookCollisionPosition
                    : hookCurrent;
                _hookCollisionSampleEnd = hookCurrent;
                _hookCollisionSampleFrame = frame;
                _lastHookCollisionPosition = hookCurrent;
                _hasLastHookCollisionPosition = true;
            }

            hookPrevious = _hookCollisionSampleStart;
            hookResolvedCurrent = _hookCollisionSampleEnd;
        }

        private static float ResolvePointToSegmentDistanceSqr(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
        {
            var segment = segmentEnd - segmentStart;
            var lengthSqr = segment.sqrMagnitude;
            if (lengthSqr <= 0.000001f)
            {
                return (point - segmentStart).sqrMagnitude;
            }

            var t = Mathf.Clamp01(Vector2.Dot(point - segmentStart, segment) / lengthSqr);
            var closest = segmentStart + (segment * t);
            return (point - closest).sqrMagnitude;
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

        private static float ResolveTrackCollisionRadius(SwimTrack track)
        {
            if (track == null || track.transform == null)
            {
                return 0.2f;
            }

            var lossyScale = track.transform.lossyScale;
            var averageScale = (Mathf.Abs(lossyScale.x) + Mathf.Abs(lossyScale.y)) * 0.5f;
            return Mathf.Clamp(averageScale * 0.3f, 0.12f, 0.55f);
        }
    }
}
