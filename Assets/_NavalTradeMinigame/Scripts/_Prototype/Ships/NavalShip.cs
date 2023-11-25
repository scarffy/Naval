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

    [Header("Ship Details")]
    public string shipName;
    public int shipId;  // For saving

    [Header("Navigation")]
    private NavMeshAgent agent;
    public float speed;
    public float distance;
    public float timeArrival;

    [Space]
    public Transform destination;
    
    
    public bool isOnSail = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed = defaultShip.shipSpeed;
    }

    // Start is called before the first frame update
    void Start()
    {
        if(destination != null)
            agent.SetDestination(destination.position);
    }

    public void SetSail()
    {
        agent.SetDestination(destination.position);
    }

    public void ResetSail()
    {
        if (agent.isStopped)
        {
            agent.autoRepath = true;
            agent.SetDestination(destination.position);
            agent.isStopped = false;
        }
    }

    public void StopSail()
    {
        agent.isStopped = true;
    }

    // Update is called once per frame
    private void Update()
    {
        //! TODO: To gather all details in one place. As in to follow ECS pattern
        if (isOnSail)
        {
            //distance = Vector3.Distance(startPortTransform.position, endPortTransform.position);
            distance = agent.remainingDistance;

            timeArrival = distance / speed;
        }
    }
}
