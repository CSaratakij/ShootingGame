using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class Gun : MonoBehaviour, IShootable, IPickable
    {
        static readonly RaycastHit EmptyHitInfo = new RaycastHit();
        static readonly Quaternion Z_90_DEGREE = Quaternion.Euler(0, 0, 90.0f);

        [Header("General")]
        [SerializeField]
        int maxPoolAudioSources = 5;

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

        public int AmmoInMagazine => ammoInMagazine;
        public int MaxAmmoInMagazine => maxAmmoInMagazine;

        public bool IsFireAble => (!IsEmptyMagazine) && (lastFireTimeStamp < Time.time);
        public bool IsEmptyMagazine => (ammoInMagazine <= 0);
        public bool IsFullMagazine => (ammoInMagazine >= maxAmmoInMagazine);

        float lastFireTimeStamp = 0.0f;
        float delayAfterReloadedTimeStamp = 0.0f;

        AudioSource[] audioSources;
        Collider[] colliders;
        Coroutine reloadCallback;

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
            rigidbody = GetComponent<Rigidbody>();
            colliders = GetComponents<Collider>();

            audioSources = new AudioSource[maxPoolAudioSources];

            for (int i = 0; i < maxPoolAudioSources; ++i)
            {
                audioSources[i] = gameObject.AddComponent(typeof(AudioSource)) as AudioSource;
                audioSources[i].playOnAwake = false;
            }
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
            foreach (AudioSource source in audioSources)
            {
                if (source.isPlaying)
                    continue;
                
                int i = (int) sound;
                source.PlayOneShot(audioClips[i]);
                break;
            }
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
            else
            {
                if (IsEmptyMagazine)
                {
                    hitInfo = EmptyHitInfo;
                    PlaySound(GunSound.PullTriggerWithEmptyMagazine);
                }
            }

            callback?.Invoke(canFireSuccess, hitInfo);
        }

        public void PlayReloadSound()
        {
            PlaySound(GunSound.Reload);
        }

        public void Reload()
        {
            ammoInMagazine = maxAmmoInMagazine;
        }

        public void Pickup(Transform parent = null)
        {
            rigidbody.detectCollisions = false;
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.velocity = Vector3.zero;

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

            rigidbody.detectCollisions = true;
            rigidbody.isKinematic = false;
            rigidbody.useGravity = true;
        }
    }
}
