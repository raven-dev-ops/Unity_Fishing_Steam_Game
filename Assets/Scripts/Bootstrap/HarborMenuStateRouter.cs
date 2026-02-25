using UnityEngine;
using UnityEngine.EventSystems;

namespace RavenDevOps.Fishing.Core
{
    internal enum HarborMenuType
    {
        None = 0,
        Hook = 1,
        Boat = 2,
        Fish = 3,
        Fishery = 4,
        Profile = 5,
        Shipyard = 6,
        MainMenuConfirm = 7
    }

    internal sealed class HarborMenuStateRouter
    {
        public sealed class DependencyBundle
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
        }

        private DependencyBundle _dependencies = new DependencyBundle();

        public HarborMenuType ActiveMenu { get; private set; }

        public void ConfigureDependencies(DependencyBundle dependencies)
        {
            _dependencies = dependencies ?? new DependencyBundle();
        }

        public void OpenMenu(HarborMenuType menuType, GameObject menuPanel, GameObject defaultSelection)
        {
            ActiveMenu = menuType;
            SetPanel(_dependencies.ActionPanel, false);
            SetPanel(_dependencies.HookShopPanel, menuType == HarborMenuType.Hook);
            SetPanel(_dependencies.BoatShopPanel, menuType == HarborMenuType.Boat);
            SetPanel(_dependencies.FishShopPanel, menuType == HarborMenuType.Fish);
            SetPanel(_dependencies.FisheryPanel, menuType == HarborMenuType.Fishery);
            SetPanel(_dependencies.ProfilePanel, menuType == HarborMenuType.Profile);
            SetPanel(_dependencies.ShipyardPanel, menuType == HarborMenuType.Shipyard);
            SetPanel(_dependencies.MainMenuConfirmPanel, menuType == HarborMenuType.MainMenuConfirm);
            if (menuPanel != null)
            {
                menuPanel.transform.SetAsLastSibling();
            }

            SetSelected(defaultSelection);
        }

        public void CloseMenus(bool selectMainAction)
        {
            ActiveMenu = HarborMenuType.None;
            SetPanel(_dependencies.ActionPanel, true);
            SetPanel(_dependencies.HookShopPanel, false);
            SetPanel(_dependencies.BoatShopPanel, false);
            SetPanel(_dependencies.FishShopPanel, false);
            SetPanel(_dependencies.FisheryPanel, false);
            SetPanel(_dependencies.ProfilePanel, false);
            SetPanel(_dependencies.ShipyardPanel, false);
            SetPanel(_dependencies.MainMenuConfirmPanel, false);
            if (selectMainAction)
            {
                SetSelected(_dependencies.MainMenuDefaultSelection);
            }
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
    }
}
