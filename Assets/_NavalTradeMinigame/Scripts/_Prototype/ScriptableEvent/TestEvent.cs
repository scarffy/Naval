using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.Prototype
{
    public class TestEvent : MonoBehaviour
    {
        public GameEventSO gameEvent;
        
        private void Start()
        {
            // Triggers the event
            gameEvent.TriggerEvent();
        }

        public void Triggered()
        {
            Debug.Log("Is this how you use it?");
        }
    }
}
