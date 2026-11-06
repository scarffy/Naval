using System;
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
        const string ScenesFolder = "Assets/_NavalTradeMinigame/Scenes";
        const string ScenePath = ScenesFolder + "/Slice.unity";

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
            var demo = game.AddComponent<SliceDemo>();

            SliceHud hud = BuildHud(game.transform);

            SetRef(clock, "bootstrap", bootstrap);
            SetRef(map, "bootstrap", bootstrap);
            SetRef(map, "sceneCamera", camera);
            SetRef(ships, "bootstrap", bootstrap);
            SetRef(ships, "clock", clock);
            SetRef(ships, "map", map);
            SetRef(demo, "bootstrap", bootstrap);
            SetRef(demo, "clock", clock);
            SetRef(hud, "bootstrap", bootstrap);
            SetRef(hud, "clock", clock);

            if (!AssetDatabase.IsValidFolder(ScenesFolder))
                AssetDatabase.CreateFolder("Assets/_NavalTradeMinigame", "Scenes");
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("Built " + ScenePath);
        }

        static Camera BuildCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            Camera camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 35f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.2f, 0.32f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            go.transform.position = new Vector3(50f, 30f, -10f);
            go.AddComponent<AudioListener>();
            return camera;
        }

        static SliceHud BuildHud(Transform parent)
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(parent, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            ConfigureScaler(scaler);
            canvasGo.AddComponent<GraphicRaycaster>();
            BuildEventSystem();

            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            Transform root = canvasGo.transform;
            Text date = MakeText(root, "Date", font, 44, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(700f, 56f));
            Text days = MakeText(root, "DaysLeft", font, 30, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(30f, -78f), new Vector2(700f, 40f));
            Text gold = MakeText(root, "Gold", font, 34, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(30f, -118f), new Vector2(900f, 44f));
            Text status = MakeText(root, "Status", font, 120, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 400f));
            status.raycastTarget = false;

            string[] labels = { "Pause", "1×", "2×", "4×" };
            var buttons = new Button[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                // Right-aligned row, Pause leftmost.
                float x = -30f - (labels.Length - 1 - i) * 130f;
                buttons[i] = MakeButton(root, "Speed " + labels[i], font, labels[i], new Vector2(x, -24f));
            }

            var hud = canvasGo.AddComponent<SliceHud>();
            SetRef(hud, "dateText", date);
            SetRef(hud, "daysLeftText", days);
            SetRef(hud, "goldText", gold);
            SetRef(hud, "statusText", status);
            var so = new SerializedObject(hud);
            SerializedProperty array = so.FindProperty("speedButtons");
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
        /// The project uses the new Input System only (activeInputHandler 1), where StandaloneInputModule does not work.
        /// The Input System's module has no reference assembly for the compile check, so it is added by name.
        /// </summary>
        static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            Type module = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (module == null)
            {
                go.AddComponent<StandaloneInputModule>();
                Debug.LogWarning("Input System package not found: added StandaloneInputModule, which needs the old Input Manager.");
                return;
            }
            Component added = go.AddComponent(module);
            var assign = module.GetMethod("AssignDefaultActions", Type.EmptyTypes);
            if (assign != null) assign.Invoke(added, null);
        }

        static Text MakeText(Transform parent, string name, Font font, int size, TextAnchor align,
                             Vector2 anchor, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = align;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static Button MakeButton(Transform parent, string name, Font font, string label, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(120f, 64f);
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.75f, 0.75f, 0.75f);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;

            Text text = MakeText(go.transform, "Label", font, 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 64f));
            text.color = new Color(0.1f, 0.1f, 0.1f);
            return button;
        }

        static void SetRef(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null) throw new InvalidOperationException(target.GetType().Name + " has no serialized field '" + field + "'");
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
