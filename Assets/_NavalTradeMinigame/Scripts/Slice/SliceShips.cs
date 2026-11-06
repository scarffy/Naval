using System.Collections.Generic;
using Naval.Core;
using UnityEngine;

namespace Naval.Slice
{
    /// <summary>
    /// One marker per ship. In port it sits beside the port; at sea it is placed along its voyage, moving
    /// smoothly between the progress at the last day tick and the current one.
    /// The picture trails the rules by up to a day: a ship that arrives is shown reaching the port before it settles in.
    /// </summary>
    public sealed class SliceShips : MonoBehaviour
    {
        sealed class Marker
        {
            public Ship Ship;
            public Transform Transform;
            public Voyage Voyage;     // the voyage being drawn, or null when drawn in port
            public bool Finished;     // the drawn voyage has ended, the marker is on its way to the last stop
            public float Prev;
            public float Cur;
        }

        [SerializeField] SliceBootstrap bootstrap;
        [SerializeField] SliceClock clock;
        [SerializeField] SliceMap map;
        [SerializeField] float shipSize = 1.8f;

        readonly Dictionary<int, Marker> markers = new Dictionary<int, Marker>();
        readonly Dictionary<Port, int> slots = new Dictionary<Port, int>();
        readonly HashSet<int> alive = new HashSet<int>();
        readonly List<int> gone = new List<int>();

        void Start()
        {
            if (bootstrap == null || clock == null || map == null)
            {
                Debug.LogError("SliceShips: the bootstrap, clock and map fields must be assigned.");
                enabled = false;
                return;
            }
            clock.DayAdvanced += OnDay;
        }

        void OnDestroy()
        {
            if (clock != null) clock.DayAdvanced -= OnDay;
        }

        void OnDay(IReadOnlyList<GameEvent> events)
        {
            GameState state = bootstrap.State;
            if (state == null) return;
            Sync(state);
            foreach (Marker m in markers.Values)
            {
                Voyage now = m.Ship.Voyage;
                if (m.Voyage == now && now != null)
                {
                    m.Prev = m.Cur;
                    m.Cur = (float)now.Progress;
                }
                else if (m.Voyage != null && !m.Finished)
                {
                    // The drawn voyage ended (arrived): finish the trip on screen this day.
                    m.Prev = m.Cur;
                    m.Cur = 1f;
                    m.Finished = true;
                }
                else
                {
                    m.Voyage = now;
                    m.Finished = false;
                    m.Prev = 0f;
                    m.Cur = now != null ? (float)now.Progress : 0f;
                }
            }
        }

        void LateUpdate()
        {
            GameState state = bootstrap.State;
            if (state == null) return;
            Sync(state);
            slots.Clear();
            float fraction = clock.DayFraction;
            foreach (Marker m in markers.Values)
            {
                if (m.Voyage != null) PlaceAtSea(m, fraction);
                else PlaceInPort(m);
            }
        }

        /// <summary>Creates markers for new ships and removes those of ships that are gone.</summary>
        void Sync(GameState state)
        {
            alive.Clear();
            foreach (Ship ship in state.Fleet)
            {
                alive.Add(ship.Id);
                if (!markers.ContainsKey(ship.Id)) markers[ship.Id] = Create(ship);
            }
            gone.Clear();
            foreach (int id in markers.Keys) if (!alive.Contains(id)) gone.Add(id);
            foreach (int id in gone)
            {
                Destroy(markers[id].Transform.gameObject);
                markers.Remove(id);
            }
        }

        Marker Create(Ship ship)
        {
            var go = new GameObject("Ship " + ship.Id + " (" + ship.Tier.Name + ")");
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(shipSize, shipSize, 1f) * map.Scale;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SliceSprites.Triangle();
            sr.sortingOrder = 20;
            int tier = 0;
            IReadOnlyList<ShipTier> tiers = bootstrap.State.Data.ShipTiers;
            for (int i = 0; i < tiers.Count; i++) if (tiers[i] == ship.Tier) tier = i;
            sr.color = Color.HSVToRGB((0.02f + 0.17f * tier + 0.07f * (ship.Id % 3)) % 1f, 0.7f, 1f);
            return new Marker { Ship = ship, Transform = go.transform };
        }

        void PlaceAtSea(Marker m, float fraction)
        {
            Port from, to;
            double t;
            m.Voyage.PositionAt(Mathf.Lerp(m.Prev, m.Cur, fraction), out from, out to, out t);
            Vector3 a = map.PortPosition(from);
            Vector3 b = map.PortPosition(to);
            m.Transform.position = Vector3.Lerp(a, b, (float)t);
            Vector3 d = b - a;
            m.Transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
        }

        void PlaceInPort(Marker m)
        {
            // A ship just sent (not yet ticked) has no port but a voyage that starts where it was.
            Port port = m.Ship.Port ?? (m.Ship.Voyage != null ? m.Ship.Voyage.Stops[0] : null);
            if (port == null) return;
            int slot;
            slots.TryGetValue(port, out slot);
            slots[port] = slot + 1;
            Vector3 offset = new Vector3(1.6f + 0.9f * (slot % 4), -1.2f - 0.9f * (slot / 4), 0f) * map.Scale;
            m.Transform.position = map.PortPosition(port) + offset;
        }
    }
}
