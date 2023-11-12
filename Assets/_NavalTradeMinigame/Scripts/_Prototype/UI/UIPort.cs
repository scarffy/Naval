using System.Collections;
using System.Collections.Generic;
using MiniGame.Manager;
using UnityEngine;
using TMPro;

namespace MiniGame.UI
{
    public class UIPort : MonoBehaviour
    {
        public int portId;
        public string portName;
        public NavalPortSO port;

        [SerializeField] private TextMeshProUGUI portNameText;

        public void Initialize()
        {
            gameObject.name = port.PortName;
            
            if (portNameText == null)
                portNameText = GetComponentInChildren<TextMeshProUGUI>();
            
            portNameText.SetText(port.PortName);
            portName = port.PortName;
            portId = port.ID;
            
            Debug.Log($"[Core]: UI Port Initialized {portName}");
        }

        public void SetPort()
        {
            PortManager.Instance.SetCurrentPort(this);
        }
    }
}