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




        #region Accessor
        public PortManager PortManager => portManager;
        #endregion
    }
}