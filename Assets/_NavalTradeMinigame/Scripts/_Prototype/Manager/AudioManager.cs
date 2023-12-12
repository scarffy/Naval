using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<AudioManager>();
                return _instance;
            }
        }

        public static AudioManager _instance;
    }
}
