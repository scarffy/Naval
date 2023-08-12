using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.Manager
{
    public class Manager : MonoBehaviour
    {
        public static Manager Instance;

        private void OnEnable()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public int playerMoney = 1500;

        public UIPort currentSelectedPort;


        public List<NavalShip> TotalShips;

        // === Port Management === //

        // === Ship Management == //


    }
}