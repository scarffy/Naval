using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.Manager
{
    public class PortManager : MonoBehaviour
    {
        public static PortManager Instance;

        public void Initialized()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(this);
        }
    }
}