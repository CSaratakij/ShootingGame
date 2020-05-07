using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyGame.Network;

namespace MyGame
{
    public class Spawner : MonoBehaviour
    {
        [SerializeField]
        GameObject prefab;

        [SerializeField]
        NetworkType networkType;

        enum NetworkType
        {
            Offline,
            Online
        }

        void Awake()
        {
            if (NetworkType.Offline == networkType)
            {
                Spawn(prefab, transform.position);
            }
        }

        public GameObject Spawn()
        {
            return Spawn(prefab, transform.position);
        }

        public GameObject Spawn(GameObject prefab, Vector3 spawnPoint)
        {
            var obj = Instantiate(prefab, spawnPoint, Quaternion.identity) as GameObject;

            if (NetworkType.Online == networkType)
            {
                var entity = obj.AddComponent(typeof(NetworkEntity));
            }

            return obj;
        }
    }
}
