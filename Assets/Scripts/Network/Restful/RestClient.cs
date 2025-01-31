﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyGame.Network
{
    [DisallowMultipleComponent]
    public sealed class RestClient : MonoBehaviour
    {
        public static bool IsVerbose = false;

/*
#if UNITY_EDITOR
        string id = "";
        string token = "";

        void OnGUI()
        {
            id = GUILayout.TextArea(id, 200);
            token = GUILayout.TextArea(token, 200);

            if (GUILayout.Button("GET Lobby"))
            {
                Get($"{endpoint}/lobby", (res) =>
                {
                    Debug.Log(res.downloadHandler.text);
                });
            }

            if (GUILayout.Button("POST Lobby"))
            {
                JObject obj = new JObject();

                obj["title"] = "My Server";
                obj["ip"] = "127.0.0.1";
                obj["port"] = 27015;
                obj["player"] = 0;
                obj["maxPlayer"] = 8;

                string json = obj.ToString();

                Post($"{endpoint}/lobby", json, (res) => {
                    Debug.Log(res.downloadHandler.text);
                });
            }

            if (GUILayout.Button("PUT Lobby")) {
                JObject obj = new JObject();

                obj["title"] = "Community Server #1";
                obj["ip"] = "172.0.2.34";
                obj["port"] = 6000;
                obj["player"] = 0;
                obj["maxPlayer"] = 12;

                obj["id"] = id;
                obj["token"] = token;

                string json = obj.ToString();

                Put($"{endpoint}/lobby", json, (res) => {
                    Debug.Log(res.downloadHandler.text);
                });
            }

            if (GUILayout.Button("DELETE Lobby")) {
                JObject obj = new JObject();

                obj["id"] = id;
                obj["token"] = token;

                string json = obj.ToString();

                Delete($"{endpoint}/lobby", json, (res) => {
                    Debug.Log(res.downloadHandler.text);
                });
            }
        }

        void Awake()
        {
            IsVerbose = true;
        }
#endif
*/
        // void Start()
        // {
//DELETE
            // Delete($"{endpoint}/lobby", json, (respond) => {
                // if (IsVerbose)
                //     Debug.Log(respond.responseCode);
            // });
//PUT
            // Put($"{endpoint}/lobby", json, (respond) => {
            //     Debug.Log(respond.responseCode);
            // });

//POST
            // Post($"{endpoint}/lobby", json, (respond) => {
            //     Debug.Log(respond.downloadHandler.text);
            // });

//GET
            // Get($"{endpoint}/lobby", (respond) =>
            // {
            //     if (respond.isNetworkError)
            //     {
            //         Debug.Log(respond.error);
            //     }
            //     else
            //     {
            //         Debug.Log(respond.downloadHandler.text);
            //     }
            // });
        // }

        public void Get(string url, Action<UnityWebRequest> callback)
        {
            StartCoroutine(GetRequestCallback(url, callback));
        }

        public void Post(string url, string json, Action<UnityWebRequest> callback)
        {
            StartCoroutine(PostRequestCallback(url, json, callback));
        }

        public void Put(string url, string json, Action<UnityWebRequest> callback)
        {
            StartCoroutine(PutRequestCallback(url, json, callback));
        }

        public void Delete(string url, string json, Action<UnityWebRequest> callback)
        {
            StartCoroutine(DeleteRequestCallback(url, json, callback));
        }

        IEnumerator GetRequestCallback(string url, Action<UnityWebRequest> callback)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                callback?.Invoke(request);
            }
        }

        IEnumerator PostRequestCallback(string url, string json, Action<UnityWebRequest> callback)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            callback?.Invoke(request);
        }

        IEnumerator PutRequestCallback(string url, string json, Action<UnityWebRequest> callback)
        {
            var request = new UnityWebRequest(url, "PUT");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            callback?.Invoke(request);
        }

        IEnumerator DeleteRequestCallback(string url, string json, Action<UnityWebRequest> callback)
        {
            var request = new UnityWebRequest(url, "DELETE");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            callback?.Invoke(request);
        }
    }
}
