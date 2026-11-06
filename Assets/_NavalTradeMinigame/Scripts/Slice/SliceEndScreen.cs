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

        public bool IsOpen { get { return panel.activeSelf; } }

        void Start()
        {
            if (bootstrap == null || panel == null || titleText == null || detailText == null || restartButton == null)
            {
                Debug.LogError("SliceEndScreen: every field must be assigned.");
                enabled = false;
                return;
            }
            restartButton.onClick.AddListener(SliceBootstrap.Restart);
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
                string reason = state.LostBecause == FailReason.NoShips
                    ? "No ships left and not enough gold to buy one."
                    : "Time is up: the mission's " + state.Data.Rules.MissionDays + " days have passed.";
                detailText.text = reason + "\n" + standing;
            }
            panel.SetActive(true);
        }
    }
}
