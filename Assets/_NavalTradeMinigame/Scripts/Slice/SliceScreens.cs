using Naval.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Naval.Slice
{
    /// <summary>
    /// Runs the flow of the game: choose a fleet, choose the free routes, play, then the end screen.
    /// Opens and closes the screens so that at most one decision panel is open, and holds the clock while the player decides.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class SliceScreens : MonoBehaviour
    {
        enum Phase { ChooseFleet, FreeRoutes, Play, Over }

        [SerializeField] SliceBootstrap bootstrap;
        [SerializeField] SliceClock clock;
        [SerializeField] SliceMap map;
        [SerializeField] SliceFleetChoice fleetChoice;
        [SerializeField] SliceRoutePanel routePanel;
        [SerializeField] SlicePortPanel portPanel;
        [SerializeField] SliceSendPanel sendPanel;
        [SerializeField] SliceEndScreen endScreen;
        [SerializeField] SliceFleetList fleetList;
        [SerializeField] GameObject banner;
        [SerializeField] Text bannerText;

        int shownRoutesLeft = -1;

        void Start()
        {
            if (bootstrap == null || clock == null || map == null || fleetChoice == null || routePanel == null ||
                portPanel == null || sendPanel == null || endScreen == null || fleetList == null || banner == null ||
                bannerText == null)
            {
                Debug.LogError("SliceScreens: every field must be assigned.");
                enabled = false;
                return;
            }
            map.PortClicked += OnPortClicked;
            map.RouteClicked += OnRouteClicked;
            fleetList.ShipClicked += OnShipClicked;
            routePanel.CloseRequested += CloseDecisionPanels;
            portPanel.CloseRequested += CloseDecisionPanels;
            portPanel.SendRequested += OnSendRequested;
            sendPanel.BackRequested += OnSendBack;
            sendPanel.Sailed += OnSailed;
        }

        void OnDestroy()
        {
            if (map != null)
            {
                map.PortClicked -= OnPortClicked;
                map.RouteClicked -= OnRouteClicked;
            }
            if (fleetList != null) fleetList.ShipClicked -= OnShipClicked;
            if (routePanel != null) routePanel.CloseRequested -= CloseDecisionPanels;
            if (portPanel != null)
            {
                portPanel.CloseRequested -= CloseDecisionPanels;
                portPanel.SendRequested -= OnSendRequested;
            }
            if (sendPanel != null)
            {
                sendPanel.BackRequested -= OnSendBack;
                sendPanel.Sailed -= OnSailed;
            }
        }

        void Update()
        {
            GameState state = bootstrap.State;
            if (state == null) return;
            Phase phase = CurrentPhase(state);

            fleetChoice.SetVisible(phase == Phase.ChooseFleet);
            banner.SetActive(phase == Phase.FreeRoutes);
            if (phase == Phase.FreeRoutes && state.Routes.FreeRoutesLeft != shownRoutesLeft)
            {
                shownRoutesLeft = state.Routes.FreeRoutesLeft;
                bannerText.text = "Choose your free routes: " + shownRoutesLeft + " left. Click a route on the map that touches your network.";
            }
            if (phase == Phase.Over && !endScreen.IsOpen)
            {
                CloseDecisionPanels();
                endScreen.Show(state);
            }

            UpdateHold(phase);
        }

        void UpdateHold(Phase phase)
        {
            clock.Held = phase == Phase.ChooseFleet || phase == Phase.FreeRoutes
                || routePanel.IsOpen || portPanel.IsOpen || sendPanel.IsOpen;
        }

        // A click can arrive before the clock's Update in the same frame, so the hold is set as soon as a panel opens.
        void HoldNow()
        {
            GameState state = bootstrap.State;
            if (state != null) UpdateHold(CurrentPhase(state));
        }

        static Phase CurrentPhase(GameState state)
        {
            if (state.Outcome != GameOutcome.InProgress) return Phase.Over;
            if (!state.FleetChosen) return Phase.ChooseFleet;
            return state.Routes.FreeRoutesLeft > 0 ? Phase.FreeRoutes : Phase.Play;
        }

        void CloseDecisionPanels()
        {
            routePanel.Hide();
            portPanel.Hide();
            sendPanel.Hide();
        }

        void OnPortClicked(Port port)
        {
            OpenPort(port, null, null);
        }

        void OnShipClicked(Ship ship)
        {
            if (ship.Port != null) OpenPort(ship.Port, ship, null);
        }

        void OpenPort(Port port, Ship ship, CommandResult result)
        {
            GameState state = bootstrap.State;
            if (state == null || CurrentPhase(state) != Phase.Play) return;
            CloseDecisionPanels();
            portPanel.Show(port, ship, result);
            HoldNow();
        }

        void OnRouteClicked(Route route)
        {
            GameState state = bootstrap.State;
            if (state == null) return;
            Phase phase = CurrentPhase(state);
            if (phase != Phase.FreeRoutes && phase != Phase.Play) return;
            CloseDecisionPanels();
            routePanel.Show(route);
            HoldNow();
        }

        void OnSendRequested(Ship ship)
        {
            CloseDecisionPanels();
            sendPanel.Show(ship);
            HoldNow();
        }

        void OnSendBack(Port port, Ship ship)
        {
            OpenPort(port, ship, null);
        }

        void OnSailed(Port port, CommandResult result)
        {
            OpenPort(port, null, result);
        }
    }
}
