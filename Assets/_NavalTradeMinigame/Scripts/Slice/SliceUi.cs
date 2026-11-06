using System.Globalization;
using Naval.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Naval.Slice
{
    /// <summary>Colours and small helpers shared by the screens. The widgets themselves are made by <see cref="SliceWidgets"/>.</summary>
    public static class SliceUi
    {
        public static readonly Color ButtonColor = new Color(0.75f, 0.75f, 0.75f);
        public static readonly Color SelectedColor = new Color(1f, 0.85f, 0.4f);
        public static readonly Color GoodColor = new Color(0.6f, 1f, 0.65f);
        public static readonly Color BadColor = new Color(1f, 0.45f, 0.4f);
        public static readonly Color WarnColor = new Color(1f, 0.8f, 0.45f);

        /// <summary>
        /// False, with a clear error naming the component, its object and the field, when an Inspector reference is empty.
        /// Check every field with it and combine the results with &amp; so all the missing ones are reported at once.
        /// </summary>
        public static bool IsAssigned(Object value, string field, Component owner)
        {
            if (value != null) return true;
            Debug.LogError(owner.GetType().Name + " on '" + owner.name + "': the " + field + " field is empty. Assign it in the Inspector.", owner);
            return false;
        }

        public static Text LabelOf(Button button)
        {
            return button.GetComponentInChildren<Text>();
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
