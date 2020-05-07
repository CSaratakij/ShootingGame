//todo: make player spawner add this component if want to connect
using System;
using UnityEngine;

namespace MyGame.Network
{
    [DisallowMultipleComponent]
    public class NetworkEntity : MonoBehaviour
    {
        [SerializeField]
        bool isEnable = true;

        [SerializeField]
        bool isRemoteSync = true;

        [SerializeField]
        float syncTheshold = 0.01f;

        [SerializeField]
        NetworkSyncType syncType;

        public uint ID => id;
        public NetworkSyncType SyncType => syncType;

        public bool ShouldSyncPosition => (positionDelta > syncTheshold);
        public bool ShouldSyncRotation => (rotationDelta > syncTheshold);

        Vector3 previousPosition;
        Vector3 lastPosition;

        Quaternion previousRotation;
        Quaternion lastRotation;

        Vector3 targetPosition;
        Quaternion targetRotation;

        float positionDelta;
        float rotationDelta;

        uint id;

        [Flags]
        public enum NetworkSyncType
        {
            None = 0,
            Position = 1 << 1,
            Rotation = 1 << 2
        }

        public void SetID(uint id)
        {
            this.id = id;
        }

        void Start()
        {
            Initialize();
        }

        void Initialize()
        {
            targetPosition = transform.position;
            targetRotation = transform.rotation;

            previousPosition = targetPosition;
            lastPosition = targetPosition;

            ComputeDelta();
        }

        void Update()
        {
            UpdateCache();
        }

        void LateUpdate()
        {
            ComputeDelta();
            Interpolation();
        }

        void UpdateCache()
        {
            previousPosition = lastPosition;
            lastPosition = transform.position;

            previousRotation = lastRotation;
            lastRotation = transform.rotation;
        }

        void ComputeDelta()
        {
            if (isRemoteSync)
            {
                //receiver
                positionDelta = (targetPosition - transform.position).sqrMagnitude;
                rotationDelta = Quaternion.Angle(targetRotation, transform.rotation);
            }
            else
            {
                //sender
                positionDelta = (lastPosition - previousPosition).sqrMagnitude;
                rotationDelta = Quaternion.Angle(previousRotation, lastRotation);
            }
        }

        void Interpolation()
        {
            if (!isEnable)
                return;

            var table = NetworkEntityTable.Instance;

            if (!table)
                return;

            if (table.OwnerID == id)
                return;

            if (isRemoteSync)
            {
                // InterpolatePosition();
                // InterpolateRotation();
            }
        }

        void InterpolatePosition()
        {
            int currentFlag = (int) syncType;
            int flag = (int) NetworkSyncType.Position;

            bool shouldInterpolatePosition = ((currentFlag & flag) == flag);

            if (!shouldInterpolatePosition)
                return;

            var newPosition = targetPosition;
            newPosition.y = transform.position.y;

            transform.position = Vector3.Lerp(transform.position, newPosition, 0.02f);
        }

        void InterpolateRotation()
        {
            int currentFlag = (int) syncType;
            int flag = (int) NetworkSyncType.Rotation;

            bool shouldInterpolateRotation = ((currentFlag & flag) == flag);

            if (!shouldInterpolateRotation)
                return;

            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 0.02f);
        }

        public void SyncPosition(Vector3 position)
        {
            targetPosition = position;
        }

        public void SyncRotation(Quaternion rotation)
        {
            targetRotation = rotation;
        }

        public void EnableSync(bool value = true)
        {
            isEnable = value;
        }

        public void SetSyncType(NetworkSyncType syncType)
        {
            this.syncType = syncType;
        }

        public void SetRemoteSync(bool value)
        {
            this.isRemoteSync = value;
        }
    }
}
