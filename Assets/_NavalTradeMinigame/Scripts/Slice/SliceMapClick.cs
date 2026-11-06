using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Naval.Slice
{
    /// <summary>Forwards a left click on a map object (through the camera's Physics2DRaycaster) to whoever set <see cref="Clicked"/>.</summary>
    public sealed class SliceMapClick : MonoBehaviour, IPointerClickHandler
    {
        public Action Clicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && Clicked != null) Clicked();
        }
    }
}
