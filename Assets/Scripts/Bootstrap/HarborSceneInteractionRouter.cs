using System;
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
        private const int MaxActivityLines = 4;

        [SerializeField] private List<WorldInteractable> _interactables = new List<WorldInteractable>();
        [SerializeField] private HookShopController _hookShop;
        [SerializeField] private BoatShopController _boatShop;
        [SerializeField] private FishShopController _fishShop;
        [SerializeField] private HarborInteractionController _interactionController;
        [SerializeField] private GameFlowOrchestrator _orchestrator;
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _selectionText;
        [SerializeField] private Text _economyText;
        [SerializeField] private Text _equipmentText;
        [SerializeField] private Text _cargoText;
        [SerializeField] private Text _activityLogText;

        private readonly Queue<string> _recentActivity = new Queue<string>();

        public void Configure(
            List<WorldInteractable> interactables,
            HookShopController hookShop,
            BoatShopController boatShop,
            FishShopController fishShop,
            Text statusText,
            Text selectionText = null,
            Text economyText = null,
            Text equipmentText = null,
            Text cargoText = null,
            Text activityLogText = null,
            HarborInteractionController interactionController = null)
        {
            _interactables = interactables ?? new List<WorldInteractable>();
            _hookShop = hookShop;
            _boatShop = boatShop;
            _fishShop = fishShop;
            _statusText = statusText;
            _selectionText = selectionText;
            _economyText = economyText;
            _equipmentText = equipmentText;
            _cargoText = cargoText;
            _activityLogText = activityLogText;
            _interactionController = interactionController;
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _orchestrator, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            _interactionController ??= GetComponent<HarborInteractionController>();
        }

        private void OnEnable()
        {
            BindInteractables();
            BindRuntimeEvents();
            SetStatus("Harbor ready. Move with arrows/WASD and press Enter to interact.");
            PushActivity("Harbor systems online.");
            RefreshSaveSnapshot();
            RefreshSelectionHint(_interactionController != null ? _interactionController.ActiveInteractable : null);
        }

        private void OnDisable()
        {
            UnbindRuntimeEvents();
            UnbindInteractables();
        }

        public void OnHookShopRequested()
        {
            HandleHookShop();
        }

        public void OnBoatShopRequested()
        {
            HandleBoatShop();
        }

        public void OnFishShopRequested()
        {
            HandleFishShop();
        }

        public void OnSailRequested()
        {
            HandleSail();
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

        private void BindRuntimeEvents()
        {
            if (_saveManager != null)
            {
                _saveManager.SaveDataChanged -= HandleSaveDataChanged;
                _saveManager.SaveDataChanged += HandleSaveDataChanged;
            }

            if (_interactionController != null)
            {
                _interactionController.ActiveInteractableChanged -= HandleActiveInteractableChanged;
                _interactionController.ActiveInteractableChanged += HandleActiveInteractableChanged;
            }
        }

        private void UnbindRuntimeEvents()
        {
            if (_saveManager != null)
            {
                _saveManager.SaveDataChanged -= HandleSaveDataChanged;
            }

            if (_interactionController != null)
            {
                _interactionController.ActiveInteractableChanged -= HandleActiveInteractableChanged;
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
                    HandleSail();
                    return;
                default:
                    SetStatus("No interaction configured.");
                    PushActivity("No interaction configured.");
                    return;
            }
        }

        private void HandleHookShop()
        {
            if (_hookShop == null)
            {
                SetStatus("Hook shop is unavailable.");
                PushActivity("Hook shop unavailable.");
                return;
            }

            for (var i = 0; i < HookPriority.Length; i++)
            {
                var hookId = HookPriority[i];
                if (_hookShop.BuyOrEquip(hookId))
                {
                    var label = ToDisplayLabel(hookId);
                    SetStatus($"Hook equipped: {label}. Balance: {CurrentCopecs()} copecs.");
                    PushActivity($"Hook shop: equipped {label}.");
                    RefreshSaveSnapshot();
                    return;
                }
            }

            SetStatus("Could not buy/equip any hook (locked or not enough copecs).");
            PushActivity("Hook shop: no eligible upgrades.");
            RefreshSaveSnapshot();
        }

        private void HandleBoatShop()
        {
            if (_boatShop == null)
            {
                SetStatus("Boat shop is unavailable.");
                PushActivity("Boat shop unavailable.");
                return;
            }

            for (var i = 0; i < ShipPriority.Length; i++)
            {
                var shipId = ShipPriority[i];
                if (_boatShop.BuyOrEquip(shipId))
                {
                    var label = ToDisplayLabel(shipId);
                    SetStatus($"Boat equipped: {label}. Balance: {CurrentCopecs()} copecs.");
                    PushActivity($"Boat shop: equipped {label}.");
                    RefreshSaveSnapshot();
                    return;
                }
            }

            SetStatus("Could not buy/equip any boat (locked or not enough copecs).");
            PushActivity("Boat shop: no eligible upgrades.");
            RefreshSaveSnapshot();
        }

        private void HandleFishShop()
        {
            if (_fishShop == null)
            {
                SetStatus("Fish shop is unavailable.");
                PushActivity("Fish shop unavailable.");
                return;
            }

            var earned = _fishShop.SellAll();
            SetStatus($"Sold catch for {earned} copecs. Balance: {CurrentCopecs()} copecs.");
            PushActivity($"Fish market: sold cargo for {earned} copecs.");
            RefreshSaveSnapshot();
        }

        private void HandleSail()
        {
            SetStatus("Casting off for fishing grounds...");
            PushActivity("Departure confirmed. Sailing to fishing waters.");
            _orchestrator?.RequestOpenFishing();
        }

        private int CurrentCopecs()
        {
            return _saveManager != null && _saveManager.Current != null
                ? _saveManager.Current.copecs
                : 0;
        }

        private void HandleSaveDataChanged(SaveDataV1 _)
        {
            RefreshSaveSnapshot();
        }

        private void HandleActiveInteractableChanged(WorldInteractable interactable)
        {
            RefreshSelectionHint(interactable);
        }

        private void RefreshSaveSnapshot()
        {
            if (_saveManager == null || _saveManager.Current == null)
            {
                SetText(_economyText, "Copecs: unavailable");
                SetText(_equipmentText, "Gear: unavailable");
                SetText(_cargoText, "Cargo: unavailable");
                return;
            }

            var save = _saveManager.Current;
            var fishCount = 0;
            if (save.fishInventory != null)
            {
                for (var i = 0; i < save.fishInventory.Count; i++)
                {
                    var entry = save.fishInventory[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    fishCount += Mathf.Max(0, entry.count);
                }
            }

            var totalTrips = save.stats != null ? Mathf.Max(0, save.stats.totalTrips) : 0;
            SetText(_economyText, $"Copecs: {Mathf.Max(0, save.copecs)}");
            SetText(_equipmentText, $"Equipped Ship: {ToDisplayLabel(save.equippedShipId)} | Hook: {ToDisplayLabel(save.equippedHookId)}");
            SetText(_cargoText, $"Cargo: {fishCount} fish | Trips: {totalTrips} | Level: {_saveManager.CurrentLevel}");
        }

        private void RefreshSelectionHint(WorldInteractable interactable)
        {
            if (_selectionText == null)
            {
                return;
            }

            if (interactable == null)
            {
                _selectionText.text = "Nearby target: none. Move near market stalls or the dock.";
                return;
            }

            var message = interactable.Type switch
            {
                InteractableType.HookShop => "Nearby target: Hook Shop. Press Enter to upgrade or equip hooks.",
                InteractableType.BoatShop => "Nearby target: Boat Shop. Press Enter to purchase or equip ships.",
                InteractableType.FishShop => "Nearby target: Fish Market. Press Enter to sell all fish cargo.",
                InteractableType.Sail => "Nearby target: Dock. Press Enter to sail to fishing waters.",
                _ => "Nearby target: Interaction available. Press Enter."
            };

            _selectionText.text = message;
        }

        private void PushActivity(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            _recentActivity.Enqueue(message.Trim());
            while (_recentActivity.Count > MaxActivityLines)
            {
                _recentActivity.Dequeue();
            }

            if (_activityLogText == null)
            {
                return;
            }

            var entries = _recentActivity.ToArray();
            var output = "Recent Activity:";
            for (var i = entries.Length - 1; i >= 0; i--)
            {
                output += $"\n- {entries[i]}";
            }

            _activityLogText.text = output;
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message ?? string.Empty;
            }
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        private static string ToDisplayLabel(string rawId)
        {
            if (string.IsNullOrWhiteSpace(rawId))
            {
                return "None";
            }

            var tokens = rawId.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                return rawId;
            }

            for (var i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                if (token.Length == 0)
                {
                    continue;
                }

                if (token.Length == 1)
                {
                    tokens[i] = char.ToUpperInvariant(token[0]).ToString();
                    continue;
                }

                tokens[i] = char.ToUpperInvariant(token[0]) + token.Substring(1);
            }

            return string.Join(" ", tokens);
        }
    }
}
