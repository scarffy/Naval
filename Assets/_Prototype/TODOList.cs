using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.Prototype
{
    [CreateAssetMenu(fileName = "TODO List")]
    public class TODOList : ScriptableObject
    {
        public List<string> priorities;
    }
}
