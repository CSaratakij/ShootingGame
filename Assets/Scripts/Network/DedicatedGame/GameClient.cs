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
                    peer.DisconnectNow(0);
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
            peer = client.Connect(address);
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
                    //todo: try send message
                    SendTestMessage();
                    break;

                case EventType.Disconnect:
                    Debug.Log("Client disconnected from server");
                    break;

                case EventType.Timeout:
                    Debug.Log("Client connection timeout");
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

        public void OnNetworkCommand(Event e, NetworkCommand command, BinaryReader reader)
        {
            Debug.Log("Get Command : " + command.ToString());

            switch (command)
            {
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
