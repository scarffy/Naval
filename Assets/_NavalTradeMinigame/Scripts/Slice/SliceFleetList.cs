using System;
using Naval.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Naval.Slice
{
    /// <summary>One line per ship at the bottom right. A ship in port can be clicked to open that port. Rebuilt only when the fleet changes.</summary>
    public sealed class SliceFleetList : MonoBehaviour
    {
        [SerializeField] SliceBootstrap bootstrap;
        [SerializeField] SliceWidgets widgets;
        [SerializeField] RectTransform rows;

        int shownKey;
        bool shown;

        public event Action<Ship> ShipClicked;

        void Start()
        {
            bool ok = SliceUi.IsAssigned(bootstrap, "Bootstrap", this)
                & SliceUi.IsAssigned(widgets, "Widgets", this)
                & SliceUi.IsAssigned(rows, "Rows", this);
            if (!ok) enabled = false;
        }

        void Update()
        {
            GameState state = bootstrap.State;
            if (state == null) return;
            int key = Key(state);
            if (shown && key == shownKey) return;
            shown = true;
            shownKey = key;
            Rebuild(state);
        }

        /// <summary>A number that changes whenever a ship's line would: no strings built, so it is cheap to check every frame.</summary>
        static int Key(GameState state)
        {
            int key = state.Fleet.Count;
            foreach (Ship ship in state.Fleet)
            {
                Port at = ship.Port ?? ship.Voyage.Destination;
                key = key * 31 + ship.Id;
                key = key * 31 + at.Index * 2 + (ship.Port == null ? 1 : 0);
                key = key * 31 + ship.Hull;
                key = key * 31 + ship.MaxHull;
            }
            return key;
        }

        void Rebuild(GameState state)
        {
            SliceUi.Clear(rows);
            foreach (Ship ship in state.Fleet)
            {
                string name = SliceUi.ShipName(ship);
                if (ship.Port == null)
                {
                    widgets.MakeLabel(rows, name + " – at sea to " + ship.Voyage.Destination.Name, 20, Color.white, 0f, 36f);
                    continue;
                }
                Ship clicked = ship;
                Button button = widgets.MakeButton(rows, name + " – " + ship.Port.Name + " – hull " + ship.Hull + "/" + ship.MaxHull,
                    new Vector2(520f, 36f), () => { Action<Ship> handler = ShipClicked; if (handler != null) handler(clicked); }, 20);
                SliceUi.LabelOf(button).alignment = TextAnchor.MiddleLeft;
            }
        }
    }
}
