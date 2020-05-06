using System;
using UnityEngine;

namespace MyGame.Network
{
    [DisallowMultipleComponent]
    public class NetworkEntity : MonoBehaviour
    {
        [SerializeField]
        NetworkSyncType syncType;

        public uint ID => id;
        public NetworkSyncType SyncType => syncType;

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
    }
}
