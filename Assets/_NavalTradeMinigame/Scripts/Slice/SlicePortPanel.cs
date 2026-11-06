using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MiniGame;
using Naval.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Naval.Slice
{
    /// <summary>
    /// A port: the ships of the player in it (stepped through with a selector), the market for the selected ship, repairs and, at a shipyard,
    /// new ships and upgrades. Everything is rebuilt after every command.
    /// </summary>
    public sealed class SlicePortPanel : MonoBehaviour
    {
        static readonly UpgradeTrack[] Tracks = { UpgradeTrack.Sails, UpgradeTrack.Hull, UpgradeTrack.Hold };
        const float NameWidth = 230f;
        const float CellWidth = 110f;
        const float ButtonWidth = 100f;
        const float RowHeight = 40f;

        [SerializeField] SliceBootstrap bootstrap;
        [SerializeField] SliceClock clock;
        [SerializeField] SliceWidgets widgets;
        [SerializeField] GameObject panel;
        [SerializeField] Text titleText;
        [SerializeField] Text goldText;
        [SerializeField] RectTransform shipRows;
        [SerializeField] Text shipText;
        [SerializeField] Button repairButton;
        [SerializeField] Button sendButton;
        [SerializeField] GameObject shipyardRoot;
        [SerializeField] RectTransform shipyardRows;
        [SerializeField] RectTransform marketRows;
        [SerializeField] Text notTradedText;
        [SerializeField] Text messageText;
        [SerializeField] Button closeButton;

        Port port;
        Ship selected;

        public event Action CloseRequested;
        public event Action<Ship> SendRequested;

        bool ready;

        public bool IsOpen { get { return ready && panel.activeSelf; } }

        /// <summary>True when the panel is open and shows this port.</summary>
        public bool IsOpenAt(Port target) { return IsOpen && port == target; }

        void Start()
        {
            bool ok = SliceUi.IsAssigned(bootstrap, "Bootstrap", this)
                & SliceUi.IsAssigned(clock, "Clock", this)
                & SliceUi.IsAssigned(widgets, "Widgets", this)
                & SliceUi.IsAssigned(panel, "Panel", this)
                & SliceUi.IsAssigned(titleText, "Title Text", this)
                & SliceUi.IsAssigned(goldText, "Gold Text", this)
                & SliceUi.IsAssigned(shipRows, "Ship Rows", this)
                & SliceUi.IsAssigned(shipText, "Ship Text", this)
                & SliceUi.IsAssigned(repairButton, "Repair Button", this)
                & SliceUi.IsAssigned(sendButton, "Send Button", this)
                & SliceUi.IsAssigned(shipyardRoot, "Shipyard Root", this)
                & SliceUi.IsAssigned(shipyardRows, "Shipyard Rows", this)
                & SliceUi.IsAssigned(marketRows, "Market Rows", this)
                & SliceUi.IsAssigned(notTradedText, "Not Traded Text", this)
                & SliceUi.IsAssigned(messageText, "Message Text", this)
                & SliceUi.IsAssigned(closeButton, "Close Button", this);
            if (!ok)
            {
                enabled = false;
                return;
            }
            clock.DayAdvanced += OnDay;
            repairButton.onClick.AddListener(Repair);
            sendButton.onClick.AddListener(() =>
            {
                Action<Ship> handler = SendRequested;
                if (selected != null && handler != null) handler(selected);
            });
            closeButton.onClick.AddListener(() => { Action handler = CloseRequested; if (handler != null) handler(); });
            ready = true;
        }

        void OnDestroy()
        {
            if (clock != null) clock.DayAdvanced -= OnDay;
        }

        void OnDay(IReadOnlyList<GameEvent> events)
        {
            if (IsOpen) Refresh();
        }

        /// <summary>Opens the port with a ship selected (the first one there when null), optionally showing the result of an earlier command.</summary>
        public void Show(Port target, Ship ship, CommandResult result)
        {
            if (!ready) return;
            port = target;
            selected = ship;
            if (result != null) SliceUi.ShowResult(messageText, result); else messageText.text = "";
            panel.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            if (!ready) return;
            panel.SetActive(false);
            port = null;
            selected = null;
        }

        void Refresh()
        {
            GameState state = bootstrap.State;
            if (state == null || port == null) return;
            if (selected == null || selected.Port != port) selected = FirstShipHere(state);

            titleText.text = port.HasShipyard ? port.Name + " – Shipyard" : port.Name;
            goldText.text = "Gold: " + MoneyManager.Format(state.Gold);
            BuildShips(state);
            BuildShipInfo(state);
            BuildMarket(state);
            BuildShipyard(state);
        }

        Ship FirstShipHere(GameState state)
        {
            foreach (Ship ship in state.Fleet) if (ship.Port == port) return ship;
            return null;
        }

        void BuildShips(GameState state)
        {
            SliceUi.Clear(shipRows);
            var here = new List<Ship>();
            foreach (Ship ship in state.Fleet) if (ship.Port == port) here.Add(ship);
            int index = here.IndexOf(selected);
            RectTransform row = widgets.MakeRow(shipRows, 44f);
            Button previous = widgets.MakeButton(row, "<", new Vector2(60f, 44f), () => Step(here, index, -1), 24);
            string text = here.Count == 0 ? "No ships here" : "Ship " + (index + 1) + " of " + here.Count + " here";
            widgets.MakeLabel(row, text, 24, Color.white, 0f, 0f, TextAnchor.MiddleCenter);
            Button next = widgets.MakeButton(row, ">", new Vector2(60f, 44f), () => Step(here, index, 1), 24);
            previous.interactable = here.Count > 1;
            next.interactable = here.Count > 1;
        }

        void Step(List<Ship> here, int index, int direction)
        {
            if (here.Count < 2) return;
            selected = here[(index + direction + here.Count) % here.Count];
            Refresh();
        }

        void BuildShipInfo(GameState state)
        {
            repairButton.gameObject.SetActive(selected != null && selected.Hull < selected.MaxHull);
            sendButton.gameObject.SetActive(selected != null);
            if (selected == null)
            {
                shipText.text = "None of your ships is in " + port.Name + ".";
                return;
            }
            shipText.text = SliceUi.ShipName(selected) + "\nHull " + selected.Hull + "/" + selected.MaxHull
                + "\nHold " + selected.CargoCount + "/" + selected.Capacity + " crates";
            if (repairButton.gameObject.activeSelf)
                SliceUi.LabelOf(repairButton).text = "Repair (" + MoneyManager.Format(state.RepairCost(selected)) + ")";
        }

        void BuildMarket(GameState state)
        {
            SliceUi.Clear(marketRows);
            notTradedText.gameObject.SetActive(false);
            if (selected == null)
            {
                widgets.MakeLabel(marketRows, "Bring a ship here to trade.", 26, Color.white, 0f, RowHeight);
                return;
            }

            RectTransform header = widgets.MakeRow(marketRows, 36f);
            widgets.MakeLabel(header, "Good", 22, SliceUi.WarnColor, NameWidth);
            widgets.MakeLabel(header, "Buy", 22, SliceUi.WarnColor, CellWidth);
            widgets.MakeLabel(header, "Sell", 22, SliceUi.WarnColor, CellWidth);
            widgets.MakeLabel(header, "Aboard", 22, SliceUi.WarnColor, CellWidth);

            var untraded = new StringBuilder();
            foreach (Good good in state.Data.Goods)
            {
                int aboard = selected.CargoOf(good);
                if (!state.Market.Trades(port, good))
                {
                    if (aboard > 0) untraded.Append(untraded.Length == 0 ? "" : ", ").Append(good.Name).Append(' ').Append(aboard);
                    continue;
                }
                Good g = good;
                RectTransform row = widgets.MakeRow(marketRows, RowHeight);
                widgets.MakeLabel(row, good.Name, 26, Color.white, NameWidth);
                widgets.MakeLabel(row, state.BuyPrice(port, good).ToString("N0", CultureInfo.InvariantCulture), 26, Color.white, CellWidth);
                widgets.MakeLabel(row, state.SellPrice(port, good).ToString("N0", CultureInfo.InvariantCulture), 26, Color.white, CellWidth);
                widgets.MakeLabel(row, aboard.ToString(), 26, Color.white, CellWidth);
                TradeButton(row, "Buy 1", true, () => Trade(true, g, 1));
                TradeButton(row, "Buy 10", true, () => Trade(true, g, 10));
                TradeButton(row, "Sell 1", aboard > 0, () => Trade(false, g, 1));
                TradeButton(row, "Sell all", aboard > 0, () => Trade(false, g, selected.CargoOf(g)));
            }
            if (untraded.Length == 0) return;
            notTradedText.text = "Aboard, not traded here: " + untraded;
            notTradedText.gameObject.SetActive(true);
        }

        void TradeButton(Transform row, string label, bool enabled, UnityEngine.Events.UnityAction onClick)
        {
            Button button = widgets.MakeButton(row, label, new Vector2(ButtonWidth, RowHeight), onClick, 22);
            button.interactable = enabled;
        }

        void Trade(bool buy, Good good, int crates)
        {
            GameState state = bootstrap.State;
            CommandResult result = buy ? state.BuyGoods(selected, good.Name, crates) : state.SellGoods(selected, good.Name, crates);
            SliceUi.ShowResult(messageText, result);
            Refresh();
        }

        void BuildShipyard(GameState state)
        {
            shipyardRoot.SetActive(port.HasShipyard);
            SliceUi.Clear(shipyardRows);
            if (!port.HasShipyard) return;
            foreach (ShipTier tier in state.Data.ShipTiers)
            {
                ShipTier t = tier;
                widgets.MakeButton(shipyardRows, "Buy " + tier.Name + " – " + MoneyManager.Format(tier.Price),
                    new Vector2(540f, 44f), () => BuyShip(t), 24);
            }
            if (selected == null) return;
            foreach (UpgradeTrack track in Tracks)
            {
                UpgradeTrack tr = track;
                UpgradeLevel next = state.Data.FindUpgrade(track, selected.Level(track) + 1);
                if (next == null)
                    widgets.MakeLabel(shipyardRows, track + ": complete", 24, Color.white, 0f, 44f);
                else
                    widgets.MakeButton(shipyardRows, track + ": " + next.Name + " – " + MoneyManager.Format(state.UpgradeCost(selected, track)),
                        new Vector2(540f, 44f), () => Upgrade(tr), 24);
            }
        }

        void BuyShip(ShipTier tier)
        {
            Ship bought;
            CommandResult result = bootstrap.State.BuyShip(port.Name, tier.Name, out bought);
            SliceUi.ShowResult(messageText, result);
            if (bought != null) selected = bought;
            Refresh();
        }

        void Upgrade(UpgradeTrack track)
        {
            SliceUi.ShowResult(messageText, bootstrap.State.Upgrade(selected, track));
            Refresh();
        }

        void Repair()
        {
            SliceUi.ShowResult(messageText, bootstrap.State.Repair(selected));
            Refresh();
        }
    }
}
