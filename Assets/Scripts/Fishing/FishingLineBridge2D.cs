using UnityEngine;

namespace RavenDevOps.Fishing.Fishing
{
    [DefaultExecutionOrder(900)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class FishingLineBridge2D : MonoBehaviour
    {
        [SerializeField] private Transform _ship;
        [SerializeField] private Transform _hook;
        [SerializeField] private float _lineThickness = 0.05f;
        [SerializeField] private float _shipOffsetY = -0.36f;
        [SerializeField] private float _minVisibleLength = 0.12f;
        [SerializeField] private bool _hideWhenTooShort = true;
        [SerializeField] private int _segmentCount = 12;
        [SerializeField] private float _sagAmount = 0.22f;
        [SerializeField] private float _waveAmplitude = 0.04f;
        [SerializeField] private float _waveFrequency = 2.4f;
        [SerializeField] private float _waveSpeed = 2.1f;
        [SerializeField] private float _shipDragSlackHorizontal = 1.1f;
        [SerializeField] private float _shipDragSlackSag = 0.3f;
        [SerializeField] private float _shipDragSlackMaxVelocity = 12f;
        [SerializeField] private float _shipVelocitySmoothing = 9f;
        [SerializeField] private float _shipDragTowardHookBias = 1.8f;
        [SerializeField, Range(0f, 1f)] private float _movingWaveSuppression = 0.85f;
        [SerializeField] private HookMovementController _hookMovement;
        [SerializeField] private float _depthCurvePer500Meters = 0.22f;
        [SerializeField] private float _maxDepthCurveMultiplier = 4.2f;
        [SerializeField] private float _minimumMovingLag = 0.3f;
        [SerializeField] private float _wakeLagResponse = 6.5f;
        [SerializeField] private float _wakeLagRecovery = 2.4f;
        [SerializeField] private float _wakeLagMaxDistance = 5f;
        [SerializeField] private float _movingVelocityThreshold = 0.08f;
        [SerializeField] private FishingSceneWeatherController _weatherController;
        [SerializeField] private float _weatherWaveToLineScale = 0.05f;
        [SerializeField] private float _weatherFogToLineScale = 0.035f;
        [SerializeField] private float _weatherLineBobFrequency = 0.62f;
        [SerializeField] private float _weatherLineBobSecondaryFrequency = 1.08f;

        private LineRenderer _renderer;
        private SpriteRenderer _hookRenderer;
        private ShipMovementController _shipMovement;
        private bool _hasRecordedShipX;
        private float _lastShipX;
        private float _smoothedShipVelocityX;
        private float _wakeLagOffsetX;
        private bool _hasLineWeatherPhase;
        private float _lineWeatherPhase;

        public void Configure(Transform ship, Transform hook, float lineThickness = 0.05f, float shipOffsetY = -0.36f)
        {
            _ship = ship;
            _hook = hook;
            _lineThickness = Mathf.Max(0.01f, lineThickness);
            _shipOffsetY = shipOffsetY;
            _hasRecordedShipX = false;
            _smoothedShipVelocityX = 0f;
            _wakeLagOffsetX = 0f;
            _hookMovement = hook != null ? hook.GetComponent<HookMovementController>() : _hookMovement;
        }

        private void Awake()
        {
            CacheRenderer();
            ConfigureRenderer();
            ResolveWeatherController();
            EnsureLineWeatherPhase();
        }

        private void OnValidate()
        {
            _lineThickness = Mathf.Max(0.01f, _lineThickness);
            _segmentCount = Mathf.Clamp(_segmentCount, 2, 32);
            _shipDragSlackHorizontal = Mathf.Max(0f, _shipDragSlackHorizontal);
            _shipDragSlackSag = Mathf.Max(0f, _shipDragSlackSag);
            _shipDragSlackMaxVelocity = Mathf.Max(0.1f, _shipDragSlackMaxVelocity);
            _shipVelocitySmoothing = Mathf.Max(0.1f, _shipVelocitySmoothing);
            _shipDragTowardHookBias = Mathf.Max(1f, _shipDragTowardHookBias);
            _movingWaveSuppression = Mathf.Clamp01(_movingWaveSuppression);
            _depthCurvePer500Meters = Mathf.Max(0f, _depthCurvePer500Meters);
            _maxDepthCurveMultiplier = Mathf.Max(1f, _maxDepthCurveMultiplier);
            _minimumMovingLag = Mathf.Max(0f, _minimumMovingLag);
            _wakeLagResponse = Mathf.Max(0.1f, _wakeLagResponse);
            _wakeLagRecovery = Mathf.Max(0.1f, _wakeLagRecovery);
            _wakeLagMaxDistance = Mathf.Max(0.01f, _wakeLagMaxDistance);
            _movingVelocityThreshold = Mathf.Max(0.001f, _movingVelocityThreshold);
            _weatherWaveToLineScale = Mathf.Max(0f, _weatherWaveToLineScale);
            _weatherFogToLineScale = Mathf.Max(0f, _weatherFogToLineScale);
            _weatherLineBobFrequency = Mathf.Max(0.01f, _weatherLineBobFrequency);
            _weatherLineBobSecondaryFrequency = Mathf.Max(0.01f, _weatherLineBobSecondaryFrequency);
            if (!Application.isPlaying)
            {
                CacheRenderer();
                ConfigureRenderer();
            }
        }

        private void LateUpdate()
        {
            CacheRenderer();
            if (_renderer == null || _ship == null || _hook == null)
            {
                return;
            }

            CacheHookRenderer();
            if (_hookRenderer != null && !_hookRenderer.enabled)
            {
                _renderer.enabled = false;
                return;
            }

            ResolveWeatherController();
            EnsureLineWeatherPhase();
            var start = _ship.position + new Vector3(0f, _shipOffsetY, 0f);
            start.y += ResolveWeatherLineBobOffsetY();
            var end = _hook.position;
            var delta = end - start;
            var length = delta.magnitude;

            if (_hideWhenTooShort && length < Mathf.Max(0.01f, _minVisibleLength))
            {
                _renderer.enabled = false;
                return;
            }

            _renderer.enabled = true;
            _renderer.startWidth = _lineThickness;
            _renderer.endWidth = Mathf.Max(0.005f, _lineThickness * 0.92f);

            var segments = Mathf.Clamp(_segmentCount, 2, 32);
            if (_renderer.positionCount != segments)
            {
                _renderer.positionCount = segments;
            }

            var direction = length > 0.0001f ? delta / length : Vector3.down;
            var normal = new Vector3(-direction.y, direction.x, 0f);
            var shipVelocityX = ResolveSmoothedShipVelocityX();
            var velocityRatio = Mathf.Clamp01(Mathf.Abs(shipVelocityX) / Mathf.Max(0.1f, _shipDragSlackMaxVelocity));
            var normalizedLength = Mathf.Clamp01(length / 3.5f);
            if (_hookMovement == null && _hook != null)
            {
                _hookMovement = _hook.GetComponent<HookMovementController>();
            }

            var depthMeters = ResolveDepthMeters(start, end);
            var depthGradientPer500m = Mathf.Max(0f, depthMeters / 500f);
            var depthCurveMultiplier = Mathf.Clamp(
                1f + (depthGradientPer500m * Mathf.Max(0f, _depthCurvePer500Meters)),
                1f,
                Mathf.Max(1f, _maxDepthCurveMultiplier));
            var depthCurveRatio = Mathf.Clamp01(
                (depthCurveMultiplier - 1f) / Mathf.Max(0.001f, Mathf.Max(1f, _maxDepthCurveMultiplier) - 1f));
            var velocityWakeBoost = 1f + (velocityRatio * 1.45f);
            var depthWakeBoost = 1f + (depthCurveRatio * depthCurveRatio * 2.2f);
            var wakeCurveMultiplier = velocityWakeBoost * depthWakeBoost;
            var sag = Mathf.Sin(Mathf.Clamp01(length / 4.5f) * Mathf.PI * 0.5f) * _sagAmount;
            sag += _shipDragSlackSag * velocityRatio * normalizedLength * Mathf.Lerp(1f, 1.35f, velocityRatio);
            sag *= depthCurveMultiplier;
            sag *= Mathf.Lerp(1f, 1.6f, depthCurveRatio);
            var waveMotionScale = Mathf.Lerp(1f, 1f - Mathf.Clamp01(_movingWaveSuppression), velocityRatio);
            var wave = _waveAmplitude * Mathf.Clamp01(length / 6f) * Mathf.Max(0.05f, waveMotionScale);
            wave *= Mathf.Lerp(1f, 1.18f, Mathf.Clamp01(depthCurveMultiplier - 1f));
            var wavePhase = Time.time * _waveSpeed;
            var movingThreshold = Mathf.Max(0.001f, _movingVelocityThreshold);
            var moving = Mathf.Abs(shipVelocityX) >= movingThreshold;
            var travelLagBias = moving
                ? Mathf.Max(0f, _minimumMovingLag) * normalizedLength * Mathf.Lerp(1f, 1.35f, depthCurveRatio)
                : 0f;
            var dynamicLag = _shipDragSlackHorizontal * velocityRatio * normalizedLength * depthCurveMultiplier * wakeCurveMultiplier;
            var targetLagMagnitude = Mathf.Clamp(
                travelLagBias + dynamicLag,
                0f,
                Mathf.Max(0.01f, _wakeLagMaxDistance));
            var targetLag = moving
                ? -Mathf.Sign(shipVelocityX) * targetLagMagnitude
                : 0f;
            var lagResponse = moving
                ? Mathf.Max(0.1f, _wakeLagResponse)
                : Mathf.Max(0.1f, _wakeLagRecovery);
            var lagBlend = 1f - Mathf.Exp(-lagResponse * Time.unscaledDeltaTime);
            _wakeLagOffsetX = Mathf.Lerp(_wakeLagOffsetX, targetLag, lagBlend);
            var dragOffsetX = _wakeLagOffsetX;
            var wakeLagRatio = Mathf.Clamp01(Mathf.Abs(_wakeLagOffsetX) / Mathf.Max(0.01f, _wakeLagMaxDistance));
            var dragBiasPower = Mathf.Max(1f, _shipDragTowardHookBias);

            for (var i = 0; i < segments; i++)
            {
                var t = segments <= 1 ? 0f : i / (segments - 1f);
                if (i == 0)
                {
                    _renderer.SetPosition(i, start);
                    continue;
                }

                if (i == segments - 1)
                {
                    _renderer.SetPosition(i, end);
                    continue;
                }

                var point = Vector3.Lerp(start, end, t);
                var centerWeight = Mathf.Sin(t * Mathf.PI);
                var dragWeight = centerWeight * Mathf.Pow(t, dragBiasPower) * Mathf.Lerp(1f, 1.85f, velocityRatio);
                point.y -= centerWeight * sag * Mathf.Lerp(1f, 1.28f, Mathf.Max(velocityRatio, wakeLagRatio));
                point.x += dragOffsetX * dragWeight;
                point += normal * (Mathf.Sin((t * _waveFrequency * Mathf.PI) + wavePhase) * wave * centerWeight);
                _renderer.SetPosition(i, point);
            }
        }

        private float ResolveSmoothedShipVelocityX()
        {
            if (_ship == null)
            {
                _hasRecordedShipX = false;
                _smoothedShipVelocityX = 0f;
                _wakeLagOffsetX = 0f;
                return 0f;
            }

            var shipX = _ship.position.x;
            if (!_hasRecordedShipX)
            {
                _hasRecordedShipX = true;
                _lastShipX = shipX;
                _smoothedShipVelocityX = 0f;
                return 0f;
            }

            var deltaTime = Mathf.Max(0.0001f, Time.deltaTime);
            if (_shipMovement == null && _ship != null)
            {
                _shipMovement = _ship.GetComponent<ShipMovementController>();
            }

            var measuredVelocityX = (shipX - _lastShipX) / deltaTime;
            var controllerVelocityX = _shipMovement != null
                ? _shipMovement.CurrentHorizontalVelocity
                : measuredVelocityX;
            var rawVelocityX = Mathf.Abs(measuredVelocityX) > Mathf.Abs(controllerVelocityX)
                ? measuredVelocityX
                : controllerVelocityX;
            _lastShipX = shipX;

            var blend = 1f - Mathf.Exp(-Mathf.Max(0.1f, _shipVelocitySmoothing) * deltaTime);
            _smoothedShipVelocityX = Mathf.Lerp(_smoothedShipVelocityX, rawVelocityX, blend);
            return _smoothedShipVelocityX;
        }

        private void CacheRenderer()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<LineRenderer>();
            }
        }

        private void CacheHookRenderer()
        {
            if (_hookRenderer == null && _hook != null)
            {
                _hookRenderer = _hook.GetComponent<SpriteRenderer>();
            }
        }

        private void ConfigureRenderer()
        {
            if (_renderer == null)
            {
                return;
            }

            _renderer.useWorldSpace = true;
            _renderer.textureMode = LineTextureMode.Stretch;
            _renderer.alignment = LineAlignment.View;
            _renderer.numCapVertices = 2;
            _renderer.numCornerVertices = 2;
            _renderer.positionCount = Mathf.Clamp(_segmentCount, 2, 32);
            _renderer.startWidth = _lineThickness;
            _renderer.endWidth = _lineThickness;
        }

        private float ResolveDepthMeters(Vector3 start, Vector3 end)
        {
            if (_hookMovement != null)
            {
                return Mathf.Max(0f, _hookMovement.CurrentDepth);
            }

            return Mathf.Max(0f, start.y - end.y);
        }

        private void ResolveWeatherController()
        {
            if (_weatherController != null)
            {
                return;
            }

            _weatherController = FindAnyObjectByType<FishingSceneWeatherController>(FindObjectsInactive.Exclude);
        }

        private void EnsureLineWeatherPhase()
        {
            if (_hasLineWeatherPhase)
            {
                return;
            }

            _lineWeatherPhase = Random.Range(0f, Mathf.PI * 2f);
            _hasLineWeatherPhase = true;
        }

        private float ResolveWeatherLineBobOffsetY()
        {
            if (_weatherController == null)
            {
                return 0f;
            }

            var waveMeters = Mathf.Max(0f, _weatherController.CurrentSurfaceWaveVerticalMeters);
            var fogMeters = Mathf.Max(0f, _weatherController.CurrentFogVerticalMeters);
            var primary = Mathf.Sin((Time.unscaledTime * Mathf.Max(0.01f, _weatherLineBobFrequency)) + _lineWeatherPhase)
                * waveMeters
                * Mathf.Max(0f, _weatherWaveToLineScale);
            var secondary = Mathf.Sin((Time.unscaledTime * Mathf.Max(0.01f, _weatherLineBobSecondaryFrequency)) + (_lineWeatherPhase * 1.37f))
                * fogMeters
                * Mathf.Max(0f, _weatherFogToLineScale);
            return primary + secondary;
        }
    }
}
