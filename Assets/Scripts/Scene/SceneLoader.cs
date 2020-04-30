using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGame
{
    public class SceneLoader : MonoBehaviour
    {
        public static Action<float> OnSceneLoading;
        static SceneLoader Instance = null;
        static readonly WaitForSeconds CompleteWait = new WaitForSeconds(0.5f);

        bool canLoadScene = true;

        void Awake()
        {
            MakeSingleton();
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

        public static void LoadScene(SceneIndex target, Action callback)
        {
            Instance?._LoadScene(target, callback);
        }

        void _LoadScene(SceneIndex target, Action callback)
        {
            if (!Instance.canLoadScene) { return; }
            StartCoroutine(LoadSceneCallback(target, callback));
        }

        IEnumerator LoadSceneCallback(SceneIndex target, Action callback)
        {
            var asyncLoad = SceneManager.LoadSceneAsync((int)target);
            asyncLoad.allowSceneActivation = false;

            while (!asyncLoad.isDone)
            {
                OnSceneLoading?.Invoke(asyncLoad.progress);

                if (asyncLoad.progress >= 0.9f)
                {
                    asyncLoad.allowSceneActivation = true;
                }

                yield return null;
            }

            yield return CompleteWait;
            OnSceneLoading?.Invoke(1.0f);

            yield return CompleteWait;
            canLoadScene = true;

            callback?.Invoke();
        }
    }
}
