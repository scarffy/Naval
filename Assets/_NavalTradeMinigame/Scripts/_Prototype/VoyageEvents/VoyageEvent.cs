using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame
{
    public class VoyageEvent : MonoBehaviour
    {
        [SerializeField] private VoyageEventSO _gameEvent;
        
        private void OnTriggerEnter(Collider other)
        {
            throw new NotImplementedException();
        }
    }
}
