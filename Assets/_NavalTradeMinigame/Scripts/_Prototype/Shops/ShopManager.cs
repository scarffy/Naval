using System.Collections;
using System.Collections.Generic;
using MiniGame.Manager;
using UnityEngine;

namespace MiniGame
{
    public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<ShopManager>();

            return _instance;
        }
    }
    private static ShopManager _instance;

    [Header("Ship")] 
    [SerializeField] private GameObject _navalShipPrefab;
    
    [Header("Ship Information")]
    public NavalShipSO NavalSlowShip;
    public NavalShipSO NavalMediumShip;
    public NavalShipSO NavalFastShip;

    [Header("Goods")]
    public NavalShipSO NavalGood;

    public void PurchaseShip()
    {

    }

    /// <summary>
    /// This is debugging
    /// TODO: Refactor this
    /// </summary>
    public void PurchaseShipSlow()
    {
        GameObject slowShip = Instantiate(_navalShipPrefab);
        NavalShip ship = slowShip.GetComponent<NavalShip>();

        ship.SetShipId(1);
        ship.SetShipName("Jupiter");
        ShipManager.Instance.TotalShips.Add(ship);
        
        Debug.Log($"[Core] Shop: Buying Slow Ship {ship.GetShipName}. Placing ship at {slowShip.transform.position}");
    }

    public void SellShip()
    {

    }

    // =========================== //

    public void PurchaseGoods()
    {

    }

    public void SellGoods()
    {

    }
}
}
