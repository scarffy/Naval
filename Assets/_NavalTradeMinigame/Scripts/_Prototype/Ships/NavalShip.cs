using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

//! TODO: Remove/Rewrite this script.
//! Only to have NavMeshAgent
[RequireComponent(typeof(NavMeshAgent))]
public class NavalShip : MonoBehaviour
{
    public NavalShipSO defaultShip;

    [Space]
    public string shipName;
    public int shipId;  // For saving

    [Space]
    public float speed;
    public float distance;
    public float timeArrival;

    [Header("Navigation")]
    //! UIs
    //! TODO
    // TO No get things from UI side but get it from manager
    public MiniGame.UI.UIPort startPort;
    public MiniGame.UI.UIPort EndPort;

    [Space]
    public Transform startPortTransform;
    public Transform endPortTransform;

    public Transform destination;

    NavMeshAgent agent;

    public bool isOnSail = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed = defaultShip.shipSpeed;
    }

    // Start is called before the first frame update
    void Start()
    {
        agent.SetDestination(destination.position);
    }

    // Update is called once per frame
    void Update()
    {
        if (isOnSail)
        {
            //distance = Vector3.Distance(startPortTransform.position, endPortTransform.position);
            distance = agent.remainingDistance;

            timeArrival = distance / speed;
        }
    }
}
