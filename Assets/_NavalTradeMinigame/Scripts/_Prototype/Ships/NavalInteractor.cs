using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MiniGame
{
    public class NavalInteractor : MonoBehaviour
    {
        [Header("Ship Event")] 
        public UnityEvent OnArrival;

        public UnityEvent OnVoyageEvent;
        
        private void OnTriggerEnter(Collider collider)
        {
            if(collider.CompareTag("Finish"))
                OnArrival?.Invoke();
            
            if(collider.CompareTag("VoyageEvent"))
                OnVoyageEvent?.Invoke();
        }

        #if UNITY_EDITOR
        public void CollisionDebug()
        {
            Debug.Log($"[Naval Interactor]: Collided with port");
        }
        #endif
    }
}
