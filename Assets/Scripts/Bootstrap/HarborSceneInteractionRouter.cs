using System;
using System.Collections.Generic;
using RavenDevOps.Fishing.Audio;
using RavenDevOps.Fishing.Economy;
using RavenDevOps.Fishing.Harbor;
using RavenDevOps.Fishing.Input;
using RavenDevOps.Fishing.Data;
using RavenDevOps.Fishing.Save;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RavenDevOps.Fishing.Core
{
    public sealed class HarborSceneInteractionRouter : MonoBehaviour
    {
        public sealed class RuntimeDependencyBundle
        {
            public List<WorldInteractable> Interactables { get; set; }
            public HookShopController HookShop { get; set; }
            public BoatShopController BoatShop { get; set; }
            public FishShopController FishShop { get; set; }
            public HarborInteractionController InteractionController { get; set; }
        }

        public sealed class MenuDependencyBundle
        {
            public GameObject ActionPanel { get; set; }
            public GameObject HookShopPanel { get; set; }
            public GameObject BoatShopPanel { get; set; }
            public GameObject FishShopPanel { get; set; }
            public GameObject FisheryPanel { get; set; }
            public GameObject ProfilePanel { get; set; }
            public GameObject ShipyardPanel { get; set; }
            public GameObject MainMenuConfirmPanel { get; set; }
            public GameObject MainMenuDefaultSelection { get; set; }
            public GameObject HookShopDefaultSelection { get; set; }
            public GameObject BoatShopDefaultSelection { get; set; }
            public GameObject FishShopDefaultSelection { get; set; }
            public GameObject FisheryDefaultSelection { get; set; }
            public GameObject ProfileDefaultSelection { get; set; }
            public GameObject ShipyardDefaultSelection { get; set; }
            public GameObject MainMenuConfirmDefaultSelection { get; set; }
        }

        public sealed class TextDependencyBundle
        {
            public TMP_Text StatusText { get; set; }
            public TMP_Text SelectionText { get; set; }
            public TMP_Text EconomyText { get; set; }
            public TMP_Text EquipmentText { get; set; }
            public TMP_Text CargoText { get; set; }
            public TMP_Text ActivityLogText { get; set; }
            public TMP_Text HookShopInfoText { get; set; }
            public TMP_Text BoatShopInfoText { get; set; }
            public TMP_Text FishShopInfoText { get; set; }
            public TMP_Text FisheryCardText { get; set; }
            public Image FisheryCardIcon { get; set; }
            public TMP_Text ShipyardInfoText { get; set; }
            public TMP_Text ShipyardCargoText { get; set; }
        }

        public sealed class ButtonDependencyBundle
        {
            public Button SailButton { get; set; }
            public Button FishQuestAcceptButton { get; set; }
            public Button FishQuestFulfillButton { get; set; }
            public List<Button> HookShopButtons { get; set; }
            public List<Button> BoatShopButtons { get; set; }
            public List<Button> ShipyardHookButtons { get; set; }
            public List<Button> ShipyardShipButtons { get; set; }
            public List<Image> HookShopIcons { get; set; }
            public List<Image> BoatShopIcons { get; set; }
        }

        public sealed class DependencyBundle
        {
            public RuntimeDependencyBundle Runtime { get; set; }
            public MenuDependencyBundle Menu { get; set; }
            public TextDependencyBundle Text { get; set; }
            public ButtonDependencyBundle Buttons { get; set; }
        }

        private const int MaxActivityLines = 4;
        private static readonly string[] FallbackHookMenuOrder = { "hook_lv1", "hook_lv2", "hook_lv3", "hook_lv4", "hook_lv5" };
        private static readonly string[] FallbackShipMenuOrder = { "ship_lv1", "ship_lv2", "ship_lv3", "ship_lv4", "ship_lv5" };

        [SerializeField] private List<WorldInteractable> _interactables = new List<WorldInteractable>();
        [SerializeField] private HookShopController _hookShop;
        [SerializeField] private BoatShopController _boatShop;
        [SerializeField] private FishShopController _fishShop;
        [SerializeField] private HarborInteractionController _interactionController;
        [SerializeField] private GameFlowOrchestrator _orchestrator;
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private CatalogService _catalogService;
        [SerializeField] private InputRebindingService _inputRebindingService;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _selectionText;
        [SerializeField] private TMP_Text _economyText;
        [SerializeField] private TMP_Text _equipmentText;
        [SerializeField] private TMP_Text _cargoText;
        [SerializeField] private TMP_Text _activityLogText;
        [SerializeField] private GameObject _actionPanel;
        [SerializeField] private GameObject _hookShopPanel;
        [SerializeField] private GameObject _boatShopPanel;
        [SerializeField] private GameObject _fishShopPanel;
        [SerializeField] private GameObject _fisheryPanel;
        [SerializeField] private GameObject _profilePanel;
        [SerializeField] private GameObject _shipyardPanel;
        [SerializeField] private GameObject _mainMenuConfirmPanel;
        [SerializeField] private TMP_Text _hookShopInfoText;
        [SerializeField] private TMP_Text _boatShopInfoText;
        [SerializeField] private TMP_Text _fishShopInfoText;
        [SerializeField] private TMP_Text _fisheryCardText;
        [SerializeField] private Image _fisheryCardIcon;
        [SerializeField] private TMP_Text _shipyardInfoText;
        [SerializeField] private TMP_Text _shipyardCargoText;
        [SerializeField] private GameObject _mainMenuDefaultSelection;
        [SerializeField] private GameObject _hookShopDefaultSelection;
        [SerializeField] private GameObject _boatShopDefaultSelection;
        [SerializeField] private GameObject _fishShopDefaultSelection;
        [SerializeField] private GameObject _fisheryDefaultSelection;
        [SerializeField] private GameObject _profileDefaultSelection;
        [SerializeField] private GameObject _shipyardDefaultSelection;
        [SerializeField] private GameObject _mainMenuConfirmDefaultSelection;
        [SerializeField] private Button _sailButton;
        [SerializeField] private Button _fishQuestAcceptButton;
        [SerializeField] private Button _fishQuestFulfillButton;
        [SerializeField] private int _fallbackCargoCapacityTier1 = 12;
        [SerializeField] private int _fallbackCargoCapacityTier2 = 20;
        [SerializeField] private int _fallbackCargoCapacityTier3 = 32;
        [SerializeField] private int _fallbackCargoCapacityTier4 = 48;
        [SerializeField] private int _fallbackCargoCapacityTier5 = 72;
        [SerializeField] private bool _unlockAllShopItemsForQa;

        private readonly Queue<string> _recentActivity = new Queue<string>();
        private readonly List<Button> _hookShopButtons = new List<Button>();
        private readonly List<Button> _boatShopButtons = new List<Button>();
        private readonly List<Button> _shipyardHookButtons = new List<Button>();
        private readonly List<Button> _shipyardShipButtons = new List<Button>();
        private readonly List<Image> _hookShopIcons = new List<Image>();
        private readonly List<Image> _boatShopIcons = new List<Image>();
        private readonly List<string> _fisheryCardFishIds = new List<string>();
        private readonly HarborMenuStateRouter _menuRouter = new HarborMenuStateRouter();
        private readonly HarborShopTransactionHandler _shopTransactionHandler = new HarborShopTransactionHandler();
        private readonly HarborFisheryTransactionHandler _fisheryTransactionHandler = new HarborFisheryTransactionHandler();
        private readonly HarborShipyardTransactionHandler _shipyardTransactionHandler = new HarborShipyardTransactionHandler();
        private readonly HarborShopViewPresenter _shopViewPresenter = new HarborShopViewPresenter();
        private readonly HarborFisheryCardViewPresenter _fisheryCardPresenter = new HarborFisheryCardViewPresenter();
        private readonly HarborInteractionViewPresenter _interactionViewPresenter = new HarborInteractionViewPresenter();
        private int _fisheryCardIndex;
        private static Sprite _shopFallbackIconSprite;

        public void Configure(DependencyBundle dependencies)
        {
            var runtime = dependencies != null ? dependencies.Runtime : null;
            var menu = dependencies != null ? dependencies.Menu : null;
            var text = dependencies != null ? dependencies.Text : null;
            var buttons = dependencies != null ? dependencies.Buttons : null;

            _interactables = runtime != null && runtime.Interactables != null
                ? runtime.Interactables
                : new List<WorldInteractable>();
            _hookShop = runtime != null ? runtime.HookShop : null;
            _boatShop = runtime != null ? runtime.BoatShop : null;
            _fishShop = runtime != null ? runtime.FishShop : null;
            _interactionController = runtime != null ? runtime.InteractionController : null;

            _actionPanel = menu != null ? menu.ActionPanel : null;
            _hookShopPanel = menu != null ? menu.HookShopPanel : null;
            _boatShopPanel = menu != null ? menu.BoatShopPanel : null;
            _fishShopPanel = menu != null ? menu.FishShopPanel : null;
            _fisheryPanel = menu != null ? menu.FisheryPanel : null;
            _profilePanel = menu != null ? menu.ProfilePanel : null;
            _shipyardPanel = menu != null ? menu.ShipyardPanel : null;
            _mainMenuConfirmPanel = menu != null ? menu.MainMenuConfirmPanel : null;
            _mainMenuDefaultSelection = menu != null ? menu.MainMenuDefaultSelection : null;
            _hookShopDefaultSelection = menu != null ? menu.HookShopDefaultSelection : null;
            _boatShopDefaultSelection = menu != null ? menu.BoatShopDefaultSelection : null;
            _fishShopDefaultSelection = menu != null ? menu.FishShopDefaultSelection : null;
            _fisheryDefaultSelection = menu != null ? menu.FisheryDefaultSelection : null;
            _profileDefaultSelection = menu != null ? menu.ProfileDefaultSelection : null;
            _shipyardDefaultSelection = menu != null ? menu.ShipyardDefaultSelection : null;
            _mainMenuConfirmDefaultSelection = menu != null ? menu.MainMenuConfirmDefaultSelection : null;

            _statusText = text != null ? text.StatusText : null;
            _selectionText = text != null ? text.SelectionText : null;
            _economyText = text != null ? text.EconomyText : null;
            _equipmentText = text != null ? text.EquipmentText : null;
            _cargoText = text != null ? text.CargoText : null;
            _activityLogText = text != null ? text.ActivityLogText : null;
            _hookShopInfoText = text != null ? text.HookShopInfoText : null;
            _boatShopInfoText = text != null ? text.BoatShopInfoText : null;
            _fishShopInfoText = text != null ? text.FishShopInfoText : null;
            _fisheryCardText = text != null ? text.FisheryCardText : null;
            _fisheryCardIcon = text != null ? text.FisheryCardIcon : null;
            _shipyardInfoText = text != null ? text.ShipyardInfoText : null;
            _shipyardCargoText = text != null ? text.ShipyardCargoText : null;

            _sailButton = buttons != null ? buttons.SailButton : null;
            _fishQuestAcceptButton = buttons != null ? buttons.FishQuestAcceptButton : null;
            _fishQuestFulfillButton = buttons != null ? buttons.FishQuestFulfillButton : null;
            AssignUiList(_hookShopButtons, buttons != null ? buttons.HookShopButtons : null);
            AssignUiList(_boatShopButtons, buttons != null ? buttons.BoatShopButtons : null);
            AssignUiList(_shipyardHookButtons, buttons != null ? buttons.ShipyardHookButtons : null);
            AssignUiList(_shipyardShipButtons, buttons != null ? buttons.ShipyardShipButtons : null);
            AssignUiList(_hookShopIcons, buttons != null ? buttons.HookShopIcons : null);
            AssignUiList(_boatShopIcons, buttons != null ? buttons.BoatShopIcons : null);

            ConfigureBoundedComponents();
        }

        public void Configure(
            List<WorldInteractable> interactables,
            HookShopController hookShop,
            BoatShopController boatShop,
            FishShopController fishShop,
            TMP_Text statusText,
            TMP_Text selectionText = null,
            TMP_Text economyText = null,
            TMP_Text equipmentText = null,
            TMP_Text cargoText = null,
            TMP_Text activityLogText = null,
            HarborInteractionController interactionController = null,
            GameObject actionPanel = null,
            GameObject hookShopPanel = null,
            GameObject boatShopPanel = null,
            GameObject fishShopPanel = null,
            GameObject fisheryPanel = null,
            GameObject profilePanel = null,
            GameObject shipyardPanel = null,
            GameObject mainMenuConfirmPanel = null,
            TMP_Text hookShopInfoText = null,
            TMP_Text boatShopInfoText = null,
            TMP_Text fishShopInfoText = null,
            TMP_Text fisheryCardText = null,
            Image fisheryCardIcon = null,
            TMP_Text shipyardInfoText = null,
            TMP_Text shipyardCargoText = null,
            GameObject mainMenuDefaultSelection = null,
            GameObject hookShopDefaultSelection = null,
            GameObject boatShopDefaultSelection = null,
            GameObject fishShopDefaultSelection = null,
            GameObject fisheryDefaultSelection = null,
            GameObject profileDefaultSelection = null,
            GameObject shipyardDefaultSelection = null,
            GameObject mainMenuConfirmDefaultSelection = null,
            Button sailButton = null,
            Button fishQuestAcceptButton = null,
            Button fishQuestFulfillButton = null,
            List<Button> hookShopButtons = null,
            List<Button> boatShopButtons = null,
            List<Button> shipyardHookButtons = null,
            List<Button> shipyardShipButtons = null,
            List<Image> hookShopIcons = null,
            List<Image> boatShopIcons = null)
        {
            Configure(
                new DependencyBundle
                {
                    Runtime = new RuntimeDependencyBundle
                    {
                        Interactables = interactables,
                        HookShop = hookShop,
                        BoatShop = boatShop,
                        FishShop = fishShop,
                        InteractionController = interactionController
                    },
                    Menu = new MenuDependencyBundle
                    {
                        ActionPanel = actionPanel,
                        HookShopPanel = hookShopPanel,
                        BoatShopPanel = boatShopPanel,
                        FishShopPanel = fishShopPanel,
                        FisheryPanel = fisheryPanel,
                        ProfilePanel = profilePanel,
                        ShipyardPanel = shipyardPanel,
                        MainMenuConfirmPanel = mainMenuConfirmPanel,
                        MainMenuDefaultSelection = mainMenuDefaultSelection,
                        HookShopDefaultSelection = hookShopDefaultSelection,
                        BoatShopDefaultSelection = boatShopDefaultSelection,
                        FishShopDefaultSelection = fishShopDefaultSelection,
                        FisheryDefaultSelection = fisheryDefaultSelection,
                        ProfileDefaultSelection = profileDefaultSelection,
                        ShipyardDefaultSelection = shipyardDefaultSelection,
                        MainMenuConfirmDefaultSelection = mainMenuConfirmDefaultSelection
                    },
                    Text = new TextDependencyBundle
                    {
                        StatusText = statusText,
                        SelectionText = selectionText,
                        EconomyText = economyText,
                        EquipmentText = equipmentText,
                        CargoText = cargoText,
                        ActivityLogText = activityLogText,
                        HookShopInfoText = hookShopInfoText,
                        BoatShopInfoText = boatShopInfoText,
                        FishShopInfoText = fishShopInfoText,
                        FisheryCardText = fisheryCardText,
                        FisheryCardIcon = fisheryCardIcon,
                        ShipyardInfoText = shipyardInfoText,
                        ShipyardCargoText = shipyardCargoText
                    },
                    Buttons = new ButtonDependencyBundle
                    {
                        SailButton = sailButton,
                        FishQuestAcceptButton = fishQuestAcceptButton,
                        FishQuestFulfillButton = fishQuestFulfillButton,
                        HookShopButtons = hookShopButtons,
                        BoatShopButtons = boatShopButtons,
                        ShipyardHookButtons = shipyardHookButtons,
                        ShipyardShipButtons = shipyardShipButtons,
                        HookShopIcons = hookShopIcons,
                        BoatShopIcons = boatShopIcons
                    }
                });
        }

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _orchestrator, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _catalogService, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _inputRebindingService, this, warnIfMissing: false);
            _interactionController ??= GetComponent<HarborInteractionController>();
            ConfigureBoundedComponents();
        }

        private void OnEnable()
        {
            ConfigureBoundedComponents();
            BindInteractables();
            BindRuntimeEvents();
            CloseShopMenus(selectMainAction: false);
            SetStatus("Harbor ready. Use the center menu for Profile, Shipyard, Dockyard, Warehouse, and Fishery access.");
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

        private void ConfigureBoundedComponents()
        {
            _menuRouter.ConfigureDependencies(
                new HarborMenuStateRouter.DependencyBundle
                {
                    ActionPanel = _actionPanel,
                    HookShopPanel = _hookShopPanel,
                    BoatShopPanel = _boatShopPanel,
                    FishShopPanel = _fishShopPanel,
                    FisheryPanel = _fisheryPanel,
                    ProfilePanel = _profilePanel,
                    ShipyardPanel = _shipyardPanel,
                    MainMenuConfirmPanel = _mainMenuConfirmPanel,
                    MainMenuDefaultSelection = _mainMenuDefaultSelection
                });

            _shopTransactionHandler.ConfigureDependencies(
                new HarborShopTransactionHandler.DependencyBundle
                {
                    SaveManager = _saveManager,
                    HookShop = _hookShop,
                    BoatShop = _boatShop
                });
            _shopTransactionHandler.SetUnlockAllShopItemsForQa(_unlockAllShopItemsForQa);

            _fisheryTransactionHandler.ConfigureDependencies(
                new HarborFisheryTransactionHandler.DependencyBundle
                {
                    SaveManager = _saveManager,
                    FishShop = _fishShop
                });

            _shipyardTransactionHandler.ConfigureDependencies(
                new HarborShipyardTransactionHandler.DependencyBundle
                {
                    SaveManager = _saveManager
                });
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

        public void OnProfileRequested()
        {
            OpenProfileMenu();
        }

        public void OnShipyardRequested()
        {
            OpenShipyardMenu();
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

        public void OnFishShopQuestAcceptRequested()
        {
            HandleFishQuestAccept();
        }

        public void OnFishShopQuestClaimRequested()
        {
            HandleFishQuestClaim();
        }

        public void OnFisheryRequested()
        {
            OpenFisheryMenu();
        }

        public void OnFisheryPreviousRequested()
        {
            ShiftFisheryCard(-1);
        }

        public void OnFisheryNextRequested()
        {
            ShiftFisheryCard(1);
        }

        public void OnFisheryBackRequested()
        {
            OpenFishShopMenu();
        }

        public void OnShipyardHookRequested(string hookId)
        {
            EquipHookFromShipyard(hookId);
        }

        public void OnShipyardShipRequested(string shipId)
        {
            EquipShipFromShipyard(shipId);
        }

        public void SetUnlockAllShopItemsForQa(bool enabled)
        {
            _unlockAllShopItemsForQa = enabled;
            _shopTransactionHandler.SetUnlockAllShopItemsForQa(enabled);
            RefreshShopMenuDetails();
        }

        public void OnShopBackRequested()
        {
            if (_menuRouter.ActiveMenu == HarborMenuType.None)
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

        public void OnMainMenuRequested()
        {
            OpenMainMenuConfirmMenu();
        }

        public void OnMainMenuConfirmAccepted()
        {
            PlaySfx(SfxEvent.UiSelect);
            CloseShopMenus(selectMainAction: false);
            SetStatus("Returning to main menu...");
            PushActivity("Returned to main menu.");
            _orchestrator?.RequestOpenMainMenu();
        }

        public void OnMainMenuConfirmDeclined()
        {
            PlaySfx(SfxEvent.UiCancel);
            CloseShopMenus(selectMainAction: true);
            SetStatus("Harbor operations menu ready.");
            PushActivity("Stayed in harbor.");
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
                SetStatus("Warehouse is unavailable.");
                PushActivity("Warehouse unavailable.");
                return;
            }

            _menuRouter.OpenMenu(HarborMenuType.Hook, _hookShopPanel, _hookShopDefaultSelection);
            SetStatus("Warehouse open. Purchase hooks for Shipyard inventory.");
            PlaySfx(SfxEvent.UiSelect);
            PushActivity("Opened warehouse.");
            RefreshHookShopDetails();
            SelectFirstInteractable(_hookShopButtons, _hookShopDefaultSelection);
        }

        private void OpenBoatShopMenu()
        {
            if (_boatShop == null)
            {
                SetStatus("Dockyard is unavailable.");
                PushActivity("Dockyard unavailable.");
                return;
            }

            _menuRouter.OpenMenu(HarborMenuType.Boat, _boatShopPanel, _boatShopDefaultSelection);
            SetStatus("Dockyard open. Purchase ships for Shipyard inventory.");
            PlaySfx(SfxEvent.UiSelect);
            PushActivity("Opened dockyard.");
            RefreshBoatShopDetails();
            SelectFirstInteractable(_boatShopButtons, _boatShopDefaultSelection);
        }

        private void OpenFishShopMenu()
        {
            if (_fishShop == null)
            {
                SetStatus("Fishery is unavailable.");
                PushActivity("Fishery unavailable.");
                return;
            }

            _menuRouter.OpenMenu(HarborMenuType.Fish, _fishShopPanel, _fishShopDefaultSelection);
            SetStatus("Fishery open. Sell cargo, track daily fish, and manage the Fishing Charter.");
            PlaySfx(SfxEvent.UiSelect);
            PushActivity("Opened fishery.");
            RefreshFishShopDetails();
            SetSelected(_fishShopDefaultSelection);
        }

        private void OpenFisheryMenu()
        {
            _menuRouter.OpenMenu(HarborMenuType.Fishery, _fisheryPanel, _fisheryDefaultSelection);
            SetStatus("Fishery open. Review capture cards, fish stats, and latest catch details.");
            PlaySfx(SfxEvent.UiSelect);
            PushActivity("Opened fishery cards.");
            RefreshFisheryCard(forceRebuild: true);
            SetSelected(_fisheryDefaultSelection);
        }

        private void OpenProfileMenu()
        {
            _menuRouter.OpenMenu(HarborMenuType.Profile, _profilePanel, _profileDefaultSelection);
            SetStatus("Profile menu open. Review progression and catches.");
            PlaySfx(SfxEvent.UiSelect);
            PushActivity("Opened profile menu.");
        }

        private void OpenShipyardMenu()
        {
            _menuRouter.OpenMenu(HarborMenuType.Shipyard, _shipyardPanel, _shipyardDefaultSelection);
            SetStatus("Shipyard open. Equip owned gear and set sail.");
            PlaySfx(SfxEvent.UiSelect);
            PushActivity("Opened shipyard.");
            RefreshShipyardDetails();
            SelectFirstInteractable(_shipyardShipButtons, _shipyardDefaultSelection);
        }

        private void OpenMainMenuConfirmMenu()
        {
            _menuRouter.OpenMenu(HarborMenuType.MainMenuConfirm, _mainMenuConfirmPanel, _mainMenuConfirmDefaultSelection);
            SetStatus("Return to the main menu?");
            PlaySfx(SfxEvent.UiSelect);
            PushActivity("Prompted for main menu confirmation.");
        }

        private void HandleHookShopSelection(string hookId)
        {
            ApplyTransactionResult(_shopTransactionHandler.HandleHookPurchase(hookId, ResolveHookMenuOrder()));
        }

        private void HandleBoatShopSelection(string shipId)
        {
            ApplyTransactionResult(_shopTransactionHandler.HandleBoatPurchase(shipId, ResolveShipMenuOrder()));
        }

        private void HandleFishShopSale()
        {
            ApplyTransactionResult(_fisheryTransactionHandler.HandleSellAll());
        }

        private void HandleFishQuestAccept()
        {
            ApplyTransactionResult(_fisheryTransactionHandler.HandleQuestAccept());
        }

        private void HandleFishQuestClaim()
        {
            ApplyTransactionResult(_fisheryTransactionHandler.HandleQuestClaim());
        }

        private void EquipHookFromShipyard(string hookId)
        {
            ApplyTransactionResult(_shipyardTransactionHandler.HandleEquipHook(hookId));
        }

        private void EquipShipFromShipyard(string shipId)
        {
            ApplyTransactionResult(_shipyardTransactionHandler.HandleEquipShip(shipId));
        }

        private void HandleSail()
        {
            ApplyTransactionResult(_shipyardTransactionHandler.HandleSail(ResolveCargoCapacity));
        }

        private void CloseShopMenus(bool selectMainAction)
        {
            _menuRouter.CloseMenus(selectMainAction);
        }

        private void ApplyTransactionResult(HarborTransactionResult result)
        {
            if (result == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(result.StatusMessage))
            {
                SetStatus(result.StatusMessage);
            }

            if (!string.IsNullOrWhiteSpace(result.ActivityMessage))
            {
                PushActivity(result.ActivityMessage);
            }

            if (result.SoundEffect.HasValue)
            {
                PlaySfx(result.SoundEffect.Value);
            }

            if (result.RefreshSaveSnapshot)
            {
                RefreshSaveSnapshot();
            }

            if (result.RefreshShopMenuDetails)
            {
                RefreshShopMenuDetails();
            }

            if (result.RefreshShipyardDetails)
            {
                RefreshShipyardDetails();
            }

            if (result.RequestOpenFishing)
            {
                _orchestrator?.RequestOpenFishing();
            }
        }

        private void RefreshShopMenuDetails()
        {
            RefreshHookShopDetails();
            RefreshBoatShopDetails();
            RefreshFishShopDetails();
            RefreshFisheryCard(forceRebuild: false);
            RefreshShipyardDetails();
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
                "Purchase hooks to add them to Shipyard inventory:";
            for (var i = 0; i < hookOrder.Length; i++)
            {
                var hookId = hookOrder[i];
                var isOwned = save.ownedHooks != null && save.ownedHooks.Contains(hookId);
                var isEquipped = string.Equals(save.equippedHookId, hookId, StringComparison.Ordinal);
                var isUnlocked = IsShopItemUnlocked(hookId);
                var unlockLevel = _saveManager.GetUnlockLevel(hookId);
                var price = _hookShop.GetPrice(hookId);
                var hasRequiredPreviousTier = HasRequiredPreviousTierForShop(save.ownedHooks, hookId, hookOrder, out var requiredTierId);
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
                "Purchase ships to add them to Shipyard inventory:";
            for (var i = 0; i < shipOrder.Length; i++)
            {
                var shipId = shipOrder[i];
                var isOwned = save.ownedShips != null && save.ownedShips.Contains(shipId);
                var isEquipped = string.Equals(save.equippedShipId, shipId, StringComparison.Ordinal);
                var isUnlocked = IsShopItemUnlocked(shipId);
                var unlockLevel = _saveManager.GetUnlockLevel(shipId);
                var price = _boatShop.GetPrice(shipId);
                var hasRequiredPreviousTier = HasRequiredPreviousTierForShop(save.ownedShips, shipId, shipOrder, out var requiredTierId);
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

            var save = _saveManager != null ? _saveManager.Current : null;
            if (_fishShop == null || save == null)
            {
                _fishShopInfoText.text = "Fishery summary unavailable.";
                return;
            }

            var marketSnapshot = _fishShop.BuildMarketSnapshot(maxHistoryEntries: 5);
            var summary = _fishShop.PreviewSellAll();
            var fishCount = CountCargoFish(save);
            var cargoCapacity = ResolveCargoCapacity(save.equippedShipId);
            _fishShopInfoText.text = _shopViewPresenter.BuildFishShopDetails(
                new HarborShopViewPresenter.FishShopDetailsRequest
                {
                    MarketSnapshot = marketSnapshot,
                    PendingSaleSummary = summary,
                    FishCount = fishCount,
                    CargoCapacity = cargoCapacity,
                    BalanceCopecs = save.copecs
                });
            RefreshFishQuestButtons(marketSnapshot);
        }

        private void RefreshFishQuestButtons(FishMarketSnapshot snapshot)
        {
            var buttonState = _shopViewPresenter.BuildFishQuestButtonState(snapshot);
            ApplyShopButtonState(_fishQuestAcceptButton, buttonState.AcceptInteractable, buttonState.AcceptLabel);
            ApplyShopButtonState(_fishQuestFulfillButton, buttonState.FulfillInteractable, buttonState.FulfillLabel);
        }

        private void ShiftFisheryCard(int direction)
        {
            if (_fisheryCardFishIds.Count == 0)
            {
                RefreshFisheryCard(forceRebuild: true);
                return;
            }

            var cardCount = _fisheryCardFishIds.Count;
            if (cardCount <= 0)
            {
                return;
            }

            _fisheryCardIndex += direction;
            while (_fisheryCardIndex < 0)
            {
                _fisheryCardIndex += cardCount;
            }

            while (_fisheryCardIndex >= cardCount)
            {
                _fisheryCardIndex -= cardCount;
            }

            RefreshFisheryCard(forceRebuild: false);
            PlaySfx(SfxEvent.UiSelect);
        }

        private void RefreshFisheryCard(bool forceRebuild)
        {
            if (_fisheryCardText == null)
            {
                return;
            }

            EnsureFisheryCardList(forceRebuild);
            if (_fisheryCardFishIds.Count == 0)
            {
                _fisheryCardText.text = "Fishery catalog unavailable.";
                if (_fisheryCardIcon != null)
                {
                    _fisheryCardIcon.sprite = null;
                    _fisheryCardIcon.color = new Color(0.65f, 0.65f, 0.65f, 0.92f);
                }

                return;
            }

            _fisheryCardIndex = Mathf.Clamp(_fisheryCardIndex, 0, _fisheryCardFishIds.Count - 1);
            var fishId = _fisheryCardFishIds[_fisheryCardIndex];
            FishDefinitionSO fishDefinition = null;
            if (_catalogService != null && !string.IsNullOrWhiteSpace(fishId))
            {
                _catalogService.TryGetFish(fishId, out fishDefinition);
            }

            CatchLogEntry latestCatch;
            if (!TryGetLatestLandedCatchForFish(fishId, out latestCatch))
            {
                latestCatch = null;
            }
            var hasLifetimeStats = TryGetFisheryLifetimeStats(fishId, out var landedCount, out var bestWeightKg, out var bestValueCopecs);
            _fisheryCardText.text = _fisheryCardPresenter.BuildCardText(
                new HarborFisheryCardViewPresenter.CardRequest
                {
                    CardIndex = _fisheryCardIndex,
                    TotalCards = _fisheryCardFishIds.Count,
                    FishId = fishId,
                    FishDefinition = fishDefinition,
                    LatestCatch = latestCatch,
                    HasLifetimeStats = hasLifetimeStats,
                    LandedCount = landedCount,
                    BestWeightKg = bestWeightKg,
                    BestValueCopecs = bestValueCopecs
                });
            if (_fisheryCardIcon != null)
            {
                _fisheryCardIcon.sprite = fishDefinition != null ? fishDefinition.icon : null;
                _fisheryCardIcon.color = _fisheryCardIcon.sprite != null
                    ? new Color(1f, 1f, 1f, 0.98f)
                    : new Color(0.65f, 0.65f, 0.65f, 0.92f);
            }
        }

        private void EnsureFisheryCardList(bool forceRebuild)
        {
            if (!forceRebuild && _fisheryCardFishIds.Count > 0)
            {
                return;
            }

            _fisheryCardFishIds.Clear();
            var seenFishIds = new HashSet<string>(StringComparer.Ordinal);
            if (_catalogService != null && _catalogService.FishById != null)
            {
                foreach (var pair in _catalogService.FishById)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key) || !seenFishIds.Add(pair.Key))
                    {
                        continue;
                    }

                    _fisheryCardFishIds.Add(pair.Key);
                }
            }

            if (_saveManager != null && _saveManager.Current != null && _saveManager.Current.catchLog != null)
            {
                var catchLog = _saveManager.Current.catchLog;
                for (var i = 0; i < catchLog.Count; i++)
                {
                    var entry = catchLog[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.fishId) || !seenFishIds.Add(entry.fishId))
                    {
                        continue;
                    }

                    _fisheryCardFishIds.Add(entry.fishId);
                }
            }

            _fisheryCardFishIds.Sort(StringComparer.Ordinal);
            if (_fisheryCardFishIds.Count == 0)
            {
                _fisheryCardFishIds.Add("fish_cod");
            }

            _fisheryCardIndex = Mathf.Clamp(_fisheryCardIndex, 0, _fisheryCardFishIds.Count - 1);
        }

        private bool TryGetLatestLandedCatchForFish(string fishId, out CatchLogEntry entry)
        {
            entry = null;
            var save = _saveManager != null ? _saveManager.Current : null;
            if (save == null || save.catchLog == null || save.catchLog.Count == 0 || string.IsNullOrWhiteSpace(fishId))
            {
                return false;
            }

            for (var i = save.catchLog.Count - 1; i >= 0; i--)
            {
                var candidate = save.catchLog[i];
                if (candidate == null || !candidate.landed)
                {
                    continue;
                }

                if (!string.Equals(candidate.fishId, fishId, StringComparison.Ordinal))
                {
                    continue;
                }

                entry = candidate;
                return true;
            }

            return false;
        }

        private bool TryGetFisheryLifetimeStats(string fishId, out int landedCount, out float bestWeightKg, out int bestValueCopecs)
        {
            landedCount = 0;
            bestWeightKg = 0f;
            bestValueCopecs = 0;

            var save = _saveManager != null ? _saveManager.Current : null;
            if (save == null || save.catchLog == null || save.catchLog.Count == 0 || string.IsNullOrWhiteSpace(fishId))
            {
                return false;
            }

            for (var i = 0; i < save.catchLog.Count; i++)
            {
                var entry = save.catchLog[i];
                if (entry == null || !entry.landed || !string.Equals(entry.fishId, fishId, StringComparison.Ordinal))
                {
                    continue;
                }

                landedCount++;
                bestWeightKg = Mathf.Max(bestWeightKg, Mathf.Max(0f, entry.weightKg));
                bestValueCopecs = Mathf.Max(bestValueCopecs, Mathf.Max(0, entry.valueCopecs));
            }

            return landedCount > 0;
        }

        private void RefreshShipyardDetails()
        {
            if (_saveManager == null || _saveManager.Current == null)
            {
                SetText(_shipyardInfoText, "Shipyard inventory unavailable.");
                SetText(_shipyardCargoText, "Cargo hold unavailable.");
                RefreshShipyardHookButtons(null);
                RefreshShipyardShipButtons(null);
                RefreshSailButton(save: null, allowSail: false, fishCount: 0, cargoCapacity: 0);
                return;
            }

            var save = _saveManager.Current;
            var shipId = save.equippedShipId;
            var hookId = save.equippedHookId;
            var fishCount = CountCargoFish(save);
            var cargoCapacity = ResolveCargoCapacity(shipId);

            SetText(
                _shipyardInfoText,
                _shopViewPresenter.BuildShipyardInfoText(
                    shipId,
                    hookId,
                    CountOwned(save.ownedShips),
                    CountOwned(save.ownedHooks)));
            SetText(
                _shipyardCargoText,
                _shopViewPresenter.BuildShipyardCargoText(save, fishCount, cargoCapacity));

            RefreshShipyardHookButtons(save);
            RefreshShipyardShipButtons(save);
            RefreshSailButton(save, allowSail: fishCount < cargoCapacity, fishCount, cargoCapacity);
        }

        private void RefreshShipyardHookButtons(SaveDataV1 save)
        {
            var hookOrder = ResolveHookMenuOrder();
            for (var i = 0; i < hookOrder.Length; i++)
            {
                var hookId = hookOrder[i];
                var button = i >= 0 && i < _shipyardHookButtons.Count ? _shipyardHookButtons[i] : null;
                if (button == null)
                {
                    continue;
                }

                if (save == null)
                {
                    ApplyShopButtonState(button, false, "Unavailable");
                    continue;
                }

                var isOwned = save.ownedHooks != null && save.ownedHooks.Contains(hookId);
                var isEquipped = string.Equals(save.equippedHookId, hookId, StringComparison.Ordinal);
                if (!isOwned)
                {
                    ApplyShopButtonState(button, false, "Not Owned");
                    continue;
                }

                ApplyShopButtonState(button, !isEquipped, isEquipped ? "Equipped" : "Equip");
            }
        }

        private void RefreshShipyardShipButtons(SaveDataV1 save)
        {
            var shipOrder = ResolveShipMenuOrder();
            for (var i = 0; i < shipOrder.Length; i++)
            {
                var shipId = shipOrder[i];
                var button = i >= 0 && i < _shipyardShipButtons.Count ? _shipyardShipButtons[i] : null;
                if (button == null)
                {
                    continue;
                }

                if (save == null)
                {
                    ApplyShopButtonState(button, false, "Unavailable");
                    continue;
                }

                var isOwned = save.ownedShips != null && save.ownedShips.Contains(shipId);
                var isEquipped = string.Equals(save.equippedShipId, shipId, StringComparison.Ordinal);
                if (!isOwned)
                {
                    ApplyShopButtonState(button, false, "Not Owned");
                    continue;
                }

                ApplyShopButtonState(button, !isEquipped, isEquipped ? "Selected" : "Select");
            }
        }

        private static int CountOwned(List<string> ownedIds)
        {
            return ownedIds != null ? Mathf.Max(0, ownedIds.Count) : 0;
        }

        private string BuildShopItemLine(
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
            return _shopViewPresenter.BuildShopItemLine(
                itemId,
                owned,
                equipped,
                unlocked,
                unlockLevel,
                price,
                hasRequiredPreviousTier,
                requiredTierId,
                balance);
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
                var isUnlocked = IsShopItemUnlocked(hookId);
                var unlockLevel = _saveManager.GetUnlockLevel(hookId);
                var hasRequiredPreviousTier = HasRequiredPreviousTierForShop(save.ownedHooks, hookId, hookOrder, out _);
                var price = _hookShop.GetPrice(hookId);
                var canUpgrade = price >= 0 && save.copecs >= Mathf.Max(0, price);
                var interactable = !isOwned && (isUnlocked && hasRequiredPreviousTier && canUpgrade && price >= 0);
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
                var isUnlocked = IsShopItemUnlocked(shipId);
                var unlockLevel = _saveManager.GetUnlockLevel(shipId);
                var hasRequiredPreviousTier = HasRequiredPreviousTierForShop(save.ownedShips, shipId, shipOrder, out _);
                var price = _boatShop.GetPrice(shipId);
                var canUpgrade = price >= 0 && save.copecs >= Mathf.Max(0, price);
                var interactable = !isOwned && (isUnlocked && hasRequiredPreviousTier && canUpgrade && price >= 0);
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

            if (!IsShopItemUnlocked(normalizedItemId))
            {
                return new Color(0.56f, 0.56f, 0.56f, 0.85f);
            }

            var orderedIds = isHookCatalog ? ResolveHookMenuOrder() : ResolveShipMenuOrder();
            var ownedIds = isHookCatalog ? save.ownedHooks : save.ownedShips;
            if (!HasRequiredPreviousTierForShop(ownedIds, normalizedItemId, orderedIds, out _))
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

        private string BuildShopButtonLabel(
            bool owned,
            bool equipped,
            bool unlocked,
            int unlockLevel,
            bool hasRequiredPreviousTier,
            int price)
        {
            return _shopViewPresenter.BuildShopButtonLabel(
                owned,
                equipped,
                unlocked,
                unlockLevel,
                hasRequiredPreviousTier,
                price);
        }

        private bool IsShopItemUnlocked(string itemId)
        {
            if (_unlockAllShopItemsForQa)
            {
                return true;
            }

            return _saveManager != null && _saveManager.IsContentUnlocked(itemId);
        }

        private bool HasRequiredPreviousTierForShop(List<string> ownedIds, string itemId, string[] orderedIds, out string requiredTierId)
        {
            if (_unlockAllShopItemsForQa)
            {
                requiredTierId = string.Empty;
                return true;
            }

            return HasRequiredPreviousTier(ownedIds, itemId, orderedIds, out requiredTierId);
        }

        private static void ApplyShopButtonState(Button button, bool interactable, string label)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = interactable;
            var labelText = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
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
            SetText(
                _equipmentText,
                $"Equipped Ship: {HarborTextFormatting.ToDisplayLabel(save.equippedShipId)} | Hook: {HarborTextFormatting.ToDisplayLabel(save.equippedHookId)}");
            SetText(_cargoText, $"Cargo: {fishCount}/{cargoCapacity} fish{cargoStatus} | Trips: {totalTrips} | Level: {_saveManager.CurrentLevel}");
            RefreshSailButton(save, !isCargoFull, fishCount, cargoCapacity);
        }

        private void RefreshSelectionHint(WorldInteractable interactable)
        {
            if (_selectionText == null)
            {
                return;
            }

            var interactLabel = ResolveInteractPromptLabel();
            _selectionText.text = _interactionViewPresenter.BuildSelectionHint(interactable, interactLabel);
        }

        private string ResolveInteractPromptLabel()
        {
            var keyboard = _inputRebindingService != null
                ? _inputRebindingService.GetDisplayBindingsForAction("Harbor/Interact", "Keyboard", " / ", 1)
                : string.Empty;
            var gamepad = _inputRebindingService != null
                ? _inputRebindingService.GetDisplayBindingsForAction("Harbor/Interact", "Gamepad", " / ", 1)
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(keyboard) && !string.IsNullOrWhiteSpace(gamepad))
            {
                return $"{keyboard} or {gamepad}";
            }

            if (!string.IsNullOrWhiteSpace(keyboard))
            {
                return keyboard;
            }

            if (!string.IsNullOrWhiteSpace(gamepad))
            {
                return gamepad;
            }

            return "Enter or South Button";
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
            _activityLogText.text = _interactionViewPresenter.BuildActivityLog(entries);
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message ?? string.Empty;
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
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
            if (normalizedId.Contains("lv5"))
            {
                return Mathf.Max(1, _fallbackCargoCapacityTier5);
            }

            if (normalizedId.Contains("lv4"))
            {
                return Mathf.Max(1, _fallbackCargoCapacityTier4);
            }

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
                ApplyShopButtonState(_sailButton, false, "Set Sail");
                return;
            }

            if (!allowSail)
            {
                ApplyShopButtonState(_sailButton, false, $"Set Sail ({fishCount}/{cargoCapacity})");
                return;
            }

            ApplyShopButtonState(_sailButton, true, "Set Sail");
        }
    }
}
