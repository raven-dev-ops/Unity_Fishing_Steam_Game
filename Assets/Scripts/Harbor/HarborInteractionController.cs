using System;
using System.Collections.Generic;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RavenDevOps.Fishing.Harbor
{
    public sealed class HarborInteractionController : MonoBehaviour
    {
        [SerializeField] private Transform _player;
        [SerializeField] private float _interactRange = 2.5f;
        [SerializeField] private Transform _worldAura;
        [SerializeField] private List<WorldInteractable> _interactables = new List<WorldInteractable>();
        [SerializeField] private MermaidTutorialController _tutorial;
        [SerializeField] private InputActionMapController _inputMapController;

        private WorldInteractable _active;
        private InputAction _interactAction;
        public WorldInteractable ActiveInteractable => _active;

        public event Action<WorldInteractable> ActiveInteractableChanged;

        public void Configure(
            Transform player,
            Transform worldAura,
            List<WorldInteractable> interactables,
            MermaidTutorialController tutorial = null)
        {
            _player = player;
            _worldAura = worldAura;
            _tutorial = tutorial;
            _interactables = interactables ?? new List<WorldInteractable>();
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _inputMapController, this, warnIfMissing: false);

            if (_interactables.Count == 0)
            {
                _interactables.AddRange(FindObjectsByType<WorldInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None));
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
            RefreshInteractActionIfNeeded();

            if (_active != null && _interactAction != null && _interactAction.WasPressedThisFrame())
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

            ActiveInteractableChanged?.Invoke(_active);

            if (_worldAura != null)
            {
                _worldAura.gameObject.SetActive(_active != null);
                if (_active != null)
                {
                    _worldAura.position = _active.AuraAnchor.position;
                }
            }
        }

        private void RefreshInteractActionIfNeeded()
        {
            if (_interactAction != null)
            {
                return;
            }

            _interactAction = _inputMapController != null
                ? _inputMapController.FindAction("Harbor/Interact")
                : null;
        }
    }
}
