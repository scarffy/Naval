// Stand-in for the pointer event types of UnityEngine.UI. The reference assembly points at netstandard,
// which the check cannot resolve for types that are not Unity objects. Signatures match Unity 2021.3.
namespace UnityEngine.EventSystems
{
    public interface IEventSystemHandler { }
    public interface IPointerClickHandler : IEventSystemHandler { void OnPointerClick(PointerEventData eventData); }
    public class PointerEventData
    {
        public enum InputButton { Left = 0, Right = 1, Middle = 2 }
        public InputButton button { get; set; }
    }
}
