using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace MiniGame
{
    public class ShipManager : MonoBehaviour
    {
        #region Singleton

        public static ShipManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<ShipManager>();
                return _instance;
            }
        }
        private static ShipManager _instance;
        #endregion
        
        [Header("Ship List")]
        public List<NavalShip> TotalShips;

        [Header("Events")] 
        public UnityEvent OnUpdate;

        public void Initialize()
        {
            NavalShip[] shipList = FindObjectsOfType<NavalShip>();
            TotalShips = new List<NavalShip>(shipList);
        }
        
        private void Update()
        {
            if(OnUpdate != null)
                OnUpdate?.Invoke();
        }
    }
}
