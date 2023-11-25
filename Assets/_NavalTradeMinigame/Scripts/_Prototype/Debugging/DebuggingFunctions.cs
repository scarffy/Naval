using System;
using System.Collections;
using System.Collections.Generic;
using MiniGame.Manager;
using MiniGame.UI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MiniGame
{
    public class DebuggingFunctions : MonoBehaviour
    {
        public bool bIsDebugging = false;

        private void Awake()
        {
            gameObject.SetActive(bIsDebugging);
        }

        public void BuySlowShip()
        {
            if (bIsDebugging)
            {
                ShopManager.Instance.PurchaseShipSlow();

                Debug.Log("[Debugging]: Buying Ship");
            }
            else
            {
                Debug.Log("[Debugging]: Is not debugging");
            }
        }

        public void SetShipToRandomPort()
        {
            //! Get Random UIPort
            //! Get Random? ship to random UIPort
            int index = Random.Range(1, PortManager.Instance.GetPortList.Count);
            UIPort uiport = PortManager.Instance.GetPortList[index];

            NavalShip ship = FindObjectOfType<NavalShip>();
           
            Debug.Log("Ship docked at " + uiport.portName);
        }
    }
}
