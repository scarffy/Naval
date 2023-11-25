using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame
{
    public class MoneyManager : MonoBehaviour
    {
        public Action<int> OnMoneyUpdated;
        
        public static MoneyManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<MoneyManager>();

                return _instance;
            }
        }
        private static MoneyManager _instance;

        [SerializeField]
        private int _money = 1500;

        public void AddMoney(int value)
        {
            _money += value;
            OnMoneyUpdated?.Invoke(_money);
        }

        public void ReduceMoney(int value)
        {
            _money -= value;
            OnMoneyUpdated?.Invoke(_money);
        }

        public bool CanReduceMoney(int value)
        {
            if (_money >= value)
                return true;
            return false;
        }

        public int GetMoney()
        {
            return _money;
        }

        public string GetMoneyString()
        {
            return _money.ToString();
        }
    }
}
