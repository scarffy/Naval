using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Naval Ship", menuName = "Game Jam/Goods", order = 1)]
public class NavalGoodObj : ScriptableObject
{
    public int id = 0;
    public string goodName;
    public string goodBasePrice;
    public Sprite goodImage;


}
