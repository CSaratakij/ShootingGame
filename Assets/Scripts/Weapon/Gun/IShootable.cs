using System;
using UnityEngine;

namespace MyGame
{
    public interface IShootable
    {
        bool IsFireAble { get; }
        bool IsEmptyMagazine { get; }

        void PullTrigger(Ray ray, Action<bool, RaycastHit> callback);
        void Reload();
    }
}
