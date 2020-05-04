using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(Rigidbody))]
    public class Gun : MonoBehaviour, IShootable, IPickable
    {
        static readonly RaycastHit EmptyHitInfo = new RaycastHit();
        static readonly Quaternion Z_90_DEGREE = Quaternion.Euler(0, 0, 90.0f);

        [Header("General")]
        [SerializeField]
        AudioClip[] audioClips;

        [Header("Setting")]
        [SerializeField]
        int damage;

        [SerializeField]
        float fireRate = 1.0f;

        [SerializeField]
        float maxDistance = 1000.0f;

        [SerializeField]
        int ammoInMagazine;

        [SerializeField]
        int maxAmmoInMagazine;

        [SerializeField]
        int totalLostAmmoPerTrigger = 1;

        [SerializeField]
        LayerMask targetLayer;

        enum GunSound
        {
            PullTrigger,
            PullTriggerWithEmptyMagazine,
            Reload
        }

        public bool IsEmptyMagazine => (ammoInMagazine <= 0);
        public bool IsFireAble => (!IsEmptyMagazine) && (lastFireTimeStamp < Time.time);

        float lastFireTimeStamp = 0.0f;

        AudioSource audioSource;
        Collider[] colliders;

        new Rigidbody rigidbody;
        RaycastHit hitInfo;

        void Awake()
        {
            Initialize();
        }

        void FixedUpdate()
        {
            ApplyGravity();
        }

        void Initialize()
        {
            audioSource = GetComponent<AudioSource>();
            rigidbody = GetComponent<Rigidbody>();
            colliders = GetComponents<Collider>();
        }

        void ApplyGravity()
        {
            if (!rigidbody.isKinematic && rigidbody.useGravity)
            {
                rigidbody.AddForce(Physics.gravity * (rigidbody.mass * rigidbody.mass));
            }
        }

        void RemoveAmmo(int total)
        {
            ammoInMagazine = (ammoInMagazine - total) < 0 ? 0 : (ammoInMagazine - total);
        }

        void PlaySound(GunSound sound)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            int i = (int) sound;
            audioSource.PlayOneShot(audioClips[i]);
        }

        void EnableCollider(bool value = true)
        {
            for (int i = 0; i < colliders.Length; ++i)
            {
                colliders[i].enabled = value;
            }
        }

        public void PullTrigger(Ray ray, Action<bool, RaycastHit> callback = null)
        {
            bool canFireSuccess = IsFireAble;

            if (canFireSuccess)
            {
                lastFireTimeStamp = Time.time + fireRate;

                RemoveAmmo(totalLostAmmoPerTrigger);
                PlaySound(GunSound.PullTrigger);

                if (Physics.Raycast(ray, out hitInfo, maxDistance, targetLayer))
                {
                    Debug.Log("Shoot at: " + hitInfo.transform.name);
                }
                else
                {
                    Debug.Log("Shoot at nothing!");
                }
            }
            else if (IsEmptyMagazine)
            {
                hitInfo = EmptyHitInfo;
                PlaySound(GunSound.PullTriggerWithEmptyMagazine);
            }

            callback?.Invoke(canFireSuccess, hitInfo);
        }

        public void Reload()
        {
            ammoInMagazine = maxAmmoInMagazine;
        }

        public void Pickup(Transform parent = null)
        {
            rigidbody.isKinematic = true;
            rigidbody.detectCollisions = false;

            EnableCollider(false);
            transform.parent = parent;

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

        }

        public void Drop(Vector3 dropPosition)
        {
            transform.parent = null;

            transform.position = dropPosition;
            transform.rotation = Z_90_DEGREE;

            EnableCollider(true);

            rigidbody.isKinematic = false;
            rigidbody.detectCollisions = true;
        }
    }
}
