using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MiniGame.UI
{
    public class UIPort : MonoBehaviour
    {
        public int portId;
        public string portName;

        TextMeshProUGUI portNameText;

        [Header("Ship")]
        public List<NavalShip> DockedShips;
        public List<NavalShip> ExpectingShips;

        private void Awake()
        {
            // Debugging
            gameObject.name = portName;

            if (portNameText == null)
                portNameText = GetComponentInChildren<TextMeshProUGUI>();
            portNameText.SetText(portName);
        }

        public void SetPort()
        {
            //Manager.Instance.currentSelectedPort = this;
        }
    }
}