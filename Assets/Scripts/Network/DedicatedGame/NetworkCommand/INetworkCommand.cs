using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ENet;
using Event = ENet.Event;
using EventType = ENet.EventType;

namespace MyGame.Network
{
    public enum NetworkCommand : byte
    {
        SyncPosition = 1,
        SyncRotation = 2,
        SendMessage = 3,
        SpawnResponse = 4
    }

    public interface INetworkCommand
    {
        void Send(byte channelID, byte[] data, Action<bool> callback = null);
        void OnNetworkCommand(Event e, NetworkCommand command, BinaryReader reader);
    }
}
