using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("General")]
        [SerializeField]
        NetworkType networkType;

        [SerializeField]
        bool isControlable;

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
            RotateHandler();
        }

        void Initialize()
        {
            camera = Camera.main.GetComponent<ThirdPersonCamera>();
            characterController = GetComponent<CharacterController>();
            rotationBasis = camera.ExternalBasis;
        }

        void InputHandler()
        {
            inputVector.x = Input.GetAxisRaw("Horizontal");
            inputVector.z = Input.GetAxisRaw("Vertical");

            if (inputVector.sqrMagnitude > 1.0f)
            {
                inputVector = inputVector.normalized;
            }

            inputVector.y = Input.GetButtonDown("Jump") ? 1.0f : 0.0f;

            moveSpeedMultipiler = Input.GetButton("Run") ? runSpeedMultipiler : 1.0f;
            aimState = Input.GetButton("Fire2") ? AimState.Aim : AimState.None;
        }

        void MoveHandler()
        {
            var moveSideway = rotationBasis.right * inputVector.x;
            var moveForward = rotationBasis.forward * inputVector.z;

            var moveDir = (moveSideway + moveForward);

            velocity.x = (moveDir.x * moveSpeed * moveSpeedMultipiler);
            velocity.z = (moveDir.z * moveSpeed * moveSpeedMultipiler);

            if (characterController.isGrounded && inputVector.y > 0.1f)
                velocity.y = jumpSpeed;

            velocity.y -= gravity * Time.deltaTime;
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

        void RotateHandler()
        {
            if (inputVector.sqrMagnitude > 0.1f)
            {
                Quaternion targetRotation = rotationBasis.rotation;

                bool shouldReverseRotation = (AimState.None == aimState) && (inputVector.z < -0.1f);

                if (shouldReverseRotation)
                {
                    targetRotation *= Quaternion.Euler(0, 180f, 0);
                }

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationDamp);
            }
        }
    }
}
