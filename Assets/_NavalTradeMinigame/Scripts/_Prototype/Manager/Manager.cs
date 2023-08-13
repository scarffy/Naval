using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.Manager
{
    public class Manager : MonoBehaviour
    {
        public static Manager Instance;

        [SerializeField] private PortManager portManager;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            portManager.Initialized();
        }

        public int playerMoney = 1500;

        //! ToDO: Remove this
        [Space]
        public UI.UIPort currentSelectedPort;


        public List<NavalShip> TotalShips;

        // === Port Management === //

        // === Ship Management == //


        #region Accessor
        public PortManager PortManager => portManager;
        #endregion
    }
}