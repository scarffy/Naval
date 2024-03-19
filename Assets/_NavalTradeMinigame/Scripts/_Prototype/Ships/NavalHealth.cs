using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame
{
    public class NavalHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private NavalShipSO _defaultShip;
        
        [Header("Settings")]
        [SerializeField] private int _currentHealth = 0;
        [SerializeField] private int _maxHealth = 0;

        public void Initialize()
        {
            _maxHealth = _defaultShip.shipHP;
            _currentHealth = _maxHealth;
        }

        public void SetDefaultShip(NavalShipSO naval)
        {
            _defaultShip = naval;
        }
        
        public void Damage(int amount)
        {
            if (_currentHealth > 0)
                _currentHealth -= amount;
        }

        public void Repair(int amount)
        {
            _currentHealth += amount;
            if (_currentHealth > _maxHealth)
                _currentHealth = _maxHealth;
        }
    }
}
