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
            public bool active;
            public bool reserved;
            public bool hooked;
        }

        [SerializeField] private string _fishNameToken = "FishingFish";
        [SerializeField] private Vector2 _xBounds = new Vector2(-9.8f, 9.8f);
        [SerializeField] private Vector2 _yBounds = new Vector2(-3.2f, -1.55f);
        [SerializeField] private Vector2 _speedRange = new Vector2(1.15f, 2.45f);
        [SerializeField] private Vector2 _spawnIntervalRange = new Vector2(0.4f, 1.4f);
        [SerializeField] private Vector2 _scaleMultiplierRange = new Vector2(0.85f, 1.15f);
        [SerializeField] private float _edgeBuffer = 1.35f;
        [SerializeField] private float _bobAmplitude = 0.14f;
        [SerializeField] private float _bobFrequency = 1.45f;
        [SerializeField] private int _maxConcurrentFish = 3;
        [SerializeField] private bool _searchInactive = true;
        [SerializeField] private UserSettingsService _settingsService;
        [SerializeField] private float _reducedMotionSpeedScale = 0.55f;

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
                    spawnDelay = UnityEngine.Random.Range(_spawnIntervalRange.x, _spawnIntervalRange.y),
                    phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f)
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
            track.renderer.color = Color.Lerp(track.baseColor, Color.white, 0.2f);
            track.renderer.enabled = true;

            _boundTrack = track;
            _boundHookTransform = null;
            fishTransform = track.transform;
            return true;
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
            _boundTrack.renderer.enabled = true;
            _boundHookTransform = hookTransform;
        }

        public void ResolveBoundFish(bool caught)
        {
            if (_boundTrack == null)
            {
                return;
            }

            var track = _boundTrack;
            _boundTrack = null;
            _boundHookTransform = null;

            track.hooked = false;
            track.reserved = false;
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
            track.direction = UnityEngine.Random.value < 0.5f ? 1f : -1f;
            track.speed = UnityEngine.Random.Range(
                Mathf.Min(_speedRange.x, _speedRange.y),
                Mathf.Max(_speedRange.x, _speedRange.y)) * 1.15f;
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

            var minX = Mathf.Min(_xBounds.x, _xBounds.y) - Mathf.Abs(_edgeBuffer);
            var maxX = Mathf.Max(_xBounds.x, _xBounds.y) + Mathf.Abs(_edgeBuffer);
            if (p.x < minX || p.x > maxX)
            {
                if (track.reserved)
                {
                    SpawnTrack(track);
                    track.reserved = true;
                    track.hooked = false;
                    track.renderer.color = Color.Lerp(track.baseColor, Color.white, 0.2f);
                    return;
                }

                DespawnTrack(track);
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

            var side = track.direction >= 0f ? 1f : -1f;
            var targetPosition = _boundHookTransform.position + new Vector3(0.28f * side, 0.08f, 0f);
            track.transform.position = Vector3.Lerp(track.transform.position, targetPosition, Time.deltaTime * 14f);
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
            track.baseY = UnityEngine.Random.Range(bottom, top);
            track.phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            track.spawnDelay = UnityEngine.Random.Range(_spawnIntervalRange.x, _spawnIntervalRange.y);

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
            if (track.renderer != null)
            {
                track.renderer.enabled = false;
            }

            track.spawnDelay = UnityEngine.Random.Range(_spawnIntervalRange.x, _spawnIntervalRange.y);
        }
    }
}
