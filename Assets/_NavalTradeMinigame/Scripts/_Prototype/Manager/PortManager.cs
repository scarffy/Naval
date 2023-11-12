using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.Manager
{
    public class PortManager : MonoBehaviour
    {
        public static PortManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<PortManager>();
                return _instance;
            }
        }

        private static PortManager _instance;

        public void Initialized()
        {
            
        }
    }
}