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
        float rotateDampX = 0.02f;

        [SerializeField]
        float rotateRateY = 5.0f;

        bool isAiming = false;
        bool isFireWeapon = false;

        float currentAimRotateX;
        float currentAimRotateY;

        float targetAimRotateY;

        Animator animator;
        Transform chestBone;

        Vector3 facingRotation;

        enum AnimLayer
        {
            BaseLayer = 0,
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
            chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);
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

            if (isAiming)
            {
                var rotRef = Quaternion.Euler(aimReference.rotation.eulerAngles.x, 0.0f, 0.0f);
                var rotCurrent = Quaternion.Euler(chestBone.localRotation.eulerAngles.x, 0.0f, 0.0f);

                var chestRotation = Quaternion.Slerp(rotCurrent, rotRef, rotateDampX);
                animator.SetBoneLocalRotation(HumanBodyBones.Chest, chestRotation);
            }

            bool shouldStopForceRotate = !isAiming && Mathf.Approximately(currentAimRotateY, targetAimRotateY);
            if (shouldStopForceRotate) { return; }

            currentAimRotateY = Mathf.MoveTowards(currentAimRotateY, targetAimRotateY, rotateRateY);
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

        // public void ToggleFireWeapon(bool value, Quaternion facingRotation)
        public void ToggleFireWeapon(bool value, Vector3 facingRotation)
        {
            isFireWeapon = value;
            // this.facingRotation = facingRotation;
            this.facingRotation = facingRotation;
        }
    }
}
