using System;
using System.Collections.Generic;
using RavenDevOps.Fishing.Audio;
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
        private static readonly string[] FallbackHookMenuOrder = { "hook_lv1", "hook_lv2", "hook_lv3" };
        private static readonly string[] FallbackShipMenuOrder = { "ship_lv1", "ship_lv2", "ship_lv3" };

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
        [SerializeField] private Button _sailButton;
        [SerializeField] private int _fallbackCargoCapacityTier1 = 12;
        [SerializeField] private int _fallbackCargoCapacityTier2 = 20;
        [SerializeField] private int _fallbackCargoCapacityTier3 = 32;

        private readonly Queue<string> _recentActivity = new Queue<string>();
        private readonly List<Button> _hookShopButtons = new List<Button>();
        private readonly List<Button> _boatShopButtons = new List<Button>();
        private readonly List<Image> _hookShopIcons = new List<Image>();
        private readonly List<Image> _boatShopIcons = new List<Image>();
        private ShopMenuType _activeMenu = ShopMenuType.None;
        private static Sprite _shopFallbackIconSprite;

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
            GameObject fishShopDefaultSelection = null,
            Button sailButton = null,
            List<Button> hookShopButtons = null,
            List<Button> boatShopButtons = null,
            List<Image> hookShopIcons = null,
            List<Image> boatShopIcons = null)
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
            _sailButton = sailButton;
            AssignUiList(_hookShopButtons, hookShopButtons);
            AssignUiList(_boatShopButtons, boatShopButtons);
            AssignUiList(_hookShopIcons, hookShopIcons);
            AssignUiList(_boatShopIcons, boatShopIcons);
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
            SetStatus("Harbor ready. Use the center menu to access shops and sail.");
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

            PlaySfx(SfxEvent.UiCancel);
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
                "Hook shop open. Select a hook to upgrade or equip.");
            PlaySfx(SfxEvent.UiSelect);
            PushActivity("Opened hook shop.");
            RefreshHookShopDetails();
            SelectFirstInteractable(_hookShopButtons, _hookShopDefaultSelection);
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
                "Boat shop open. Select a ship to upgrade or equip.");
            PlaySfx(SfxEvent.UiSelect);
            PushActivity("Opened boat shop.");
            RefreshBoatShopDetails();
            SelectFirstInteractable(_boatShopButtons, _boatShopDefaultSelection);
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
            PlaySfx(SfxEvent.UiSelect);
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
            var hookOrder = ResolveHookMenuOrder();
            var hasRequiredTier = HasRequiredPreviousTier(save.ownedHooks, hookId, hookOrder, out var requiredTierId);

            if (!_saveManager.IsContentUnlocked(hookId))
            {
                var unlockLevel = _saveManager.GetUnlockLevel(hookId);
                SetStatus($"{ToDisplayLabel(hookId)} unlocks at level {unlockLevel}.");
                PushActivity($"Hook shop: {ToDisplayLabel(hookId)} is locked.");
                RefreshShopMenuDetails();
                return;
            }

            if (!wasOwned && !hasRequiredTier)
            {
                var requiredLabel = ToDisplayLabel(requiredTierId);
                SetStatus($"Need {requiredLabel} before upgrading to {ToDisplayLabel(hookId)}.");
                PushActivity($"Hook shop: {ToDisplayLabel(hookId)} requires {requiredLabel}.");
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
                SetStatus($"Upgraded to {ToDisplayLabel(hookId)} for {price} copecs. Balance: {CurrentCopecs()} copecs.");
                PushActivity($"Hook shop: upgraded to {ToDisplayLabel(hookId)}.");
                PlaySfx(SfxEvent.Purchase);
            }
            else if (!wasEquipped)
            {
                SetStatus($"Equipped {ToDisplayLabel(hookId)}. Balance: {CurrentCopecs()} copecs.");
                PushActivity($"Hook shop: equipped {ToDisplayLabel(hookId)}.");
                PlaySfx(SfxEvent.UiSelect);
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
            var shipOrder = ResolveShipMenuOrder();
            var hasRequiredTier = HasRequiredPreviousTier(save.ownedShips, shipId, shipOrder, out var requiredTierId);

            if (!_saveManager.IsContentUnlocked(shipId))
            {
                var unlockLevel = _saveManager.GetUnlockLevel(shipId);
                SetStatus($"{ToDisplayLabel(shipId)} unlocks at level {unlockLevel}.");
                PushActivity($"Boat shop: {ToDisplayLabel(shipId)} is locked.");
                RefreshShopMenuDetails();
                return;
            }

            if (!wasOwned && !hasRequiredTier)
            {
                var requiredLabel = ToDisplayLabel(requiredTierId);
                SetStatus($"Need {requiredLabel} before upgrading to {ToDisplayLabel(shipId)}.");
                PushActivity($"Boat shop: {ToDisplayLabel(shipId)} requires {requiredLabel}.");
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
                SetStatus($"Upgraded to {ToDisplayLabel(shipId)} for {price} copecs. Balance: {CurrentCopecs()} copecs.");
                PushActivity($"Boat shop: upgraded to {ToDisplayLabel(shipId)}.");
                PlaySfx(SfxEvent.Purchase);
            }
            else if (!wasEquipped)
            {
                SetStatus($"Equipped {ToDisplayLabel(shipId)}. Balance: {CurrentCopecs()} copecs.");
                PushActivity($"Boat shop: equipped {ToDisplayLabel(shipId)}.");
                PlaySfx(SfxEvent.UiSelect);
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
            PlaySfx(SfxEvent.Sell);
            RefreshSaveSnapshot();
            RefreshShopMenuDetails();
        }

        private void HandleSail()
        {
            if (_saveManager != null && _saveManager.Current != null && IsCargoFull(_saveManager.Current, out var fishCount, out var cargoCapacity))
            {
                SetStatus($"Cargo full ({fishCount}/{cargoCapacity}). Sell fish at the market before sailing.");
                PushActivity("Departure blocked: cargo is full.");
                PlaySfx(SfxEvent.UiCancel);
                RefreshSaveSnapshot();
                return;
            }

            PlaySfx(SfxEvent.Depart);
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
            if (_saveManager == null || _saveManager.Current == null || _hookShop == null)
            {
                SetText(_hookShopInfoText, "Hook inventory unavailable.");
                RefreshHookShopButtons(null);
                RefreshShopIcons(_hookShopIcons, _hookShopButtons, ResolveHookMenuOrder(), save: null, isHookCatalog: true);
                return;
            }

            var hookOrder = ResolveHookMenuOrder();
            var save = _saveManager.Current;
            var output =
                $"Balance: {Mathf.Max(0, save.copecs)} copecs\n" +
                "Select a hook to upgrade/equip:";
            for (var i = 0; i < hookOrder.Length; i++)
            {
                var hookId = hookOrder[i];
                var isOwned = save.ownedHooks != null && save.ownedHooks.Contains(hookId);
                var isEquipped = string.Equals(save.equippedHookId, hookId, StringComparison.Ordinal);
                var isUnlocked = _saveManager.IsContentUnlocked(hookId);
                var unlockLevel = _saveManager.GetUnlockLevel(hookId);
                var price = _hookShop.GetPrice(hookId);
                var hasRequiredPreviousTier = HasRequiredPreviousTier(save.ownedHooks, hookId, hookOrder, out var requiredTierId);
                output += "\n" + BuildShopItemLine(
                    hookId,
                    isOwned,
                    isEquipped,
                    isUnlocked,
                    unlockLevel,
                    price,
                    hasRequiredPreviousTier,
                    requiredTierId,
                    Mathf.Max(0, save.copecs));
            }

            SetText(_hookShopInfoText, output);
            RefreshHookShopButtons(save);
            RefreshShopIcons(_hookShopIcons, _hookShopButtons, hookOrder, save, isHookCatalog: true);
        }

        private void RefreshBoatShopDetails()
        {
            if (_saveManager == null || _saveManager.Current == null || _boatShop == null)
            {
                SetText(_boatShopInfoText, "Boat inventory unavailable.");
                RefreshBoatShopButtons(null);
                RefreshShopIcons(_boatShopIcons, _boatShopButtons, ResolveShipMenuOrder(), save: null, isHookCatalog: false);
                return;
            }

            var shipOrder = ResolveShipMenuOrder();
            var save = _saveManager.Current;
            var output =
                $"Balance: {Mathf.Max(0, save.copecs)} copecs\n" +
                "Select a ship to upgrade/equip:";
            for (var i = 0; i < shipOrder.Length; i++)
            {
                var shipId = shipOrder[i];
                var isOwned = save.ownedShips != null && save.ownedShips.Contains(shipId);
                var isEquipped = string.Equals(save.equippedShipId, shipId, StringComparison.Ordinal);
                var isUnlocked = _saveManager.IsContentUnlocked(shipId);
                var unlockLevel = _saveManager.GetUnlockLevel(shipId);
                var price = _boatShop.GetPrice(shipId);
                var hasRequiredPreviousTier = HasRequiredPreviousTier(save.ownedShips, shipId, shipOrder, out var requiredTierId);
                output += "\n" + BuildShopItemLine(
                    shipId,
                    isOwned,
                    isEquipped,
                    isUnlocked,
                    unlockLevel,
                    price,
                    hasRequiredPreviousTier,
                    requiredTierId,
                    Mathf.Max(0, save.copecs));
            }

            SetText(_boatShopInfoText, output);
            RefreshBoatShopButtons(save);
            RefreshShopIcons(_boatShopIcons, _boatShopButtons, shipOrder, save, isHookCatalog: false);
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

        private static string BuildShopItemLine(
            string itemId,
            bool owned,
            bool equipped,
            bool unlocked,
            int unlockLevel,
            int price,
            bool hasRequiredPreviousTier,
            string requiredTierId,
            int balance)
        {
            var label = ToDisplayLabel(itemId);
            if (!unlocked)
            {
                return $"- {label}: Locked until level {Mathf.Max(1, unlockLevel)}";
            }

            if (!owned && !hasRequiredPreviousTier)
            {
                return $"- {label}: Requires {ToDisplayLabel(requiredTierId)}";
            }

            if (equipped)
            {
                return $"- {label}: Equipped";
            }

            if (owned)
            {
                return $"- {label}: Owned (equip ready)";
            }

            if (price < 0)
            {
                return $"- {label}: Price unavailable";
            }

            var normalizedPrice = Mathf.Max(0, price);
            if (balance < normalizedPrice)
            {
                return $"- {label}: Upgrade {normalizedPrice} copecs (need {normalizedPrice - balance} more)";
            }

            return $"- {label}: Upgrade {normalizedPrice} copecs";
        }

        private void RefreshHookShopButtons(SaveDataV1 save)
        {
            var hookOrder = ResolveHookMenuOrder();
            for (var i = 0; i < hookOrder.Length; i++)
            {
                var hookId = hookOrder[i];
                var button = i >= 0 && i < _hookShopButtons.Count ? _hookShopButtons[i] : null;
                if (button == null)
                {
                    continue;
                }

                if (save == null || _hookShop == null || _saveManager == null)
                {
                    ApplyShopButtonState(button, false, "Unavailable");
                    continue;
                }

                var isOwned = save.ownedHooks != null && save.ownedHooks.Contains(hookId);
                var isEquipped = string.Equals(save.equippedHookId, hookId, StringComparison.Ordinal);
                var isUnlocked = _saveManager.IsContentUnlocked(hookId);
                var unlockLevel = _saveManager.GetUnlockLevel(hookId);
                var hasRequiredPreviousTier = HasRequiredPreviousTier(save.ownedHooks, hookId, hookOrder, out _);
                var price = _hookShop.GetPrice(hookId);
                var canUpgrade = price >= 0 && save.copecs >= Mathf.Max(0, price);
                var interactable = isOwned || (isUnlocked && hasRequiredPreviousTier && canUpgrade && price >= 0);
                var buttonLabel = BuildShopButtonLabel(
                    isOwned,
                    isEquipped,
                    isUnlocked,
                    unlockLevel,
                    hasRequiredPreviousTier,
                    price);
                ApplyShopButtonState(button, interactable, buttonLabel);
            }
        }

        private void RefreshBoatShopButtons(SaveDataV1 save)
        {
            var shipOrder = ResolveShipMenuOrder();
            for (var i = 0; i < shipOrder.Length; i++)
            {
                var shipId = shipOrder[i];
                var button = i >= 0 && i < _boatShopButtons.Count ? _boatShopButtons[i] : null;
                if (button == null)
                {
                    continue;
                }

                if (save == null || _boatShop == null || _saveManager == null)
                {
                    ApplyShopButtonState(button, false, "Unavailable");
                    continue;
                }

                var isOwned = save.ownedShips != null && save.ownedShips.Contains(shipId);
                var isEquipped = string.Equals(save.equippedShipId, shipId, StringComparison.Ordinal);
                var isUnlocked = _saveManager.IsContentUnlocked(shipId);
                var unlockLevel = _saveManager.GetUnlockLevel(shipId);
                var hasRequiredPreviousTier = HasRequiredPreviousTier(save.ownedShips, shipId, shipOrder, out _);
                var price = _boatShop.GetPrice(shipId);
                var canUpgrade = price >= 0 && save.copecs >= Mathf.Max(0, price);
                var interactable = isOwned || (isUnlocked && hasRequiredPreviousTier && canUpgrade && price >= 0);
                var buttonLabel = BuildShopButtonLabel(
                    isOwned,
                    isEquipped,
                    isUnlocked,
                    unlockLevel,
                    hasRequiredPreviousTier,
                    price);
                ApplyShopButtonState(button, interactable, buttonLabel);
            }
        }

        private void RefreshShopIcons(List<Image> iconList, List<Button> buttonList, string[] itemOrder, SaveDataV1 save, bool isHookCatalog)
        {
            if (itemOrder == null || iconList == null)
            {
                return;
            }

            for (var i = 0; i < itemOrder.Length; i++)
            {
                if (i < 0 || i >= iconList.Count)
                {
                    continue;
                }

                var icon = iconList[i];
                if (icon == null)
                {
                    continue;
                }

                var itemId = itemOrder[i];
                var iconSprite = ResolveShopIcon(itemId, isHookCatalog) ?? GetFallbackShopIconSprite();
                icon.sprite = iconSprite;
                var button = buttonList != null && i >= 0 && i < buttonList.Count
                    ? buttonList[i]
                    : null;
                icon.color = ResolveShopIconColor(itemId, save, isHookCatalog, button);
            }
        }

        private Color ResolveShopIconColor(string itemId, SaveDataV1 save, bool isHookCatalog, Button button)
        {
            if (save == null || _saveManager == null)
            {
                return new Color(0.5f, 0.5f, 0.5f, 0.85f);
            }

            var normalizedItemId = string.IsNullOrWhiteSpace(itemId)
                ? string.Empty
                : itemId.Trim();
            if (string.IsNullOrWhiteSpace(normalizedItemId))
            {
                return new Color(0.5f, 0.5f, 0.5f, 0.85f);
            }

            var isOwned = isHookCatalog
                ? save.ownedHooks != null && save.ownedHooks.Contains(normalizedItemId)
                : save.ownedShips != null && save.ownedShips.Contains(normalizedItemId);
            var isEquipped = isHookCatalog
                ? string.Equals(save.equippedHookId, normalizedItemId, StringComparison.Ordinal)
                : string.Equals(save.equippedShipId, normalizedItemId, StringComparison.Ordinal);
            if (isEquipped)
            {
                return new Color(1f, 0.92f, 0.52f, 1f);
            }

            if (isOwned)
            {
                return new Color(0.86f, 1f, 0.9f, 0.98f);
            }

            if (!_saveManager.IsContentUnlocked(normalizedItemId))
            {
                return new Color(0.56f, 0.56f, 0.56f, 0.85f);
            }

            var orderedIds = isHookCatalog ? ResolveHookMenuOrder() : ResolveShipMenuOrder();
            var ownedIds = isHookCatalog ? save.ownedHooks : save.ownedShips;
            if (!HasRequiredPreviousTier(ownedIds, normalizedItemId, orderedIds, out _))
            {
                return new Color(0.56f, 0.56f, 0.56f, 0.85f);
            }

            var price = isHookCatalog
                ? (_hookShop != null ? _hookShop.GetPrice(normalizedItemId) : -1)
                : (_boatShop != null ? _boatShop.GetPrice(normalizedItemId) : -1);
            if (price < 0)
            {
                return new Color(0.56f, 0.56f, 0.56f, 0.85f);
            }

            if (save.copecs < Mathf.Max(0, price))
            {
                return new Color(0.86f, 0.66f, 0.66f, 0.92f);
            }

            if (button != null && !button.interactable)
            {
                return new Color(0.66f, 0.66f, 0.66f, 0.88f);
            }

            return new Color(1f, 1f, 1f, 0.98f);
        }

        private Sprite ResolveShopIcon(string itemId, bool isHookCatalog)
        {
            if (_catalogService == null || string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            if (isHookCatalog)
            {
                return _catalogService.TryGetHook(itemId, out var hookDefinition) && hookDefinition != null
                    ? hookDefinition.icon
                    : null;
            }

            return _catalogService.TryGetShip(itemId, out var shipDefinition) && shipDefinition != null
                ? shipDefinition.icon
                : null;
        }

        private string[] ResolveHookMenuOrder()
        {
            if (_hookShop != null)
            {
                var orderedIds = _hookShop.GetOrderedItemIds();
                if (orderedIds != null && orderedIds.Length > 0)
                {
                    return orderedIds;
                }
            }

            return FallbackHookMenuOrder;
        }

        private string[] ResolveShipMenuOrder()
        {
            if (_boatShop != null)
            {
                var orderedIds = _boatShop.GetOrderedItemIds();
                if (orderedIds != null && orderedIds.Length > 0)
                {
                    return orderedIds;
                }
            }

            return FallbackShipMenuOrder;
        }

        private static string BuildShopButtonLabel(
            bool owned,
            bool equipped,
            bool unlocked,
            int unlockLevel,
            bool hasRequiredPreviousTier,
            int price)
        {
            if (equipped)
            {
                return "Equipped";
            }

            if (owned)
            {
                return "Equip";
            }

            if (!unlocked)
            {
                return $"Locked Lv{Mathf.Max(1, unlockLevel)}";
            }

            if (!hasRequiredPreviousTier)
            {
                return "Locked (Prev)";
            }

            if (price < 0)
            {
                return "Unavailable";
            }

            return $"Upgrade ({Mathf.Max(0, price)}c)";
        }

        private static void ApplyShopButtonState(Button button, bool interactable, string label)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = interactable;
            var labelText = button.GetComponentInChildren<Text>(includeInactive: true);
            if (labelText != null)
            {
                labelText.text = label ?? string.Empty;
            }
        }

        private static bool HasRequiredPreviousTier(List<string> ownedIds, string itemId, string[] orderedIds, out string requiredTierId)
        {
            requiredTierId = string.Empty;
            if (string.IsNullOrWhiteSpace(itemId) || orderedIds == null || orderedIds.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < orderedIds.Length; i++)
            {
                if (!string.Equals(orderedIds[i], itemId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (i <= 0)
                {
                    return true;
                }

                requiredTierId = orderedIds[i - 1];
                return ownedIds != null && ownedIds.Contains(requiredTierId);
            }

            return true;
        }

        private static void AssignUiList<T>(List<T> target, List<T> source) where T : class
        {
            if (target == null)
            {
                return;
            }

            target.Clear();
            if (source == null)
            {
                return;
            }

            for (var i = 0; i < source.Count; i++)
            {
                target.Add(source[i]);
            }
        }

        private static Sprite GetFallbackShopIconSprite()
        {
            if (_shopFallbackIconSprite != null)
            {
                return _shopFallbackIconSprite;
            }

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            texture.name = "ShopFallbackIconTexture";
            _shopFallbackIconSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit: 1f);
            _shopFallbackIconSprite.name = "ShopFallbackIconSprite";
            return _shopFallbackIconSprite;
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

        private static void PlaySfx(SfxEvent eventType)
        {
            RuntimeServiceRegistry.Get<SfxTriggerRouter>()?.Play(eventType);
        }

        private static void SelectFirstInteractable(List<Button> buttons, GameObject fallbackSelection)
        {
            if (buttons != null)
            {
                for (var i = 0; i < buttons.Count; i++)
                {
                    var button = buttons[i];
                    if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
                    {
                        continue;
                    }

                    SetSelected(button.gameObject);
                    return;
                }
            }

            SetSelected(fallbackSelection);
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
                RefreshSailButton(save: null, allowSail: false, fishCount: 0, cargoCapacity: 0);
                return;
            }

            var save = _saveManager.Current;
            var fishCount = CountCargoFish(save);
            var totalTrips = save.stats != null ? Mathf.Max(0, save.stats.totalTrips) : 0;
            var cargoCapacity = ResolveCargoCapacity(save.equippedShipId);
            var isCargoFull = fishCount >= cargoCapacity;
            var cargoStatus = isCargoFull
                ? " (FULL)"
                : string.Empty;
            SetText(_economyText, $"Copecs: {Mathf.Max(0, save.copecs)}");
            SetText(_equipmentText, $"Equipped Ship: {ToDisplayLabel(save.equippedShipId)} | Hook: {ToDisplayLabel(save.equippedHookId)}");
            SetText(_cargoText, $"Cargo: {fishCount}/{cargoCapacity} fish{cargoStatus} | Trips: {totalTrips} | Level: {_saveManager.CurrentLevel}");
            RefreshSailButton(save, !isCargoFull, fishCount, cargoCapacity);
        }

        private void RefreshSelectionHint(WorldInteractable interactable)
        {
            if (_selectionText == null)
            {
                return;
            }

            if (interactable == null)
            {
                _selectionText.text = "Nearby target: none. Use center menu actions for harbor operations.";
                return;
            }

            var message = interactable.Type switch
            {
                InteractableType.HookShop => "Nearby target: Hook Shop. Press Enter to upgrade or equip hooks.",
                InteractableType.BoatShop => "Nearby target: Boat Shop. Press Enter to purchase or equip ships.",
                InteractableType.FishShop => "Nearby target: Fish Market. Press Enter to sell all fish cargo.",
                InteractableType.Sail => "Nearby target: Dock. Press Enter to sail or use Sail Out in the menu.",
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

        private bool IsCargoFull(SaveDataV1 save, out int fishCount, out int cargoCapacity)
        {
            fishCount = CountCargoFish(save);
            cargoCapacity = ResolveCargoCapacity(save != null ? save.equippedShipId : string.Empty);
            return fishCount >= cargoCapacity;
        }

        private static int CountCargoFish(SaveDataV1 save)
        {
            if (save == null || save.fishInventory == null || save.fishInventory.Count == 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < save.fishInventory.Count; i++)
            {
                var entry = save.fishInventory[i];
                if (entry == null)
                {
                    continue;
                }

                count += Mathf.Max(0, entry.count);
            }

            return count;
        }

        private void RefreshSailButton(SaveDataV1 save, bool allowSail, int fishCount, int cargoCapacity)
        {
            if (_sailButton == null)
            {
                return;
            }

            if (save == null)
            {
                ApplyShopButtonState(_sailButton, false, "Sail Out");
                return;
            }

            if (!allowSail)
            {
                ApplyShopButtonState(_sailButton, false, $"Sell Cargo ({fishCount}/{cargoCapacity})");
                return;
            }

            ApplyShopButtonState(_sailButton, true, "Sail Out");
        }
    }
}
