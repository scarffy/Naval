using System.Globalization;
using MiniGame;
using Naval.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Naval.Slice
{
    /// <summary>
    /// Date, days left, gold against the goal, the speed buttons, and a placeholder Won/Lost text.
    /// If the data failed to load it shows the error instead. The scene builder creates the widgets.
    /// </summary>
    public sealed class SliceHud : MonoBehaviour
    {
        static readonly float[] Speeds = { 0f, 1f, 2f, 4f };
        static readonly Color ActiveColor = new Color(1f, 0.85f, 0.4f);
        static readonly Color IdleColor = new Color(0.75f, 0.75f, 0.75f);

        [SerializeField] SliceBootstrap bootstrap;
        [SerializeField] SliceClock clock;
        [SerializeField] Text dateText;
        [SerializeField] Text daysLeftText;
        [SerializeField] Text goldText;
        [SerializeField] Text statusText;
        /// <summary>In the order Pause, 1x, 2x, 4x.</summary>
        [SerializeField] Button[] speedButtons;

        void Start()
        {
            if (bootstrap == null || clock == null || dateText == null || daysLeftText == null ||
                goldText == null || statusText == null || speedButtons == null || speedButtons.Length != Speeds.Length)
            {
                Debug.LogError("SliceHud: every field must be assigned (four speed buttons).");
                enabled = false;
                return;
            }
            for (int i = 0; i < speedButtons.Length; i++)
            {
                float speed = Speeds[i];
                speedButtons[i].onClick.AddListener(() => clock.SetSpeed(speed));
            }
        }

        void Update()
        {
            GameState state = bootstrap.State;
            if (state == null)
            {
                statusText.text = bootstrap.LoadError ?? "The game did not start.";
                statusText.color = new Color(1f, 0.4f, 0.4f);
                return;
            }

            CultureInfo c = CultureInfo.InvariantCulture;
            dateText.text = state.Date.ToString("d MMM yyyy", c);
            daysLeftText.text = state.DaysLeft.ToString("N0", c) + " days left";
            goldText.text = MoneyManager.Format(state.Gold) + "   (goal " + MoneyManager.Format(state.Data.Rules.GoalGold) + ")";
            for (int i = 0; i < speedButtons.Length; i++)
                speedButtons[i].image.color = Mathf.Approximately(clock.Speed, Speeds[i]) ? ActiveColor : IdleColor;

            // Placeholder: the real win and lose screens come with the screens phase.
            statusText.color = Color.white;
            if (state.Outcome == GameOutcome.Won) statusText.text = "Won";
            else if (state.Outcome == GameOutcome.Lost) statusText.text = "Lost";
            else statusText.text = "";
        }
    }
}
