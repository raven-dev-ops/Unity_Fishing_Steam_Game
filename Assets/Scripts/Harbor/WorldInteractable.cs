using System;
using UnityEngine;

namespace RavenDevOps.Fishing.Harbor
{
    public enum InteractableType
    {
        HookShop = 0,
        BoatShop = 1,
        FishShop = 2,
        Sail = 3,
        Other = 4
    }

    public sealed class WorldInteractable : MonoBehaviour
    {
        [SerializeField] private InteractableType _interactableType = InteractableType.Other;
        [SerializeField] private Transform _auraAnchor;
        [SerializeField] private GameObject _highlightVisual;

        public InteractableType Type => _interactableType;
        public Transform AuraAnchor => _auraAnchor != null ? _auraAnchor : transform;

        public event Action<WorldInteractable> Interacted;

        public void SetHighlighted(bool highlighted)
        {
            if (_highlightVisual != null)
            {
                _highlightVisual.SetActive(highlighted);
            }
        }

        public void Interact()
        {
            Interacted?.Invoke(this);
        }
    }
}
