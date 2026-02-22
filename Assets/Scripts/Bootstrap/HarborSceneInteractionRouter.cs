using System;
using System.Collections.Generic;
using RavenDevOps.Fishing.Economy;
using RavenDevOps.Fishing.Harbor;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.Core
{
    public sealed class HarborSceneInteractionRouter : MonoBehaviour
    {
        private enum ShopMenuType
        {
            None = 0,
            Hook = 1,
            Boat = 2,
            Fish = 3
        }

        private const int MaxActivityLines = 4;
        private static readonly string[] HookMenuOrder = { "hook_lv1", "hook_lv2", "hook_lv3" };
        private static readonly string[] ShipMenuOrder = { "ship_lv1", "ship_lv2", "ship_lv3" };

        [SerializeField] private List<WorldInteractable> _interactables = new List<WorldInteractable>();
        [SerializeField] private HookShopController _hookShop;
        [SerializeField] private BoatShopController _boatShop;
        [SerializeField] private FishShopController _fishShop;
        [SerializeField] private HarborInteractionController _interactionController;
        [SerializeField] private GameFlowOrchestrator _orchestrator;
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private CatalogService _catalogService;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _selectionText;
        [SerializeField] private Text _economyText;
        [SerializeField] private Text _equipmentText;
        [SerializeField] private Text _cargoText;
        [SerializeField] private Text _activityLogText;
        [SerializeField] private GameObject _actionPanel;
        [SerializeField] private GameObject _hookShopPanel;
        [SerializeField] private GameObject _boatShopPanel;
        [SerializeField] private GameObject _fishShopPanel;
        [SerializeField] private Text _hookShopInfoText;
        [SerializeField] private Text _boatShopInfoText;
        [SerializeField] private Text _fishShopInfoText;
        [SerializeField] private GameObject _mainMenuDefaultSelection;
        [SerializeField] private GameObject _hookShopDefaultSelection;
        [SerializeField] private GameObject _boatShopDefaultSelection;
        [SerializeField] private GameObject _fishShopDefaultSelection;
        [SerializeField] private int _fallbackCargoCapacityTier1 = 12;
        [SerializeField] private int _fallbackCargoCapacityTier2 = 20;
        [SerializeField] private int _fallbackCargoCapacityTier3 = 32;

        private readonly Queue<string> _recentActivity = new Queue<string>();
        private ShopMenuType _activeMenu = ShopMenuType.None;

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
            HarborInteractionController interactionController = null,
            GameObject actionPanel = null,
            GameObject hookShopPanel = null,
            GameObject boatShopPanel = null,
            GameObject fishShopPanel = null,
            Text hookShopInfoText = null,
            Text boatShopInfoText = null,
            Text fishShopInfoText = null,
            GameObject mainMenuDefaultSelection = null,
            GameObject hookShopDefaultSelection = null,
            GameObject boatShopDefaultSelection = null,
            GameObject fishShopDefaultSelection = null)
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
            _actionPanel = actionPanel;
            _hookShopPanel = hookShopPanel;
            _boatShopPanel = boatShopPanel;
            _fishShopPanel = fishShopPanel;
            _hookShopInfoText = hookShopInfoText;
            _boatShopInfoText = boatShopInfoText;
            _fishShopInfoText = fishShopInfoText;
            _mainMenuDefaultSelection = mainMenuDefaultSelection;
            _hookShopDefaultSelection = hookShopDefaultSelection;
            _boatShopDefaultSelection = boatShopDefaultSelection;
            _fishShopDefaultSelection = fishShopDefaultSelection;
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _orchestrator, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
            _interactionController ??= GetComponent<HarborInteractionController>();
        }

        private void OnEnable()
        {
            BindInteractables();
            BindRuntimeEvents();
            CloseShopMenus(selectMainAction: false);
            SetStatus("Harbor ready. Move with arrows/WASD and press Enter to interact.");
            PushActivity("Harbor systems online.");
            RefreshSaveSnapshot();
            RefreshShopMenuDetails();
            RefreshSelectionHint(_interactionController != null ? _interactionController.ActiveInteractable : null);
        }

        private void OnDisable()
        {
            UnbindRuntimeEvents();
            UnbindInteractables();
        }

        public void OnHookShopRequested()
        {
            OpenHookShopMenu();
        }

        public void OnBoatShopRequested()
        {
            OpenBoatShopMenu();
        }

        public void OnFishShopRequested()
        {
            OpenFishShopMenu();
        }

        public void OnHookShopItemRequested(string hookId)
        {
            HandleHookShopSelection(hookId);
        }

        public void OnBoatShopItemRequested(string shipId)
        {
            HandleBoatShopSelection(shipId);
        }

        public void OnFishShopSellRequested()
        {
            HandleFishShopSale();
        }

        public void OnShopBackRequested()
        {
            if (_activeMenu == ShopMenuType.None)
            {
                return;
            }

            CloseShopMenus(selectMainAction: true);
            SetStatus("Harbor operations menu ready.");
            PushActivity("Returned to harbor operations.");
        }

        public void OnSailRequested()
        {
            CloseShopMenus(selectMainAction: false);
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
                    OpenHookShopMenu();
                    return;
                case InteractableType.BoatShop:
                    OpenBoatShopMenu();
                    return;
                case InteractableType.FishShop:
                    OpenFishShopMenu();
                    return;
                case InteractableType.Sail:
                    OnSailRequested();
                    return;
                default:
                    SetStatus("No interaction configured.");
                    PushActivity("No interaction configured.");
                    return;
            }
        }

        private void OpenHookShopMenu()
        {
            if (_hookShop == null)
            {
                SetStatus("Hook shop is unavailable.");
                PushActivity("Hook shop unavailable.");
                return;
            }

            OpenShopMenu(
                ShopMenuType.Hook,
                _hookShopPanel,
                _hookShopDefaultSelection,
                "Hook shop open. Select a hook to buy or equip.");
            PushActivity("Opened hook shop.");
            RefreshHookShopDetails();
        }

        private void OpenBoatShopMenu()
        {
            if (_boatShop == null)
            {
                SetStatus("Boat shop is unavailable.");
                PushActivity("Boat shop unavailable.");
                return;
            }

            OpenShopMenu(
                ShopMenuType.Boat,
                _boatShopPanel,
                _boatShopDefaultSelection,
                "Boat shop open. Select a ship to buy or equip.");
            PushActivity("Opened boat shop.");
            RefreshBoatShopDetails();
        }

        private void OpenFishShopMenu()
        {
            if (_fishShop == null)
            {
                SetStatus("Fish market is unavailable.");
                PushActivity("Fish market unavailable.");
                return;
            }

            OpenShopMenu(
                ShopMenuType.Fish,
                _fishShopPanel,
                _fishShopDefaultSelection,
                "Fish market open. Sell cargo from this panel.");
            PushActivity("Opened fish market.");
            RefreshFishShopDetails();
        }

        private void OpenShopMenu(ShopMenuType menuType, GameObject menuPanel, GameObject defaultSelection, string statusMessage)
        {
            _activeMenu = menuType;
            SetPanel(_actionPanel, false);
            SetPanel(_hookShopPanel, menuType == ShopMenuType.Hook);
            SetPanel(_boatShopPanel, menuType == ShopMenuType.Boat);
            SetPanel(_fishShopPanel, menuType == ShopMenuType.Fish);
            if (menuPanel != null)
            {
                menuPanel.transform.SetAsLastSibling();
            }

            SetStatus(statusMessage);
            SetSelected(defaultSelection);
        }

        private void HandleHookShopSelection(string hookId)
        {
            if (_hookShop == null)
            {
                SetStatus("Hook shop is unavailable.");
                PushActivity("Hook shop unavailable.");
                return;
            }

            if (string.IsNullOrWhiteSpace(hookId))
            {
                SetStatus("No hook selected.");
                return;
            }

            if (_saveManager == null || _saveManager.Current == null)
            {
                SetStatus("Cannot process hook purchase without save data.");
                PushActivity("Hook shop unavailable: save not ready.");
                return;
            }

            var save = _saveManager.Current;
            var wasOwned = save.ownedHooks != null && save.ownedHooks.Contains(hookId);
            var wasEquipped = string.Equals(save.equippedHookId, hookId, StringComparison.Ordinal);
            var price = Mathf.Max(0, _hookShop.GetPrice(hookId));

            if (!_saveManager.IsContentUnlocked(hookId))
            {
                var unlockLevel = _saveManager.GetUnlockLevel(hookId);
                SetStatus($"{ToDisplayLabel(hookId)} unlocks at level {unlockLevel}.");
                PushActivity($"Hook shop: {ToDisplayLabel(hookId)} is locked.");
                RefreshShopMenuDetails();
                return;
            }

            if (!wasOwned && save.copecs < price)
            {
                SetStatus($"Need {price} copecs for {ToDisplayLabel(hookId)}. Balance: {save.copecs}.");
                PushActivity($"Hook shop: insufficient copecs for {ToDisplayLabel(hookId)}.");
                RefreshShopMenuDetails();
                return;
            }

            if (!_hookShop.BuyOrEquip(hookId))
            {
                SetStatus($"Could not buy/equip {ToDisplayLabel(hookId)}.");
                PushActivity($"Hook shop: action failed for {ToDisplayLabel(hookId)}.");
                RefreshShopMenuDetails();
                return;
            }

            if (!wasOwned)
            {
                SetStatus($"Purchased and equipped {ToDisplayLabel(hookId)} for {price} copecs. Balance: {CurrentCopecs()} copecs.");
                PushActivity($"Hook shop: purchased {ToDisplayLabel(hookId)}.");
            }
            else if (!wasEquipped)
            {
                SetStatus($"Equipped {ToDisplayLabel(hookId)}. Balance: {CurrentCopecs()} copecs.");
                PushActivity($"Hook shop: equipped {ToDisplayLabel(hookId)}.");
            }
            else
            {
                SetStatus($"{ToDisplayLabel(hookId)} is already equipped.");
                PushActivity($"Hook shop: {ToDisplayLabel(hookId)} already equipped.");
            }

            RefreshSaveSnapshot();
            RefreshShopMenuDetails();
        }

        private void HandleBoatShopSelection(string shipId)
        {
            if (_boatShop == null)
            {
                SetStatus("Boat shop is unavailable.");
                PushActivity("Boat shop unavailable.");
                return;
            }

            if (string.IsNullOrWhiteSpace(shipId))
            {
                SetStatus("No ship selected.");
                return;
            }

            if (_saveManager == null || _saveManager.Current == null)
            {
                SetStatus("Cannot process boat purchase without save data.");
                PushActivity("Boat shop unavailable: save not ready.");
                return;
            }

            var save = _saveManager.Current;
            var wasOwned = save.ownedShips != null && save.ownedShips.Contains(shipId);
            var wasEquipped = string.Equals(save.equippedShipId, shipId, StringComparison.Ordinal);
            var price = Mathf.Max(0, _boatShop.GetPrice(shipId));

            if (!_saveManager.IsContentUnlocked(shipId))
            {
                var unlockLevel = _saveManager.GetUnlockLevel(shipId);
                SetStatus($"{ToDisplayLabel(shipId)} unlocks at level {unlockLevel}.");
                PushActivity($"Boat shop: {ToDisplayLabel(shipId)} is locked.");
                RefreshShopMenuDetails();
                return;
            }

            if (!wasOwned && save.copecs < price)
            {
                SetStatus($"Need {price} copecs for {ToDisplayLabel(shipId)}. Balance: {save.copecs}.");
                PushActivity($"Boat shop: insufficient copecs for {ToDisplayLabel(shipId)}.");
                RefreshShopMenuDetails();
                return;
            }

            if (!_boatShop.BuyOrEquip(shipId))
            {
                SetStatus($"Could not buy/equip {ToDisplayLabel(shipId)}.");
                PushActivity($"Boat shop: action failed for {ToDisplayLabel(shipId)}.");
                RefreshShopMenuDetails();
                return;
            }

            if (!wasOwned)
            {
                SetStatus($"Purchased and equipped {ToDisplayLabel(shipId)} for {price} copecs. Balance: {CurrentCopecs()} copecs.");
                PushActivity($"Boat shop: purchased {ToDisplayLabel(shipId)}.");
            }
            else if (!wasEquipped)
            {
                SetStatus($"Equipped {ToDisplayLabel(shipId)}. Balance: {CurrentCopecs()} copecs.");
                PushActivity($"Boat shop: equipped {ToDisplayLabel(shipId)}.");
            }
            else
            {
                SetStatus($"{ToDisplayLabel(shipId)} is already equipped.");
                PushActivity($"Boat shop: {ToDisplayLabel(shipId)} already equipped.");
            }

            RefreshSaveSnapshot();
            RefreshShopMenuDetails();
        }

        private void HandleFishShopSale()
        {
            if (_fishShop == null)
            {
                SetStatus("Fish market is unavailable.");
                PushActivity("Fish market unavailable.");
                return;
            }

            var summary = _fishShop.PreviewSellAll();
            var pendingCount = Mathf.Max(0, summary.itemCount);
            if (pendingCount <= 0)
            {
                SetStatus("No fish in cargo to sell.");
                PushActivity("Fish market: cargo already empty.");
                RefreshShopMenuDetails();
                return;
            }

            var earned = _fishShop.SellAll();
            SetStatus($"Sold {pendingCount} fish for {earned} copecs. Balance: {CurrentCopecs()} copecs.");
            PushActivity($"Fish market: sold {pendingCount} fish for {earned} copecs.");
            RefreshSaveSnapshot();
            RefreshShopMenuDetails();
        }

        private void HandleSail()
        {
            SetStatus("Casting off for fishing grounds...");
            PushActivity("Departure confirmed. Sailing to fishing waters.");
            _orchestrator?.RequestOpenFishing();
        }

        private void CloseShopMenus(bool selectMainAction)
        {
            _activeMenu = ShopMenuType.None;
            SetPanel(_actionPanel, true);
            SetPanel(_hookShopPanel, false);
            SetPanel(_boatShopPanel, false);
            SetPanel(_fishShopPanel, false);
            if (selectMainAction)
            {
                SetSelected(_mainMenuDefaultSelection);
            }
        }

        private void RefreshShopMenuDetails()
        {
            RefreshHookShopDetails();
            RefreshBoatShopDetails();
            RefreshFishShopDetails();
        }

        private void RefreshHookShopDetails()
        {
            if (_hookShopInfoText == null)
            {
                return;
            }

            if (_saveManager == null || _saveManager.Current == null || _hookShop == null)
            {
                _hookShopInfoText.text = "Hook inventory unavailable.";
                return;
            }

            var save = _saveManager.Current;
            var output =
                $"Balance: {Mathf.Max(0, save.copecs)} copecs\n" +
                "Select a hook to buy/equip:";
            for (var i = 0; i < HookMenuOrder.Length; i++)
            {
                var hookId = HookMenuOrder[i];
                output += "\n" + BuildShopItemLine(
                    hookId,
                    save.ownedHooks != null && save.ownedHooks.Contains(hookId),
                    string.Equals(save.equippedHookId, hookId, StringComparison.Ordinal),
                    _saveManager.IsContentUnlocked(hookId),
                    _saveManager.GetUnlockLevel(hookId),
                    _hookShop.GetPrice(hookId));
            }

            _hookShopInfoText.text = output;
        }

        private void RefreshBoatShopDetails()
        {
            if (_boatShopInfoText == null)
            {
                return;
            }

            if (_saveManager == null || _saveManager.Current == null || _boatShop == null)
            {
                _boatShopInfoText.text = "Boat inventory unavailable.";
                return;
            }

            var save = _saveManager.Current;
            var output =
                $"Balance: {Mathf.Max(0, save.copecs)} copecs\n" +
                "Select a ship to buy/equip:";
            for (var i = 0; i < ShipMenuOrder.Length; i++)
            {
                var shipId = ShipMenuOrder[i];
                output += "\n" + BuildShopItemLine(
                    shipId,
                    save.ownedShips != null && save.ownedShips.Contains(shipId),
                    string.Equals(save.equippedShipId, shipId, StringComparison.Ordinal),
                    _saveManager.IsContentUnlocked(shipId),
                    _saveManager.GetUnlockLevel(shipId),
                    _boatShop.GetPrice(shipId));
            }

            _boatShopInfoText.text = output;
        }

        private void RefreshFishShopDetails()
        {
            if (_fishShopInfoText == null)
            {
                return;
            }

            if (_fishShop == null || _saveManager == null || _saveManager.Current == null)
            {
                _fishShopInfoText.text = "Fish market summary unavailable.";
                return;
            }

            var summary = _fishShop.PreviewSellAll();
            var pendingCount = Mathf.Max(0, summary.itemCount);
            var pendingValue = Mathf.Max(0, summary.totalEarned);
            if (pendingCount <= 0)
            {
                _fishShopInfoText.text =
                    $"Balance: {Mathf.Max(0, _saveManager.Current.copecs)} copecs\n" +
                    "Cargo is empty. Catch fish before selling.";
                return;
            }

            _fishShopInfoText.text =
                $"Balance: {Mathf.Max(0, _saveManager.Current.copecs)} copecs\n" +
                $"Cargo ready: {pendingCount} fish\n" +
                $"Projected payout: {pendingValue} copecs";
        }

        private static string BuildShopItemLine(string itemId, bool owned, bool equipped, bool unlocked, int unlockLevel, int price)
        {
            var label = ToDisplayLabel(itemId);
            if (!unlocked)
            {
                return $"- {label}: Locked until level {Mathf.Max(1, unlockLevel)}";
            }

            if (equipped)
            {
                return $"- {label}: Equipped";
            }

            if (owned)
            {
                return $"- {label}: Owned";
            }

            if (price < 0)
            {
                return $"- {label}: Price unavailable";
            }

            return $"- {label}: {Mathf.Max(0, price)} copecs";
        }

        private static void SetPanel(GameObject panel, bool active)
        {
            if (panel == null)
            {
                return;
            }

            panel.SetActive(active);
            if (active)
            {
                panel.transform.SetAsLastSibling();
            }
        }

        private static void SetSelected(GameObject target)
        {
            if (target == null || EventSystem.current == null)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(target);
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
            RefreshShopMenuDetails();
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
            var cargoCapacity = ResolveCargoCapacity(save.equippedShipId);
            var cargoStatus = fishCount >= cargoCapacity
                ? " (FULL)"
                : string.Empty;
            SetText(_economyText, $"Copecs: {Mathf.Max(0, save.copecs)}");
            SetText(_equipmentText, $"Equipped Ship: {ToDisplayLabel(save.equippedShipId)} | Hook: {ToDisplayLabel(save.equippedHookId)}");
            SetText(_cargoText, $"Cargo: {fishCount}/{cargoCapacity} fish{cargoStatus} | Trips: {totalTrips} | Level: {_saveManager.CurrentLevel}");
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

        private int ResolveCargoCapacity(string shipId)
        {
            if (_catalogService != null
                && !string.IsNullOrWhiteSpace(shipId)
                && _catalogService.TryGetShip(shipId, out var shipDefinition)
                && shipDefinition != null)
            {
                if (shipDefinition.cargoCapacity > 0)
                {
                    return shipDefinition.cargoCapacity;
                }
            }

            var normalizedId = string.IsNullOrWhiteSpace(shipId)
                ? string.Empty
                : shipId.Trim().ToLowerInvariant();
            if (normalizedId.Contains("lv3"))
            {
                return Mathf.Max(1, _fallbackCargoCapacityTier3);
            }

            if (normalizedId.Contains("lv2"))
            {
                return Mathf.Max(1, _fallbackCargoCapacityTier2);
            }

            return Mathf.Max(1, _fallbackCargoCapacityTier1);
        }
    }
}
