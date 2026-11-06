using System.Collections.Generic;
using System.Globalization;
using MiniGame;
using Naval.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Naval.Slice
{
    /// <summary>The opening choice: one Merchantman, two Brigs or three Sloops. Shown while the fleet is not chosen.</summary>
    public sealed class SliceFleetChoice : MonoBehaviour
    {
        [SerializeField] SliceBootstrap bootstrap;
        [SerializeField] SliceWidgets widgets;
        [SerializeField] GameObject panel;
        [SerializeField] Text infoText;
        [SerializeField] RectTransform rows;
        [SerializeField] Text messageText;

        bool ready;

        void Start()
        {
            bool ok = SliceUi.IsAssigned(bootstrap, "Bootstrap", this)
                & SliceUi.IsAssigned(widgets, "Widgets", this)
                & SliceUi.IsAssigned(panel, "Panel", this)
                & SliceUi.IsAssigned(infoText, "Info Text", this)
                & SliceUi.IsAssigned(rows, "Rows", this)
                & SliceUi.IsAssigned(messageText, "Message Text", this);
            if (!ok)
            {
                enabled = false;
                return;
            }
            ready = true;
        }

        public void SetVisible(bool visible)
        {
            if (!ready || panel.activeSelf == visible) return;
            panel.SetActive(visible);
            if (visible) Build();
        }

        void Build()
        {
            GameState state = bootstrap.State;
            if (state == null) return;
            GameData data = state.Data;
            infoText.text = "Start gold: " + MoneyManager.Format(state.Gold) + "     Start port: " + data.StartPort.Name;
            messageText.text = "";
            SliceUi.Clear(rows);

            var tiers = new List<ShipTier>(data.ShipTiers);
            tiers.Sort((x, y) => y.Price.CompareTo(x.Price));
            foreach (ShipTier tier in tiers)
            {
                ShipTier chosen = tier;
                int count = data.FleetCount(tier);
                string title = count == 1 ? "1 " + tier.Name : count + " " + tier.Name + "s";
                string stats = "Each: " + SliceUi.Count(tier.Capacity, "crate") + " of hold, speed "
                    + tier.Speed.ToString("0.##", CultureInfo.InvariantCulture) + ", hull " + tier.Hull + ", wages "
                    + MoneyManager.Format(tier.WagePerDay) + "/day at sea" + DockedWageText(tier, data.Rules) + ", "
                    + (tier.OceanGoing ? "ocean-going" : "coastal only");
                string label = title + "\n" + stats + "\nTotal cost " + MoneyManager.Format(count * tier.Price);
                widgets.MakeButton(rows, label, new Vector2(1000f, 130f), () => Choose(chosen), 28);
            }
        }

        /// <summary>" (N in port)" when docked ships pay part of their wages, otherwise nothing.</summary>
        static string DockedWageText(ShipTier tier, Rules rules)
        {
            if (rules.DockedWageFraction <= 0) return "";
            return " (" + MoneyManager.Format(Num.Round(tier.WagePerDay * rules.DockedWageFraction)) + " in port)";
        }

        void Choose(ShipTier tier)
        {
            CommandResult result = bootstrap.State.ChooseFleet(tier.Name);
            SliceUi.ShowResult(messageText, result);
        }
    }
}
