using System.Collections.Generic;
using RavenDevOps.Fishing.Core;
using UnityEngine;

namespace RavenDevOps.Fishing.Fishing
{
    [DisallowMultipleComponent]
    public sealed class FishingSceneWeatherController : MonoBehaviour
    {
        private sealed class DriftSprite
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public float Speed;
            public float HorizontalDirection;
            public float VerticalSpeed;
            public float Phase;
            public float BobAmplitude;
            public float BobFrequency;
            public float BaseY;
            public float WeatherAlpha;
            public bool WeatherEnabled;
        }

        private static readonly FishingWeatherState[] RandomWeatherPool =
        {
            FishingWeatherState.Sunny,
            FishingWeatherState.Clouds,
            FishingWeatherState.PartlyCloudy,
            FishingWeatherState.Foggy,
            FishingWeatherState.Rain,
            FishingWeatherState.Thunderstorm,
            FishingWeatherState.QuarterMoon,
            FishingWeatherState.HalfMoon,
            FishingWeatherState.FullMoon
        };

        private static Sprite s_UnitSprite;

        [SerializeField] private Camera _targetCamera;
        [SerializeField] private FishingConditionController _conditionController;
        [SerializeField] private Transform _ship;
        [SerializeField] private HookMovementController _hookMovement;
        [SerializeField] private bool _allowInBatchMode = false;
        [SerializeField] private bool _randomizeOnStart = true;
        [SerializeField] private bool _autoCycleWeather = false;
        [SerializeField] private Vector2 _weatherChangeIntervalSeconds = new Vector2(180f, 360f);
        [SerializeField] private int _cloudSpriteCount = 6;
        [SerializeField] private int _rainSpriteCount = 24;
        [SerializeField] private int _fogBandCount = 3;
        [SerializeField] private float _overlayDepthFromCamera = 10f;
        [SerializeField] private int _baseSortingOrder = 48;
        [SerializeField] private float _skySurfaceYOffset = 0f;
        [SerializeField] private Vector2 _skyBandHeightMeters = new Vector2(1.25f, 7.5f);
        [SerializeField] private float _skyVisibilityBelowBandBuffer = 0.35f;
        [SerializeField] private float _skyVisibilityFadeDistance = 8f;

        private readonly List<DriftSprite> _cloudSprites = new List<DriftSprite>(12);
        private readonly List<DriftSprite> _rainSprites = new List<DriftSprite>(48);
        private readonly List<DriftSprite> _fogSprites = new List<DriftSprite>(8);
        private GameObject _visualRoot;
        private GameObject _skyRoot;
        private SpriteRenderer _skyTintOverlay;
        private SpriteRenderer _sunRenderer;
        private SpriteRenderer _moonRenderer;
        private SpriteRenderer _moonShadowRenderer;
        private SpriteRenderer _globalFogOverlay;
        private SpriteRenderer _lightningOverlay;
        private FishingWeatherState _currentWeather = FishingWeatherState.Sunny;
        private float _nextWeatherChangeAt;
        private float _lightningFlashAlpha;
        private float _nextLightningAt;
        private bool _lightningActive;
        private bool _showSunByWeather;
        private bool _showMoonByWeather;
        private bool _showMoonShadowByWeather;
        private bool _showSkyTintByWeather;
        private bool _showGlobalFogByWeather;
        private bool _skyBandVisible;
        private float _skyVisibilityFactor = 1f;
        private Color _skyTintColorByWeather = Color.clear;
        private Color _globalFogColorByWeather = Color.clear;
        private Color _sunColorByWeather = Color.clear;
        private Color _moonColorByWeather = Color.clear;
        private Color _moonShadowColorByWeather = Color.clear;

        public FishingWeatherState CurrentWeather => _currentWeather;

        public void Configure(Camera targetCamera, FishingConditionController conditionController = null, Transform ship = null)
        {
            _targetCamera = targetCamera;
            _conditionController = conditionController != null ? conditionController : _conditionController;
            _ship = ship != null ? ship : _ship;
            EnsureCamera();
            EnsureVisuals();
            if (isActiveAndEnabled)
            {
                ApplyWeather(_currentWeather, scheduleNextChange: true);
            }
        }

        public void RandomizeWeather()
        {
            var nextWeather = RandomWeatherPool[Random.Range(0, RandomWeatherPool.Length)];
            ApplyWeather(nextWeather, scheduleNextChange: true);
        }

        public void SetWeather(FishingWeatherState weather)
        {
            ApplyWeather(weather, scheduleNextChange: true);
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _conditionController, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _hookMovement, this, warnIfMissing: false);
            EnsureCamera();
            EnsureVisuals();
        }

        private void OnEnable()
        {
            if (Application.isBatchMode && !_allowInBatchMode)
            {
                enabled = false;
                return;
            }

            EnsureCamera();
            EnsureVisuals();
            if (_randomizeOnStart)
            {
                RandomizeWeather();
                return;
            }

            ApplyWeather(_currentWeather, scheduleNextChange: true);
        }

        private void LateUpdate()
        {
            EnsureCamera();
            ResolveShipReference();
            EnsureVisuals();
            UpdateViewportAnchoring();
            UpdateSkyElementVisibility();
            TickWeatherCycle();
            TickClouds();
            TickRain();
            TickFog();
            TickLightning();
        }

        private void OnDestroy()
        {
            if (_visualRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_visualRoot);
                }
                else
                {
                    DestroyImmediate(_visualRoot);
                }
            }

            if (_skyRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_skyRoot);
            }
            else
            {
                DestroyImmediate(_skyRoot);
            }
        }

        private void EnsureCamera()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_targetCamera == null)
            {
                _targetCamera = FindAnyObjectByType<Camera>(FindObjectsInactive.Exclude);
            }
        }

        private void ResolveShipReference()
        {
            if (_ship != null)
            {
                return;
            }

            if (_hookMovement == null)
            {
                RuntimeServiceRegistry.Resolve(ref _hookMovement, this, warnIfMissing: false);
            }

            if (_hookMovement != null)
            {
                _ship = _hookMovement.ShipTransform;
                return;
            }

            if (RuntimeServiceRegistry.TryGet<ShipMovementController>(out var shipMovement) && shipMovement != null)
            {
                _ship = shipMovement.transform;
            }
        }

        private void EnsureVisuals()
        {
            if (_targetCamera == null)
            {
                return;
            }

            if (_visualRoot == null)
            {
                _visualRoot = new GameObject("SceneWeatherVisuals");
            }

            if (_skyRoot == null)
            {
                _skyRoot = new GameObject("SceneWeatherSky");
            }

            if (_skyRoot.transform.parent != null)
            {
                _skyRoot.transform.SetParent(null, worldPositionStays: true);
            }

            _skyRoot.transform.localScale = Vector3.one;

            if (_visualRoot.transform.parent != _targetCamera.transform)
            {
                _visualRoot.transform.SetParent(_targetCamera.transform, worldPositionStays: false);
            }

            _visualRoot.transform.localPosition = new Vector3(0f, 0f, Mathf.Abs(_overlayDepthFromCamera));
            _visualRoot.transform.localRotation = Quaternion.identity;

            _skyTintOverlay ??= CreateSprite("WeatherSkyTint", _baseSortingOrder + 0, _visualRoot.transform);
            _sunRenderer ??= CreateSprite("WeatherSun", _baseSortingOrder + 1, _visualRoot.transform);
            _moonRenderer ??= CreateSprite("WeatherMoon", _baseSortingOrder + 1, _visualRoot.transform);
            _moonShadowRenderer ??= CreateSprite("WeatherMoonShadow", _baseSortingOrder + 2, _visualRoot.transform);
            _globalFogOverlay ??= CreateSprite("WeatherFogTint", _baseSortingOrder + 4, _visualRoot.transform);
            _lightningOverlay ??= CreateSprite("WeatherLightning", _baseSortingOrder + 12, _visualRoot.transform);

            EnsureDriftSprites(_cloudSprites, Mathf.Clamp(_cloudSpriteCount, 1, 24), "WeatherCloud", _baseSortingOrder + 3, _skyRoot.transform);
            EnsureDriftSprites(_rainSprites, Mathf.Clamp(_rainSpriteCount, 8, 100), "WeatherRain", _baseSortingOrder + 9, _skyRoot.transform);
            EnsureDriftSprites(_fogSprites, Mathf.Clamp(_fogBandCount, 1, 10), "WeatherFogBand", _baseSortingOrder + 6, _skyRoot.transform);
        }

        private void EnsureDriftSprites(List<DriftSprite> collection, int targetCount, string baseName, int sortingOrder, Transform parent)
        {
            if (collection == null || parent == null)
            {
                return;
            }

            while (collection.Count < targetCount)
            {
                var index = collection.Count;
                var renderer = CreateSprite($"{baseName}_{index}", sortingOrder, parent);
                var track = new DriftSprite
                {
                    Transform = renderer.transform,
                    Renderer = renderer,
                    Speed = Random.Range(0.04f, 0.12f),
                    HorizontalDirection = -1f,
                    VerticalSpeed = Random.Range(1.8f, 5.4f),
                    Phase = Random.Range(0f, Mathf.PI * 2f),
                    BobAmplitude = Random.Range(0.01f, 0.08f),
                    BobFrequency = Random.Range(0.25f, 1f),
                    BaseY = Random.Range(-1f, 1f)
                };
                collection.Add(track);
            }

            for (var i = 0; i < collection.Count; i++)
            {
                var track = collection[i];
                if (track == null || track.Renderer == null)
                {
                    continue;
                }

                if (track.Transform != null && track.Transform.parent != parent)
                {
                    track.Transform.SetParent(parent, worldPositionStays: true);
                }

                var shouldEnable = i < targetCount;
                track.WeatherEnabled = shouldEnable;
                track.Renderer.enabled = shouldEnable;
                track.Renderer.sortingOrder = sortingOrder;
            }
        }

        private SpriteRenderer CreateSprite(string name, int sortingOrder, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = GetUnitSprite();
            renderer.sortingOrder = sortingOrder;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(1f, 1f);
            renderer.color = Color.clear;
            return renderer;
        }

        private static Sprite GetUnitSprite()
        {
            if (s_UnitSprite != null)
            {
                return s_UnitSprite;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new[]
            {
                Color.white, Color.white,
                Color.white, Color.white
            });
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            s_UnitSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1f);
            return s_UnitSprite;
        }

        private void UpdateViewportAnchoring()
        {
            if (_targetCamera == null || _visualRoot == null)
            {
                return;
            }

            _visualRoot.transform.localPosition = new Vector3(0f, 0f, Mathf.Abs(_overlayDepthFromCamera));
            var bounds = ResolveViewportBounds(out var halfWidth, out var halfHeight);

            if (_skyTintOverlay != null)
            {
                _skyTintOverlay.size = new Vector2(bounds.x, bounds.y);
                _skyTintOverlay.transform.localPosition = Vector3.zero;
            }

            if (_globalFogOverlay != null)
            {
                _globalFogOverlay.size = new Vector2(bounds.x * 1.05f, bounds.y * 1.05f);
                _globalFogOverlay.transform.localPosition = Vector3.zero;
            }

            if (_lightningOverlay != null)
            {
                _lightningOverlay.size = new Vector2(bounds.x * 1.08f, bounds.y * 1.08f);
                _lightningOverlay.transform.localPosition = Vector3.zero;
            }

            if (_sunRenderer != null)
            {
                _sunRenderer.size = new Vector2(1.35f, 1.35f);
                _sunRenderer.transform.localPosition = new Vector3(halfWidth - 1.9f, halfHeight - 1.1f, 0f);
            }

            if (_moonRenderer != null)
            {
                _moonRenderer.size = new Vector2(1.15f, 1.15f);
                _moonRenderer.transform.localPosition = new Vector3(halfWidth - 1.9f, halfHeight - 1.05f, 0f);
            }

            if (_moonShadowRenderer != null)
            {
                _moonShadowRenderer.size = _moonRenderer != null ? _moonRenderer.size : new Vector2(1.15f, 1.15f);
            }
        }

        private Vector2 ResolveViewportBounds(out float halfWidth, out float halfHeight)
        {
            if (_targetCamera != null && _targetCamera.orthographic)
            {
                halfHeight = Mathf.Max(0.5f, _targetCamera.orthographicSize);
                halfWidth = Mathf.Max(0.5f, halfHeight * Mathf.Max(0.1f, _targetCamera.aspect));
                return new Vector2(halfWidth * 2f, halfHeight * 2f);
            }

            halfWidth = 10f;
            halfHeight = 6f;
            return new Vector2(halfWidth * 2f, halfHeight * 2f);
        }

        private void TickWeatherCycle()
        {
            if (!_autoCycleWeather || _weatherChangeIntervalSeconds.y <= 0.01f)
            {
                return;
            }

            if (Time.unscaledTime < _nextWeatherChangeAt)
            {
                return;
            }

            RandomizeWeather();
        }

        private void TickClouds()
        {
            if (_cloudSprites.Count == 0 || _targetCamera == null)
            {
                return;
            }

            ResolveViewportBounds(out var halfWidth, out var halfHeight);
            ResolveSkyBandWorldRange(out var minY, out var maxY);
            var skyVisibility = Mathf.Clamp01(_skyVisibilityFactor);
            var skyBandVisible = skyVisibility > 0.001f;
            var cameraX = _targetCamera.transform.position.x;
            var leftBound = cameraX - halfWidth - 2f;
            var rightBound = cameraX + halfWidth + 2f;
            for (var i = 0; i < _cloudSprites.Count; i++)
            {
                var cloud = _cloudSprites[i];
                if (cloud == null || cloud.Renderer == null || cloud.Transform == null)
                {
                    continue;
                }

                if (!cloud.WeatherEnabled)
                {
                    cloud.Renderer.enabled = false;
                    continue;
                }

                var p = cloud.Transform.position;
                var windScroll = cloud.Speed * cloud.HorizontalDirection * Time.unscaledDeltaTime;
                var horizontalStep = windScroll;
                p.x += horizontalStep;
                var bob = Mathf.Sin((Time.unscaledTime * cloud.BobFrequency) + cloud.Phase) * cloud.BobAmplitude;
                p.y = Mathf.Clamp(cloud.BaseY + bob, minY, maxY);
                if (horizontalStep <= 0f && p.x < leftBound)
                {
                    p.x = rightBound + Random.Range(2f, 7f);
                    cloud.BaseY = Random.Range(minY, maxY);
                }
                else if (horizontalStep > 0f && p.x > rightBound)
                {
                    p.x = leftBound - Random.Range(2f, 7f);
                    cloud.BaseY = Random.Range(minY, maxY);
                }

                cloud.Transform.position = p;
                var color = cloud.Renderer.color;
                color.a = cloud.WeatherAlpha * skyVisibility;
                cloud.Renderer.color = color;
                cloud.Renderer.enabled = skyBandVisible && color.a > 0.001f;
            }
        }

        private void TickRain()
        {
            if (_rainSprites.Count == 0 || _targetCamera == null)
            {
                return;
            }

            ResolveViewportBounds(out var halfWidth, out var halfHeight);
            ResolveSkyBandWorldRange(out var skyBandMinY, out var skyBandMaxY);
            var skyVisibility = Mathf.Clamp01(_skyVisibilityFactor);
            var skyBandVisible = skyVisibility > 0.001f;
            var cameraX = _targetCamera.transform.position.x;
            var leftBound = cameraX - (halfWidth + 1.2f);
            var rightBound = cameraX + (halfWidth + 1.2f);
            var resetTopY = skyBandMaxY + 1.6f;
            var resetBottomY = skyBandMinY - 1.6f;
            for (var i = 0; i < _rainSprites.Count; i++)
            {
                var drop = _rainSprites[i];
                if (drop == null || drop.Renderer == null || drop.Transform == null)
                {
                    continue;
                }

                if (!drop.WeatherEnabled)
                {
                    drop.Renderer.enabled = false;
                    continue;
                }

                var p = drop.Transform.position;
                var horizontalStep = -(drop.Speed * 0.12f * Time.unscaledDeltaTime);
                p.x += horizontalStep;
                p.y -= drop.VerticalSpeed * Time.unscaledDeltaTime;
                if (p.y < resetBottomY)
                {
                    p.y = resetTopY;
                    p.x = Random.Range(leftBound, rightBound);
                }
                else if (p.x < leftBound)
                {
                    p.x = rightBound;
                }
                else if (p.x > rightBound)
                {
                    p.x = leftBound;
                }

                drop.Transform.position = p;
                var color = drop.Renderer.color;
                color.a = drop.WeatherAlpha * skyVisibility;
                drop.Renderer.color = color;
                drop.Renderer.enabled = skyBandVisible && color.a > 0.001f;
            }
        }

        private void TickFog()
        {
            if (_fogSprites.Count == 0 || _targetCamera == null)
            {
                return;
            }

            ResolveViewportBounds(out var halfWidth, out var halfHeight);
            ResolveSkyBandWorldRange(out var skyBandMinY, out var skyBandMaxY);
            var skyVisibility = ResolveSkyVisibilityFactor(skyBandMinY, halfHeight);
            var skyBandVisible = skyVisibility > 0.001f;
            var cameraX = _targetCamera.transform.position.x;
            var leftBound = cameraX - (halfWidth + 3f);
            var rightBound = cameraX + (halfWidth + 3f);
            var fogMinY = skyBandMinY - 1.1f;
            var fogMaxY = Mathf.Max(fogMinY + 0.8f, skyBandMinY + ((skyBandMaxY - skyBandMinY) * 0.45f));
            for (var i = 0; i < _fogSprites.Count; i++)
            {
                var band = _fogSprites[i];
                if (band == null || band.Renderer == null || band.Transform == null)
                {
                    continue;
                }

                if (!band.WeatherEnabled)
                {
                    band.Renderer.enabled = false;
                    continue;
                }

                var p = band.Transform.position;
                var horizontalStep = band.Speed * 0.12f * Time.unscaledDeltaTime;
                p.x += horizontalStep;
                var bob = Mathf.Sin((Time.unscaledTime * band.BobFrequency) + band.Phase) * band.BobAmplitude;
                p.y = Mathf.Clamp(band.BaseY + bob, fogMinY, fogMaxY);
                if (horizontalStep >= 0f && p.x > rightBound)
                {
                    p.x = leftBound;
                    band.BaseY = Random.Range(fogMinY, fogMaxY);
                }
                else if (horizontalStep < 0f && p.x < leftBound)
                {
                    p.x = rightBound;
                    band.BaseY = Random.Range(fogMinY, fogMaxY);
                }

                band.Transform.position = p;
                var color = band.Renderer.color;
                color.a = band.WeatherAlpha * skyVisibility;
                band.Renderer.color = color;
                band.Renderer.enabled = skyBandVisible && color.a > 0.001f;
            }
        }

        private void TickLightning()
        {
            if (_lightningOverlay == null)
            {
                return;
            }

            if (_lightningActive && Time.unscaledTime >= _nextLightningAt)
            {
                _lightningFlashAlpha = Random.Range(0.14f, 0.32f);
                _nextLightningAt = Time.unscaledTime + Random.Range(4f, 9f);
            }

            if (!_lightningActive)
            {
                _lightningFlashAlpha = Mathf.MoveTowards(_lightningFlashAlpha, 0f, Time.unscaledDeltaTime * 1.8f);
            }
            else
            {
                _lightningFlashAlpha = Mathf.MoveTowards(_lightningFlashAlpha, 0f, Time.unscaledDeltaTime * 2.8f);
            }

            var lightningColor = new Color(0.94f, 0.98f, 1f, _lightningFlashAlpha);
            lightningColor.a *= Mathf.Clamp01(_skyVisibilityFactor);
            _lightningOverlay.color = lightningColor;
            _lightningOverlay.enabled = lightningColor.a > 0.001f;
        }

        private void ApplyWeather(FishingWeatherState weather, bool scheduleNextChange)
        {
            EnsureVisuals();
            _currentWeather = weather;
            var showSun = false;
            var showMoon = false;
            var moonShadowOffsetX = 0f;
            var cloudVisibleCount = 0;
            var rainVisibleCount = 0;
            var fogVisibleCount = 0;
            var skyTint = new Color(0f, 0f, 0f, 0f);
            var globalFogTint = new Color(0.84f, 0.9f, 0.95f, 0f);
            var cloudColor = new Color(0.9f, 0.95f, 1f, 0f);
            var rainColor = new Color(0.74f, 0.86f, 0.98f, 0f);
            var fogBandColor = new Color(0.8f, 0.88f, 0.96f, 0f);
            _lightningActive = false;

            switch (weather)
            {
                case FishingWeatherState.Clear:
                case FishingWeatherState.Sunny:
                    showSun = true;
                    cloudVisibleCount = 2;
                    skyTint = new Color(0.08f, 0.18f, 0.28f, 0.07f);
                    cloudColor = new Color(0.96f, 0.98f, 1f, 0.4f);
                    break;
                case FishingWeatherState.PartlyCloudy:
                    showSun = true;
                    cloudVisibleCount = 4;
                    skyTint = new Color(0.05f, 0.1f, 0.18f, 0.12f);
                    cloudColor = new Color(0.9f, 0.95f, 1f, 0.5f);
                    break;
                case FishingWeatherState.Clouds:
                case FishingWeatherState.Overcast:
                    cloudVisibleCount = 6;
                    skyTint = new Color(0.03f, 0.08f, 0.13f, 0.2f);
                    cloudColor = new Color(0.82f, 0.88f, 0.95f, 0.62f);
                    break;
                case FishingWeatherState.Foggy:
                    cloudVisibleCount = 5;
                    fogVisibleCount = Mathf.Max(2, _fogSprites.Count);
                    skyTint = new Color(0.04f, 0.09f, 0.14f, 0.2f);
                    globalFogTint = new Color(0.84f, 0.9f, 0.95f, 0.2f);
                    cloudColor = new Color(0.84f, 0.9f, 0.96f, 0.58f);
                    fogBandColor = new Color(0.82f, 0.89f, 0.96f, 0.36f);
                    break;
                case FishingWeatherState.Rain:
                    cloudVisibleCount = 6;
                    rainVisibleCount = Mathf.Max(12, Mathf.RoundToInt(_rainSprites.Count * 0.7f));
                    skyTint = new Color(0.02f, 0.07f, 0.13f, 0.27f);
                    cloudColor = new Color(0.78f, 0.86f, 0.94f, 0.67f);
                    rainColor = new Color(0.7f, 0.84f, 0.98f, 0.5f);
                    break;
                case FishingWeatherState.Thunderstorm:
                case FishingWeatherState.Storm:
                    cloudVisibleCount = Mathf.Max(4, Mathf.RoundToInt(_cloudSprites.Count * 0.85f));
                    rainVisibleCount = Mathf.Max(10, Mathf.RoundToInt(_rainSprites.Count * 0.85f));
                    fogVisibleCount = Mathf.Max(1, Mathf.RoundToInt(_fogSprites.Count * 0.33f));
                    skyTint = new Color(0.01f, 0.05f, 0.1f, 0.36f);
                    cloudColor = new Color(0.66f, 0.76f, 0.86f, 0.78f);
                    rainColor = new Color(0.72f, 0.85f, 0.98f, 0.62f);
                    fogBandColor = new Color(0.7f, 0.82f, 0.92f, 0.2f);
                    _lightningActive = true;
                    _nextLightningAt = Time.unscaledTime + Random.Range(3f, 6f);
                    break;
                case FishingWeatherState.QuarterMoon:
                    showMoon = true;
                    moonShadowOffsetX = 0.28f;
                    cloudVisibleCount = 3;
                    skyTint = new Color(0.01f, 0.02f, 0.08f, 0.37f);
                    cloudColor = new Color(0.65f, 0.74f, 0.87f, 0.34f);
                    break;
                case FishingWeatherState.HalfMoon:
                    showMoon = true;
                    moonShadowOffsetX = 0f;
                    cloudVisibleCount = 2;
                    skyTint = new Color(0.01f, 0.02f, 0.08f, 0.35f);
                    cloudColor = new Color(0.65f, 0.74f, 0.87f, 0.3f);
                    break;
                case FishingWeatherState.FullMoon:
                    showMoon = true;
                    moonShadowOffsetX = float.NaN;
                    cloudVisibleCount = 2;
                    skyTint = new Color(0.01f, 0.02f, 0.08f, 0.33f);
                    cloudColor = new Color(0.66f, 0.76f, 0.89f, 0.27f);
                    break;
                default:
                    showSun = true;
                    cloudVisibleCount = 2;
                    skyTint = new Color(0.08f, 0.18f, 0.28f, 0.07f);
                    cloudColor = new Color(0.96f, 0.98f, 1f, 0.4f);
                    break;
            }

            if (_skyTintOverlay != null)
            {
                _showSkyTintByWeather = skyTint.a > 0.001f;
                _skyTintColorByWeather = skyTint;
            }

            if (_globalFogOverlay != null)
            {
                _showGlobalFogByWeather = globalFogTint.a > 0.001f;
                _globalFogColorByWeather = globalFogTint;
            }

            if (_sunRenderer != null)
            {
                _showSunByWeather = showSun;
                _sunColorByWeather = new Color(1f, 0.94f, 0.72f, showSun ? 0.82f : 0f);
            }

            if (_moonRenderer != null)
            {
                _showMoonByWeather = showMoon;
                _moonColorByWeather = new Color(0.9f, 0.95f, 1f, showMoon ? 0.88f : 0f);
            }

            if (_moonShadowRenderer != null)
            {
                var moonColor = skyTint.a > 0f
                    ? new Color(Mathf.Clamp01(0.03f + skyTint.r), Mathf.Clamp01(0.03f + skyTint.g), Mathf.Clamp01(0.07f + skyTint.b), showMoon ? 0.92f : 0f)
                    : new Color(0.03f, 0.04f, 0.08f, showMoon ? 0.92f : 0f);
                _moonShadowColorByWeather = moonColor;
                _showMoonShadowByWeather = showMoon && !float.IsNaN(moonShadowOffsetX);
                if (_moonRenderer != null && !float.IsNaN(moonShadowOffsetX))
                {
                    _moonShadowRenderer.transform.localPosition = _moonRenderer.transform.localPosition + new Vector3(moonShadowOffsetX, 0f, 0f);
                }
            }

            ConfigureCloudVisuals(cloudVisibleCount, cloudColor);
            ConfigureRainVisuals(rainVisibleCount, rainColor);
            ConfigureFogVisuals(fogVisibleCount, fogBandColor);
            UpdateSkyElementVisibility();
            SyncConditionController(weather);

            if (scheduleNextChange)
            {
                ScheduleNextWeatherChange();
            }
        }

        private void ConfigureCloudVisuals(int visibleCount, Color cloudColor)
        {
            if (_cloudSprites.Count == 0 || _targetCamera == null)
            {
                return;
            }

            ResolveViewportBounds(out var halfWidth, out var halfHeight);
            var cameraX = _targetCamera.transform.position.x;
            ResolveSkyBandWorldRange(out var topMin, out var topMax);
            var skyVisibility = ResolveSkyVisibilityFactor(topMin, halfHeight);
            var skyBandVisible = skyVisibility > 0.001f;
            var seedMinX = cameraX - halfWidth - 4f;
            var seedMaxX = cameraX + halfWidth + 4f;
            for (var i = 0; i < _cloudSprites.Count; i++)
            {
                var cloud = _cloudSprites[i];
                if (cloud == null || cloud.Renderer == null)
                {
                    continue;
                }

                var enabled = i < Mathf.Clamp(visibleCount, 0, _cloudSprites.Count);
                cloud.WeatherEnabled = enabled;
                cloud.Renderer.enabled = enabled && skyBandVisible;
                if (!enabled)
                {
                    continue;
                }

                cloud.WeatherAlpha = cloudColor.a;
                cloud.Renderer.color = new Color(cloudColor.r, cloudColor.g, cloudColor.b, cloud.WeatherAlpha * skyVisibility);
                var scaleX = Random.Range(1.8f, 4f);
                var scaleY = Random.Range(0.45f, 1.15f);
                cloud.Renderer.size = new Vector2(scaleX, scaleY);
                cloud.Speed = Random.Range(0.025f, 0.085f);
                cloud.HorizontalDirection = -1f;
                cloud.BobAmplitude = Random.Range(0.008f, 0.05f);
                cloud.BobFrequency = Random.Range(0.12f, 0.42f);
                cloud.BaseY = Random.Range(topMin, topMax);
                cloud.Transform.position = new Vector3(
                    Random.Range(seedMinX, seedMaxX),
                    cloud.BaseY,
                    0f);
            }
        }

        private float ResolveSkyVisibilityFactor(float skyBandMinY, float halfHeight)
        {
            if (_targetCamera == null)
            {
                return 0f;
            }

            var cameraTopY = _targetCamera.transform.position.y + Mathf.Max(0.5f, halfHeight);
            var fadeStartY = skyBandMinY - Mathf.Max(0f, _skyVisibilityBelowBandBuffer);
            var fadeDistance = Mathf.Max(0.05f, _skyVisibilityFadeDistance);
            var fadeEndY = fadeStartY - fadeDistance;
            if (cameraTopY >= fadeStartY)
            {
                return 1f;
            }

            if (cameraTopY <= fadeEndY)
            {
                return 0f;
            }

            return Mathf.InverseLerp(fadeEndY, fadeStartY, cameraTopY);
        }

        private void UpdateSkyElementVisibility()
        {
            if (_targetCamera == null)
            {
                return;
            }

            ResolveViewportBounds(out _, out var halfHeight);
            ResolveSkyBandWorldRange(out var skyBandMinY, out _);
            _skyVisibilityFactor = ResolveSkyVisibilityFactor(skyBandMinY, halfHeight);
            _skyBandVisible = _skyVisibilityFactor > 0.001f;
            ApplySkyVisibilityToStaticElements();
        }

        private void ApplySkyVisibilityToStaticElements()
        {
            ApplyStaticWeatherVisibility(_skyTintOverlay, _showSkyTintByWeather, _skyTintColorByWeather);
            ApplyStaticWeatherVisibility(_globalFogOverlay, _showGlobalFogByWeather, _globalFogColorByWeather);
            ApplyStaticWeatherVisibility(_sunRenderer, _showSunByWeather, _sunColorByWeather);
            ApplyStaticWeatherVisibility(_moonRenderer, _showMoonByWeather, _moonColorByWeather);
            ApplyStaticWeatherVisibility(_moonShadowRenderer, _showMoonShadowByWeather, _moonShadowColorByWeather);
        }

        private void ApplyStaticWeatherVisibility(SpriteRenderer renderer, bool weatherEnabled, Color weatherColor)
        {
            if (renderer == null)
            {
                return;
            }

            var color = weatherColor;
            color.a *= Mathf.Clamp01(_skyVisibilityFactor);
            renderer.color = color;
            renderer.enabled = weatherEnabled && color.a > 0.001f;
        }

        private void ResolveSkyBandWorldRange(out float minY, out float maxY)
        {
            var minOffset = Mathf.Min(_skyBandHeightMeters.x, _skyBandHeightMeters.y);
            var maxOffset = Mathf.Max(_skyBandHeightMeters.x, _skyBandHeightMeters.y);
            maxOffset = Mathf.Max(minOffset + 0.5f, maxOffset);

            var surfaceY = ResolveSurfaceY();
            minY = surfaceY + minOffset;
            maxY = surfaceY + maxOffset;
        }

        private float ResolveSurfaceY()
        {
            if (_ship != null)
            {
                return _ship.position.y + _skySurfaceYOffset;
            }

            return _skySurfaceYOffset;
        }

        private void ConfigureRainVisuals(int visibleCount, Color rainColor)
        {
            if (_rainSprites.Count == 0 || _targetCamera == null)
            {
                return;
            }

            ResolveViewportBounds(out var halfWidth, out _);
            ResolveSkyBandWorldRange(out var skyBandMinY, out var skyBandMaxY);
            var skyVisibility = Mathf.Clamp01(_skyVisibilityFactor);
            var skyBandVisible = skyVisibility > 0.001f;
            var cameraX = _targetCamera.transform.position.x;
            var spawnMinX = cameraX - (halfWidth + 0.6f);
            var spawnMaxX = cameraX + (halfWidth + 0.6f);
            var spawnMinY = skyBandMinY - 1.2f;
            var spawnMaxY = skyBandMaxY + 1.6f;
            var clampedVisible = Mathf.Clamp(visibleCount, 0, _rainSprites.Count);
            for (var i = 0; i < _rainSprites.Count; i++)
            {
                var drop = _rainSprites[i];
                if (drop == null || drop.Renderer == null)
                {
                    continue;
                }

                var enabled = i < clampedVisible;
                drop.WeatherEnabled = enabled;
                drop.Renderer.enabled = enabled && skyBandVisible;
                if (!enabled)
                {
                    continue;
                }

                drop.WeatherAlpha = rainColor.a;
                drop.Renderer.color = new Color(rainColor.r, rainColor.g, rainColor.b, drop.WeatherAlpha * skyVisibility);
                drop.Renderer.size = new Vector2(Random.Range(0.015f, 0.03f), Random.Range(0.28f, 0.54f));
                drop.Speed = Random.Range(0.25f, 0.8f);
                drop.VerticalSpeed = Random.Range(2.6f, 5.4f);
                drop.Transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
                drop.Transform.position = new Vector3(
                    Random.Range(spawnMinX, spawnMaxX),
                    Random.Range(spawnMinY, spawnMaxY),
                    0f);
            }
        }

        private void ConfigureFogVisuals(int visibleCount, Color fogColor)
        {
            if (_fogSprites.Count == 0 || _targetCamera == null)
            {
                return;
            }

            ResolveViewportBounds(out var halfWidth, out var halfHeight);
            ResolveSkyBandWorldRange(out var skyBandMinY, out var skyBandMaxY);
            var skyVisibility = ResolveSkyVisibilityFactor(skyBandMinY, halfHeight);
            var skyBandVisible = skyVisibility > 0.001f;
            var cameraX = _targetCamera.transform.position.x;
            var spawnMinX = cameraX - (halfWidth + 1.5f);
            var spawnMaxX = cameraX + (halfWidth + 1.5f);
            var fogMinY = skyBandMinY - 1.1f;
            var fogMaxY = Mathf.Max(fogMinY + 0.8f, skyBandMinY + ((skyBandMaxY - skyBandMinY) * 0.45f));
            var clampedVisible = Mathf.Clamp(visibleCount, 0, _fogSprites.Count);
            for (var i = 0; i < _fogSprites.Count; i++)
            {
                var fog = _fogSprites[i];
                if (fog == null || fog.Renderer == null)
                {
                    continue;
                }

                var enabled = i < clampedVisible;
                fog.WeatherEnabled = enabled;
                fog.Renderer.enabled = enabled && skyBandVisible;
                if (!enabled)
                {
                    continue;
                }

                fog.WeatherAlpha = fogColor.a;
                fog.Renderer.color = new Color(fogColor.r, fogColor.g, fogColor.b, fog.WeatherAlpha * skyVisibility);
                fog.Renderer.size = new Vector2(Random.Range(halfWidth * 0.8f, halfWidth * 1.6f), Random.Range(0.4f, 0.95f));
                fog.Speed = Random.Range(0.05f, 0.18f);
                fog.BobAmplitude = Random.Range(0.03f, 0.1f);
                fog.BobFrequency = Random.Range(0.1f, 0.24f);
                fog.BaseY = Random.Range(fogMinY, fogMaxY);
                fog.Transform.position = new Vector3(
                    Random.Range(spawnMinX, spawnMaxX),
                    fog.BaseY,
                    0f);
            }
        }

        private void SyncConditionController(FishingWeatherState weather)
        {
            if (_conditionController == null)
            {
                return;
            }

            _conditionController.SetWeather(weather);
            if (IsMoonPhase(weather))
            {
                _conditionController.SetTimeOfDay(FishingTimeOfDay.Night);
            }
            else if (_conditionController.TimeOfDay == FishingTimeOfDay.Night)
            {
                _conditionController.SetTimeOfDay(FishingTimeOfDay.Day);
            }
        }

        private void ScheduleNextWeatherChange()
        {
            var minSeconds = Mathf.Max(20f, Mathf.Min(_weatherChangeIntervalSeconds.x, _weatherChangeIntervalSeconds.y));
            var maxSeconds = Mathf.Max(minSeconds, Mathf.Max(_weatherChangeIntervalSeconds.x, _weatherChangeIntervalSeconds.y));
            _nextWeatherChangeAt = Time.unscaledTime + Random.Range(minSeconds, maxSeconds);
        }

        private static bool IsMoonPhase(FishingWeatherState weather)
        {
            return weather == FishingWeatherState.QuarterMoon
                || weather == FishingWeatherState.HalfMoon
                || weather == FishingWeatherState.FullMoon;
        }
    }
}
