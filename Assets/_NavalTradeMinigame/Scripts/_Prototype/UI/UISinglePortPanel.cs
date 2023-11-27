using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MiniGame.Manager;
using UnityEngine;

namespace MiniGame.UI
{
    using TMPro;
    using UnityEngine.UI;
    
    /// <summary>
    /// Note: Shop manager to handle all logic related to buy and sell
    /// </summary>
    public class UISinglePortPanel : MonoBehaviour
    {
        [SerializeField] private NavalPortSO _portInformations;
        
        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI _moneyText;
        [SerializeField] private TextMeshProUGUI _portText;

        [Header("Buttons")] 
        [SerializeField] private Button _buyShipButton;
        [SerializeField] private Button _sellShipButton;
        [SerializeField] private Button _buyGoodsButton;
        [SerializeField] private Button _sendShipButton;
        
        [Header("Provides")] 
        public GameObject _providesPrefab;

        // =========== Provides =============== //
        //! Need to get data. Maybe from data manager?

        [Header("Demands")]
        [SerializeField] private GameObject _demandsPrefab;

        // =========== Demands =============== //
        //! Need to get data. Maybe from data manager?

        [Header("Docked Ships")]
        [SerializeField] private NavalShip _selectedShip;
        private List<NavalShip> shipList = new List<NavalShip>();

        public void Initialize()
        {
            _buyShipButton.onClick.AddListener(PurchaseShip);
            _sellShipButton.onClick.AddListener(SellShip);
            _buyGoodsButton.onClick.AddListener(BuyGoods);
            _sendShipButton.onClick.AddListener(SendShip);
        }
        
        public void OnEnable()
        {
            GetPortInformation();
        }

        public void GetPortInformation()
        {
            _portInformations = PortManager.Instance.CurrentPort.port;
            
            // Set Money
            _moneyText.text = MoneyManager.Instance.GetMoneyString() + " $";

            // Set Port Name
            _portText.text = _portInformations.PortName;
            
            if (_portInformations.PortSpecialty.Any())
            {
                Debug.Log($"[UI] Single Port: Port Specialty is not empty");
            }
            else
            {
                Debug.Log($"[UI] Single Port: Port Specialty is empty");
            }

            if (_portInformations.PortDemands.Any())
            {
                
            }
            else
            {
                
            }

            //! Get current list. Not the best way to get the ship list
            shipList = new List<NavalShip>(GameManager.Instance.TotalShips);
            for (int i = shipList.Count - 1; i >= 0 ; i--)
            {
                if (shipList[i].GetPort != _portInformations)
                {
                    shipList.RemoveAt(i);
                }
            }
        }

        private void SelectShip()
        {
            
        }

        private void PurchaseShip()
        {
            //! Look for ShopManager
            ShopManager.Instance.PurchaseShip();
        }

        private void SellShip()
        {
            if(!HasSelectedShip())
                return;
            
            ShopManager.Instance.SellShip();
        }

        private void BuyGoods()
        {
            if(!HasSelectedShip())
                return;
            
        }

        private void SendShip()
        {
            if(!HasSelectedShip())
                return;
        }

        private bool HasSelectedShip()
        {
            if (_selectedShip == null)
                return false;
            return true;
        }
    }
}
