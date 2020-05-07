using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Network
{
    //todo: reserve ID (0 - maxPlayer) for player entity only
    //other object that need to sync -> use futher number
    public class NetworkEntityTable : MonoBehaviour
    {
        static readonly WaitForSeconds SendInfoRate = new WaitForSeconds(0.03f);
        public static NetworkEntityTable Instance = null;

        [Header("General")]
        [SerializeField]
        Spawner playerSpawner;

        [SerializeField]
        Role role;

        enum Role
        {
            Server,
            Client
        }

        public uint OwnerID { get; set; }
        public bool IsOwnerConnected { get; set; }

        public Dictionary<uint, NetworkEntity> Item => item;
        Dictionary<uint, NetworkEntity> item;

        Coroutine sender;

        void Awake()
        {
            Initialize();
        }

        void Start()
        {
            if (Role.Client == role)
            {
                sender = StartCoroutine(SendOwnerEntityInfo());
            }
        }

        void Initialize()
        {
            Instance = this;
            item = new Dictionary<uint, NetworkEntity>();
        }

        IEnumerator SendOwnerEntityInfo()
        {
            while (true)
            {
                if (IsOwnerConnected)
                {
                    NetworkEntity entity;
                    bool shouldSendUpdate = item.TryGetValue(OwnerID, out entity);

                    if (shouldSendUpdate && entity != null)
                    {
                        if (entity.ShouldSyncPosition)
                        {
                            GameClient.Instance?.SendPositionUpdate();
                        }

                        if (entity.ShouldSyncRotation)
                        {
                            GameClient.Instance?.SendRotationUpdate();
                        }
                    }
                }

                yield return SendInfoRate;
            }
        }

        public void AddEntity(uint id)
        {
            if (item.ContainsKey(id))
                return;

            var obj = playerSpawner.Spawn();
            var entity = obj.GetComponent<NetworkEntity>();
            var playerController = obj.GetComponent<PlayerController>();

            if (!entity)
                return;
            
            if (playerController)
            {
                bool controlable = (Role.Client == role) && (OwnerID == id);
                bool shouldRemoteSync = !controlable;

                playerController.EnableControl(controlable);

                entity.SetRemoteSync(shouldRemoteSync);
                entity.SetSyncType(NetworkEntity.NetworkSyncType.Position | NetworkEntity.NetworkSyncType.Rotation);
                entity.SetID(id);
            }
            
            item.Add(id, entity);
        }

        public void RemoveEntity(uint id)
        {
            if (!item.ContainsKey(id))
                return;

            NetworkEntity entity;

            if (item.TryGetValue(id, out entity))
            {
                var obj = entity.gameObject;

                if (obj)
                {
                    var playerController = obj.GetComponent<PlayerController>();

                    if (playerController)
                    {
                        playerController.DestroyCompletely();
                    }
                    else
                    {
                        Destroy(obj);
                    }
                }
            }

            item.Remove(id);
        }

        public void ResetTable()
        {
            foreach (var pair in item)
            {
                var obj = pair.Value.gameObject;

                if (obj)
                {
                    var playerController = obj.GetComponent<PlayerController>();

                    if (playerController)
                    {
                        playerController.DestroyCompletely();
                    }
                    else
                    {
                        Destroy(obj);
                    }
                }
            }

            item.Clear();
        }
    }
}
