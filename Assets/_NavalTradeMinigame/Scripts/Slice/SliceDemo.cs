using System.Collections.Generic;
using Naval.Core;
using UnityEngine;

namespace Naval.Slice
{
    /// <summary>
    /// TEMPORARY, phase 2 only. The fleet-choice and route screens do not exist yet, so this gives the player
    /// something to watch: it buys two Brigs, opens a few routes and keeps every idle ship sailing to a random
    /// reachable port. It uses only GameState commands. Remove it when the phase 3 screens arrive.
    /// </summary>
    public sealed class SliceDemo : MonoBehaviour
    {
        const string ShipTierName = "Brig";

        [SerializeField] SliceBootstrap bootstrap;
        [SerializeField] SliceClock clock;
        [SerializeField] bool runDemo = true;

        // Presentation only, so a plain System.Random with a fixed seed rather than the game's.
        readonly System.Random random = new System.Random(1770);

        void Start()
        {
            if (!runDemo) return;
            if (bootstrap == null || clock == null)
            {
                Debug.LogError("SliceDemo: the bootstrap and clock fields must be assigned.");
                return;
            }
            GameState state = bootstrap.State;
            if (state == null) return;

            // The two free routes make a three-port line: New Orleans - Havana - Port-au-Prince.
            OpenRoute(state, "Havana", "New Orleans");
            OpenRoute(state, "Havana", "Port-au-Prince");
            for (int i = 0; i < 2; i++)
            {
                Ship ship;
                CommandResult result = state.BuyShip(state.Data.StartPort.Name, ShipTierName, out ship);
                if (!result.Ok) Debug.LogWarning("SliceDemo: could not buy a " + ShipTierName + ": " + result.Message);
            }
            // Any gold left buys more of the network, if it connects and is affordable.
            OpenIfAffordable(state, "Port-au-Prince", "Bridgetown");
            OpenIfAffordable(state, "Havana", "Campeche");

            SendIdleShips(state);
            clock.DayAdvanced += OnDay;
        }

        void OnDestroy()
        {
            if (clock != null) clock.DayAdvanced -= OnDay;
        }

        void OnDay(IReadOnlyList<GameEvent> events)
        {
            GameState state = bootstrap.State;
            if (state != null && state.Outcome == GameOutcome.InProgress) SendIdleShips(state);
        }

        static void OpenRoute(GameState state, string a, string b)
        {
            Route route = state.Data.FindRoute(a, b);
            CommandResult result = state.OpenRoute(route);
            if (!result.Ok) Debug.LogWarning("SliceDemo: could not open " + a + " - " + b + ": " + result.Message);
        }

        static void OpenIfAffordable(GameState state, string a, string b)
        {
            Route route = state.Data.FindRoute(a, b);
            if (route != null && state.Gold >= state.RouteCost(route)) OpenRoute(state, a, b);
        }

        void SendIdleShips(GameState state)
        {
            foreach (Ship ship in new List<Ship>(state.Fleet))
            {
                if (ship.Port == null) continue;
                var targets = new List<Port>();
                foreach (Port port in state.Data.Ports)
                    if (port != ship.Port && state.Routes.Reaches(port)) targets.Add(port);
                string lastFailure = "no port to sail to";
                while (targets.Count > 0)
                {
                    int pick = random.Next(targets.Count);
                    CommandResult result = state.Send(ship, targets[pick].Name);
                    if (result.Ok) { lastFailure = null; break; }
                    lastFailure = result.Message;
                    targets.RemoveAt(pick);
                }
                if (lastFailure != null)
                    Debug.LogWarning("SliceDemo: could not send the ship at " + ship.Port.Name + ": " + lastFailure);
            }
        }
    }
}
