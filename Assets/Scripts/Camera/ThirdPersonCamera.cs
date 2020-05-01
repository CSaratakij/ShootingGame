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
        float rotationClamp;

        [SerializeField]
        Vector2 mouseSensitivity;

        [SerializeField]
        Vector3 offset;

        public Transform ExternalBasis => externalBasis;

        Vector2 mouseInput;
        Vector3 rotationAxis;

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
        }

        void Initialize()
        {
            externalBasis.parent = null;
        }

        void InputHandler()
        {
            mouseInput.x = Input.GetAxis("Mouse X");
            mouseInput.y = Input.GetAxis("Mouse Y");

            rotationAxis.x += -mouseInput.y * mouseSensitivity.x;
            rotationAxis.y += mouseInput.x * mouseSensitivity.y;

            rotationAxis.x = Mathf.Clamp(rotationAxis.x, -75.0f, 75.0f);

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
            var orbitPosition = (transform.rotation * new Vector3(offset.x, offset.y, -offset.z)) + target.position;
            transform.position = orbitPosition;
        }
    }
}
