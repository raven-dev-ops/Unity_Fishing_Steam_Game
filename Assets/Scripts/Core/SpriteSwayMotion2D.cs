using UnityEngine;

namespace RavenDevOps.Fishing.Core
{
    [DisallowMultipleComponent]
    public sealed class SpriteSwayMotion2D : MonoBehaviour
    {
        [SerializeField] private Vector2 _positionAmplitude = new Vector2(0.08f, 0.05f);
        [SerializeField, Min(0f)] private float _positionFrequency = 0.35f;
        [SerializeField, Min(0f)] private float _rotationAmplitude = 2.5f;
        [SerializeField, Min(0f)] private float _rotationFrequency = 0.28f;
        [SerializeField, Min(0f)] private float _scaleAmplitude = 0.02f;
        [SerializeField, Min(0f)] private float _scaleFrequency = 0.24f;
        [SerializeField] private bool _useUnscaledTime;
        [SerializeField] private float _phaseOffset;

        private Vector3 _baseLocalPosition;
        private Quaternion _baseLocalRotation;
        private Vector3 _baseLocalScale;

        public void Configure(
            Vector2 positionAmplitude,
            float positionFrequency,
            float rotationAmplitude,
            float rotationFrequency,
            float scaleAmplitude,
            float scaleFrequency,
            float phaseOffset = 0f,
            bool useUnscaledTime = false)
        {
            _positionAmplitude = positionAmplitude;
            _positionFrequency = Mathf.Max(0f, positionFrequency);
            _rotationAmplitude = Mathf.Max(0f, rotationAmplitude);
            _rotationFrequency = Mathf.Max(0f, rotationFrequency);
            _scaleAmplitude = Mathf.Max(0f, scaleAmplitude);
            _scaleFrequency = Mathf.Max(0f, scaleFrequency);
            _phaseOffset = phaseOffset;
            _useUnscaledTime = useUnscaledTime;
        }

        private void Awake()
        {
            CacheBaseTransform();
        }

        private void OnEnable()
        {
            CacheBaseTransform();
        }

        private void Update()
        {
            var time = (_useUnscaledTime ? Time.unscaledTime : Time.time) + _phaseOffset;

            var xOffset = Mathf.Sin(time * Mathf.PI * 2f * _positionFrequency) * _positionAmplitude.x;
            var yOffset = Mathf.Cos(time * Mathf.PI * 2f * _positionFrequency * 0.75f) * _positionAmplitude.y;
            transform.localPosition = _baseLocalPosition + new Vector3(xOffset, yOffset, 0f);

            var rotation = Mathf.Sin(time * Mathf.PI * 2f * _rotationFrequency) * _rotationAmplitude;
            transform.localRotation = _baseLocalRotation * Quaternion.Euler(0f, 0f, rotation);

            var scaleWave = Mathf.Sin(time * Mathf.PI * 2f * _scaleFrequency) * _scaleAmplitude;
            var scaleMultiplier = 1f + scaleWave;
            transform.localScale = new Vector3(
                _baseLocalScale.x * scaleMultiplier,
                _baseLocalScale.y * scaleMultiplier,
                _baseLocalScale.z);
        }

        private void CacheBaseTransform()
        {
            _baseLocalPosition = transform.localPosition;
            _baseLocalRotation = transform.localRotation;
            _baseLocalScale = transform.localScale;
        }
    }
}
