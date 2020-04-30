using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ENet;
using Event = ENet.Event;
using EventType = ENet.EventType;

namespace MyGame.Network
{
    [DisallowMultipleComponent]
    public sealed class GameClient : MonoBehaviour
    {
        [SerializeField]
        string ip = "localhost";

        [SerializeField]
        ushort port = 27015;

        Address address;
        Host client;
        Peer peer;
        Event netEvent;

        void OnGUI()
        {
            GUILayout.Label("Client");
            GUILayout.Label($"state : {peer.State.ToString()}");

            if (PeerState.Connected == peer.State)
            {
                if (GUILayout.Button("Disconnect"))
                {
                    peer.DisconnectNow(0);
                }
            }

            if (PeerState.Disconnected == peer.State)
            {
                if (GUILayout.Button("Connect"))
                {
                    peer = client.Connect(address);
                }
            }
        }

        void Awake()
        {
            Initialize();
        }

        void Update()
        {
            ConnectionHandler();
        }

        // void LateUpdate()
        // {
        //     if (Time.frameCount % 3 == 0)
        //         Debug.Log(peer.State.ToString());
        // }

        void OnDestroy()
        {
            CleanUp();
        }

        void Initialize()
        {
            Library.Initialize();

            client = new Host();
            address = new Address();

            address.SetHost(ip);
            address.Port = port;

            client.Create();
            peer = client.Connect(address);
        }

        void ConnectionHandler()
        {
            if (client.CheckEvents(out netEvent) <= 0)
            {
                if (client.Service(0, out netEvent) <= 0)
                    return;
            }

            switch (netEvent.Type)
            {
                case EventType.None:
                    break;

                case EventType.Connect:
                    Debug.Log("Client connected to server");
                    break;

                case EventType.Disconnect:
                    Debug.Log("Client disconnected from server");
                    break;

                case EventType.Timeout:
                    Debug.Log("Client connection timeout");
                    break;

                case EventType.Receive:
                    Debug.Log("Packet received from server - Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                    netEvent.Packet.Dispose();
                    break;
            }
        }

        void CleanUp()
        {
            client.Flush();
            Library.Deinitialize();
        }
    }
}
