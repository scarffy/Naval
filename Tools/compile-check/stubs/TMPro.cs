// Stand-in for the TextMeshPro package, which has no downloadable reference assembly.
// Only the members the game uses. Add members here as the game starts using them.
namespace TMPro
{
    public enum TextAlignmentOptions { TopLeft, Top, TopRight, Left, Center, Right, BottomLeft, Bottom, BottomRight, Midline, MidlineLeft, MidlineRight }
    public enum FontStyles { Normal = 0, Bold = 1, Italic = 2, Underline = 4, UpperCase = 16, SmallCaps = 32 }
    public class TMP_FontAsset : UnityEngine.ScriptableObject { }
    public abstract class TMP_Text : UnityEngine.UI.MaskableGraphic
    {
        public virtual string text { get; set; }
        public float fontSize { get; set; }
        public FontStyles fontStyle { get; set; }
        public TextAlignmentOptions alignment { get; set; }
        public TMP_FontAsset font { get; set; }
        public bool enableWordWrapping { get; set; }
        public bool richText { get; set; }
        public void SetText(string sourceText) { }
        public void SetText(string sourceText, float arg0) { }
    }
    public class TextMeshProUGUI : TMP_Text { }
    public class TextMeshPro : TMP_Text { }
    public class TMP_InputField : UnityEngine.UI.Selectable { public string text { get; set; } }
}
