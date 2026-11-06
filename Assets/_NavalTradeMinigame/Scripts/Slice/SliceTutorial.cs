using System;
using System.Collections.Generic;
using MiniGame;
using Naval.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Naval.Slice
{
    /// <summary>
    /// A guided first voyage: buy, load, preview, send, arrive, sell. One objective at a time, with the relevant widget outlined.
    /// It starts once the opening (fleet and free routes) is over, and new voyages are sheltered from hazards while it runs.
    /// The player can skip it at any time. It runs only until it is completed or skipped once (remembered in PlayerPrefs);
    /// <see cref="Replay"/> forgets that.
    /// </summary>
    public sealed class SliceTutorial : MonoBehaviour
    {
        sealed class Step
        {
            public Func<string> Text;
            public Func<bool> Done;
            /// <summary>The widget to outline, or null.</summary>
            public Func<RectTransform> Target;
            /// <summary>A port on the map to outline when Target gives nothing.</summary>
            public Func<Port> MapPort;
            public Action OnDone;
        }

        const string SeenKey = "Naval.Slice.TutorialSeen";
        const float PortBox = 110f;
        const float Pad = 6f;
        /// <summary>Steps before "Arrive" in <see cref="BuildSteps"/>; from there on the tutorial ship must exist.</summary>
        const int SailStepCount = 5;

        [SerializeField] SliceBootstrap bootstrap;
        [SerializeField] SliceMap map;
        [SerializeField] Camera sceneCamera;
        [SerializeField] SlicePortPanel portPanel;
        [SerializeField] SliceSendPanel sendPanel;
        [SerializeField] GameObject box;
        [SerializeField] Text objectiveText;
        [SerializeField] Button skipButton;
        [SerializeField] RectTransform frame;
        [SerializeField] RectTransform marketRows;
        [SerializeField] Button portSendButton;
        [SerializeField] Button portCloseButton;
        [SerializeField] RectTransform destinationRows;
        [SerializeField] Button sailButton;
        [SerializeField] Button fastestSpeedButton;

        readonly List<Step> steps = new List<Step>();
        readonly Vector3[] corners = new Vector3[4];
        RectTransform canvasRect;
        Image[] bars;
        bool running;
        bool finished;
        int index;
        Port startPort;
        string tip = "";
        Ship tutorialShip;
        Port arrivedAt;
        int lastCargo;
        Text skipLabel;

        void Start()
        {
            if (bootstrap == null || map == null || sceneCamera == null || portPanel == null || sendPanel == null ||
                box == null || objectiveText == null || skipButton == null || frame == null || marketRows == null ||
                portSendButton == null || portCloseButton == null || destinationRows == null || sailButton == null ||
                fastestSpeedButton == null)
            {
                Debug.LogError("SliceTutorial: every field must be assigned.");
                enabled = false;
                return;
            }
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("SliceTutorial: must be on or under the canvas.");
                enabled = false;
                return;
            }
            skipLabel = SliceUi.LabelOf(skipButton);
            if (skipLabel == null)
            {
                Debug.LogError("SliceTutorial: the Skip button needs a Text label.");
                enabled = false;
                return;
            }
            canvasRect = (RectTransform)canvas.rootCanvas.transform;
            bars = frame.GetComponentsInChildren<Image>(true);
            skipButton.onClick.AddListener(() => Finish(true));
            box.SetActive(false);
            frame.gameObject.SetActive(false);
            if (PlayerPrefs.GetInt(SeenKey, 0) == 1) finished = true;
        }

        void Update()
        {
            GameState state = bootstrap.State;
            if (state == null || finished) return;
            if (state.Outcome != GameOutcome.InProgress)
            {
                Finish(false);
                return;
            }
            if (!running)
            {
                if (!state.FleetChosen || state.Routes.FreeRoutesLeft > 0) return;
                Begin(state);
            }

            if (tutorialShip == null)
            {
                tutorialShip = FindShelteredShip(state);
                // Only the first voyage is sheltered.
                if (tutorialShip != null) state.ShelterNewVoyages = false;
            }

            // Past the sending step the ship matters; if it is gone there is nothing left to show.
            if (index >= SailStepCount && !InFleet(state, tutorialShip))
            {
                Finish(true);
                return;
            }
            Step step = steps[index];
            if (step.Done())
            {
                if (step.OnDone != null) step.OnDone();
                index++;
                if (index >= steps.Count)
                {
                    Finish(true);
                    return;
                }
                // Reaching the wrap-up counts as seen, even if the game then ends or the player quits before clicking Done.
                if (index == steps.Count - 1) MarkSeen();
                step = steps[index];
            }

            string text = step.Text();
            if (objectiveText.text != text) objectiveText.text = text;
            string skip = index == steps.Count - 1 ? "Done" : "Skip";
            if (skipLabel.text != skip) skipLabel.text = skip;
        }

        void LateUpdate()
        {
            if (!running || finished) return;
            Step step = steps[index];
            RectTransform target = step.Target != null ? step.Target() : null;
            Vector2 center;
            Vector2 size;
            if (target != null && target.gameObject.activeInHierarchy)
            {
                target.GetWorldCorners(corners);
                Vector2 low, high;
                if (!ToCanvas(corners[0], out low) || !ToCanvas(corners[2], out high)) { frame.gameObject.SetActive(false); return; }
                center = (low + high) * 0.5f;
                size = new Vector2(Mathf.Abs(high.x - low.x), Mathf.Abs(high.y - low.y)) + Vector2.one * (2f * Pad);
            }
            else
            {
                Port port = step.MapPort != null ? step.MapPort() : null;
                if (port == null || !ToCanvas(sceneCamera.WorldToScreenPoint(map.PortPosition(port)), out center))
                {
                    frame.gameObject.SetActive(false);
                    return;
                }
                size = Vector2.one * PortBox;
            }
            frame.gameObject.SetActive(true);
            frame.anchoredPosition = center;
            frame.sizeDelta = size;

            Color color = SliceUi.SelectedColor;
            color.a = Mathf.Lerp(0.35f, 1f, Mathf.PingPong(Time.unscaledTime * 1.5f, 1f));
            foreach (Image bar in bars) bar.color = color;
        }

        // Screen point (the canvas is Screen Space Overlay) to the canvas's local space.
        bool ToCanvas(Vector3 screen, out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out local);
        }

        void Begin(GameState state)
        {
            running = true;
            state.ShelterNewVoyages = true;
            startPort = state.Data.StartPort;
            tip = ComputeTip(state);
            BuildSteps(state);
            box.SetActive(true);
        }

        /// <summary>Forgets that the tutorial was seen and starts a new game, so it runs again.</summary>
        public static void Replay()
        {
            // If the reload can't happen, keep the flag so nothing changes.
            if (SliceBootstrap.CanRestart)
            {
                PlayerPrefs.DeleteKey(SeenKey);
                PlayerPrefs.Save();
            }
            SliceBootstrap.Restart();
        }

        static void MarkSeen()
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
        }

        /// <param name="seen">True when the player completed or skipped it (also saved on reaching the last step), so it does not run again.</param>
        void Finish(bool seen)
        {
            if (finished) return;
            finished = true;
            if (seen) MarkSeen();
            GameState state = bootstrap.State;
            if (state != null) state.ShelterNewVoyages = false;
            box.SetActive(false);
            frame.gameObject.SetActive(false);
        }

        static bool InFleet(GameState state, Ship ship)
        {
            if (ship == null) return false;
            foreach (Ship s in state.Fleet) if (s == ship) return true;
            return false;
        }

        /// <summary>The good with the best margin from the start port to a port the first ship can reach, or "" when none pays.</summary>
        string ComputeTip(GameState state)
        {
            if (state.Fleet.Count == 0) return "";
            Ship ship = state.Fleet[0];
            Good bestGood = null;
            Port bestPort = null;
            int best = 0;
            foreach (Port dest in state.Data.Ports)
            {
                if (dest == startPort || state.Routes.ShortestPath(startPort, dest, ship.Tier.OceanGoing) == null) continue;
                foreach (Good good in state.Data.Goods)
                {
                    if (!state.Market.Trades(startPort, good) || !state.Market.Trades(dest, good)) continue;
                    int margin = state.SellPrice(dest, good) - state.BuyPrice(startPort, good);
                    if (margin <= best) continue;
                    best = margin;
                    bestGood = good;
                    bestPort = dest;
                }
            }
            return bestGood == null ? "" : bestGood.Name + " sells for more in " + bestPort.Name + ".";
        }

        void BuildSteps(GameState state)
        {
            steps.Clear();
            string start = startPort.Name;
            steps.Add(new Step
            {
                Text = () => "Your fleet waits in " + start + ". Click " + start + " on the map to open its market.",
                Done = () => portPanel.IsOpenAt(startPort) || Sailed(),
                MapPort = () => startPort
            });
            steps.Add(new Step
            {
                Text = () => "Buy goods: click Buy 10 next to a good." + (tip.Length > 0 ? " " + tip : "")
                    + (portPanel.IsOpenAt(startPort) ? "" : " Click " + start + " to open its market."),
                Done = () => AnyCargo(state) || Sailed(),
                Target = () => PortTarget(startPort, marketRows),
                MapPort = () => startPort
            });
            steps.Add(new Step
            {
                Text = () => "The crates went straight into the ship's hold. Buy more to fill it while your gold allows, then click Send.",
                Done = () => sendPanel.IsOpen || Sailed(),
                Target = SendTarget,
                MapPort = () => startPort
            });
            steps.Add(new Step
            {
                Text = () => "Pick a destination and read the preview: the arrival date, the pirate and hurricane risk, and the chance of losing the ship."
                    + (sendPanel.IsOpen ? "" : " Click Send in " + start + "'s market first."),
                Done = () => sendPanel.HasPreview || Sailed(),
                Target = SendTarget,
                MapPort = () => startPort
            });
            steps.Add(new Step
            {
                Text = () => "Your first voyage sails under escort, so nothing will happen to it on the way. Click Set sail."
                    + (sendPanel.IsOpen ? "" : " Click Send in " + start + "'s market first."),
                Done = () => Sailed(),
                Target = SendTarget,
                MapPort = () => startPort
            });
            steps.Add(new Step
            {
                Text = () => "Your ship is at sea. Close the port and watch it sail. 2× and 4× speed up time.",
                Done = () => tutorialShip.Port != null,
                Target = () => portPanel.IsOpen ? portCloseButton.GetComponent<RectTransform>() : fastestSpeedButton.GetComponent<RectTransform>(),
                OnDone = () =>
                {
                    arrivedAt = tutorialShip.Port;
                    lastCargo = tutorialShip.CargoCount;
                }
            });
            steps.Add(new Step
            {
                Text = () => "Your ship reached " + arrivedAt.Name + ". Click " + arrivedAt.Name + " and sell your cargo with Sell all.",
                Done = SoldOrLeft,
                Target = () => PortTarget(arrivedAt, marketRows),
                MapPort = () => arrivedAt
            });
            steps.Add(new Step
            {
                Text = () => "That's the trade loop. Reach " + MoneyManager.Format(state.Data.Rules.GoalGold) + " by "
                    + SliceUi.Date(state.Calendar.DateOf(state.Data.Rules.MissionDays))
                    + ". Ports pay more for Gulf and Caribbean goods in hurricane season, but the risks rise too.",
                Done = () => false   // the Done button (the skip button) ends this step
            });
        }

        static bool AnyCargo(GameState state)
        {
            foreach (Ship ship in state.Fleet) if (ship.CargoCount > 0) return true;
            return false;
        }

        /// <summary>True once a ship set sail under the tutorial's shelter (the player may get there before the text asks).</summary>
        bool Sailed()
        {
            return tutorialShip != null;
        }

        /// <summary>The widget inside the port panel when it shows this port; Close when it shows another; null (outline the port on the map) when it is shut.</summary>
        RectTransform PortTarget(Port port, RectTransform inside)
        {
            if (portPanel.IsOpenAt(port)) return inside;
            return portPanel.IsOpen ? (RectTransform)portCloseButton.transform : null;
        }

        /// <summary>Where the player is on the way to setting sail: Set sail, the destinations, Send in the start port's market, or the start port.</summary>
        RectTransform SendTarget()
        {
            if (sendPanel.IsOpen) return sendPanel.HasPreview ? (RectTransform)sailButton.transform : destinationRows;
            return PortTarget(startPort, (RectTransform)portSendButton.transform);
        }

        /// <summary>
        /// True once the player sells from the tutorial ship (crates bought here first don't hide the sale), once the ship has left
        /// the port, or when nothing aboard sells here, so the player is never stuck on this step.
        /// </summary>
        bool SoldOrLeft()
        {
            GameState state = bootstrap.State;
            if (tutorialShip.Port != arrivedAt) return true;
            int cargo = tutorialShip.CargoCount;
            bool sold = cargo < lastCargo;
            lastCargo = cargo;
            return sold || !SellsAny(state, tutorialShip, arrivedAt);
        }

        static bool SellsAny(GameState state, Ship ship, Port port)
        {
            foreach (Good good in state.Data.Goods)
                if (ship.CargoOf(good) > 0 && state.Market.Trades(port, good)) return true;
            return false;
        }

        static Ship FindShelteredShip(GameState state)
        {
            foreach (Ship ship in state.Fleet) if (ship.Voyage != null && ship.Voyage.Sheltered) return ship;
            return null;
        }
    }
}
