using System;
using System.Collections.Generic;
using System.IO;
using Naval.Slice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Naval.SliceEditor
{
    /// <summary>Builds Slice.unity from scratch: camera, game objects, HUD and the references between them.</summary>
    public static class SliceSceneBuilder
    {
        static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        static readonly Vector2 TopRight = new Vector2(1f, 1f);
        static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        static readonly Vector2 BottomRight = new Vector2(1f, 0f);
        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);

        const string ScenesFolder = "Assets/_NavalTradeMinigame/Scenes";
        const string ScenePath = ScenesFolder + "/Slice.unity";

        [MenuItem("Naval/Build Slice Scene", true)]
        static bool CanBuild() { return !EditorApplication.isPlaying; }

        [MenuItem("Naval/Build Slice Scene")]
        public static void Build()
        {
            string disk = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ScenePath));
            if (File.Exists(disk) && !EditorUtility.DisplayDialog("Build Slice Scene",
                    ScenePath + " already exists. Replace it?", "Replace", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = BuildCamera();
            var game = new GameObject("Game");
            var bootstrap = game.AddComponent<SliceBootstrap>();
            var clock = game.AddComponent<SliceClock>();
            var map = game.AddComponent<SliceMap>();
            var ships = game.AddComponent<SliceShips>();

            Transform root = BuildCanvas(game.transform);
            Button fastest;
            SliceHud hud = BuildHud(root, out fastest);
            Screens screens = BuildScreens(root, bootstrap, clock, map);
            BuildTutorial(root, bootstrap, map, camera, screens, fastest);   // last, so it renders on top

            SetRef(clock, "bootstrap", bootstrap);
            SetRef(map, "bootstrap", bootstrap);
            SetRef(map, "sceneCamera", camera);
            SetRef(ships, "bootstrap", bootstrap);
            SetRef(ships, "clock", clock);
            SetRef(ships, "map", map);
            SetRef(hud, "bootstrap", bootstrap);
            SetRef(hud, "clock", clock);

            if (!AssetDatabase.IsValidFolder(ScenesFolder))
                AssetDatabase.CreateFolder("Assets/_NavalTradeMinigame", "Scenes");
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            AddToBuildSettings();
            AssetDatabase.Refresh();
            Debug.Log("Built " + ScenePath);
        }

        /// <summary>Restart reloads the scene by its build index, so the scene has to be in Build Settings.</summary>
        static void AddToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (scene.path != ScenePath) continue;
                scene.enabled = true;
                found = true;
            }
            if (!found) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static Camera BuildCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            Camera camera = go.AddComponent<Camera>();
            // SliceMap frames the camera and sets the sea colour at Start.
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            go.transform.position = new Vector3(0f, 0f, -10f);
            go.AddComponent<AudioListener>();
            go.AddComponent<Physics2DRaycaster>();   // lets the EventSystem click ports and routes
            return camera;
        }

        static Transform BuildCanvas(Transform parent)
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(parent, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            ConfigureScaler(scaler);
            canvasGo.AddComponent<GraphicRaycaster>();
            BuildEventSystem();
            return canvasGo.transform;
        }

        static SliceHud BuildHud(Transform root, out Button fastest)
        {
            Text date = SliceUi.MakeText(root, "Date", 44, TextAnchor.UpperLeft, new Vector2(700f, 56f));
            SliceUi.Place(date, TopLeft, new Vector2(30f, -20f));
            Text days = SliceUi.MakeText(root, "DaysLeft", 30, TextAnchor.UpperLeft, new Vector2(700f, 40f));
            SliceUi.Place(days, TopLeft, new Vector2(30f, -78f));
            Text gold = SliceUi.MakeText(root, "Gold", 34, TextAnchor.UpperLeft, new Vector2(900f, 44f));
            SliceUi.Place(gold, TopLeft, new Vector2(30f, -118f));
            // Only shown when the data fails to load.
            Text status = SliceUi.MakeText(root, "Status", 36, TextAnchor.MiddleCenter, new Vector2(1600f, 400f));
            SliceUi.Place(status, Center, Vector2.zero);

            string[] labels = { "Pause", "1×", "2×", "4×" };
            var buttons = new Button[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                // Right-aligned row, Pause leftmost.
                float x = -30f - (labels.Length - 1 - i) * 130f;
                buttons[i] = SliceUi.MakeButton(root, labels[i], new Vector2(120f, 64f), null, 30);
                buttons[i].name = "Speed " + labels[i];
                SliceUi.Place(buttons[i], TopRight, new Vector2(x, -24f));
            }

            fastest = buttons[buttons.Length - 1];
            // Under 4× and 2×, clear of the tutorial box.
            Button restart = SliceUi.MakeButton(root, "Restart", new Vector2(120f, 44f), null, 24);
            restart.name = "Restart";
            SliceUi.Place(restart, TopRight, new Vector2(-30f, -98f));
            Button tutorial = SliceUi.MakeButton(root, "Tutorial", new Vector2(120f, 44f), null, 24);
            tutorial.name = "Tutorial";
            SliceUi.Place(tutorial, TopRight, new Vector2(-160f, -98f));

            var hud = root.gameObject.AddComponent<SliceHud>();
            SetRef(hud, "restartButton", restart);
            SetRef(hud, "tutorialButton", tutorial);
            SetRef(hud, "dateText", date);
            SetRef(hud, "daysLeftText", days);
            SetRef(hud, "goldText", gold);
            SetRef(hud, "statusText", status);
            var so = new SerializedObject(hud);
            SerializedProperty array = Prop(so, "speedButtons");
            array.arraySize = buttons.Length;
            for (int i = 0; i < buttons.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return hud;
        }

        /// <summary>
        /// Scale with screen size, 1920x1080, matching width and height equally. Set through the serialized fields because
        /// the compile check cannot see the enums of CanvasScaler.
        /// </summary>
        static void ConfigureScaler(CanvasScaler scaler)
        {
            var so = new SerializedObject(scaler);
            Prop(so, "m_UiScaleMode").intValue = 1;                      // ScaleMode.ScaleWithScreenSize
            Prop(so, "m_ReferenceResolution").vector2Value = new Vector2(1920f, 1080f);
            Prop(so, "m_ScreenMatchMode").intValue = 0;                  // ScreenMatchMode.MatchWidthOrHeight
            Prop(so, "m_MatchWidthOrHeight").floatValue = 0.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static SerializedProperty Prop(SerializedObject so, string name)
        {
            SerializedProperty property = so.FindProperty(name);
            if (property == null) throw new InvalidOperationException("No serialized field '" + name + "'");
            return property;
        }

        /// <summary>
        /// The slice uses the old Input Manager (Project Settings > Player > Active Input Handling = Input Manager),
        /// so UI clicks go through StandaloneInputModule.
        /// </summary>
        static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);

        /// <summary>The panels and widgets the tutorial points at.</summary>
        sealed class Screens
        {
            public SlicePortPanel Port;
            public SliceSendPanel Send;
            public RectTransform MarketRows;
            public Button PortSend;
            public Button PortClose;
            public RectTransform DestinationRows;
            public Button Sail;
        }

        static Screens BuildScreens(Transform root, SliceBootstrap bootstrap, SliceClock clock, SliceMap map)
        {
            GameObject host = root.gameObject;
            var screens = host.AddComponent<SliceScreens>();

            var fleetList = host.AddComponent<SliceFleetList>();
            RectTransform fleetRows = SliceUi.MakeColumn(root, "Fleet List", new Vector2(520f, 420f), 4f, TextAnchor.LowerRight);
            // Bottom right: open sea, clear of the port markers.
            SliceUi.Place(fleetRows, BottomRight, new Vector2(-30f, 30f));
            SetRef(fleetList, "bootstrap", bootstrap);
            SetRef(fleetList, "rows", fleetRows);

            var notifications = host.AddComponent<SliceNotifications>();
            RectTransform feedRows = SliceUi.MakeColumn(root, "Feed", new Vector2(520f, 260f), 4f, TextAnchor.LowerLeft);
            SliceUi.Place(feedRows, BottomLeft, new Vector2(30f, 30f));
            SetRef(notifications, "clock", clock);
            SetRef(notifications, "rows", feedRows);

            GameObject banner = SliceUi.MakePanel(root, "Banner", new Vector2(800f, 90f), new Color(0f, 0f, 0f, 0.6f));
            banner.GetComponent<Image>().raycastTarget = false;
            SliceUi.Place(banner.transform, BottomCenter, new Vector2(0f, 30f));
            Text bannerText = SliceUi.MakeText(banner.transform, "Text", 28, TextAnchor.MiddleCenter, new Vector2(780f, 86f));
            SliceUi.Place(bannerText, Center, Vector2.zero);
            banner.SetActive(false);

            SetRef(screens, "bootstrap", bootstrap);
            SetRef(screens, "clock", clock);
            SetRef(screens, "map", map);
            SetRef(screens, "fleetList", fleetList);
            SetRef(screens, "banner", banner);
            SetRef(screens, "bannerText", bannerText);
            SetRef(screens, "fleetChoice", BuildFleetChoice(root, host, bootstrap));
            SetRef(screens, "routePanel", BuildRoutePanel(root, host, bootstrap));
            var widgets = new Screens();
            widgets.Port = BuildPortPanel(root, host, bootstrap, clock, widgets);
            widgets.Send = BuildSendPanel(root, host, bootstrap, clock, widgets);
            SetRef(screens, "portPanel", widgets.Port);
            SetRef(screens, "sendPanel", widgets.Send);
            SetRef(screens, "endScreen", BuildEndScreen(root, host, bootstrap));
            return widgets;
        }

        static GameObject BuildFrame(Transform root, string name, Vector2 size, string title, int titleSize)
        {
            GameObject panel = SliceUi.MakePanel(root, name, size, SliceUi.PanelColor);
            Text text = SliceUi.MakeText(panel.transform, "Title", titleSize, TextAnchor.UpperLeft, new Vector2(size.x - 60f, 64f));
            text.text = title;
            SliceUi.Place(text, TopLeft, new Vector2(30f, -20f));
            return panel;
        }

        static Text AddMessage(GameObject panel, float width)
        {
            Text message = SliceUi.MakeText(panel.transform, "Message", 28, TextAnchor.MiddleLeft, new Vector2(width, 40f));
            SliceUi.Place(message, BottomLeft, new Vector2(30f, 90f));
            return message;
        }

        static SliceFleetChoice BuildFleetChoice(Transform root, GameObject host, SliceBootstrap bootstrap)
        {
            GameObject panel = BuildFrame(root, "Fleet Choice", new Vector2(1100f, 700f), "Choose your fleet", 48);
            Text info = SliceUi.MakeText(panel.transform, "Info", 28, TextAnchor.UpperLeft, new Vector2(1040f, 40f));
            SliceUi.Place(info, TopLeft, new Vector2(30f, -92f));
            RectTransform rows = SliceUi.MakeColumn(panel.transform, "Rows", new Vector2(1000f, 420f));
            SliceUi.Place(rows, TopCenter, new Vector2(0f, -150f));
            Text message = SliceUi.MakeText(panel.transform, "Message", 28, TextAnchor.MiddleLeft, new Vector2(1040f, 40f));
            SliceUi.Place(message, BottomLeft, new Vector2(30f, 24f));

            var choice = host.AddComponent<SliceFleetChoice>();
            SetRef(choice, "bootstrap", bootstrap);
            SetRef(choice, "panel", panel);
            SetRef(choice, "infoText", info);
            SetRef(choice, "rows", rows);
            SetRef(choice, "messageText", message);
            panel.SetActive(false);
            return choice;
        }

        static SliceRoutePanel BuildRoutePanel(Transform root, GameObject host, SliceBootstrap bootstrap)
        {
            GameObject panel = BuildFrame(root, "Route Panel", new Vector2(760f, 540f), "Route", 44);
            Text detail = SliceUi.MakeText(panel.transform, "Detail", 30, TextAnchor.UpperLeft, new Vector2(700f, 280f));
            SliceUi.Place(detail, TopLeft, new Vector2(30f, -100f));
            Text message = AddMessage(panel, 700f);
            Button open = SliceUi.MakeButton(panel.transform, "Open route", new Vector2(220f, 60f), null, 28);
            SliceUi.Place(open, BottomLeft, new Vector2(30f, 24f));
            Button close = SliceUi.MakeButton(panel.transform, "Close", new Vector2(160f, 60f), null, 28);
            SliceUi.Place(close, BottomRight, new Vector2(-30f, 24f));

            var routePanel = host.AddComponent<SliceRoutePanel>();
            SetRef(routePanel, "bootstrap", bootstrap);
            SetRef(routePanel, "panel", panel);
            SetRef(routePanel, "titleText", panel.transform.Find("Title").GetComponent<Text>());
            SetRef(routePanel, "detailText", detail);
            SetRef(routePanel, "messageText", message);
            SetRef(routePanel, "openButton", open);
            SetRef(routePanel, "closeButton", close);
            panel.SetActive(false);
            return routePanel;
        }

        static SlicePortPanel BuildPortPanel(Transform root, GameObject host, SliceBootstrap bootstrap, SliceClock clock, Screens widgets)
        {
            GameObject panel = BuildFrame(root, "Port Panel", new Vector2(1700f, 960f), "Port", 48);
            Transform p = panel.transform;
            Text gold = SliceUi.MakeText(p, "Gold", 34, TextAnchor.UpperRight, new Vector2(500f, 44f));
            SliceUi.Place(gold, TopRight, new Vector2(-30f, -28f));

            // Left: ships in port, the selected ship, repair and send, the shipyard.
            RectTransform left = SliceUi.MakeColumn(p, "Left", new Vector2(560f, 720f));
            SliceUi.Place(left, TopLeft, new Vector2(30f, -100f));
            SliceUi.MakeLabel(left, "Your ships here", 24, SliceUi.WarnColor, 0f, 32f);
            RectTransform shipRows = SliceUi.MakeColumn(left, "Ships", Vector2.zero, 4f);
            Text shipText = SliceUi.MakeLabel(left, "", 26, Color.white);
            RectTransform actions = SliceUi.MakeRow(left, 44f);
            Button repair = SliceUi.MakeButton(actions, "Repair", new Vector2(266f, 44f), null, 24);
            Button send = SliceUi.MakeButton(actions, "Send", new Vector2(266f, 44f), null, 24);
            RectTransform shipyard = SliceUi.MakeColumn(left, "Shipyard", Vector2.zero, 4f);
            SliceUi.MakeLabel(shipyard, "Shipyard", 24, SliceUi.WarnColor, 0f, 32f);
            RectTransform shipyardRows = SliceUi.MakeColumn(shipyard, "Rows", Vector2.zero, 4f);

            // Right: the market of the selected ship.
            RectTransform right = SliceUi.MakeColumn(p, "Right", new Vector2(1050f, 720f));
            SliceUi.Place(right, TopLeft, new Vector2(620f, -100f));
            RectTransform marketRows = SliceUi.MakeColumn(right, "Market", Vector2.zero, 2f);
            Text notTraded = SliceUi.MakeLabel(right, "", 24, SliceUi.WarnColor);

            Text message = AddMessage(panel, 1200f);
            Button close = SliceUi.MakeButton(p, "Close", new Vector2(160f, 60f), null, 28);
            SliceUi.Place(close, BottomRight, new Vector2(-30f, 24f));

            widgets.MarketRows = marketRows;
            widgets.PortSend = send;
            widgets.PortClose = close;
            var portPanel = host.AddComponent<SlicePortPanel>();
            SetRef(portPanel, "bootstrap", bootstrap);
            SetRef(portPanel, "clock", clock);
            SetRef(portPanel, "panel", panel);
            SetRef(portPanel, "titleText", p.Find("Title").GetComponent<Text>());
            SetRef(portPanel, "goldText", gold);
            SetRef(portPanel, "shipRows", shipRows);
            SetRef(portPanel, "shipText", shipText);
            SetRef(portPanel, "repairButton", repair);
            SetRef(portPanel, "sendButton", send);
            SetRef(portPanel, "shipyardRoot", shipyard.gameObject);
            SetRef(portPanel, "shipyardRows", shipyardRows);
            SetRef(portPanel, "marketRows", marketRows);
            SetRef(portPanel, "notTradedText", notTraded);
            SetRef(portPanel, "messageText", message);
            SetRef(portPanel, "closeButton", close);
            panel.SetActive(false);
            return portPanel;
        }

        static SliceSendPanel BuildSendPanel(Transform root, GameObject host, SliceBootstrap bootstrap, SliceClock clock, Screens widgets)
        {
            GameObject panel = BuildFrame(root, "Send Panel", new Vector2(1100f, 760f), "Send", 44);
            Transform p = panel.transform;
            RectTransform destinations = SliceUi.MakeColumn(p, "Destinations", new Vector2(340f, 560f), 4f);
            SliceUi.Place(destinations, TopLeft, new Vector2(30f, -100f));
            Text preview = SliceUi.MakeText(p, "Preview", 30, TextAnchor.UpperLeft, new Vector2(660f, 480f));
            SliceUi.Place(preview, TopLeft, new Vector2(400f, -100f));
            Text message = AddMessage(panel, 1000f);
            Button sail = SliceUi.MakeButton(p, "Set sail", new Vector2(200f, 60f), null, 28);
            SliceUi.Place(sail, BottomRight, new Vector2(-210f, 24f));
            Button back = SliceUi.MakeButton(p, "Back", new Vector2(160f, 60f), null, 28);
            SliceUi.Place(back, BottomRight, new Vector2(-30f, 24f));

            widgets.DestinationRows = destinations;
            widgets.Sail = sail;
            var sendPanel = host.AddComponent<SliceSendPanel>();
            SetRef(sendPanel, "bootstrap", bootstrap);
            SetRef(sendPanel, "clock", clock);
            SetRef(sendPanel, "panel", panel);
            SetRef(sendPanel, "titleText", p.Find("Title").GetComponent<Text>());
            SetRef(sendPanel, "destinationRows", destinations);
            SetRef(sendPanel, "previewText", preview);
            SetRef(sendPanel, "messageText", message);
            SetRef(sendPanel, "sailButton", sail);
            SetRef(sendPanel, "backButton", back);
            panel.SetActive(false);
            return sendPanel;
        }

        static SliceEndScreen BuildEndScreen(Transform root, GameObject host, SliceBootstrap bootstrap)
        {
            GameObject panel = SliceUi.MakeBlocker(root, "End Screen", new Color(0.02f, 0.05f, 0.1f, 0.92f));
            Text title = SliceUi.MakeText(panel.transform, "Title", 110, TextAnchor.MiddleCenter, new Vector2(1600f, 150f));
            SliceUi.Place(title, Center, new Vector2(0f, 120f));
            Text detail = SliceUi.MakeText(panel.transform, "Detail", 44, TextAnchor.UpperCenter, new Vector2(1600f, 300f));
            SliceUi.Place(detail, Center, new Vector2(0f, -10f));

            Button restart = SliceUi.MakeButton(panel.transform, "Restart", new Vector2(300f, 80f), null, 36);
            SliceUi.Place(restart, Center, new Vector2(0f, -250f));

            var endScreen = host.AddComponent<SliceEndScreen>();
            SetRef(endScreen, "restartButton", restart);
            SetRef(endScreen, "bootstrap", bootstrap);
            SetRef(endScreen, "panel", panel);
            SetRef(endScreen, "titleText", title);
            SetRef(endScreen, "detailText", detail);
            panel.SetActive(false);
            return endScreen;
        }

        static void BuildTutorial(Transform root, SliceBootstrap bootstrap, SliceMap map, Camera camera, Screens screens, Button fastest)
        {
            GameObject box = SliceUi.MakePanel(root, "Tutorial Box", new Vector2(620f, 130f), SliceUi.PanelColor);
            box.GetComponent<Image>().raycastTarget = false;   // only Skip is clickable
            // Stretches between the gold text (left, ends near x 630) and the Pause button (right edge 540 from the right), 10 to 140 from the top.
            var boxRect = (RectTransform)box.transform;
            boxRect.anchorMin = new Vector2(0f, 1f);
            boxRect.anchorMax = new Vector2(1f, 1f);
            boxRect.pivot = new Vector2(0.5f, 1f);
            boxRect.offsetMin = new Vector2(650f, -140f);
            boxRect.offsetMax = new Vector2(-560f, -10f);
            Text text = SliceUi.MakeText(box.transform, "Text", 22, TextAnchor.MiddleLeft, new Vector2(470f, 110f));
            // Fills the box to the left of Skip.
            var textRect = (RectTransform)text.transform;
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.offsetMin = new Vector2(14f, 8f);
            textRect.offsetMax = new Vector2(-136f, -8f);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 22;
            Button skip = SliceUi.MakeButton(box.transform, "Skip", new Vector2(110f, 56f), null, 28);
            SliceUi.Place(skip, new Vector2(1f, 0.5f), new Vector2(-12f, 0f));

            // Four thin bars, not clickable, so the frame never blocks the widget it outlines.
            var frame = new GameObject("Tutorial Frame", typeof(RectTransform));
            frame.transform.SetParent(root, false);
            var frameRect = (RectTransform)frame.transform;
            frameRect.anchorMin = frameRect.anchorMax = frameRect.pivot = Center;
            frameRect.sizeDelta = new Vector2(100f, 100f);
            const float t = 4f;
            AddBar(frameRect, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, t));
            AddBar(frameRect, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, t));
            AddBar(frameRect, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(t, 0f));
            AddBar(frameRect, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(t, 0f));

            var tutorial = root.gameObject.AddComponent<SliceTutorial>();
            SetRef(tutorial, "bootstrap", bootstrap);
            SetRef(tutorial, "map", map);
            SetRef(tutorial, "sceneCamera", camera);
            SetRef(tutorial, "portPanel", screens.Port);
            SetRef(tutorial, "sendPanel", screens.Send);
            SetRef(tutorial, "box", box);
            SetRef(tutorial, "objectiveText", text);
            SetRef(tutorial, "skipButton", skip);
            SetRef(tutorial, "frame", frameRect);
            SetRef(tutorial, "marketRows", screens.MarketRows);
            SetRef(tutorial, "portSendButton", screens.PortSend);
            SetRef(tutorial, "portCloseButton", screens.PortClose);
            SetRef(tutorial, "destinationRows", screens.DestinationRows);
            SetRef(tutorial, "sailButton", screens.Sail);
            SetRef(tutorial, "fastestSpeedButton", fastest);
            box.SetActive(false);
            frame.SetActive(false);
        }

        static void AddBar(RectTransform frame, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(frame, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            Image image = go.AddComponent<Image>();
            image.color = SliceUi.SelectedColor;
            image.raycastTarget = false;
        }

        static void SetRef(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            Prop(so, field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
