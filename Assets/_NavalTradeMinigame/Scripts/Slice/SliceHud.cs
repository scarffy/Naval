using System.Globalization;
using MiniGame;
using Naval.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Naval.Slice
{
    /// <summary>
    /// Date, days left, gold against the goal, the speed buttons, and the Restart and Tutorial buttons (both need a second click;
    /// Tutorial calls <see cref="SliceTutorial.Replay"/>, which clears the saved tutorial-seen flag and restarts).
    /// If the data failed to load it shows the error in the status text. Its widgets are set up in Slice.unity.
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
        /// <summary>Needs a second click within a few seconds, so a misclick does not lose the game.</summary>
        [SerializeField] Button restartButton;
        /// <summary>Replays the tutorial in a new game. Like Restart, it needs a second click.</summary>
        [SerializeField] Button tutorialButton;

        const float ConfirmSeconds = 3f;
        Button confirming;
        Text confirmingLabel;
        string confirmingText;
        float confirmUntil = -1f;

        void Start()
        {
            bool ok = SliceUi.IsAssigned(bootstrap, "Bootstrap", this)
                & SliceUi.IsAssigned(clock, "Clock", this)
                & SliceUi.IsAssigned(dateText, "Date Text", this)
                & SliceUi.IsAssigned(daysLeftText, "Days Left Text", this)
                & SliceUi.IsAssigned(goldText, "Gold Text", this)
                & SliceUi.IsAssigned(statusText, "Status Text", this)
                & SliceUi.IsAssigned(restartButton, "Restart Button", this)
                & SliceUi.IsAssigned(tutorialButton, "Tutorial Button", this)
                & SpeedButtonsAssigned()
                & HasLabel(restartButton, "Restart Button")
                & HasLabel(tutorialButton, "Tutorial Button");
            if (!ok)
            {
                enabled = false;
                return;
            }
            restartButton.onClick.AddListener(() => Confirm(restartButton, SliceBootstrap.Restart));
            tutorialButton.onClick.AddListener(() => Confirm(tutorialButton, SliceTutorial.Replay));
            for (int i = 0; i < speedButtons.Length; i++)
            {
                float speed = Speeds[i];
                speedButtons[i].onClick.AddListener(() => clock.SetSpeed(speed));
            }
        }

        /// <summary>The confirm swaps a button's label text, so it needs a Text child. An empty field is reported elsewhere.</summary>
        bool HasLabel(Button button, string field)
        {
            if (button == null || SliceUi.LabelOf(button) != null) return true;
            Debug.LogError("SliceHud on '" + name + "': the button in " + field + " needs a Text label as a child.", this);
            return false;
        }

        /// <summary>The four speed buttons (Pause, 1x, 2x, 4x) must be there, each with an Image to tint for the selected speed.</summary>
        bool SpeedButtonsAssigned()
        {
            if (speedButtons == null)
            {
                Debug.LogError("SliceHud on '" + name + "': the Speed Buttons field is empty. Assign it in the Inspector.", this);
                return false;
            }
            if (speedButtons.Length != Speeds.Length)
            {
                Debug.LogError("SliceHud on '" + name + "': the Speed Buttons field needs " + Speeds.Length
                    + " buttons (Pause, 1x, 2x, 4x) but has " + speedButtons.Length + ". Fix it in the Inspector.", this);
                return false;
            }
            bool ok = true;
            for (int i = 0; i < speedButtons.Length; i++)
            {
                string element = "Speed Buttons element " + i;
                if (!SliceUi.IsAssigned(speedButtons[i], element, this)) ok = false;
                else if (speedButtons[i].image == null)
                {
                    Debug.LogError("SliceHud on '" + name + "': the button in " + element + " has no Image as its Target Graphic. Assign it in the Inspector.", this);
                    ok = false;
                }
            }
            return ok;
        }

        // What is currently shown, so the strings are only rebuilt when something changes.
        bool shownLoadError;
        bool shownState;
        int shownDay;
        int shownGold;
        float shownSpeed = -1f;

        /// <summary>Runs the action on a second click of the same button within the time; the first click only asks "Sure?".</summary>
        void Confirm(Button button, System.Action action)
        {
            if (confirming == button && Time.unscaledTime <= confirmUntil)
            {
                action();
                return;
            }
            ClearConfirm();
            confirming = button;
            confirmingLabel = SliceUi.LabelOf(button);
            confirmingText = confirmingLabel.text;
            confirmingLabel.text = "Sure?";
            confirmUntil = Time.unscaledTime + ConfirmSeconds;
        }

        void ClearConfirm()
        {
            if (confirming == null) return;
            confirmingLabel.text = confirmingText;
            confirming = null;
            confirmingLabel = null;
        }

        void Update()
        {
            if (confirming != null && Time.unscaledTime > confirmUntil) ClearConfirm();

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
