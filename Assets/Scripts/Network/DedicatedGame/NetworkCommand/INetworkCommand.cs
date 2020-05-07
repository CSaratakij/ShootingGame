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
        PlayerConnected = 1,
        SyncPosition = 2,
        SyncRotation = 3,
        SyncPlayerStatus = 4,
        SendMessage = 5,
        PlayerDisconnected = 6
    }

    public interface INetworkCommand
    {
        void Send(byte channelID, byte[] data, Action<bool> callback = null);
        void OnNetworkCommand(Event e, NetworkCommand command, BinaryReader reader);
    }
}
