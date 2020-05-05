using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    public class AnimatorEventCallback : MonoBehaviour
    {
        [SerializeField]
        GameObject listener;

        void ReloadGunFinish()
        {
            listener?.SendMessage("ReloadGunFinish");
        }
    }
}
