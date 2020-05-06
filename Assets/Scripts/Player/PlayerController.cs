using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        const int MAX_PENETRATION_TEST_BUFFER_SIZE = 3;

        static readonly Vector3 VIEWPORT_CENTER = new Vector3(0.5f, 0.5f, 0.0f);
        static readonly Quaternion Y_90_DEGREE = Quaternion.Euler(0, 90.0f, 0);
        static readonly Quaternion Y_NEGATIVE_90_DEGREE = Quaternion.Euler(0, -90.0f, 0);
        static readonly Quaternion Y_180_DEGREE = Quaternion.Euler(0, 180.0f, 0);
        static readonly Quaternion DROP_ITEM_ROTATION = Quaternion.Euler(0, 0, 90.0f);

        [Header("General")]
        [SerializeField]
        bool isControlable;

        [SerializeField]
        bool isStopProcessInput;

        [SerializeField]
        NetworkType networkType;

        [SerializeField]
        Transform dropItemRef;

        [SerializeField]
        Animator animator;

        [SerializeField]
        HumaniodIK humaniodIK;

        [SerializeField]
        BoxCollider dummyCollider;

        [SerializeField]
        Gun gun;

        [SerializeField]
        Transform[] gunHandSide;

        [SerializeField]
        AudioClip[] audioClips;

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
        float maxDropItemDistance = 4.0f;

        [SerializeField]
        LayerMask itemLayer;

        [SerializeField]
        LayerMask defaultLayer;

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

        enum SFX
        {
            Pickup,
            Drop
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

        bool isRunning = false;
        bool isSwitchCameraSide = false;
        bool isFireWeapon = false;
        bool isHipFireWeapon = false;

        HUDInfo hudInfo;

        Vector3 inputVector;
        Vector3 velocity;

        Transform rotationBasis;
        new ThirdPersonCamera camera;
        CharacterController characterController;

        AimState aimState;
        AudioSource audioSource;

        GunHand currentHand = GunHand.Right;
        int currentHandIndex = (int) GunHand.Right;

        Collider[] penetrationBuffer;

        void Awake()
        {
            Initialize();
            HideCursor();
            SetGunSide(GunHand.Right);
        }

        void Start()
        {
            if (isControlable)
            {
                UpdateHUD(hudInfo);
            }
            else
            {
                humaniodIK.DisableRotateY(true);
            }

            //Test
            UIController.Instance?.ShowInGameMenu();
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

            audioSource = GetComponent<AudioSource>();

            rotationBasis = camera.ExternalBasis;
            penetrationBuffer = new Collider[MAX_PENETRATION_TEST_BUFFER_SIZE];

            dummyCollider.enabled = false;
            dummyCollider.isTrigger = true;
        }

        void InputHandler()
        {
            if (isStopProcessInput)
            {
                inputVector = Vector3.zero;
                return;
            }

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

            isRunning = (inputVector.sqrMagnitude > 0.0f) && (AimState.None == aimState) && Input.GetButton("Run");
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
                            var facingDir = new Vector3(inputVector.x, 0.0f, inputVector.z);
                            var aimDir = rotationBasis.InverseTransformDirection(rotationBasis.forward);

                            var product = Vector3.Dot(facingDir, aimDir);
                            hipShootType = (product > 0.1f) ? 1 : 2;
                        }

                        animator.SetFloat("HipShootType", hipShootType);
                        animator.SetTrigger("HipFire");

                        lastHipFireTimeStamp = (Time.time + hipFireTimeDuration);
                        isHipFireWeapon = true;

                        humaniodIK.ToggleHipFireWeapon(true);
                    }
                    else if (AimState.Aim == aimState)
                    {
                        animator.SetTrigger("Fire");

                        lastFireTimeStamp = (Time.time + fireTimeDuration);
                        isFireWeapon = true;

                        humaniodIK.ToggleFireWeapon(true);
                    }

                    hudInfo.currentMagazine = gun.AmmoInMagazine;
                    UpdateHUD(hudInfo);
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
                bool canTriggerReload = !gun.IsFullMagazine || gun.IsEmptyMagazine;

                if (canTriggerReload)
                {
                    AttemptReload();
                }
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                PickUpNewGun();
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                DropCurrentGun();
            }
        }

        void AnimationHandler()
        {
            bool isMove = Mathf.Abs(inputVector.x) > 0.0f || Mathf.Abs(inputVector.z) > 0.0f;
            bool isAim = (AimState.Aim == aimState);

            float moveX = (isRunning) ? inputVector.x * 2.0f : inputVector.x;
            float moveZ = (isRunning) ? inputVector.z * 2.0f : inputVector.z;

            float moveAnimationMultipiler = (moveSpeedMultipiler > 1.0f) ? 1.1f : 1.0f;

            animator.SetBool("IsPressShoot", isPressShoot);
            animator.SetBool("IsMove", isMove);
            animator.SetBool("IsRun", isRunning);
            animator.SetBool("IsAim", isAim);
            animator.SetBool("IsGround", characterController.isGrounded);
            animator.SetBool("IsHasWeapon", IsHasWeapon);
            animator.SetBool("IsReloading", isStartReload);

            animator.SetFloat("MoveMultipiler", moveAnimationMultipiler);
            animator.SetFloat("MoveX", moveX, 1.0f, Time.deltaTime * 10.0f);
            animator.SetFloat("MoveZ", moveZ, 1.0f, Time.deltaTime * 10.0f);
            animator.SetFloat("NonZeroMoveX", nonZeroMoveX);
            animator.SetFloat("GunHandSide", currentHandIndex);
        }

        void FlagHandler()
        {
            if (isFireWeapon && Time.time > lastFireTimeStamp)
            {
                isFireWeapon = false;
                humaniodIK.ToggleFireWeapon(false);
            }

            if (isHipFireWeapon && Time.time > lastHipFireTimeStamp)
            {
                isHipFireWeapon = false;
                humaniodIK.ToggleHipFireWeapon(false);
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
            if (!isHipFireWeapon)
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
                gun.transform.parent = gunHandSide[currentHandIndex];
                gun.transform.localPosition = Vector3.zero;
                gun.transform.localRotation = Quaternion.identity;
            }
        }

        void PickUpNewGun()
        {
            if (isStartReload)
                return;

            if (IsHasWeapon)
            {
                DropCurrentGun();
            }

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

                if (!isItemIsAGun)
                {
                    return;
                }

                gun = pickUpHitInfo.transform.gameObject.GetComponent<Gun>();

                if (gun == null)
                {
                    return;
                }

                if (gun.IsHasOwner)
                {
                    return;
                }

                var collider = gun.GetComponent<BoxCollider>();

                if (collider)
                {
                    dummyCollider.center = collider.center;
                    dummyCollider.size = collider.size;
                }

                gun.Pickup(gunHandSide[(int) GunHand.Right]);

                hudInfo.currentMagazine = gun.AmmoInMagazine;
                hudInfo.maxMagazine = gun.MaxAmmoInMagazine;
                hudInfo.IsHasWeapon = IsHasWeapon;

                UpdateHUD(hudInfo);
                PlaySFXSound(SFX.Pickup);
            }
        }

        void DropCurrentGun()
        {
            if (!IsHasWeapon)
                return;

            if (isStartReload)
                return;

            var origin = transform.position;
            origin.y = dropItemRef.position.y;

            RaycastHit hitInfo;
            Ray ray = camera.Camera.ViewportPointToRay(VIEWPORT_CENTER);

            var dropPosition = Vector3.zero;
            bool shouldDropToAimDirection = Physics.Raycast(ray, out hitInfo, 10.0f, defaultLayer);

            if (shouldDropToAimDirection)
            {
                var expectPosition = ray.origin + (ray.direction.normalized * Mathf.Clamp(hitInfo.distance, 0.0f, maxDropItemDistance));
                int count = Physics.OverlapSphereNonAlloc(expectPosition, 1.0f, penetrationBuffer, defaultLayer);

                if (count > 0)
                    dummyCollider.enabled = true;

                for (int i = 0; i < count; ++i)
                {
                    var collider = penetrationBuffer[i];

                    Vector3 otherPosition = collider.gameObject.transform.position;
                    Quaternion otherRotation = collider.gameObject.transform.rotation;

                    Vector3 direction;
                    float distance;

                    bool overlapped = Physics.ComputePenetration(
                                    dummyCollider, expectPosition, DROP_ITEM_ROTATION,
                                    collider, otherPosition, otherRotation,
                                    out direction, out distance);
                    
                    if (overlapped)
                    {
                        var offset = direction * distance;
                        expectPosition += offset;
                    }
                }

                dropPosition = expectPosition;
            }
            else
            {
                dropPosition = gun.transform.position;
            }

            gun.Drop(dropPosition);

            gun = null;
            dummyCollider.enabled = false;

            hudInfo.currentMagazine = 0;
            hudInfo.maxMagazine = 0;
            hudInfo.IsHasWeapon = IsHasWeapon;

            UpdateHUD(hudInfo);
        }

        void AttemptReload(bool forceReload = false)
        {
            bool shouldReload = !isStartReload;

            if (forceReload || shouldReload)
            {
                isStartReload = true;

                animator.SetTrigger("Reload");
                gun.PlayReloadSound();

                hudInfo.IsReloading = isStartReload;
                UpdateHUD(hudInfo);
            }
        }

        void ReloadGunFinish()
        {
            gun?.Reload();
            isStartReload = false;

            hudInfo.currentMagazine = gun.AmmoInMagazine;
            hudInfo.IsReloading = isStartReload;

            UpdateHUD(hudInfo);
        }

        void Dead()
        {
            if (isStartReload)
            {
                isStartReload = false;
                gun?.StopReloadSound();
            }
            
            StopProcessInput(true);
        }

        void UpdateHUD(HUDInfo hudInfo)
        {
            UIHUDController.Instance?.UpdateUI(hudInfo);
        }

        void PlaySFXSound(SFX sound)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            int i = (int) sound;
            audioSource.PlayOneShot(audioClips[i]);
        }

        public void StopProcessInput(bool value = true)
        {
            isStopProcessInput = value;
        }
    }
}
