using RavenDevOps.Fishing.Core;
using UnityEngine;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class WaveAnimator : MonoBehaviour
    {
        [SerializeField] private Transform _waveLayerA;
        [SerializeField] private Transform _waveLayerB;
        [SerializeField] private float _waveSpeedA = 0.3f;
        [SerializeField] private float _waveSpeedB = 0.6f;

        private void Awake()
        {
            RuntimeServiceRegistry.Register(this);
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }

        public void SetWaveSpeeds(float waveSpeedA, float waveSpeedB)
        {
            _waveSpeedA = waveSpeedA;
            _waveSpeedB = waveSpeedB;
        }

        private void Update()
        {
            AnimateLayer(_waveLayerA, _waveSpeedA);
            AnimateLayer(_waveLayerB, _waveSpeedB);
        }

        private static void AnimateLayer(Transform layer, float speed)
        {
            if (layer == null)
            {
                return;
            }

            var p = layer.localPosition;
            p.x += speed * Time.deltaTime;
            if (p.x > 1000f)
            {
                p.x = 0f;
            }

            layer.localPosition = p;
        }
    }
}
