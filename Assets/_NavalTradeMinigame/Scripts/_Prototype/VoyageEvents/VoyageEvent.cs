using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame
{
    public class VoyageEvent : MonoBehaviour
    {
        [SerializeField] private VoyageEventSO _gameEvent;
        
        //! Placeholder. Voyage events will be rolled per route instead (see Design/VerticalSlice.md),
        //! so this only logs. It used to throw, which broke any ship sailing through an event area.
        private void OnTriggerEnter(Collider other)
        {
#if UNITY_EDITOR
            if (_gameEvent != null)
                Debug.Log($"[Voyage Event]: {other.name} entered {_gameEvent._voyageEvent}");
#endif
        }
    }
}
