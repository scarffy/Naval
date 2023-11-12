using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame
{
    [CreateAssetMenu(fileName = "Voyage Event", menuName = "Mini Game/Voyage Event", order = 1)]
    public class VoyageEventSO : ScriptableObject
    {
        // public Enum EventType
        // {
        //     Storm, // 30% of probability that the voyage will be longer by +15
        //     Pirate, // 30% of probability that you will lose 10-40 pieces of your goods, depending on the amount, regardless of the type.
        //     Hurricane //30% of probability that the voyage will be longer by +30
        // }
        
        public enum  EventType
        {
            None,
            Storm,
            Pirate,
            Hurricane
        }

        public EventType _voyageEvent = EventType.None;
    }
}
