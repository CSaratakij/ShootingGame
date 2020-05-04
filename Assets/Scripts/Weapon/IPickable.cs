using UnityEngine;

namespace MyGame
{
    public interface IPickable
    {
        void Pickup(Transform parent = null);
        void Drop(Vector3 dropPosition);
    }
}
