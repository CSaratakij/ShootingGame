using UnityEngine;

namespace MyGame
{
    public class DeadStateMachineBehaviour : StateMachineBehaviour
    {
        AnimatorEventCallback publisher;

        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!publisher)
            {
               publisher = animator.gameObject.GetComponent<AnimatorEventCallback>();
               publisher?.SendMessage("Dead");
            }
        }
    }
}
