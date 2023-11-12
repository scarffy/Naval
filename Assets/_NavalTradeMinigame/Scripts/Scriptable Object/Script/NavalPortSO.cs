using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Naval Inventory", menuName = "Mini Game/Port", order = 1)]
public class NavalPortSO : ScriptableObject
{
    [SerializeField] private int id = -1;
    [SerializeField] private string portName = "Port Name";
    [SerializeField] private string portDescription = "Port Description";

    [SerializeField] private List<NavalItem> portSpecialty;
    [SerializeField] private List<NavalItem> portDemands;
    
    #region Accessor
    public int ID => id;
    public string PortName => portName;
    public string PortDescription => PortDescription;

    public List<NavalItem> PortSpecialty => PortSpecialty;
    public List<NavalItem> PortDemands => PortDemands;
    #endregion
}
