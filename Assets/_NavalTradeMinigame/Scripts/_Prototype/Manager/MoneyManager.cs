using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MiniGame
{
    public class MoneyManager : MonoBehaviour
    {
        //! The game's only currency. Always show money through Format() so every screen matches.
        public const string CurrencyName = "gold";

        public static string Format(int amount) => string.Format(CultureInfo.InvariantCulture, "{0:N0} {1}", amount, CurrencyName);

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
            return Format(_money);
        }
    }
}
