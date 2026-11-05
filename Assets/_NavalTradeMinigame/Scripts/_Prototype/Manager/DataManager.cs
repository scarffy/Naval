using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame
{
    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<DataManager>();

                return _instance;
            }
        }
        private static DataManager _instance;

        [Header("Ports")] 
        [SerializeField] private List<NavalPortSO> portList;
    
        #region Accessor
        public List<NavalPortSO> PortList => portList;
        #endregion
    }
}
