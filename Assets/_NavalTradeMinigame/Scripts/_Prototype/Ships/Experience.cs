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

        [Header("Required Experience")]
        [Tooltip("Experience needed to go from level 1 to level 2")]
        [SerializeField] private int _firstLevelExperience = 100;
        [Tooltip("Experience needed to reach the max level")]
        [SerializeField] private int _lastLevelExperience = 10000;

        [Header("Animation Curve")] 
        [Tooltip("How the requirement grows: 0 = first level's requirement, 1 = last level's")]
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

            //! Excess experience carries over to the next level
            while (_level < _maxLevel && _experienceAmount >= _nextExperience)
            {
                _experienceAmount -= _nextExperience;
                _level++;
                _nextExperience = GetRequiredExperience(_level);
                OnLevelUp?.Invoke();
            }

            if (_level >= _maxLevel)
            {
                _experienceAmount = 0;
                OnLevelMaxReach?.Invoke();
            }
        }

        //! Experience needed to go from this level to the next. Never less than 1, so leveling always ends.
        private int GetRequiredExperience(int level)
        {
            float t = animationCurve.Evaluate(Mathf.InverseLerp(1, _maxLevel - 1, level));
            int requiredExperience = Mathf.RoundToInt(Mathf.Lerp(_firstLevelExperience, _lastLevelExperience, t));
            return Mathf.Max(1, requiredExperience);
        }
    }
}
