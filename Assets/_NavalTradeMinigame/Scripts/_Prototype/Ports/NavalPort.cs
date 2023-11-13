using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavalPort : MonoBehaviour
{
    [SerializeField] private List<NavalPortSO> portList;

    
    
    #region Accessor
    public List<NavalPortSO> PortList => portList;
    #endregion
    
}
