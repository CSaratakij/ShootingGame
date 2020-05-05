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
        int idlePistolNameHash;

        bool isAiming = false;
        bool isFireWeapon = false;

        bool isPreviousAiming = false;
        bool isPreviousFireWeapon = false;

        bool isDisableRotateY = false;

        bool isInitChestIdleRotation = false;
        bool isInitChestIdlePistolRotation = false;

        float currentAimRotateX;
        float currentAimRotateY;

        float targetAimRotateY;

        Animator animator;
        Transform chestBone;

        Quaternion originalIdleChestRotation;
        Quaternion originalIdlePistolChestRotation;

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

        void OnAnimatorIK(int layerIndex)
        {
            LayerHandler(layerIndex);
        }

        void Initialize()
        {
            animator = GetComponent<Animator>();
            chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);

            currentAimRotateY = maxAimRotateY;

            originalIdleChestRotation = Quaternion.identity;
            originalIdlePistolChestRotation = Quaternion.identity;

            idleNameHash = Animator.StringToHash("UpperArm.Idle");
            idlePistolNameHash = Animator.StringToHash("UpperArm.Pistol_Idle");
        }

        void InitializeChestRotation()
        {
            if (!isInitChestIdleRotation)
            {
                int expectLayer = (int) AnimLayer.UpperArm;
                int hash = idleNameHash;

                bool isPlayingIdleAnimation = (hash == animator.GetCurrentAnimatorStateInfo(expectLayer).fullPathHash);

                if (isPlayingIdleAnimation)
                {
                    originalIdleChestRotation = chestBone.localRotation;
                    isInitChestIdleRotation = true;
                }
            }

            if (!isInitChestIdlePistolRotation)
            {
                int expectLayer = (int) AnimLayer.UpperArm;
                int hash = idlePistolNameHash;

                bool isPlayingIdleAnimation = (hash == animator.GetCurrentAnimatorStateInfo(expectLayer).fullPathHash);

                if (isPlayingIdleAnimation)
                {
                    originalIdlePistolChestRotation = chestBone.localRotation;
                    isInitChestIdlePistolRotation = true;
                }
            }
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
            InitializeChestRotation();

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

            bool shouldFlickLowerArm = isAiming && isFireWeapon;

            if (shouldFlickLowerArm)
            {
                Debug.Log("Fire");

                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1.0f);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, Quaternion.Euler(90, 0.0f, 0.0f));

                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1.0f);
                animator.SetIKRotation(AvatarIKGoal.RightHand, Quaternion.Euler(90, 0.0f, 0.0f));
            }

            bool shouldRotateChestYAxis = isAiming || isFireWeapon;

            if (shouldRotateChestYAxis)
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
                    bool isHasWeapon = animator.GetBool("IsHasWeapon");

                    var rotRef = isHasWeapon ? originalIdlePistolChestRotation : originalIdleChestRotation;
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

        public void ToggleFireWeapon(bool value)
        {
            isPreviousFireWeapon = isFireWeapon;
            isFireWeapon = value;
        }

        public void DisableRotateY(bool value)
        {
            isDisableRotateY = value;
        }
    }
}
