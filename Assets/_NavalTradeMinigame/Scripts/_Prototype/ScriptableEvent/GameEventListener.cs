using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MiniGame.Prototype
{
    public class GameEventListener : MonoBehaviour
    {
        public GameEventSO gameEvent;
        public UnityEvent onEventTriggered;
        
        void OnEnable()
        {
            gameEvent.AddListener(this);
        }
        void OnDisable()
        {
            gameEvent.RemoveListener(this);
        }
        public void OnEventTriggered()
        {
            onEventTriggered.Invoke();
        }
    }
}
