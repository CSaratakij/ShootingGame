using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame
{
    public class UILoadingScreenController : MonoBehaviour
    {
        [SerializeField]
        Slider sliderProgress;

        void Awake()
        {
            Subscribe();
        }

        void OnDestroy()
        {
            CleanUp();
        }

        void Subscribe()
        {
            UIController.OnShowView += OnShowView;
            SceneLoader.OnSceneLoading += OnSceneLoading;
        }

        void CleanUp()
        {
            UIController.OnShowView -= OnShowView;
            SceneLoader.OnSceneLoading -= OnSceneLoading;
        }

        void OnShowView(UIController.UIView view)
        {
            if (UIController.UIView.LoadingScreen != view) { return; }
            sliderProgress.value = 0.0f;
        }

        void OnSceneLoading(float progress)
        {
            sliderProgress.value = progress;
        }
    }
}
