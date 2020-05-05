using UnityEngine;

namespace MyGame
{
    public class ReloadStateMachineBehaviour : StateMachineBehaviour
    {
        AnimatorEventCallback publisher;

        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!publisher)
            {
               publisher = animator.gameObject.GetComponent<AnimatorEventCallback>();
            }
        }

        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            publisher?.SendMessage("ReloadGunFinish");
        }
    }
}
