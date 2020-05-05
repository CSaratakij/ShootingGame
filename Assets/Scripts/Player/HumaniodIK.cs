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
        float negateHipFireXAxis = 30.0f;

        [SerializeField]
        float maxAimRotateY = 25.0f;

        [SerializeField]
        float rotateDampX = 0.02f;

        [SerializeField]
        float toNormalChestDamp = 0.05f;

        [SerializeField]
        float rotateRateY = 5.0f;

        int idleNameHash;

        bool isInitChestOriginalRotation = false;
        bool isAiming = false;
        bool isFireWeapon = false;

        bool isPreviousAiming = false;
        bool isPreviousFireWeapon = false;

        bool isDisableRotateY = false;

        float currentAimRotateX;
        float currentAimRotateY;

        float targetAimRotateY;

        Animator animator;
        Transform chestBone;

        Vector3 facingRotation;

        Quaternion originalChestRotation;
        Quaternion lastAimChestRotation;

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
            chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);

            currentAimRotateY = maxAimRotateY;
            originalChestRotation = Quaternion.identity;
            idleNameHash = Animator.StringToHash("UpperArm.Pistol_Idle");
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
            if (!isInitChestOriginalRotation)
            {
                int expectLayer = (int) AnimLayer.UpperArm;
                bool isPlayingIdleAnimation = (idleNameHash == animator.GetCurrentAnimatorStateInfo(expectLayer).fullPathHash);

                if (isPlayingIdleAnimation)
                {
                    originalChestRotation = Quaternion.Euler(chestBone.localRotation.eulerAngles.x, 0.0f, 0.0f);
                    isInitChestOriginalRotation = true;
                }
            }

            bool shouldLookAtTarget = (!isAiming && lookReference != null);

            if (shouldLookAtTarget)
            {
                animator.SetLookAtWeight(0.5f);
                animator.SetLookAtPosition(lookReference.position);
            }
            else
            {
                animator.SetLookAtWeight(0.0f);
            }

            if (isAiming || isFireWeapon)
            {
                float angleX = aimReference.rotation.eulerAngles.x;

                bool shouldNegateChestRotation = (!isAiming && isFireWeapon);

                if (shouldNegateChestRotation)
                {
                    angleX += negateHipFireXAxis;
                }

                var rotRef = Quaternion.Euler(angleX, 0.0f, 0.0f);
                var rotCurrent = Quaternion.Euler(chestBone.localRotation.eulerAngles.x, 0.0f, 0.0f);

                var chestRotation = Quaternion.Slerp(rotCurrent, rotRef, rotateDampX);
                lastAimChestRotation = chestRotation;

                animator.SetBoneLocalRotation(HumanBodyBones.Chest, chestRotation);
            }
            else
            {
                bool shouldRotateChestBackWithSmooth = (isPreviousAiming && !isAiming) || (isPreviousFireWeapon && !isFireWeapon);

                if (shouldRotateChestBackWithSmooth)
                {
                    var rotRef = originalChestRotation;
                    var rotCurrent = lastAimChestRotation;

                    lastAimChestRotation = Quaternion.Slerp(rotCurrent, rotRef, toNormalChestDamp);
                    animator.SetBoneLocalRotation(HumanBodyBones.Chest, lastAimChestRotation);

                    float angle = Quaternion.Angle(lastAimChestRotation, rotRef);
                    bool acceptableError = Mathf.Approximately(angle, 0.0f);

                    if (acceptableError)
                    {
                        isPreviousAiming = false;
                        isPreviousFireWeapon = false;
                    }
                }
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
            isPreviousAiming = isAiming;
            isAiming = value;
            
            if (isDisableRotateY)
            {
                targetAimRotateY = 0.0f;
            }
            else
            {
                var maxTarget = isFlipSide ? -maxAimRotateY : maxAimRotateY;
                var result = value ? maxTarget : 0.0f;

                targetAimRotateY = result;
            }
        }

        public void ToggleFlipSide(bool value, float weight = 1.0f)
        {
            var absTargetAim = Mathf.Abs(targetAimRotateY) * weight;
            targetAimRotateY = value ? -absTargetAim : absTargetAim;
        }

        // public void ToggleFireWeapon(bool value, Quaternion facingRotation)
        public void ToggleFireWeapon(bool value, Vector3 facingRotation)
        {
            isPreviousFireWeapon = isFireWeapon;
            isFireWeapon = value;
            // this.facingRotation = facingRotation;
            this.facingRotation = facingRotation;
        }

        public void DisableRotateY(bool value)
        {
            isDisableRotateY = value;
        }
    }
}
