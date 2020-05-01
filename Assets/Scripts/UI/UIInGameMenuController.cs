using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    public class UIInGameMenuController : MonoBehaviour
    {
        [SerializeField]
        CanvasGroup panelPlayerStat;

        void Awake()
        {
            HidePlayerStat();
        }

        void Update()
        {
            InputHandler();
        }

        void InputHandler()
        {
            bool isShowPlayerStatShowing = (panelPlayerStat.alpha > 0.1f);

            if (isShowPlayerStatShowing && Input.GetKeyUp(KeyCode.Tab))
            {
                ShowPlayerStat(false);
            }
            else if (!isShowPlayerStatShowing && Input.GetKeyDown(KeyCode.Tab))
            {
                ShowPlayerStat(true);
            }
        }

        void HidePlayerStat()
        {
            ShowPlayerStat(false);
        }

        void ShowPlayerStat(bool value)
        {
            panelPlayerStat.alpha = value ? 1.0f : 0.0f;
            panelPlayerStat.interactable = value;
            panelPlayerStat.blocksRaycasts = value;
        }
    }
}