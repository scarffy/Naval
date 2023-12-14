using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame
{
    public interface IDamageable
    { 
        void Damage(int amount);
        void Repair(int amount);
    }
}
