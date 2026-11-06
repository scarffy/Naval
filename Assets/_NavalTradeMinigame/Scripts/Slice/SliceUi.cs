using System.Globalization;
using Naval.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Naval.Slice
{
    /// <summary>Widget factory and text helpers for the rows the screens build at runtime.</summary>
    public static class SliceUi
    {
        public static readonly Color PanelColor = new Color(0.07f, 0.11f, 0.17f, 0.96f);
        public static readonly Color ButtonColor = new Color(0.75f, 0.75f, 0.75f);
        public static readonly Color SelectedColor = new Color(1f, 0.85f, 0.4f);
        public static readonly Color GoodColor = new Color(0.6f, 1f, 0.65f);
        public static readonly Color BadColor = new Color(1f, 0.45f, 0.4f);
        public static readonly Color WarnColor = new Color(1f, 0.8f, 0.45f);

        static Font font;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return font;
            }
        }

        /// <summary>Anchors and pivots a widget at one point of its parent and moves it from there.</summary>
        public static void Place(Component widget, Vector2 anchor, Vector2 position)
        {
            var rect = (RectTransform)widget.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
        }

        /// <summary>A background panel centred in its parent. It blocks clicks to whatever lies behind it.</summary>
        public static GameObject MakePanel(Transform parent, string name, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            go.AddComponent<Image>().color = color;
            return go;
        }

        /// <summary>A panel that covers the whole parent.</summary>
        public static GameObject MakeBlocker(Transform parent, string name, Color color)
        {
            GameObject go = MakePanel(parent, name, Vector2.zero, color);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }

        /// <summary>A fixed-size text that wraps to its width. Position it with <see cref="Place"/>.</summary>
        public static Text MakeText(Transform parent, string name, int fontSize, TextAnchor align, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = size;
            Text text = go.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>A text for layout groups. A width of 0 lets it take the space that is left.</summary>
        public static Text MakeLabel(Transform parent, string content, int fontSize, Color color, float width = 0f,
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
        public static Button MakeButton(Transform parent, string label, Vector2 size, UnityAction onClick, int fontSize = 26)
        {
            var go = new GameObject("Button " + label.Split('\n')[0], typeof(RectTransform));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = size;
            LayoutElement element = go.AddComponent<LayoutElement>();
            element.preferredWidth = size.x;
            element.preferredHeight = size.y;
            Image image = go.AddComponent<Image>();
            image.color = ButtonColor;
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

        public static Text LabelOf(Button button)
        {
            return button.GetComponentInChildren<Text>();
        }

        /// <summary>
        /// A vertical stack of rows laid out inside a rectangle of the given size (a nested column takes its size from its parent,
        /// so pass zero). Rows start at the top, or at the bottom with a lower alignment.
        /// </summary>
        public static RectTransform MakeColumn(Transform parent, string name, Vector2 size, float spacing = 6f,
                                               TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = size;
            VerticalLayoutGroup layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rect;
        }

        public static RectTransform MakeRow(Transform parent, float height, float spacing = 8f)
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

        /// <summary>Removes every row. They leave the layout at once and are destroyed at the end of the frame.</summary>
        public static void Clear(Transform rows)
        {
            for (int i = rows.childCount - 1; i >= 0; i--)
            {
                GameObject child = rows.GetChild(i).gameObject;
                child.SetActive(false);
                Object.Destroy(child);
            }
        }

        public static void ShowResult(Text message, CommandResult result)
        {
            message.text = result.Message;
            message.color = result.Ok ? GoodColor : BadColor;
        }

        public static string ShipName(Ship ship)
        {
            return ship.Tier.Name + " " + ship.Id;
        }

        public static string Count(int n, string noun)
        {
            return n + " " + noun + (n == 1 ? "" : "s");
        }

        /// <summary>A chance from 0 to 100 as whole percent, "&lt;1%" when it is above zero but would round to nothing.</summary>
        public static string Percent(double percent)
        {
            if (percent > 0 && percent < 0.5) return "<1%";
            return Mathf.RoundToInt((float)percent).ToString(CultureInfo.InvariantCulture) + "%";
        }

        public static string Date(System.DateTime date)
        {
            return date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
        }
    }
}
