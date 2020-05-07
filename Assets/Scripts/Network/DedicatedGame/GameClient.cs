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
    public sealed class GameClient : MonoBehaviour, INetworkCommand
    {
        public static GameClient Instance = null;

        [SerializeField]
        string ip = "localhost";

        [SerializeField]
        ushort port = 27015;

        Address address;
        Host client;
        Peer peer;
        Event netEvent;

        byte[] buffer;
        MemoryStream stream;

        BinaryWriter writer;
        BinaryReader reader;

        void OnGUI()
        {
            GUILayout.Label("Client");
            GUILayout.Label($"state : {peer.State.ToString()}");

            if (PeerState.Connected == peer.State)
            {
                if (GUILayout.Button("Disconnect"))
                {
                    peer.Disconnect(0);
                }

                if (GUILayout.Button("SE"))
                {
                    SendTestMessage();
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
            MakeSingleton();
            Initialize();
        }

        void Update()
        {
            ConnectionHandler();
        }

        void OnDestroy()
        {
            CleanUp();
        }

        void MakeSingleton()
        {
            if (Instance)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        void Initialize()
        {
            Library.Initialize();

            buffer = new byte[1024];
            stream = new MemoryStream(buffer);
            writer = new BinaryWriter(stream);

            client = new Host();
            address = new Address();

            address.SetHost(ip);
            address.Port = port;

            client.Create();

            //Test
            // peer = client.Connect(address);
        }

        void InitWriter(int size)
        {
            // const int bufSize = sizeof(byte) + sizeof(int) + sizeof(float) + sizeof(float);
            // InitWriter(bufSize);

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
                    HandleClientConnected(ref netEvent);
                    break;

                case EventType.Disconnect:
                    Debug.Log("Client disconnected from server");
                    HandleClientDisconnected();
                    break;

                case EventType.Timeout:
                    Debug.Log("Client connection timeout");
                    HandleClientTimeout();
                    break;

                case EventType.Receive:
                    Debug.Log("Packet received from server - Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                    HandlePacket(ref netEvent);
                    netEvent.Packet.Dispose();
                    break;
            }
        }

        void CleanUp()
        {
            client.Flush();
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

        void HandleClientConnected(ref Event e)
        {
            SetOwnerID(e.Peer.ID);
        }

        void HandleClientDisconnected()
        {
            SetOwnerID(peer.ID, false);
        }

        void HandleClientTimeout()
        {
            SetOwnerID(peer.ID, false);
        }

        void SetOwnerID(uint id, bool isConnected = true)
        {
            if (NetworkEntityTable.Instance)
            {
                NetworkEntityTable.Instance.OwnerID = id;
                NetworkEntityTable.Instance.IsOwnerConnected = isConnected;

                if (isConnected)
                {
                    NetworkEntityTable.Instance.AddEntity(id);
                }
                else
                {
                    NetworkEntityTable.Instance.ResetTable();
                }
            }
        }

        void SendTestMessage()
        {
            var message = "Hello World";
            var bufSize = sizeof(byte) + Encoding.Default.GetByteCount(message) + 1;

            var buffer = new byte[bufSize];

            var stream = new MemoryStream(buffer);
            var writer = new BinaryWriter(stream);

            byte commandID = (byte) NetworkCommand.SendMessage;

            writer.Write(commandID);
            writer.Write(message);

            Send(0, buffer, (error) => {
                if (error)
                {
                    Debug.Log("Not connected...");
                }
            });
        }

        public void SendPositionUpdate()
        {
            var table = NetworkEntityTable.Instance;

            if (table == null)
                return;

            var id = NetworkEntityTable.Instance.OwnerID;
            NetworkEntity entity;

            if (table.Item.TryGetValue(id, out entity))
            {
                Vector3 position = entity.transform.position;

                var bufSize = sizeof(byte) + sizeof(uint) + sizeof(float) + sizeof(float) + sizeof(float) + 1;
                var buffer = new byte[bufSize];

                var stream = new MemoryStream(buffer);
                var writer = new BinaryWriter(stream);

                byte commandID = (byte) NetworkCommand.SyncPosition;

                writer.Write(commandID);
                writer.Write(id);
                writer.Write(position.x);
                writer.Write(position.y);
                writer.Write(position.z);

                Send(0, buffer, (error) => {
                    if (error)
                    {
                        Debug.Log("Not connected...");
                    }
                });
            }
            else
            {
                Debug.Log("Owner id not in the entity table..");
            }
        }

        public void SendRotationUpdate()
        {
            //todo:
        }

        public void Send(byte channelID, byte[] data, Action<bool> callback = null)
        {
            if (PeerState.Connected != peer.State)
            {
                callback?.Invoke(true);
                return;
            }

            var packet = default(Packet);
            packet.Create(data);

            peer.Send(channelID, ref packet);
            callback?.Invoke(false);
        }

        public void ConnectToGameServer()
        {
            peer = client.Connect(address);
        }

        public void ConnectToGameServer(string ip, ushort port)
        {
            address.SetHost(ip);
            address.Port = port;
            peer = client.Connect(address);
        }

        public void OnNetworkCommand(Event e, NetworkCommand command, BinaryReader reader)
        {
            Debug.Log("Get Command : " + command.ToString());

            switch (command)
            {
                case NetworkCommand.PlayerConnected:
                {
                    var id = reader.ReadUInt32();

                    if (id == NetworkEntityTable.Instance.OwnerID)
                        return;

                    var table = NetworkEntityTable.Instance;

                    if (table == null)
                        return;
                    
                    table.AddEntity(id);
                }
                break;

                case NetworkCommand.PlayerDisconnected:
                {
                    var id = reader.ReadUInt32();
                    var table = NetworkEntityTable.Instance;

                    if (table == null)
                        return;
                    
                    table.RemoveEntity(id);
                }
                break;

                case NetworkCommand.SyncPosition:
                {
                    var id = reader.ReadUInt32();

                    if (id == NetworkEntityTable.Instance.OwnerID)
                        return;

                    var table = NetworkEntityTable.Instance;

                    if (table == null)
                        return;

                    Vector3 position = Vector3.zero;

                    position.x = reader.ReadSingle();
                    position.y = reader.ReadSingle();
                    position.z = reader.ReadSingle();

                    NetworkEntity entity;

                    if (table.Item.TryGetValue(id, out entity))
                    {
                        entity.SyncPosition(position);
                    }
                    else
                    {
                        Console.WriteLine("Could not find the specified key.");
                    }
                }
                break;

                case NetworkCommand.SendMessage:
                {
                    var sender = reader.ReadUInt32();
                    var message = reader.ReadString();

                    Debug.Log($"Receive message from {sender} : {message}");
                }
                break;

                default:
                break;
            }
        }
    }
}
