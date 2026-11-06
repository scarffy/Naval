using System.Collections.Generic;
using MiniGame;
using Naval.Core;
using UnityEngine;
using UnityEngine.UI;
using EventType = Naval.Core.EventType;

namespace Naval.Slice
{
    /// <summary>A short feed of what happened at sea, newest at the bottom. Each line fades out after a while.</summary>
    public sealed class SliceNotifications : MonoBehaviour
    {
        const int MaxLines = 6;
        const float LineSeconds = 10f;

        sealed class Line
        {
            public GameObject Object;
            public float Expires;
        }

        [SerializeField] SliceClock clock;
        [SerializeField] RectTransform rows;

        readonly List<Line> lines = new List<Line>();

        void Start()
        {
            if (clock == null || rows == null)
            {
                Debug.LogError("SliceNotifications: the clock and rows fields must be assigned.");
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
            foreach (GameEvent e in events) Add(e);
        }

        void Update()
        {
            while (lines.Count > 0 && lines[0].Expires <= Time.unscaledTime) RemoveFirst();
        }

        void Add(GameEvent e)
        {
            string name = e.Ship == null ? null : SliceUi.ShipName(e.Ship);
            switch (e.Type)
            {
                case EventType.ShipArrived:
                    Add(name + " arrived in " + e.Port.Name + ". Dues " + MoneyManager.Format(e.Dues) + ".", Color.white);
                    break;
                case EventType.PirateAttack:
                    Add("Pirates attacked " + name + ": " + SliceUi.Count(e.CratesLost, "crate") + " taken, hull -" + e.HullDamage + ".", SliceUi.WarnColor);
                    break;
                case EventType.Storm:
                    Add("A storm hit " + name + ": delayed " + SliceUi.Count(e.DelayDays, "day") + ", hull -" + e.HullDamage + ".", SliceUi.WarnColor);
                    break;
                case EventType.Hurricane:
                    Add("A hurricane hit " + name + ": delayed " + SliceUi.Count(e.DelayDays, "day") + ", hull -" + e.HullDamage + ", "
                        + SliceUi.Count(e.CratesLost, "crate") + " washed overboard.", SliceUi.WarnColor);
                    break;
                case EventType.ShipSunk:
                    Add(name + " sank with its cargo.", SliceUi.BadColor);
                    break;
            }
        }

        void Add(string text, Color color)
        {
            if (lines.Count >= MaxLines) RemoveFirst();
            Text label = SliceUi.MakeLabel(rows, text, 26, color, 0f, 34f);
            label.gameObject.AddComponent<Shadow>();
            lines.Add(new Line { Object = label.gameObject, Expires = Time.unscaledTime + LineSeconds });
        }

        void RemoveFirst()
        {
            lines[0].Object.SetActive(false);
            Destroy(lines[0].Object);
            lines.RemoveAt(0);
        }
    }
}
