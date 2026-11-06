using System;
using System.Collections.Generic;
using System.Text;
using Naval.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Naval.Slice
{
    /// <summary>Pick a destination for a ship, read the route preview, then set sail.</summary>
    public sealed class SliceSendPanel : MonoBehaviour
    {
        [SerializeField] SliceBootstrap bootstrap;
        [SerializeField] SliceClock clock;
        [SerializeField] GameObject panel;
        [SerializeField] Text titleText;
        [SerializeField] RectTransform destinationRows;
        [SerializeField] Text previewText;
        [SerializeField] Text messageText;
        [SerializeField] Button sailButton;
        [SerializeField] Button backButton;

        Ship ship;
        Port origin;
        Port destination;
        RoutePreview preview;

        /// <summary>Back was pressed. Gives the port and the ship to return to.</summary>
        public event Action<Port, Ship> BackRequested;
        /// <summary>The ship set sail. Gives the port it left and the result to show there.</summary>
        public event Action<Port, CommandResult> Sailed;

        public bool IsOpen { get { return panel.activeSelf; } }

        void Start()
        {
            if (bootstrap == null || clock == null || panel == null || titleText == null || destinationRows == null ||
                previewText == null || messageText == null || sailButton == null || backButton == null)
            {
                Debug.LogError("SliceSendPanel: every field must be assigned.");
                enabled = false;
                return;
            }
            clock.DayAdvanced += OnDay;
            sailButton.onClick.AddListener(SetSail);
            backButton.onClick.AddListener(() => { Action<Port, Ship> handler = BackRequested; if (handler != null) handler(origin, ship); });
        }

        void OnDestroy()
        {
            if (clock != null) clock.DayAdvanced -= OnDay;
        }

        void OnDay(IReadOnlyList<GameEvent> events)
        {
            if (IsOpen && destination != null) Preview();
        }

        public void Show(Ship target)
        {
            GameState state = bootstrap.State;
            if (state == null || target.Port == null) return;
            ship = target;
            origin = target.Port;
            destination = null;
            preview = null;
            messageText.text = "";
            titleText.text = "Send " + SliceUi.ShipName(ship) + " from " + origin.Name;
            panel.SetActive(true);

            SliceUi.Clear(destinationRows);
            foreach (Port port in state.Data.Ports)
            {
                if (port == origin || !state.Routes.Reaches(port)) continue;
                Port chosen = port;
                SliceUi.MakeButton(destinationRows, port.Name, new Vector2(340f, 44f), () => { destination = chosen; Preview(); }, 24);
            }
            ShowPreview();
        }

        public void Hide()
        {
            panel.SetActive(false);
            ship = null;
            destination = null;
            preview = null;
        }

        void Preview()
        {
            preview = bootstrap.State.Preview(ship, destination.Name);
            ShowPreview();
        }

        void ShowPreview()
        {
            sailButton.gameObject.SetActive(preview != null && preview.Ok);
            if (destination == null)
            {
                previewText.text = "Choose a destination.";
                previewText.color = Color.white;
                return;
            }
            if (!preview.Ok)
            {
                previewText.text = preview.Status.Message;
                previewText.color = SliceUi.BadColor;
                return;
            }
            var stops = new StringBuilder();
            foreach (Port stop in preview.Stops) stops.Append(stops.Length == 0 ? "" : " → ").Append(stop.Name);
            var text = new StringBuilder();
            text.Append(stops).Append('\n');
            text.Append("Arrives ").Append(SliceUi.Date(preview.ArrivalDate)).Append(" (").Append(SliceUi.Count(preview.Days, "day")).Append(")\n");
            text.Append("Pirate risk: ").Append(SliceUi.Percent(preview.PirateRiskPercent)).Append('\n');
            if (preview.HurricaneRiskPercent > 0)
                text.Append("Hurricane season risk: ").Append(SliceUi.Percent(preview.HurricaneRiskPercent)).Append('\n');
            text.Append("Chance of losing this ship: ").Append(SliceUi.Percent(preview.LossChancePercent))
                .Append(" (hull ").Append(ship.Hull).Append('/').Append(ship.MaxHull).Append(')');
            previewText.text = text.ToString();
            previewText.color = Color.white;
        }

        void SetSail()
        {
            if (ship == null || destination == null) return;
            CommandResult result = bootstrap.State.Send(ship, destination.Name);
            if (!result.Ok)
            {
                SliceUi.ShowResult(messageText, result);
                return;
            }
            Port from = origin;
            Action<Port, CommandResult> handler = Sailed;
            if (handler != null) handler(from, result);
        }
    }
}
