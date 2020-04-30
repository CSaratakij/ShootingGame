using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MyGame
{
    public class UIController : MonoBehaviour
    {
        public static UIController Instance = null;
        public static Action<UIView> OnShowView;

        [SerializeField]
        RectTransform[] ui;

        public enum UIView
        {
            LoginMenu = 0,
            MainMenu,
            InGameMenu,
            PauseMenu,
            LoadingScreen
        }

        UIView currentView = UIView.LoginMenu;
        CanvasGroup[] canvasGroups;

        void Awake()
        {
            MakeSingleton();
            Initialize();
        }

        void MakeSingleton()
        {
            if (Instance) {
                Destroy(gameObject);
            }
            else {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        void Initialize()
        {
            canvasGroups = new CanvasGroup[ui.Length];

            for (int i = 0; i < ui.Length; ++i)
            {
                canvasGroups[i] = ui[i].gameObject.GetComponent<CanvasGroup>();
            }

            InitializeView();
        }

        void InitializeView()
        {
            HideAll();
            Show(UIView.LoginMenu);
        }

        void HideAll()
        {
            for (int i = 0; i < ui.Length; ++i)
            {
                Hide(i);
            }
        }

        public void Hide(UIView view)
        {
            Hide((int)view);
        }

        public void Hide(int id)
        {
            canvasGroups[id].alpha = 0.0f;
            canvasGroups[id].interactable = false;
            canvasGroups[id].blocksRaycasts = false;
        }

        public void Show(UIView view)
        {
            Show((int)view);
            OnShowView?.Invoke(view);
        }

        public void Show(int id)
        {
            Hide((int)currentView);
            currentView = (UIView)id;

            canvasGroups[id].alpha = 1.0f;
            canvasGroups[id].interactable = true;
            canvasGroups[id].blocksRaycasts = true;
        }

        public void ShowMainMenu()
        {
            Show(UIView.MainMenu);
        }

        public void ShowLoadingScreen()
        {
            Show(UIView.LoadingScreen);
        }

        public void ShowInGameMenu()
        {
            Show(UIView.InGameMenu);
        }
    }
}
