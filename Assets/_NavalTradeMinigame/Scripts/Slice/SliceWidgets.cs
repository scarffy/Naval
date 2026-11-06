using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Naval.Slice
{
    /// <summary>
    /// Makes the rows, texts and buttons the screens build at runtime (market rows, fleet list, destinations...).
    /// It sits on the Canvas and gives them the font assigned here.
    /// </summary>
    public sealed class SliceWidgets : MonoBehaviour
    {
        [SerializeField, Tooltip("Font for the rows the screens build at runtime (market, fleet list, destinations...). Use the same font as the scene's texts.")]
        Font font;

        void Awake()
        {
            // Without a font the rows built at runtime show no text.
            SliceUi.IsAssigned(font, "Font", this);
        }

        /// <summary>A fixed-size text that wraps to its width.</summary>
        public Text MakeText(Transform parent, string name, int fontSize, TextAnchor align, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = size;
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>A text for layout groups. A width of 0 lets it take the space that is left.</summary>
        public Text MakeLabel(Transform parent, string content, int fontSize, Color color, float width = 0f,
                              float height = 0f, TextAnchor align = TextAnchor.MiddleLeft)
        {
            Text text = MakeText(parent, "Label", fontSize, align, Vector2.zero);
            text.text = content;
            text.color = color;
            LayoutElement element = text.gameObject.AddComponent<LayoutElement>();
            if (width > 0f) element.preferredWidth = width; else element.flexibleWidth = 1f;
            if (height > 0f) element.preferredHeight = height;
            return text;
        }

        /// <summary>A button with a centred label. In a layout group it asks for the given size.</summary>
        public Button MakeButton(Transform parent, string label, Vector2 size, UnityAction onClick, int fontSize = 26)
        {
            var go = new GameObject("Button " + label.Split('\n')[0], typeof(RectTransform));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = size;
            LayoutElement element = go.AddComponent<LayoutElement>();
            element.preferredWidth = size.x;
            element.preferredHeight = size.y;
            Image image = go.AddComponent<Image>();
            image.color = SliceUi.ButtonColor;
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null) button.onClick.AddListener(onClick);

            Text text = MakeText(go.transform, "Label", fontSize, TextAnchor.MiddleCenter, Vector2.zero);
            text.text = label;
            text.color = new Color(0.1f, 0.1f, 0.1f);
            var rect = (RectTransform)text.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 0f);
            rect.offsetMax = new Vector2(-8f, 0f);
            return button;
        }

        public RectTransform MakeRow(Transform parent, float height, float spacing = 8f)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            HorizontalLayoutGroup layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            go.AddComponent<LayoutElement>().preferredHeight = height;
            return (RectTransform)go.transform;
        }
    }
}
