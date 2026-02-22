using System;
using System.Collections.Generic;
using RavenDevOps.Fishing.Core;
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
        private bool _initialized;
        private SwimTrack _boundTrack;
        private Transform _boundHookTransform;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _settingsService, this, warnIfMissing: false);
        }

        private void OnEnable()
        {
            if (!_initialized)
            {
                InitializeTracks();
            }
        }

        private void Update()
        {
            if (!_initialized || _tracks.Count == 0)
            {
                return;
            }

            UpdateDynamicWaterBand();

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
                        track.spawnDelay -= Time.deltaTime;
                    }
                }
            }

            for (var i = 0; i < _tracks.Count; i++)
            {
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
                    active = false,
                    reserved = false,
                    hooked = false,
                    approaching = false,
                    approachTimeoutAt = 0f,
                    hookedOffsetSign = 1f,
                    spawnDelay = UnityEngine.Random.Range(_spawnIntervalRange.x, _spawnIntervalRange.y),
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

            var track = FindBindingCandidateTrack();
            if (track == null || track.transform == null || track.renderer == null)
            {
                return false;
            }

            if (!track.active)
            {
                SpawnTrack(track);
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
            track.spawnDelay = UnityEngine.Random.Range(_spawnIntervalRange.x, _spawnIntervalRange.y);
            track.renderer.flipX = track.direction < 0f;
        }

        private SwimTrack FindBindingCandidateTrack()
        {
            SwimTrack inactiveCandidate = null;
            for (var i = 0; i < _tracks.Count; i++)
            {
                var track = _tracks[i];
                if (track == null || track.transform == null || track.renderer == null || track.reserved || track.hooked)
                {
                    continue;
                }

                if (track.active && track.renderer.enabled)
                {
                    return track;
                }

                if (inactiveCandidate == null)
                {
                    inactiveCandidate = track;
                }
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

        private void SpawnTrack(SwimTrack track)
        {
            if (track.transform == null || track.renderer == null)
            {
                return;
            }

            var left = Mathf.Min(_xBounds.x, _xBounds.y);
            var right = Mathf.Max(_xBounds.x, _xBounds.y);
            var top = Mathf.Max(_yBounds.x, _yBounds.y);
            var bottom = Mathf.Min(_yBounds.x, _yBounds.y);

            track.direction = UnityEngine.Random.value < 0.5f ? 1f : -1f;
            track.speed = UnityEngine.Random.Range(Mathf.Min(_speedRange.x, _speedRange.y), Mathf.Max(_speedRange.x, _speedRange.y));
            track.baseY = ResolveSpawnY(bottom, top);
            track.phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            track.spawnDelay = UnityEngine.Random.Range(_spawnIntervalRange.x, _spawnIntervalRange.y);
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

            if (_spriteLibrary.Count > 0)
            {
                track.renderer.sprite = _spriteLibrary[UnityEngine.Random.Range(0, _spriteLibrary.Count)];
            }

            track.renderer.color = track.baseColor;
            track.renderer.flipX = track.direction < 0f;

            if (TryResolveCameraWorldBounds(out var cameraLeft, out var cameraRight, out _, out _))
            {
                left = cameraLeft;
                right = cameraRight;
            }

            var spawnX = track.direction > 0f
                ? left - Mathf.Abs(_edgeBuffer)
                : right + Mathf.Abs(_edgeBuffer);
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

            track.spawnDelay = UnityEngine.Random.Range(_spawnIntervalRange.x, _spawnIntervalRange.y);
            track.offscreenSeconds = 0f;
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
