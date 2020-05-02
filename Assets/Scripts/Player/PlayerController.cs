using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("General")]
        [SerializeField]
        bool isControlable;

        [SerializeField]
        NetworkType networkType;

        [SerializeField]
        Animator animator;

        [SerializeField]
        HumaniodIK humaniodIK;

        [Header("Setting")]
        [SerializeField]
        float moveSpeed;

        [SerializeField]
        float runSpeedMultipiler = 1.2f;

        [SerializeField]
        float jumpSpeed;

        [SerializeField]
        float gravity;

        [SerializeField]
        float rotationDamp;

        public enum NetworkType
        {
            Offline,
            Online
        }

        enum AimState
        {
            None,
            Aim
        }

        public uint ID { get; set; }

        float moveSpeedMultipiler = 1.0f;
        float zoomDistanceMultipiler = 1.0f;

        bool isStartRun = false;
        bool isSwitchCameraSide = false;

        Vector3 inputVector;
        Vector3 velocity;

        Transform rotationBasis;
        new ThirdPersonCamera camera;
        CharacterController characterController;

        AimState aimState;

        void Awake()
        {
            Initialize();
            HideCursor();
        }

        void Update()
        {
            InputHandler();
            MoveHandler();
        }

        void LateUpdate()
        {
            FacingHandler();
            AnimationHandler();
        }

        void Initialize()
        {
            camera = Camera.main.GetComponent<ThirdPersonCamera>();
            characterController = GetComponent<CharacterController>();

            rotationBasis = camera.ExternalBasis;
        }

        void InputHandler()
        {
            inputVector.x = Input.GetAxis("Horizontal");
            inputVector.z = Input.GetAxis("Vertical");

            if (inputVector.sqrMagnitude > 1.0f)
            {
                inputVector = inputVector.normalized;
            }

            inputVector.y = Input.GetButtonDown("Jump") ? 1.0f : 0.0f;

            bool isRunning = (inputVector.sqrMagnitude > 0.0f) && (AimState.None == aimState) && Input.GetButton("Run");
            moveSpeedMultipiler = isRunning ? runSpeedMultipiler : 1.0f;

            aimState = Input.GetButton("Fire2") ? AimState.Aim : AimState.None;
            bool shouldZoomCamera = (AimState.Aim == aimState);

            if (Input.GetButtonDown("Fire2") || Input.GetButtonUp("Fire2"))
            {
                camera.ToggleZoom(shouldZoomCamera);
                humaniodIK.ToggleAim(shouldZoomCamera, isSwitchCameraSide);
            }

            if (isRunning)
            {
                if (!isStartRun)
                {
                    camera.ToggleExtraOffset(true);
                    isStartRun = true;
                }
            }
            else
            {
                if (isStartRun)
                {
                    camera.ToggleExtraOffset(false);
                    isStartRun = false;
                }
            }

            if (Input.GetButtonDown("SwitchCamera"))
            {
                isSwitchCameraSide = !isSwitchCameraSide;
                camera.ToggleViewSide();

                var weight = isSwitchCameraSide ? 0.0f : 1.0f;
                humaniodIK.ToggleFlipSide(isSwitchCameraSide, weight);
            }
        }

        void AnimationHandler()
        {
            bool isMove = Mathf.Abs(inputVector.x) > 0.0f || Mathf.Abs(inputVector.z) > 0.0f;
            bool isAim = (AimState.Aim == aimState);

            float moveAnimationMultipiler = moveSpeedMultipiler > 1.0f ? 1.1f : 1.0f;

            animator.SetFloat("MoveMultipiler", moveAnimationMultipiler);
            animator.SetBool("IsMove", isMove);
            animator.SetBool("IsAim", isAim);
            animator.SetFloat("MoveX", inputVector.x);
            animator.SetFloat("MoveZ", inputVector.z);
        }

        void MoveHandler()
        {
            var moveSideway = rotationBasis.right * inputVector.x;
            var moveForward = rotationBasis.forward * inputVector.z;

            var moveDir = (moveSideway + moveForward);

            if (characterController.isGrounded)
            {
                velocity.x = (moveDir.x * moveSpeed * moveSpeedMultipiler);
                velocity.z = (moveDir.z * moveSpeed * moveSpeedMultipiler);

                if (inputVector.y > 0.0f)
                {
                    animator.SetTrigger("Jump");
                    velocity.y = jumpSpeed;
                }
                else
                {
                    velocity.y = -gravity * Time.deltaTime;
                }
            }
            else
            {
                velocity.y -= gravity * Time.deltaTime;
            }

            velocity.y = Mathf.Clamp(velocity.y, -gravity, gravity);
            characterController.Move(velocity * Time.deltaTime);
        }

        void HideCursor()
        {
            ShowCursor(false);
        }

        void ShowCursor(bool value = true)
        {
            Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = value;
        }

        void FacingHandler()
        {
            bool shouldFacingBasis = (inputVector.sqrMagnitude > 0.0f) || (AimState.Aim == aimState);

            if (shouldFacingBasis)
            {
                Quaternion targetRotation = rotationBasis.rotation;

                bool shouldForceFacingBasis = (AimState.Aim == aimState);

                if (!shouldForceFacingBasis)
                {
                    float absHorizontal = Mathf.Abs(inputVector.x);
                    float absForward = Mathf.Abs(inputVector.z);

                    bool isOnlyPressHorizontal = (absHorizontal > 0.0f) && Mathf.Approximately(absForward, 0.0f);
                    bool isOnlyPressForward = (absForward > 0.0f) && Mathf.Approximately(absHorizontal, 0.0f);

                    if (isOnlyPressHorizontal)
                    {
                        if (inputVector.x > 0.0f)
                        {
                            targetRotation *= Quaternion.Euler(0, 90.0f, 0);
                        }
                        else if (inputVector.x < 0.0f)
                        {
                            targetRotation *= Quaternion.Euler(0, -90.0f, 0);
                        }
                    }
                    else if (isOnlyPressForward)
                    {
                        bool shouldReverseRotation = (AimState.None == aimState) && (inputVector.z < -0.0f);

                        if (shouldReverseRotation)
                        {
                            targetRotation *= Quaternion.Euler(0, 180.0f, 0);
                        }
                    }
                    else
                    {
                        if (inputVector.z < -0.0f)
                        {
                            targetRotation *= Quaternion.Euler(0, 180.0f, 0);
                        }
                    }
                }

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationDamp);
            }
        }
    }
}
