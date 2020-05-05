using UnityEngine;
using TMPro;

namespace MyGame
{
    [DisallowMultipleComponent]
    public class UIHUDController : MonoBehaviour
    {
        public static UIHUDController Instance = null;

        [Header("Health")]
        [SerializeField]
        string strHealthInfoFormat = "H | {0}";

        [SerializeField]
        TextMeshProUGUI lblHealthInfo;

        [Header("Gun Magazine")]
        [SerializeField]
        string strMagazineInfoFormat = "{0} / {1}";

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

            string strHealthInfo = string.Format(strHealthInfoFormat, info.health);
            lblHealthInfo.SetText(strHealthInfo);

            string strMagazineInfo = string.Format(strMagazineInfoFormat, info.currentMagazine, info.maxMagazine);
            lblMagazineInfo.SetText(strMagazineInfo);
        }
    }
}
