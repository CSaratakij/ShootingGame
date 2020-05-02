using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class HumaniodIK : MonoBehaviour
    {
        [Header("General")]
        [SerializeField]
        Transform lookReference;

        [SerializeField]
        Transform aimReference;

        [Header("Setting")]
        [SerializeField]
        float maxAimRotateY = 25.0f;

        [SerializeField]
        float rotateRate = 5.0f;

        bool isAiming = false;

        float currentAimRotateY;
        float targetAimRotateY;

        Animator animator;

        enum AnimLayer
        {
            UpperArm = 1,
            LowerBody
        }

        void Awake()
        {
            Initialize();
        }

        void Initialize()
        {
            animator = GetComponent<Animator>();
            currentAimRotateY = maxAimRotateY;
        }

        void OnAnimatorIK(int layerIndex)
        {
            LayerHandler(layerIndex);
        }

        void LayerHandler(int layerIndex)
        {
            var givenLayer = (AnimLayer) layerIndex;

            switch (givenLayer)
            {
                case AnimLayer.UpperArm:
                {
                    HandleUpperArmLayer();
                }
                break;

                // case AnimLayer.LowerBody:
                // {
                //     HandleLowerBodyLayer();
                // }
                // break;

                default:
                    // Debug.Log("Unknown animator layer...");
                break;
            }
        }

        void HandleUpperArmLayer()
        {
            if (lookReference)
            {
                animator.SetLookAtWeight(0.5f);
                animator.SetLookAtPosition(lookReference.position);
            }

            bool shouldStopForcingRotate = !isAiming && Mathf.Approximately(currentAimRotateY, 0.0f);
            if (shouldStopForcingRotate) { return; }

            currentAimRotateY = Mathf.MoveTowards(currentAimRotateY, targetAimRotateY, rotateRate);
            animator.SetBoneLocalRotation(HumanBodyBones.UpperChest, Quaternion.Euler(0, currentAimRotateY, 0));
        }

        void HandleLowerBodyLayer()
        {

        }

        public void ToggleAim(bool value, bool isFlipSide = false)
        {
            isAiming = value;
            
            var maxTarget = isFlipSide ? -maxAimRotateY : maxAimRotateY;
            var result = value ? maxTarget : 0.0f;

            targetAimRotateY = result;
        }

        public void ToggleFlipSide(bool value, float weight = 1.0f)
        {
            var absTargetAim = Mathf.Abs(targetAimRotateY) * weight;
            targetAimRotateY = value ? -absTargetAim : absTargetAim;
        }
    }
}
