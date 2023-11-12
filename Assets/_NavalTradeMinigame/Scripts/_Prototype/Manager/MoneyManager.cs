using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame
{
    public class MoneyManager : MonoBehaviour
    {
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
        }

        public void ReduceMoney(int value)
        {
            _money -= value;
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
    }
}
