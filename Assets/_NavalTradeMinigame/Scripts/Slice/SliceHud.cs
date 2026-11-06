using System.Globalization;
using MiniGame;
using Naval.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Naval.Slice
{
    /// <summary>
    /// Date, days left, gold against the goal and the speed buttons.
    /// If the data failed to load it shows the error in the status text. The scene builder creates the widgets.
    /// </summary>
    public sealed class SliceHud : MonoBehaviour
    {
        static readonly float[] Speeds = { 0f, 1f, 2f, 4f };

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

        // What is currently shown, so the strings are only rebuilt when something changes.
        bool shownLoadError;
        bool shownState;
        int shownDay;
        int shownGold;
        float shownSpeed = -1f;

        void Update()
        {
            GameState state = bootstrap.State;
            if (state == null)
            {
                if (shownLoadError) return;
                shownLoadError = true;
                statusText.text = bootstrap.LoadError ?? "The game did not start.";
                statusText.color = new Color(1f, 0.4f, 0.4f);
                return;
            }

            if (!shownState || state.Day != shownDay || state.Gold != shownGold)
            {
                dateText.text = SliceUi.Date(state.Date);
                daysLeftText.text = state.DaysLeft.ToString("N0", CultureInfo.InvariantCulture) + " days left";
                goldText.text = MoneyManager.Format(state.Gold) + "   (goal " + MoneyManager.Format(state.Data.Rules.GoalGold) + ")";
                shownDay = state.Day;
                shownGold = state.Gold;
            }

            if (!Mathf.Approximately(clock.Speed, shownSpeed))
            {
                shownSpeed = clock.Speed;
                for (int i = 0; i < speedButtons.Length; i++)
                    speedButtons[i].image.color = Mathf.Approximately(shownSpeed, Speeds[i]) ? SliceUi.SelectedColor : SliceUi.ButtonColor;
            }

            shownState = true;
        }
    }
}
