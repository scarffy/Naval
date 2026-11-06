using System;
using System.Collections.Generic;
using Naval.Core;
using UnityEngine;

namespace Naval.Slice
{
    /// <summary>
    /// Owns time. Turns real seconds into game days (one AdvanceDay call per day) and tells everyone
    /// else through <see cref="DayAdvanced"/>. Pause is speed 0.
    /// </summary>
    public sealed class SliceClock : MonoBehaviour
    {
        /// <summary>Most days processed in one frame, so a long hitch cannot snowball.</summary>
        const int MaxDaysPerFrame = 4;

        [SerializeField] SliceBootstrap bootstrap;
        [SerializeField] float secondsPerDay = 1.5f;

        float elapsed;   // days of progress into the current day, 0 to 1

        /// <summary>Raised after each day with what happened that day.</summary>
        public event Action<IReadOnlyList<GameEvent>> DayAdvanced;

        /// <summary>0 (paused), 1, 2 or 4.</summary>
        public float Speed { get; private set; }

        /// <summary>How far into the current day we are, 0 to 1. Used to move ships smoothly.</summary>
        public float DayFraction { get; private set; }

        void Start()
        {
            if (bootstrap == null)
            {
                Debug.LogError("SliceClock: the bootstrap field is not assigned.");
                enabled = false;
                return;
            }
            Speed = 1f;
        }

        public void SetSpeed(float speed)
        {
            Speed = Mathf.Max(0f, speed);
        }

        void Update()
        {
            GameState state = bootstrap.State;
            if (state == null) return;
            if (state.Outcome != GameOutcome.InProgress)
            {
                DayFraction = 1f;
                return;
            }

            elapsed += Time.deltaTime * Speed / Mathf.Max(0.01f, secondsPerDay);
            int days = 0;
            while (elapsed >= 1f && days < MaxDaysPerFrame && state.Outcome == GameOutcome.InProgress)
            {
                elapsed -= 1f;
                days++;
                IReadOnlyList<GameEvent> events = state.AdvanceDay();
                Action<IReadOnlyList<GameEvent>> handler = DayAdvanced;
                if (handler != null) handler(events);
            }
            if (elapsed >= 1f) elapsed = 0.999f;   // hit the cap: drop the backlog
            DayFraction = state.Outcome == GameOutcome.InProgress ? Mathf.Clamp01(elapsed) : 1f;
        }
    }
}
