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

        private void Start()
        {
            _shipName = _defaultShip.shipName;
            _shipId = _defaultShip.shipId;
            
            _navalNavigation.SetDefaultShip(_defaultShip);
            _navalNavigation.Initialize();
            
            _navalNavigation.SetSail();
        }

        private void OnTriggerEnter(Collider other)
        {
            //! Probably can do when it collide with collider do some event

        }

        public void SetShipId(int id) => _shipId = id;
        public void SetShipName(string shipName) => _shipName = shipName;

        #region  Accessor
        public string GetShipName => _shipName;
        public NavalPortSO GetPort => _navalNavigation.GetPortInformation;
        #endregion
    }
}
