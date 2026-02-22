using System.Collections.Generic;
using RavenDevOps.Fishing.Economy;
using RavenDevOps.Fishing.Harbor;
using RavenDevOps.Fishing.Save;
using UnityEngine;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.Core
{
    public sealed class HarborSceneInteractionRouter : MonoBehaviour
    {
        private static readonly string[] HookPriority = { "hook_lv3", "hook_lv2", "hook_lv1" };
        private static readonly string[] ShipPriority = { "ship_lv3", "ship_lv2", "ship_lv1" };

        [SerializeField] private List<WorldInteractable> _interactables = new List<WorldInteractable>();
        [SerializeField] private HookShopController _hookShop;
        [SerializeField] private BoatShopController _boatShop;
        [SerializeField] private FishShopController _fishShop;
        [SerializeField] private GameFlowOrchestrator _orchestrator;
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private Text _statusText;

        public void Configure(
            List<WorldInteractable> interactables,
            HookShopController hookShop,
            BoatShopController boatShop,
            FishShopController fishShop,
            Text statusText)
        {
            _interactables = interactables ?? new List<WorldInteractable>();
            _hookShop = hookShop;
            _boatShop = boatShop;
            _fishShop = fishShop;
            _statusText = statusText;
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _orchestrator, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
        }

        private void OnEnable()
        {
            BindInteractables();
            SetStatus("Harbor ready. Move with arrows/WASD and press Enter to interact.");
        }

        private void OnDisable()
        {
            UnbindInteractables();
        }

        private void BindInteractables()
        {
            for (var i = 0; i < _interactables.Count; i++)
            {
                var interactable = _interactables[i];
                if (interactable == null)
                {
                    continue;
                }

                interactable.Interacted -= HandleInteractable;
                interactable.Interacted += HandleInteractable;
            }
        }

        private void UnbindInteractables()
        {
            for (var i = 0; i < _interactables.Count; i++)
            {
                var interactable = _interactables[i];
                if (interactable == null)
                {
                    continue;
                }

                interactable.Interacted -= HandleInteractable;
            }
        }

        private void HandleInteractable(WorldInteractable interactable)
        {
            if (interactable == null)
            {
                return;
            }

            switch (interactable.Type)
            {
                case InteractableType.HookShop:
                    HandleHookShop();
                    return;
                case InteractableType.BoatShop:
                    HandleBoatShop();
                    return;
                case InteractableType.FishShop:
                    HandleFishShop();
                    return;
                case InteractableType.Sail:
                    SetStatus("Casting off for fishing grounds...");
                    _orchestrator?.RequestOpenFishing();
                    return;
                default:
                    SetStatus("No interaction configured.");
                    return;
            }
        }

        private void HandleHookShop()
        {
            if (_hookShop == null)
            {
                SetStatus("Hook shop is unavailable.");
                return;
            }

            for (var i = 0; i < HookPriority.Length; i++)
            {
                var hookId = HookPriority[i];
                if (_hookShop.BuyOrEquip(hookId))
                {
                    SetStatus($"Hook equipped: {hookId}. Balance: {CurrentCopecs()} copecs.");
                    return;
                }
            }

            SetStatus("Could not buy/equip any hook (locked or not enough copecs).");
        }

        private void HandleBoatShop()
        {
            if (_boatShop == null)
            {
                SetStatus("Boat shop is unavailable.");
                return;
            }

            for (var i = 0; i < ShipPriority.Length; i++)
            {
                var shipId = ShipPriority[i];
                if (_boatShop.BuyOrEquip(shipId))
                {
                    SetStatus($"Boat equipped: {shipId}. Balance: {CurrentCopecs()} copecs.");
                    return;
                }
            }

            SetStatus("Could not buy/equip any boat (locked or not enough copecs).");
        }

        private void HandleFishShop()
        {
            if (_fishShop == null)
            {
                SetStatus("Fish shop is unavailable.");
                return;
            }

            var earned = _fishShop.SellAll();
            SetStatus($"Sold catch for {earned} copecs. Balance: {CurrentCopecs()} copecs.");
        }

        private int CurrentCopecs()
        {
            return _saveManager != null && _saveManager.Current != null
                ? _saveManager.Current.copecs
                : 0;
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message ?? string.Empty;
            }
        }
    }
}
