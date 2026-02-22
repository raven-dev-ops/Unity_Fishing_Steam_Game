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

        private LineRenderer _renderer;

        public void Configure(Transform ship, Transform hook, float lineThickness = 0.05f, float shipOffsetY = -0.36f)
        {
            _ship = ship;
            _hook = hook;
            _lineThickness = Mathf.Max(0.01f, lineThickness);
            _shipOffsetY = shipOffsetY;
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
            var sag = Mathf.Sin(Mathf.Clamp01(length / 4.5f) * Mathf.PI * 0.5f) * _sagAmount;
            var wave = _waveAmplitude * Mathf.Clamp01(length / 6f);
            var wavePhase = Time.time * _waveSpeed;

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
                point.y -= centerWeight * sag;
                point += normal * (Mathf.Sin((t * _waveFrequency * Mathf.PI) + wavePhase) * wave * centerWeight);
                _renderer.SetPosition(i, point);
            }
        }

        private void CacheRenderer()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<LineRenderer>();
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
