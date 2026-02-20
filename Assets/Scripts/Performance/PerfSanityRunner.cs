using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.Performance
{
    public sealed class PerfSanityRunner : MonoBehaviour
    {
        [SerializeField] private Text _fpsLabel;
        [SerializeField] private int _sampleFrames = 120;

        private int _frameCounter;
        private float _timeCounter;

        private void Update()
        {
            _frameCounter++;
            _timeCounter += Time.unscaledDeltaTime;

            if (_frameCounter < _sampleFrames)
            {
                return;
            }

            var fps = _frameCounter / Mathf.Max(0.0001f, _timeCounter);
            if (_fpsLabel != null)
            {
                _fpsLabel.text = $"FPS: {fps:0.0}";
            }

            Debug.Log($"Perf sanity sample: {fps:0.0} FPS over {_frameCounter} frames");

            _frameCounter = 0;
            _timeCounter = 0f;
        }
    }
}
