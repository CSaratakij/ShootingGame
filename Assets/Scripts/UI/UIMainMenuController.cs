using System;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MyGame.Network;

namespace MyGame
{
    [RequireComponent(typeof(RestClient))]
    public class UIMainMenuController : MonoBehaviour
    {
        [Header("General")]
        [SerializeField]
        UIController uIController;

        [Header("MainMenu")]
        [SerializeField]
        TextMeshProUGUI lblDisplayName;

        [SerializeField]
        TextMeshProUGUI lblLevel;

        [SerializeField]
        Slider sliderExp;

        [SerializeField]
        CanvasGroup[] ui;

        [SerializeField]
        EventSystem uiEvent;

        [SerializeField]
        Button btnFindLobby;

        [SerializeField]
        Button btnRangePractice;

        [SerializeField]
        Button btnToggleChat;

        [SerializeField]
        Button btnProfileStat;

        [SerializeField]
        Button btnQuit;

        [Header("LobbyList")]
        [SerializeField]
        GameObject prefabLobbyListInfo;

        [SerializeField]
        TextMeshProUGUI lblFetchStatus;

        [SerializeField]
        TextMeshProUGUI lblTotalLobbyFound;

        [SerializeField]
        Transform contents;

        [SerializeField]
        TMP_InputField inputAddress;

        [SerializeField]
        TMP_InputField inputPort;

        [SerializeField]
        Button btnJoin;

        [SerializeField]
        Button btnRefreshLobbyList;

        [Header("ProfileStat")]
        [SerializeField]
        TextMeshProUGUI lblProfileStat;

        enum View
        {
            LobbyList = 0,
            Chat,
            LobbyFetchStatus,
            ProfileStat
        }

        bool canFindLobby = true;
        bool isInit = false;
        bool isCacheMiss = true;

        RestClient restClient;

        void Awake()
        {
            Initialize();
            Subscribe();
        }

        void OnDestroy()
        {
            CleanUp();
        }

        void Initialize()
        {
            restClient = GetComponent<RestClient>();
        }

        void Subscribe()
        {
            UIController.OnShowView += OnShowView;

            btnFindLobby.onClick.AddListener(() => {
                ShowLobbyList();

                if (isInit) { return; }
                if (!canFindLobby) { return; }

                canFindLobby = false;
                ShowLobbyFetchStatus("Pending...");

                FetchLobby((res) => {
                    if (res.isNetworkError || res.isHttpError)
                    {
                        ShowLobbyFetchStatus("Network error...");
                    }
                    else 
                    {
                        StartCoroutine(UpdateLobbyList_Callback(res.downloadHandler.text, 1.0f));
                        isInit = true;
                    }

                    canFindLobby = true;
                });
            });

            btnRangePractice.onClick.AddListener(() => {
                uIController.ShowLoadingScreen();
                SceneLoader.LoadScene(SceneIndex.RangePractice, () => {
                    uIController.ShowInGameMenu();
                    GameClient.Instance?.ConnectToGameServer();
                });
            });

            btnRefreshLobbyList.onClick.AddListener(() => {
                if (!canFindLobby) { return; }

                canFindLobby = false;
                ShowLobbyFetchStatus("Pending...");

                FetchLobby((res) => {
                    if (res.isNetworkError || res.isHttpError)
                    {
                        ShowLobbyFetchStatus("Network error...");
                    }
                    else 
                    {
                        StartCoroutine(UpdateLobbyList_Callback(res.downloadHandler.text, 1.0f));
                    }

                    canFindLobby = true;
                });
            });

            btnToggleChat.onClick.AddListener(() => {
                ToggleChat();
            });

            btnProfileStat.onClick.AddListener(() => {
                ToggleProfileStat();
            });

            btnQuit.onClick.AddListener(() => {
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
            });
        }

        void CleanUp()
        {
            UIController.OnShowView -= OnShowView;
        }

        void OnShowView(UIController.UIView view)
        {
            if (view != UIController.UIView.MainMenu) { return; }
            UpdateView();
        }

        void UpdateView()
        {
            if (!isCacheMiss) { return; }
            lblDisplayName.SetText(Global.DisplayName);

            FetchUserInfo((res) => {

                if (res.isNetworkError)
                {
                    //alert network error here...
                    return;
                }

                if (res.responseCode == 404)
                {
                    lblLevel.SetText("0");
                    sliderExp.value = 0;
                }
                else if (res.responseCode == 200)
                {
                    var resultArray = JArray.Parse(res.downloadHandler.text);
                    var result = resultArray[0];

                    Global.Level = (uint) result["level"];
                    Global.TotalPlay = (uint) result["totalPlay"];
                    Global.TotalKill = (uint) result["totalKill"];
                    Global.MaxKil = (uint) result["maxKill"];

                    lblLevel.SetText(Global.Level.ToString());
                    Global.TotalExp = (uint) result["exp"];

                    sliderExp.value = (Global.TotalExp % 200) / 200.0f;

                    UpdateProfileStatView();
                    isCacheMiss = false;
                }
                else
                {
                    //alert network error here...
                    return;
                }
            });
        }

        void UpdateProfileStatView()
        {
            var info = $"Level: {Global.Level}\n" +
                       $"Kill: {Global.TotalKill}\n" +
                       $"Max Kill: {Global.MaxKil}\n" +
                       $"Exp: {Global.TotalExp}\n";

            lblProfileStat.SetText(info);
        }

        void FetchUserInfo(Action<UnityWebRequest> callback)
        {
            var url = $"http://localhost:6000/users/info?client_id={Global.CLIENT_ID}&user_id={Global.UserID}";

            restClient.Get(url, (res) =>
            {
                callback(res);
            });
        }

        void FetchLobby(Action<UnityWebRequest> callback)
        {
            restClient.Get("http://localhost:8080/lobby", (res) =>
            {
                callback(res);
            });
        }

        void UpdateLobbyList(string textJson, Action<uint> callback)
        {
            for (int i = 0; i < contents.childCount; ++i)
            {
                Destroy(contents.GetChild(i).gameObject);
            }

            var result = JObject.Parse(textJson);
            var totalLobby = (uint) result["total"];

            var postFix = totalLobby > 1 ? "founds" : "found";
            var totalLobbyText = $"{totalLobby} {postFix}";

            lblTotalLobbyFound.SetText(totalLobbyText);

            var lobby = result["lobby"].Children<JProperty>();

            foreach (var i in lobby)
            {
                var item = (JObject) i.Value;

                var objInfo = Instantiate(prefabLobbyListInfo); 
                var objInfoText = objInfo.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                var objInfoButton = objInfo.GetComponent<Button>();

                var text = $"{item["title"]} - {item["ip"]}:{item["port"]} ({item["player"]}/{item["maxPlayer"]})";
                objInfoText.SetText(text);

                objInfoButton.onClick.AddListener(() => {
                    OnLobbyInfoClicked((string) item["ip"], (string) item["port"]);
                });

                objInfo.transform.SetParent(contents.transform, false); 
            }

            callback?.Invoke(totalLobby);
        }

        IEnumerator UpdateLobbyList_Callback(string textJson, float wait)
        {
            yield return new WaitForSeconds(wait);
            UpdateLobbyList(textJson, (total) => {
                if (total > 0)
                {
                    HideLobbyFetchStatus();
                }
                else
                {
                    ShowLobbyFetchStatus("No lobby found");
                }
            });
        }

        void OnLobbyInfoClicked(string address, string port)
        {
            inputAddress.text = address;
            inputPort.text = port;
        }

        public void ShowLobbyList()
        {
            int indice = (int)View.LobbyList;

            ui[indice].alpha = 1.0f;
            ui[indice].blocksRaycasts = true;
            ui[indice].interactable = true;
        }

        public void ShowLobbyFetchStatus(string status)
        {
            lblFetchStatus.SetText(status);
            int indice = (int)View.LobbyFetchStatus;

            ui[indice].alpha = 1.0f;
            ui[indice].blocksRaycasts = true;
            ui[indice].interactable = true;
        }

        public void ShowProfileStat()
        {
            int indice = (int)View.ProfileStat;

            ui[indice].alpha = 1.0f;
            ui[indice].blocksRaycasts = true;
            ui[indice].interactable = true;
        }

        public void HideProfileStat()
        {
            int indice = (int)View.ProfileStat;

            ui[indice].alpha = 0.0f;
            ui[indice].blocksRaycasts = false;
            ui[indice].interactable = false;
        }

        public void HideLobbyFetchStatus()
        {
            int indice = (int)View.LobbyFetchStatus;

            ui[indice].alpha = 0.0f;
            ui[indice].blocksRaycasts = false;
            ui[indice].interactable = false;
        }

        public void HideLobbyList()
        {
            int indice = (int)View.LobbyList;

            ui[indice].alpha = 0.0f;
            ui[indice].blocksRaycasts = false;
            ui[indice].interactable = false;
        }

        public void ToggleChat()
        {
            int indice = (int)View.Chat;
            bool isShow = ui[indice].alpha > 0.1f;

            isShow = !isShow;

            ui[indice].alpha = isShow ? 1.0f : 0.0f;
            ui[indice].blocksRaycasts = isShow;
            ui[indice].interactable = isShow;
        }

        public void ToggleProfileStat()
        {
            int indice = (int)View.ProfileStat;
            bool isShow = ui[indice].alpha > 0.1f;

            isShow = !isShow;

            if (isShow)
            {
                ShowProfileStat();
            }
            else
            {
                HideProfileStat();
            }
        }

        public void SetDirty(bool value = true)
        {
            isCacheMiss = value;
        }
    }
}
