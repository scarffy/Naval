using MiniGame;
using Naval.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Naval.Slice
{
    /// <summary>The full-screen win or lose panel. It also covers the map, so nothing behind it can be clicked.</summary>
    public sealed class SliceEndScreen : MonoBehaviour
    {
        [SerializeField] SliceBootstrap bootstrap;
        [SerializeField] GameObject panel;
        [SerializeField] Text titleText;
        [SerializeField] Text detailText;
        [SerializeField] Button restartButton;

        bool ready;

        public bool IsOpen { get { return ready && panel.activeSelf; } }

        void Start()
        {
            bool ok = SliceUi.IsAssigned(bootstrap, "Bootstrap", this)
                & SliceUi.IsAssigned(panel, "Panel", this)
                & SliceUi.IsAssigned(titleText, "Title Text", this)
                & SliceUi.IsAssigned(detailText, "Detail Text", this)
                & SliceUi.IsAssigned(restartButton, "Restart Button", this);
            if (!ok)
            {
                enabled = false;
                return;
            }
            restartButton.onClick.AddListener(SliceBootstrap.Restart);
            ready = true;
        }

        public void Show(GameState state)
        {
            if (!ready) return;
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
                string reason = state.LostBecause == FailReason.NoShips
                    ? "No ships left and not enough gold to buy one."
                    : "Time is up: the mission's " + state.Data.Rules.MissionDays + " days have passed.";
                detailText.text = reason + "\n" + standing;
            }
            panel.SetActive(true);
        }
    }
}
