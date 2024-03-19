using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame
{
    public class NavalShip : MonoBehaviour
    {
        public NavalShipSO _defaultShip;

        [Header("Ship Details")] 
        [SerializeField] private string _shipName;
        [SerializeField] private int _shipId; // For saving

        [Header("Dependant Script")] 
        [SerializeField] private NavalNavigation _navalNavigation;
        [SerializeField] private Experience _navalExperience;
        [SerializeField] private NavalInteractor _navalInteractor;
        [SerializeField] private NavalHealth _navalHealth;
        
        private void Start()
        {
            //! This need to be set from ship purchase
            _shipName = _defaultShip.shipName;
            _shipId = _defaultShip.shipId;
            
            _navalNavigation.SetDefaultShip(_defaultShip);
            _navalHealth.SetDefaultShip(_defaultShip);
            
            _navalNavigation.Initialize();
            _navalHealth.Initialize();

#if UNITY_EDITOR
                _navalNavigation.SetSail();
            #endif
        }

        public void SetShipId(int id) => _shipId = id;
        public void SetShipName(string shipName) => _shipName = shipName;

        #region  Accessor
        public string GetShipName => _shipName;
        public NavalPortSO GetPort => _navalNavigation.GetPortInformation;
        public NavalNavigation GetNavigation => _navalNavigation;

        #endregion
    }
}
