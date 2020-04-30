using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ENet;
using Event = ENet.Event;
using EventType = ENet.EventType;

namespace MyGame.Network
{
    [DisallowMultipleComponent]
    public sealed class GameServer : MonoBehaviour
    {
        const int MAX_CHANNEL = 3;
        const int DEFAULT_CHANNEL = 0;
        const int RELIABLE_GAMESTATE_CHANNEL = 1;
        const int RELIABLE_CHAT_CHANNEL = 2;

        public static GameServer Instance = null;

        [SerializeField]
        string ip = "0.0.0.0";

        [SerializeField]
        ushort port = 27015;

        [SerializeField]
        int maxClient = 16;

        Address address;
        Host server;
        Event netEvent;

        void OnGUI()
        {
            GUILayout.Label("Server");
            GUILayout.Label($"Total peer : {server.PeersCount}");
        }

        void Awake()
        {
            MakeSingleton();
            Initialize();
        }

        void Update()
        {
            ConnectionHandler();
        }

        // void LateUpdate()
        // {
        //     if (Time.frameCount % 3 == 0)
        //         Debug.Log("P: " + server.PeersCount);
        // }

        void OnDestroy()
        {
            CleanUp();
        }

        void MakeSingleton()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
        }

        void Initialize()
        {
            Application.targetFrameRate = 30;
            QualitySettings.vSyncCount = 1;

            Library.Initialize();

            server = new Host();
            address = new Address();

            address.Port = port;

            server.Create(address, maxClient, MAX_CHANNEL);
            server.PreventConnections(false);

            Debug.Log($"Game server start on : {port}");
        }

        void ConnectionHandler()
        {
            if (server.CheckEvents(out netEvent) <= 0)
            {
                if (server.Service(0, out netEvent) <= 0)
                    return;
            }

            switch (netEvent.Type)
            {
                case EventType.None:
                    break;

                case EventType.Connect:
                    Debug.Log("Client connected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                    break;

                case EventType.Disconnect:
                    Debug.Log("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                    break;

                case EventType.Timeout:
                    Debug.Log("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                    break;

                case EventType.Receive:
                    Debug.Log("Packet received from - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                    netEvent.Packet.Dispose();
                    break;
            }
        }

        void CleanUp()
        {
            server.Flush();
            Library.Deinitialize();
        }
    }
}
