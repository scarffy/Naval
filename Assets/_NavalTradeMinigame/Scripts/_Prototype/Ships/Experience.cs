using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MiniGame
{
    /// <summary>
    /// This is already good code. I think. Usable for most stuffs.
    /// </summary>
    public class Experience : MonoBehaviour
    {
        [SerializeField] private int _level = 1;
        [SerializeField] private int _maxLevel = 100;
        [SerializeField] private int _experienceAmount = 0;
        [SerializeField] private int _nextExperience = 100;

        [Header("Animation Curve")] 
        public AnimationCurve animationCurve;

        #region UnityEvent
        public UnityEvent OnLevelUp;
        public UnityEvent OnLevelMaxReach;
        #endregion

        public int GetExperienceAmount => _experienceAmount;

        public void AddExperience(int amount)
        {
            if (_level >= _maxLevel)
                return;

            _experienceAmount += amount;
            if (_experienceAmount >= _nextExperience)
            {
                //! This code block make excess experience to carry over
                while (_experienceAmount >= _nextExperience)
                {
                    _experienceAmount -= _nextExperience;
                    //! Level up
                    _level++;
                    
                    //! Calculate the next experience amount require to level up
                    _nextExperience = GetRequiredExperience(_level);
                    
                    //! Invoke event for subscribers
                    OnLevelUp?.Invoke();
                }
            }
        }

        private int GetRequiredExperience(int level)
        {
            int requiredExperience =
                Mathf.RoundToInt(animationCurve.Evaluate(Mathf.InverseLerp(0, _maxLevel, level)) * _nextExperience);
            return requiredExperience;
        }
    }
}
