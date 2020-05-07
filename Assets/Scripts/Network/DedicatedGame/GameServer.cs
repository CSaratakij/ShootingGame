using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ENet;
using Event = ENet.Event;
using EventType = ENet.EventType;

namespace MyGame.Network
{
    [DisallowMultipleComponent]
    public sealed class GameServer : MonoBehaviour, INetworkCommand
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

        byte[] buffer;
        MemoryStream stream;

        BinaryWriter writer;
        BinaryReader reader;

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
            //todo: parse command line argument of -port here..

            Application.targetFrameRate = 30;
            QualitySettings.vSyncCount = 1;

            Library.Initialize();

            server = new Host();
            address = new Address();

            address.Port = port;

            server.Create(address, maxClient, MAX_CHANNEL);
            server.PreventConnections(false);

            buffer = new byte[1024];
            stream = new MemoryStream(buffer);
            writer = new BinaryWriter(stream);

            Debug.Log($"Game server start on : {port}");
        }

        void InitWriter(int size)
        {
            buffer = new byte[size];
            stream = new MemoryStream(buffer);
            writer = new BinaryWriter(stream);
        }

        void InitReader(byte[] buffer)
        {
            stream = new MemoryStream(buffer);
            reader = new BinaryReader(stream);
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
                    BroadcastPlayerConnect(ref netEvent);
                    //todo: broadcast other player here?
                    break;

                case EventType.Disconnect:
                    Debug.Log("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                    BroadcastPlayerDisconnect(ref netEvent);
                    break;

                case EventType.Timeout:
                    Debug.Log("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                    BroadcastPlayerDisconnect(ref netEvent);
                    break;

                case EventType.Receive:
                    Debug.Log("Packet received from - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                    HandlePacket(ref netEvent);
                    netEvent.Packet.Dispose();
                    break;
            }
        }

        void CleanUp()
        {
            server.Flush();
            Library.Deinitialize();
        }

        void HandlePacket(ref Event e)
        {
            var readBuffer = new byte[e.Packet.Length];
            var readStream = new MemoryStream(readBuffer);
            var reader = new BinaryReader(readStream);

            readStream.Position = 0;
            netEvent.Packet.CopyTo(readBuffer);

            byte commandID = reader.ReadByte();
            var command = (NetworkCommand) commandID;

            OnNetworkCommand(e, command, reader);
        }

        void BroadcastPlayerConnect(ref Event e)
        {
            uint id = e.Peer.ID;
            NetworkEntityTable.Instance?.AddEntity(id);

            var bufSize = sizeof(byte) + sizeof(uint);
            var buffer = new byte[bufSize];

            var stream = new MemoryStream(buffer);
            var writer = new BinaryWriter(stream);

            byte commandID = (byte) NetworkCommand.PlayerConnected;

            writer.Write(commandID);
            writer.Write(id);

            Send(0, buffer);
        }

        void BroadcastPlayerDisconnect(ref Event e)
        {
            uint id = e.Peer.ID;
            NetworkEntityTable.Instance?.RemoveEntity(id);

            var bufSize = sizeof(byte) + sizeof(uint);
            var buffer = new byte[bufSize];

            var stream = new MemoryStream(buffer);
            var writer = new BinaryWriter(stream);

            byte commandID = (byte) NetworkCommand.PlayerDisconnected;

            writer.Write(commandID);
            writer.Write(id);

            Send(0, buffer);
        }

        public void Send(byte channelID, byte[] data, Action<bool> callback = null)
        {
            var packet = default(Packet);
            packet.Create(data);
            server.Broadcast(channelID, ref packet);
        }

        public void OnNetworkCommand(Event e, NetworkCommand command, BinaryReader reader)
        {
            switch (command)
            {
                case NetworkCommand.SyncPosition:
                {
                    var sender = e.Peer.ID;
                    Vector3 position = Vector3.zero;

                    position.x = reader.ReadSingle();
                    position.y = reader.ReadSingle();
                    position.z = reader.ReadSingle();

                    var bufSize = sizeof(byte) + sizeof(uint) + sizeof(float) + sizeof(float) + sizeof(float) + 1;
                    InitWriter(bufSize);

                    writer.Write((byte) command);
                    writer.Write(sender);
                    writer.Write(position.x);
                    writer.Write(position.y);
                    writer.Write(position.z);

                    Debug.Log("Receive : " + position);

                    var packet = default(Packet);
                    packet.Create(buffer);

                    server.Broadcast(0, ref packet);
                }
                break;

                case NetworkCommand.SendMessage:
                {
                    var sender = e.Peer.ID;
                    var message = reader.ReadString();

                    var bufSize = sizeof(byte) + sizeof(uint) + Encoding.Default.GetByteCount(message) + 1;
                    InitWriter(bufSize);

                    writer.Write((byte) command);
                    writer.Write(sender);
                    writer.Write(message);

                    var packet = default(Packet);
                    packet.Create(buffer);

                    server.Broadcast(0, ref packet);
                }
                break;

                default:
                break;
            }
        }
    }
}
