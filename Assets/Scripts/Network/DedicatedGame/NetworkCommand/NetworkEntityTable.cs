using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Network
{
    [DisallowMultipleComponent]
    public class NetworkEntityTable : MonoBehaviour
    {
        public static NetworkEntityTable Instance = null;

        public uint OwnerID { get; set; }
        public Dictionary<uint, NetworkEntity> Item => item;

        Dictionary<uint, NetworkEntity> item;

        void Awake()
        {
            Initialize();
        }

        void Initialize()
        {
            Instance = this;
            item = new Dictionary<uint, NetworkEntity>();
        }
    }
}
