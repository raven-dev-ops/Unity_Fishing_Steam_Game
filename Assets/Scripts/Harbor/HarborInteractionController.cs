using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using RavenDevOps.Fishing.UI;

namespace RavenDevOps.Fishing.Harbor
{
    public sealed class HarborInteractionController : MonoBehaviour
    {
        [SerializeField] private Transform _player;
        [SerializeField] private float _interactRange = 2.5f;
        [SerializeField] private Transform _worldAura;
        [SerializeField] private List<WorldInteractable> _interactables = new List<WorldInteractable>();
        [SerializeField] private MermaidTutorialController _tutorial;

        private WorldInteractable _active;

        private void Awake()
        {
            if (_interactables.Count == 0)
            {
                _interactables.AddRange(FindObjectsOfType<WorldInteractable>(true));
            }
        }

        private void Update()
        {
            if (_tutorial != null && _tutorial.IsBlockingInteractions)
            {
                SetActive(null);
                return;
            }

            RefreshActiveInteractable();

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame && _active != null)
            {
                _active.Interact();
            }
        }

        private void RefreshActiveInteractable()
        {
            if (_player == null)
            {
                return;
            }

            WorldInteractable best = null;
            var bestDistance = float.MaxValue;

            foreach (var interactable in _interactables)
            {
                if (interactable == null)
                {
                    continue;
                }

                var distance = Vector3.Distance(_player.position, interactable.transform.position);
                if (distance <= _interactRange && distance < bestDistance)
                {
                    best = interactable;
                    bestDistance = distance;
                }
            }

            SetActive(best);
        }

        private void SetActive(WorldInteractable next)
        {
            if (_active == next)
            {
                return;
            }

            if (_active != null)
            {
                _active.SetHighlighted(false);
            }

            _active = next;
            if (_active != null)
            {
                _active.SetHighlighted(true);
            }

            if (_worldAura != null)
            {
                _worldAura.gameObject.SetActive(_active != null);
                if (_active != null)
                {
                    _worldAura.position = _active.AuraAnchor.position;
                }
            }
        }
    }
}
