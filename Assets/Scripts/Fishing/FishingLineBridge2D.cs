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

        private LineRenderer _renderer;
        private SpriteRenderer _hookRenderer;
        private ShipMovementController _shipMovement;
        private bool _hasRecordedShipX;
        private float _lastShipX;
        private float _smoothedShipVelocityX;

        public void Configure(Transform ship, Transform hook, float lineThickness = 0.05f, float shipOffsetY = -0.36f)
        {
            _ship = ship;
            _hook = hook;
            _lineThickness = Mathf.Max(0.01f, lineThickness);
            _shipOffsetY = shipOffsetY;
            _hasRecordedShipX = false;
            _smoothedShipVelocityX = 0f;
        }

        private void Awake()
        {
            CacheRenderer();
            ConfigureRenderer();
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

            var start = _ship.position + new Vector3(0f, _shipOffsetY, 0f);
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
            var sag = Mathf.Sin(Mathf.Clamp01(length / 4.5f) * Mathf.PI * 0.5f) * _sagAmount;
            sag += _shipDragSlackSag * velocityRatio * normalizedLength;
            var waveMotionScale = Mathf.Lerp(1f, 1f - Mathf.Clamp01(_movingWaveSuppression), velocityRatio);
            var wave = _waveAmplitude * Mathf.Clamp01(length / 6f) * Mathf.Max(0.05f, waveMotionScale);
            var wavePhase = Time.time * _waveSpeed;
            var dragOffsetX = -Mathf.Sign(shipVelocityX) * _shipDragSlackHorizontal * velocityRatio * normalizedLength;
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
                var dragWeight = centerWeight * Mathf.Pow(t, dragBiasPower);
                point.y -= centerWeight * sag;
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

            var rawVelocityX = _shipMovement != null
                ? _shipMovement.CurrentHorizontalVelocity
                : (shipX - _lastShipX) / deltaTime;
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
    }
}
