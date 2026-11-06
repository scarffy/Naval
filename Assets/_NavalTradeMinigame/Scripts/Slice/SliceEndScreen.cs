using System.Collections.Generic;
using MiniGame;
using Naval.Core;
using UnityEngine;
using UnityEngine.UI;
using EventType = Naval.Core.EventType;

namespace Naval.Slice
{
    /// <summary>The full-screen win or lose panel. It also covers the map, so nothing behind it can be clicked.</summary>
    public sealed class SliceEndScreen : MonoBehaviour
    {
        [SerializeField] SliceBootstrap bootstrap;
        [SerializeField] SliceClock clock;
        [SerializeField] GameObject panel;
        [SerializeField] Text titleText;
        [SerializeField] Text detailText;

        FailReason? failReason;

        public bool IsOpen { get { return panel.activeSelf; } }

        void Start()
        {
            if (bootstrap == null || clock == null || panel == null || titleText == null || detailText == null)
            {
                Debug.LogError("SliceEndScreen: every field must be assigned.");
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
            foreach (GameEvent e in events) if (e.Type == EventType.MissionFailed) failReason = e.Reason;
        }

        public void Show(GameState state)
        {
            string standing = "Gold: " + MoneyManager.Format(state.Gold) + " (goal " + MoneyManager.Format(state.Data.Rules.GoalGold) + ")\n"
                + SliceUi.Date(state.Date);
            if (state.Outcome == GameOutcome.Won)
            {
                titleText.text = "Goal reached";
                detailText.text = standing;
            }
            else
            {
                titleText.text = "Mission failed";
                string reason = failReason == FailReason.NoShips
                    ? "No ships left and not enough gold to buy one."
                    : "The two years are up.";
                detailText.text = reason + "\n" + standing;
            }
            panel.SetActive(true);
        }
    }
}
