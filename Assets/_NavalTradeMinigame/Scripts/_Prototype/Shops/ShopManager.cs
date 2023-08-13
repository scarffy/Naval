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
    public NavalShipSO NavalSlowShip;
    public NavalShipSO NavalMediumShip;
    public NavalShipSO NavalFastShip;

    [Header("Goods")]
    public NavalShipSO NavalGood;

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
