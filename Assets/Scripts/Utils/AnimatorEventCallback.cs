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

        void Dead()
        {
            listener?.SendMessage("Dead");
        }
    }
}
