using System.Collections;
using System.Collections.Generic;
using MiniGame.UI;
using UnityEngine;
using UnityEngine.Events;

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

        [Header("Port")] 
        [SerializeField] private DataManager _port;
        
        [Header("UI")]
        [SerializeField] private UIPort _currentSelectedPort = null;

        [Space]
        [SerializeField] private List<UIPort> _uiPortList = new List<UIPort>();
        
        public void Initialized()
        {
            SetPort();
            Debug.Log($"[Core]: Port Initialized");
        }

        public void SetPort()
        {
            if (_uiPortList.Count == 0)
                Debug.LogWarning($"[Core](Warning): UI Port list is empty");

            int count = Mathf.Min(_uiPortList.Count, _port.PortList.Count);
            if (_uiPortList.Count != _port.PortList.Count)
                Debug.LogWarning($"[Core](Warning): {_uiPortList.Count} UI ports but {_port.PortList.Count} ports in the data. Only the first {count} are set up");
            
            for (int i = 0; i < count; i++)
            {
                _uiPortList[i].port = _port.PortList[i];
                
                _uiPortList[i].Initialize();
            }
        }

        public void SetCurrentPort(UIPort port)
        {
            _currentSelectedPort = port;
        }

        public UIPort CurrentPort => _currentSelectedPort;
        public List<UIPort> GetPortList => _uiPortList;
    }
}