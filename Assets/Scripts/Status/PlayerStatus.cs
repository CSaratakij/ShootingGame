//todo

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    [DisallowMultipleComponent]
    //this is fishy
    public class PlayerStatus : MonoBehaviour
    {
        public uint ID { get; set; }

        //todo: if it is a command, how to deal with a health?
        // public Status Health { get; set; }

        //todo: controller should be a NetworkCommand
        // public PlayerController Controller { get; set; }
        //and make PlayerController implement INetworkCommand
        //then process command from there...
        //The mean to of communication value packet is a BSON (Json.net can serialize/deserialize to BSON, don't worry)

        //FlatBuffer?
        //wait, BSON packet how much it is?
    }
}
