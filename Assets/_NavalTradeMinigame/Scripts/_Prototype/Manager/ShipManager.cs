using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        public void Initialize()
        {
            NavalShip[] shipList = FindObjectsOfType<NavalShip>();
            TotalShips = new List<NavalShip>(shipList);
        }
        
        private void Update()
        {
            
        }
    }
}
