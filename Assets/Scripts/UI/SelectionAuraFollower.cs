using UnityEngine;
using UnityEngine.EventSystems;

namespace RavenDevOps.Fishing.UI
{
    public sealed class SelectionAuraFollower : MonoBehaviour
    {
        [SerializeField] private RectTransform _auraTransform;
        [SerializeField] private float _followSpeed = 18f;

        private RectTransform _target;

        private void LateUpdate()
        {
            if (_auraTransform == null || EventSystem.current == null)
            {
                return;
            }

            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null)
            {
                _auraTransform.gameObject.SetActive(false);
                _target = null;
                return;
            }

            _target = selected.transform as RectTransform;
            if (_target == null)
            {
                _auraTransform.gameObject.SetActive(false);
                return;
            }

            _auraTransform.gameObject.SetActive(true);
            _auraTransform.position = Vector3.Lerp(
                _auraTransform.position,
                _target.position,
                1f - Mathf.Exp(-_followSpeed * Time.unscaledDeltaTime));
        }
    }
}
