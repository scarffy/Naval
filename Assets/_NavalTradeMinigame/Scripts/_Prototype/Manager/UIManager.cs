using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MiniGame.UI
{
    using TMPro;
    
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<UIManager>();

                return _instance;
            }
        }
        private static UIManager _instance;
        
        public enum  UIState
        {
            none,
            pause,
            portList,
            singlePort
        }
        public UIState UiState = UIState.none;

        [Header("Scripts")] 
        [SerializeField] private UISinglePortPanel _singlePort;
        
        [Header("Panels")]
        [SerializeField] private GameObject _portPanelList;

        [SerializeField] private GameObject _individualPortPanel;
        
        public void Initialize()
        {
            if (_singlePort == null)
                _singlePort = FindObjectOfType<UISinglePortPanel>();
            
            _singlePort.Initialize();
            
            Debug.Log($"[Core]: UI Manager initialized");
        }

        public void DeInitialize()
        {
            
        }

        public virtual void SetCurrentUIState(UIState uiState)
        {
            _portPanelList.SetActive(false);
            _individualPortPanel.SetActive(false);
            
            switch (uiState)
            {
                case UIState.pause:
                    
                    break;
                
                case UIState.singlePort:
                    _portPanelList.SetActive(true);
                    _individualPortPanel.SetActive(true);
                    break;
                
                case UIState.portList:
                    _portPanelList.SetActive(true);
                    break;
            }
        }

        public virtual void SetCurrentPort(UIPort uiPort)
        {
        }
    }
}
