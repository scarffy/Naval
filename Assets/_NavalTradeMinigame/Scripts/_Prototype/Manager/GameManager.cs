using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.Manager
{
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
        
        
        //! ToDO: Remove this
        [Space]
        public UI.UIPort currentSelectedPort;


        public List<NavalShip> TotalShips;

        // === Port Management === //

        // === Ship Management == //
    }
}
