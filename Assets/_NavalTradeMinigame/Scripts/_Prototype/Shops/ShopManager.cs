using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public void PurchaseShipSlow()
    {
        GameObject slowShip = Instantiate(_navalShipPrefab);
        NavalShip ship = slowShip.GetComponent<NavalShip>();

        ship.shipId = 1;
        ship.shipName = "Jupiter";
        ship.speed = NavalSlowShip.shipSpeed;
        
        Debug.Log($"[Core] Shop: Buying Slow Ship {ship.shipName}. Placing ship at {slowShip.transform.position}");
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
