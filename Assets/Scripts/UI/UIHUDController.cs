using UnityEngine;
using TMPro;

namespace MyGame
{
    [DisallowMultipleComponent]
    public class UIHUDController : MonoBehaviour
    {
        const string COLOR_CRITICAL_LEVEL = "red";
        const string COLOR_NORMAL_LEVEL = "white";

        public static UIHUDController Instance = null;

        [Header("Health")]
        [SerializeField]
        int healthCriticalLevel = 20;

        [SerializeField]
        string strHealthInfoFormat = "H | <color={0}>{1}</color>";

        [SerializeField]
        TextMeshProUGUI lblHealthInfo;

        [Header("Gun Magazine")]
        [SerializeField]
        int magazineCriticalLevel = 3;

        [SerializeField]
        CanvasGroup groupMagazine;

        [SerializeField]
        string strMagazineInfoFormat = "<color={0}>{1}</color> / {2}";

        [SerializeField]
        TextMeshProUGUI lblMagazineInfo;

        HUDInfo hudInfo;
        HUDInfo HUDInfo => hudInfo;

        void Awake()
        {
            Instance = this;
        }

        public void UpdateUI(HUDInfo info)
        {
            hudInfo = info;

            string color = (info.health <= healthCriticalLevel) ? COLOR_CRITICAL_LEVEL : COLOR_NORMAL_LEVEL;
            string strHealthInfo = string.Format(strHealthInfoFormat, color, info.health);
            lblHealthInfo.SetText(strHealthInfo);

            groupMagazine.alpha = (info.maxMagazine > 0) ? 1.0f : 0.0f;

            var currentMagazine = info.IsReloading ? 0 : info.currentMagazine;
            color = (currentMagazine <= magazineCriticalLevel) ? COLOR_CRITICAL_LEVEL : COLOR_NORMAL_LEVEL;

            string strMagazineInfo = string.Format(strMagazineInfoFormat, color, currentMagazine, info.maxMagazine);
            lblMagazineInfo.SetText(strMagazineInfo);
        }
    }
}
