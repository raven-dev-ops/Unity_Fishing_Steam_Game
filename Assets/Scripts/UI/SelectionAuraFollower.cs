using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.UI
{
    public sealed class SelectionAuraFollower : MonoBehaviour
    {
        [SerializeField] private RectTransform _auraTransform;
        [SerializeField] private Vector2 _padding = new Vector2(18f, 12f);
        [SerializeField] private float _followSpeed = 18f;

        private RectTransform _target;

        public void Configure(RectTransform auraTransform, Vector2 padding, float followSpeed = 18f)
        {
            _auraTransform = auraTransform;
            _padding = new Vector2(Mathf.Max(0f, padding.x), Mathf.Max(0f, padding.y));
            _followSpeed = Mathf.Max(1f, followSpeed);
        }

        private void LateUpdate()
        {
            if (_auraTransform == null || EventSystem.current == null)
            {
                return;
            }

            var selected = EventSystem.current.currentSelectedGameObject;
            var selectedTransform = ResolveSelectedRectTransform(selected);
            if (selectedTransform == null)
            {
                _auraTransform.gameObject.SetActive(false);
                _target = null;
                return;
            }

            _target = selectedTransform;

            _auraTransform.gameObject.SetActive(true);
            var blend = 1f - Mathf.Exp(-Mathf.Max(1f, _followSpeed) * Time.unscaledDeltaTime);
            _auraTransform.position = Vector3.Lerp(
                _auraTransform.position,
                _target.position,
                blend);

            var desiredSize = _target.rect.size + (_padding * 2f);
            _auraTransform.sizeDelta = Vector2.Lerp(_auraTransform.sizeDelta, desiredSize, blend);
        }

        private static RectTransform ResolveSelectedRectTransform(GameObject selected)
        {
            if (selected == null)
            {
                return null;
            }

            var selectable = selected.GetComponent<Selectable>() ?? selected.GetComponentInParent<Selectable>();
            if (selectable != null)
            {
                return selectable.transform as RectTransform;
            }

            return selected.transform as RectTransform;
        }
    }
}
