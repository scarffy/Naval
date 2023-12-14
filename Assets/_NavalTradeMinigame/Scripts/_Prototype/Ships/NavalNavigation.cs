using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MiniGame
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavalNavigation : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private Transform _destination;
        [SerializeField] private NavalPortSO _expectedPort;
        [SerializeField] private bool _isOnSail = false;

        [Header("Navigation Settings")] 
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private float _speed;
        [SerializeField] private float _distance;
        [SerializeField] private float _arrivalTime;
        
        [SerializeField] private NavalShipSO _defaultShip;

        public void Initialize()
        {
            if (_agent == null)
                _agent = GetComponent<NavMeshAgent>();

            _speed = _defaultShip.shipSpeed;
            _agent.speed = _speed;
        }

        public void SetDefaultShip(NavalShipSO ship) => _defaultShip = ship;
        
        public void SetDestination(Transform destination) => _destination = destination;
        public void SetDestinationPort(NavalPortSO port) => _expectedPort = port;

        public void SetSail()
        {
            _agent.SetDestination(_destination.position);
            _isOnSail = true;
            ShipManager.Instance.OnUpdate.AddListener(UpdateSail);
        } 

        public void ResetSail()
        {
            if (_agent.isStopped)
            {
                _agent.autoRepath = true;
                _agent.SetDestination(_destination.position);
                _agent.isStopped = false;
            }
        }

        public void StopSail()
        {
            _agent.isStopped = true;
            _isOnSail = false;
            ShipManager.Instance.OnUpdate.RemoveListener(UpdateSail);
        }
        
        //! Experiment until I found the best way to go forward
        public void UpdateSail()
        {
            if (_isOnSail)
            {
                _distance = _agent.remainingDistance;

                _arrivalTime = _distance / _speed;
            }
        }

        public NavalPortSO GetPortInformation => _expectedPort;
        public bool IsOnSail => _isOnSail;
    }
}
