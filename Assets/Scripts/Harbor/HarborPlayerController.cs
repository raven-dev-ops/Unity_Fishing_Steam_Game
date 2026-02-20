using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Harbor
{
    public sealed class HarborPlayerController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private Vector2 _xBounds = new Vector2(-8f, 8f);
        [SerializeField] private Vector2 _zBounds = new Vector2(-4f, 4f);

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var move = Vector3.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.z += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.z -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;

            move = move.normalized * (_moveSpeed * Time.deltaTime);
            transform.position += move;

            var p = transform.position;
            p.x = Mathf.Clamp(p.x, Mathf.Min(_xBounds.x, _xBounds.y), Mathf.Max(_xBounds.x, _xBounds.y));
            p.z = Mathf.Clamp(p.z, Mathf.Min(_zBounds.x, _zBounds.y), Mathf.Max(_zBounds.x, _zBounds.y));
            transform.position = p;
        }
    }
}
