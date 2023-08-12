using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavalShip : MonoBehaviour
{
    public NavalShipObj defaultShip;

    [Space]
    public string shipName;
    public int shipId;  // For saving

    [Space]
    public float speed;
    public float distance;
    public float timeArrival;

    [Header("Navigation")]
    //! UIs
    public UIPort startPort;
    public UIPort EndPort;

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
