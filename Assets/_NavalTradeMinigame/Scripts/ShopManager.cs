using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    private void OnEnable()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    [Header("Ship")]
    public NavalShipObj NavalSlowShip;
    public NavalShipObj NavalMediumShip;
    public NavalShipObj NavalFastShip;

    [Header("Goods")]
    public NavalGoodObj NavalGood;

    public void PurchaseShip()
    {

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
