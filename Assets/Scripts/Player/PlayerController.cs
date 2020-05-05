using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        static readonly Vector3 VIEWPORT_CENTER = new Vector3(0.5f, 0.5f, 0.0f);
        static readonly Quaternion Y_90_DEGREE = Quaternion.Euler(0, 90.0f, 0);
        static readonly Quaternion Y_NEGATIVE_90_DEGREE = Quaternion.Euler(0, -90.0f, 0);
        static readonly Quaternion Y_180_DEGREE = Quaternion.Euler(0, 180.0f, 0);

        [Header("General")]
        [SerializeField]
        bool isControlable;

        [SerializeField]
        NetworkType networkType;

        [SerializeField]
        Transform tempDir;

        [SerializeField]
        Transform dropItemRef;

        [SerializeField]
        Animator animator;

        [SerializeField]
        HumaniodIK humaniodIK;

        [SerializeField]
        Gun gun;

        [SerializeField]
        Transform[] gunHandSide;

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

        [SerializeField]
        float maxPickItemDistance = 5.0f;

        [SerializeField]
        LayerMask itemLayer;

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
        
        enum GunHand
        {
            Left,
            Right,
        }

        public uint ID { get; set; }
        public bool IsHasWeapon => (gun != null);

        int hipShootType = 0;

        float moveSpeedMultipiler = 1.0f;
        float zoomDistanceMultipiler = 1.0f;

        float nonZeroMoveX = 1.0f;

        float lastFireTimeStamp = 0.0f;
        float fireTimeDuration = 0.5f;

        float lastHipFireTimeStamp = 0.0f;
        float hipFireTimeDuration = 0.5f;

        bool isPressShoot = false;

        bool isStartRun = false;
        bool isStartReload = false;

        bool isSwitchCameraSide = false;
        bool isFireWeapon = false;
        bool isHipFireWeapon = false;

        Vector3 inputVector;
        Vector3 velocity;

        Transform rotationBasis;
        new ThirdPersonCamera camera;
        CharacterController characterController;

        AimState aimState;

        GunHand currentHand = GunHand.Right;
        int currentHandIndex = (int) GunHand.Right;

        void Awake()
        {
            Initialize();
            HideCursor();
            SetGunSide(GunHand.Right);
        }

        void Start()
        {
            if (!isControlable)
            {
                humaniodIK.DisableRotateY(true);
            }
        }

        void Update()
        {
            InputHandler();
            MoveHandler();
        }

        void LateUpdate()
        {
            FlagHandler();
            SwitchGunSideHandler();
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

            if (Mathf.Abs(inputVector.x) > 0.0f)
            {
                nonZeroMoveX = inputVector.x;
            }

            if (inputVector.sqrMagnitude > 1.0f)
            {
                inputVector = inputVector.normalized;
            }

            inputVector.y = Input.GetButtonDown("Jump") ? 1.0f : 0.0f;

            bool isRunning = (inputVector.sqrMagnitude > 0.0f) && (AimState.None == aimState) && Input.GetButton("Run");
            moveSpeedMultipiler = isRunning ? runSpeedMultipiler : 1.0f;

            aimState = Input.GetButton("Fire2") ? AimState.Aim : AimState.None;
            bool shouldZoomCamera = (AimState.Aim == aimState);

            isPressShoot = Input.GetButtonDown("Fire1");
            bool canPullTrigger = isPressShoot && IsHasWeapon && !isStartReload;

            if (canPullTrigger)
            {
                Ray ray = camera.Camera.ViewportPointToRay(VIEWPORT_CENTER);

                gun?.PullTrigger(ray, (success, hitInfo) => {
                    if (!success)
                    {
                        if (gun.IsEmptyMagazine)
                        {
                            AttemptReload();
                        }

                        return;
                    }

                    if (AimState.None == aimState)
                    {
                        bool isNotMove = Mathf.Approximately(inputVector.sqrMagnitude, 0.0f);

                        if (isNotMove)
                        {
                            hipShootType = 1;
                        }
                        else
                        {
                            var forwardDir = transform.forward;
                            var relativeVector = (tempDir.position - transform.position);

                            var product = Vector3.Dot(forwardDir, relativeVector);
                            hipShootType = (product <= 4.5f) ? 2 : 1;
                        }

                        animator.SetFloat("HipShootType", hipShootType);
                        animator.SetTrigger("HipFire");

                        lastHipFireTimeStamp = (Time.time + hipFireTimeDuration);
                        isHipFireWeapon = true;

                        humaniodIK.ToggleFireWeapon(true, Vector3.zero);
                    }

                    lastFireTimeStamp = (Time.time + fireTimeDuration);
                    isFireWeapon = true;
                });
            }

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

            if (Input.GetKeyDown(KeyCode.R) && IsHasWeapon)
            {
                if (!gun.IsFullMagazine)
                {
                    AttemptReload(true);
                }
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                Ray ray = camera.Camera.ViewportPointToRay(VIEWPORT_CENTER);
                RaycastHit pickUpHitInfo;

                if (Physics.Raycast(ray, out pickUpHitInfo, 100.0f, itemLayer))
                {
                    if (pickUpHitInfo.transform == null)
                        return;

                    var distance = pickUpHitInfo.transform.position - transform.position;
                    bool allowPickup = distance.sqrMagnitude <= (maxPickItemDistance * maxPickItemDistance);

                    if (!allowPickup)
                        return;

                    bool isItemIsAGun = pickUpHitInfo.transform.gameObject.CompareTag("Gun");

                    if (isItemIsAGun)
                    {
                        gun = pickUpHitInfo.transform.gameObject.GetComponent<Gun>();
                        gun?.Pickup(gunHandSide[(int) GunHand.Right]);
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.G) && IsHasWeapon)
            {
                if (isStartReload)
                    return;

                gun?.Drop(dropItemRef.position);
                gun = null;
            }
        }

        void AnimationHandler()
        {
            bool isMove = Mathf.Abs(inputVector.x) > 0.0f || Mathf.Abs(inputVector.z) > 0.0f;
            bool isAim = (AimState.Aim == aimState) && IsHasWeapon;

            float moveAnimationMultipiler = moveSpeedMultipiler > 1.0f ? 1.1f : 1.0f;

            animator.SetFloat("MoveMultipiler", moveAnimationMultipiler);
            animator.SetBool("IsPressShoot", isPressShoot);
            animator.SetBool("IsMove", isMove);
            animator.SetBool("IsAim", isAim);
            animator.SetFloat("MoveX", inputVector.x);
            animator.SetFloat("MoveZ", inputVector.z);
            animator.SetFloat("NonZeroMoveX", nonZeroMoveX);
            animator.SetFloat("GunHandSide", currentHandIndex);
        }

        void FlagHandler()
        {
            if (isFireWeapon && Time.time > lastFireTimeStamp)
            {
                isFireWeapon = false;
            }

            if (isHipFireWeapon && Time.time > lastHipFireTimeStamp)
            {
                isHipFireWeapon = false;
                humaniodIK.ToggleFireWeapon(false, Vector3.zero);
            }
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
            bool shouldForceRotateOnHipFire = isHipFireWeapon && Mathf.Approximately(inputVector.sqrMagnitude, 0.0f) && (AimState.None == aimState);

            if (shouldForceRotateOnHipFire)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, rotationBasis.rotation, rotationDamp);
                return;
            }

            if (shouldFacingBasis)
            {
                Quaternion targetRotation = rotationBasis.rotation;

                bool shouldForceFacingBasis = (AimState.Aim == aimState);

                if (!shouldForceFacingBasis)
                {
                    float absHorizontal = Mathf.Abs(inputVector.x);
                    float absForward = Mathf.Abs(inputVector.z);

                    bool isOnlyPressHorizontal = (absHorizontal > 0.0f) && Mathf.Approximately(absForward, 0.0f);
                    bool isOnlyPressForward = (inputVector.z > 0.0f) && Mathf.Approximately(absHorizontal, 0.0f);

                    if (isOnlyPressHorizontal)
                    {
                        if (inputVector.x > 0.0f)
                        {
                            targetRotation *= Y_90_DEGREE;
                        }
                        else if (inputVector.x < 0.0f)
                        {
                            targetRotation *= Y_NEGATIVE_90_DEGREE;
                        }
                    }
                    else if (isOnlyPressForward)
                    {
                        bool shouldReverseRotation = (AimState.None == aimState) && (inputVector.z < 0.0f);

                        if (shouldReverseRotation)
                        {
                            targetRotation *= Y_180_DEGREE;
                        }
                    }
                    else
                    {
                        if (inputVector.z < 0.0f)
                        {
                            bool shouldRotateSideWay = isHipFireWeapon && (AimState.None == aimState) && (hipShootType == 2);

                            if (shouldRotateSideWay)
                            {
                                switch (currentHand)
                                {
                                    case GunHand.Right:
                                    {
                                        targetRotation *= Y_90_DEGREE;
                                    }
                                    break;

                                    case GunHand.Left:
                                    {
                                        targetRotation *= Y_NEGATIVE_90_DEGREE;
                                    }
                                    break;

                                    default:
                                    break;
                                }
                            }
                            else
                            {
                                targetRotation *= Y_180_DEGREE;
                            }
                        }
                    }
                }

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationDamp);
            }
        }

        void SwitchGunSideHandler()
        {
            if (!isFireWeapon)
            {
                if (AimState.Aim == aimState && GunHand.Left == currentHand)
                {
                    SetGunSide(GunHand.Right);
                }

                return;
            }

            switch (aimState)
            {
                case AimState.None:
                {
                    if (hipShootType == 1)
                    {
                        SetGunSide(GunHand.Right);
                        return;
                    }

                    if (nonZeroMoveX > 0.0f)
                    {
                        SetGunSide(GunHand.Right);
                    }
                    else if (nonZeroMoveX < 0.0f)
                    {
                        SetGunSide(GunHand.Left);
                    }
                }
                break;

                case AimState.Aim:
                {
                    if (GunHand.Left == currentHand)
                    {
                        SetGunSide(GunHand.Right);
                    }
                }
                break;

                default:
                break;
            }
        }

        void SetGunSide(GunHand side)
        {
            var currentIndice = (int) currentHand;
            var indice = (int) side;

            if (currentIndice == indice)
                return;

            currentHand = side;
            currentHandIndex = indice;

            if (IsHasWeapon)
            {
                gun.transform.parent = gunHandSide[indice];
                gun.transform.localPosition = Vector3.zero;
                gun.transform.localRotation = Quaternion.identity;
            }
        }

        void AttemptReload(bool forceReload = false)
        {
            bool shouldReload = !isStartReload && (gun.IsEmptyMagazine);

            if (forceReload || shouldReload)
            {
                isStartReload = true;

                animator.SetTrigger("Reload");
                gun.PlayReloadSound();
            }
        }

        void ReloadGunFinish()
        {
            gun?.Reload();
            isStartReload = false;
        }
    }
}
