using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    public class ExternalBasis : MonoBehaviour
    {
        [SerializeField]
        Transform target; 

        [SerializeField]
        SyncAxis syncPositionMask; 

        [SerializeField]
        SyncAxis syncRotationMask; 

        Vector3 newPosition;
        Vector3 newEulerRotation;

        [Flags]
        enum SyncAxis
        {
            X  = 1,
            Y = 1 << 1,
            Z = 1 << 2
        }

        #if UNITY_EDITOR
        void Awake()
        {
            if (target == null)
            {
                Debug.LogError("Cannot sync when target object is null...");
            }
        }
        #endif

        void Update()
        {
            ApplyTransformation();
        }

        void ApplyTransformation()
        {
            SyncPosition();
            SyncRotation();
        }

        void SyncPosition()
        {
            if (syncPositionMask == 0)
                return;

            if ((syncPositionMask & SyncAxis.X) == SyncAxis.X)
                newPosition.x = target.position.x;

            if ((syncPositionMask & SyncAxis.Y) == SyncAxis.Y)
                newPosition.y = target.position.y;

            if ((syncPositionMask & SyncAxis.Z) == SyncAxis.Z)
                newPosition.z = target.position.z;
            
            transform.position = newPosition;
        }

        void SyncRotation()
        {
            if (syncRotationMask == 0)
                return;

            if ((syncRotationMask & SyncAxis.X) == SyncAxis.X)
                newEulerRotation.x = target.rotation.eulerAngles.x;

            if ((syncRotationMask & SyncAxis.Y) == SyncAxis.Y)
                newEulerRotation.y = target.rotation.eulerAngles.y;

            if ((syncRotationMask & SyncAxis.Z) == SyncAxis.Z)
                newEulerRotation.z = target.rotation.eulerAngles.z;
            
            transform.rotation = Quaternion.Euler(newEulerRotation);
        }
    }
}
