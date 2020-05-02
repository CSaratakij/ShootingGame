using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("General")]
        [SerializeField]
        Transform target;

        [SerializeField]
        Transform externalBasis;

        [Header("Setting")]
        [SerializeField]
        ViewSide viewSide = ViewSide.Right;

        [SerializeField]
        float normalFOV = 60.0f;

        [SerializeField]
        float zoomFOV = 40.0f;

        [SerializeField]
        float zoomRate = 100.0f;

        [SerializeField]
        float zoomOutRate = 3.0f;

        [SerializeField]
        float switchSideRate = 6.0f;

        [SerializeField]
        float rotationClamp;

        [SerializeField]
        Vector2 mouseSensitivity;

        [SerializeField]
        Vector3 normalOffset;

        [SerializeField]
        Vector3 extraOffset;

        enum ViewSide
        {
            Right = 1,
            Left = -1
        }

        public Transform ExternalBasis => externalBasis;

        float currentZoomFOV;
        float targetZoomFOV;

        bool shouldToggleZoom;

        Vector2 mouseInput;
        Vector3 rotationAxis;

        Vector3 offset;
        Vector3 currentOffset;

        ViewSide previousViewSide;

        void Awake()
        {
            Initialize();
        }

        void Update()
        {
            InputHandler();
        }

        void LateUpdate()
        {
            RotationHandler();
            OrbitHandler();
            OffsetHandler();
            ZoomHandler();
        }

        void Initialize()
        {
            externalBasis.parent = null;
            currentZoomFOV = normalFOV;
            targetZoomFOV = normalFOV;

            offset = normalOffset;
            currentOffset = offset;

            previousViewSide = viewSide;
        }

        void InputHandler()
        {
            mouseInput.x = Input.GetAxis("Mouse X");
            mouseInput.y = Input.GetAxis("Mouse Y");

            rotationAxis.x += -mouseInput.y * mouseSensitivity.x;
            rotationAxis.y += mouseInput.x * mouseSensitivity.y;

            rotationAxis.x = Mathf.Clamp(rotationAxis.x, -40.0f, 30.0f);

            if (rotationAxis.y > 360.0f)
            {
                rotationAxis.y -= 360.0f;
            }
            else if (rotationAxis.y < -360.0f)
            {
                rotationAxis.y += 360.0f;
            }
        }

        void RotationHandler()
        {
            var targetRotation = Quaternion.Euler(rotationAxis);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationClamp);
        }

        void OrbitHandler()
        {
            var orbitPosition = (transform.rotation * new Vector3(currentOffset.x, currentOffset.y, -currentOffset.z)) + target.position;
            transform.position = orbitPosition;
        }

        void OffsetHandler()
        {
            var isViewSideChanged = (previousViewSide != viewSide);

            if (isViewSideChanged)
            {
                offset.x = Mathf.Abs(offset.x) * ((int) viewSide);
                previousViewSide = viewSide;
            }

            var resultOffset = Vector3.MoveTowards(currentOffset, offset, zoomOutRate * Time.deltaTime);
            var switchSideOffset = Mathf.MoveTowards(currentOffset.x, offset.x, switchSideRate * Time.deltaTime);

            currentOffset.x = switchSideOffset;
            currentOffset.y = resultOffset.y;
            currentOffset.z = resultOffset.z;
        }

        void ZoomHandler()
        {
            currentZoomFOV = Mathf.MoveTowards(currentZoomFOV, targetZoomFOV, zoomRate * Time.deltaTime);
            Camera.main.fieldOfView = currentZoomFOV;
        }

        public void SetZoomFOV(float normalFOV, float zoomFOV)
        {
            this.normalFOV = normalFOV;
            this.zoomFOV = zoomFOV;

            this.currentZoomFOV = normalFOV;
            this.targetZoomFOV = normalFOV;
        }

        public void ToggleZoom(bool value)
        {
            targetZoomFOV = value ? zoomFOV : normalFOV;
        }

        public void ToggleExtraOffset(bool value)
        {
            var resultOffset = value ? extraOffset : normalOffset;
            resultOffset.x = Mathf.Abs(resultOffset.x) * ((int) viewSide);
            offset = resultOffset;
        }

        public void ToggleViewSide()
        {
            previousViewSide = viewSide;
            viewSide = (viewSide == ViewSide.Right) ? ViewSide.Left : ViewSide.Right;
        }
    }
}
