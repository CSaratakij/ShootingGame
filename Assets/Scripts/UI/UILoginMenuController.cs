using System;
using TMPro;
using UnityEngine;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyGame.Network
{
    [RequireComponent(typeof(RestClient))]
    public class UILoginMenuController : MonoBehaviour
    {
        [SerializeField]
        TMP_InputField inputEmail;

        [SerializeField]
        TMP_InputField inputPassword;

        [SerializeField]
        EventSystem uiEvent;

        string email = "";
        string password = "";
        bool canSubmit = true;
        RestClient restClient;

        void Awake()
        {
            Initialize();
            Subscribe();
        }

        void OnApplicationQuit()
        {
            RevokeRefreshToken();
        }

        void Initialize()
        {
            Application.targetFrameRate = 60;
            restClient = GetComponent<RestClient>();
        }

        void Subscribe()
        {
            // inputEmail.onValueChanged.AddListener((text) => {
            //     if (Input.GetKeyDown(KeyCode.Tab)) {
            //         uiEvent.SetSelectedGameObject(inputPassword.gameObject);
            //     }
            // });

            inputEmail.onSubmit.AddListener((text) => {
                if (!canSubmit || Input.GetKeyDown(KeyCode.Escape)) { return; }
                uiEvent.SetSelectedGameObject(inputPassword.gameObject);
            });

            inputPassword.onSubmit.AddListener((text) => {
                if (!canSubmit || Input.GetKeyDown(KeyCode.Escape)) { return; }

                canSubmit = false;
                email = inputEmail.text;

                var hash = ComputeSha256Hash(text);
                var obj = new JObject();

                obj["grant_type"] = "password";
                obj["username"] = email;
                obj["client_id"] = Global.CLIENT_ID;
                obj["password"] = hash;

                var json = obj.ToString();

                //make ui loading indicator here..
                //alert connecting...

                restClient.Post("http://localhost:3000/token", json, (res) => {
                    //stop ui loading indicator here..
                    canSubmit = true;

                    if (res.isNetworkError || res.isHttpError)
                    {
                        Debug.Log("Failled..");
                        //alert here..
                    }
                    else
                    {
                        if (res.responseCode == 200) {
                            var result = JObject.Parse(res.downloadHandler.text);

                            Global.Email = email;
                            Global.IssuedAt = DateTime.UtcNow;
                            Global.AccessToken = (string) result["access_token"];
                            Global.ExpiresIn = (uint) result["expires_in"];
                            Global.RefreshToken = (string) result["refresh_token"];

                            FetchUserIdentity(success => {
                                if (success) {
                                    UIController.Instance?.ShowMainMenu();
                                }
                                else {
                                    Debug.Log("Failed to fetch user info...");
                                }
                            });
                        }
                    }
                });
            });
        }

        void FetchUserIdentity(Action<bool> callback)
        {
            var url = $"http://localhost:3000/user/publicinfo?email={Global.Email}&client_id={Global.CLIENT_ID}";

            restClient.Get(url, (res) =>
            {
                if (res.isNetworkError || res.isHttpError)
                {
                    Debug.Log("Failled to get user id");
                    callback?.Invoke(false);
                }
                else
                { 
                    if (res.responseCode == 200)
                    {
                        var result = JObject.Parse(res.downloadHandler.text);

                        Global.UserID = (string) result["id"];
                        Global.DisplayName = (string) result["name"];

                        callback?.Invoke(true);
                    }
                }
            });
        }

        void RevokeRefreshToken()
        {
            Debug.Log("Application ending after " + Time.time + " seconds");
            if (string.IsNullOrEmpty(Global.RefreshToken)) { return; }

            var obj = new JObject();

            obj["client_id"] = Global.CLIENT_ID;
            obj["refresh_token"] = Global.RefreshToken;
            obj["user_id"] = Global.UserID;

            var json = obj.ToString();

            restClient.Post("http://localhost:3000/token/revoke", json, (res) => {

            });
        }

        static string ComputeSha256Hash(string rawData)  
        {  
            using (SHA256 sha256Hash = SHA256.Create())  
            {  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));  
  
                var builder = new StringBuilder();  

                for (int i = 0; i < bytes.Length; i++)  
                {  
                    builder.Append(bytes[i].ToString("x2"));  
                }  

                return builder.ToString();  
            }  
        }  
    }
}
