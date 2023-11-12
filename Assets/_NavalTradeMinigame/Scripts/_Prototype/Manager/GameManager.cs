using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.Manager
{
    /// <summary>
    /// This handle higher level game events
    /// Let appropriate manager handles the lower level game events
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<GameManager>();

                return _instance;
            }
        }
        private static GameManager _instance;
        
        private void Start()
        {
            Initialized();
        }

        private void Initialized()
        {
            PortManager.Instance.Initialized();
        }

        public List<NavalShip> TotalShips;

        // === Port Management === //
        // === Ship Management == //
    }
}
