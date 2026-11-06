using System;
using MiniGame;
using Naval.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Naval.Slice
{
    /// <summary>Details of one route, with a button to open it when it is not owned yet.</summary>
    public sealed class SliceRoutePanel : MonoBehaviour
    {
        [SerializeField] SliceBootstrap bootstrap;
        [SerializeField] GameObject panel;
        [SerializeField] Text titleText;
        [SerializeField] Text detailText;
        [SerializeField] Text messageText;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;

        Route route;

        public event Action CloseRequested;

        public bool IsOpen { get { return panel.activeSelf; } }

        void Start()
        {
            if (bootstrap == null || panel == null || titleText == null || detailText == null || messageText == null ||
                openButton == null || closeButton == null)
            {
                Debug.LogError("SliceRoutePanel: every field must be assigned.");
                enabled = false;
                return;
            }
            openButton.onClick.AddListener(OpenRoute);
            closeButton.onClick.AddListener(() => { Action handler = CloseRequested; if (handler != null) handler(); });
        }

        public void Show(Route target)
        {
            route = target;
            messageText.text = "";
            panel.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            panel.SetActive(false);
            route = null;
        }

        void Refresh()
        {
            GameState state = bootstrap.State;
            if (state == null || route == null) return;
            bool owned = state.Routes.Owns(route);
            int cost = state.RouteCost(route);
            titleText.text = route.A.Name + " – " + route.B.Name;
            string kind = route.Ocean ? "Ocean route (sloops cannot use it)" : "Coastal route";
            detailText.text = kind + "\nDistance " + route.Distance.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
                + "\nChance per day at sea:\n  storm " + SliceUi.Percent(route.Storm * 100)
                + "   pirates " + SliceUi.Percent(route.Pirates * 100)
                + "\n  hurricane " + SliceUi.Percent(route.Hurricanes * 100) + " at the peak of the season (Aug to Oct)"
                + "\n" + (owned ? "Owned" : "Cost: " + (cost == 0 ? "Free" : MoneyManager.Format(cost)));
            openButton.gameObject.SetActive(!owned);
        }

        void OpenRoute()
        {
            if (route == null) return;
            CommandResult result = bootstrap.State.OpenRoute(route);
            SliceUi.ShowResult(messageText, result);
            Refresh();
        }
    }
}
