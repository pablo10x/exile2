using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.ApiModels;
using core.Managers;
using Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocketIOClient;
using SocketIOClient.Transport;
using UnityEngine;
using UnityEngine.Networking;

namespace Core.Managers
{
    public class ClientManager : Singleton<ClientManager>
    {
        public User LocalPlayerData { get; private set; }
        public AuthenticationHandler AuthenticationHandler;

        private SocketIOUnity _socket;
        private string _token;
        private string _key;

        #region Events

        public static event Action<UserInfo> OnFriendRequestReceived;
        public static event Action<UserInfo> OnIncomingFriendRequestAccepted;
        public static event Action<UserInfo> OnOutgoingFriendRequestAccepted;
        public static event Action<UserInfo> OnFriendRequestRejected;
        public static event Action<UserInfo> OnUnfriended;
        public static event Action<UserInfo> OnFriendActivityChange;
        public static event Action<UserInfo> OnSearchUser;
        public static event Action<MessageSchema> OnPrivateMessageReceived;
        public static event Action OnPrivateMessagesLoaded;
        public static event Action OnClientDisconnected;
        public static event Action OnClientReconnected;
        public static event Action OnClientReconnectFailed;

        #endregion

        private void Start()
        {
            AuthenticationHandler.OnFirebaseSignedIn += OnFirebaseSignedIn;
        }

        private async void OnFirebaseSignedIn(string authKey, string token)
        {
            _token = token;
            _key = authKey;

            try
            {
                var initResponse = await MakeRequest<InitResponse>($"{RemoteData.MasterServer}/api/auth",
                    new Dictionary<string, string>
                    {
                        { "token", token },
                        { "uid", authKey },
                        { "deviceid", SystemInfo.deviceUniqueIdentifier }
                    });

                if (initResponse == null)
                {
                    await HandleInitializationError();
                    return;
                }

                if (initResponse.Allowed)
                {
                    await LoadPlayerAccount(authKey, initResponse.Key);
                }
                else
                {
                    UiManager.Instance.SetBan(initResponse?.Message);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during Firebase sign-in: {ex.Message}");
                await HandleInitializationError();
            }
        }

        private async Task HandleInitializationError()
        {
            await UiManager.Instance.errorHandler.SetErrorAsync(new ErrorOptions
            {
                ErrorTitle = " Server Unreachable",
                ErrorDescription = "Couldn't connect to server",
                CanCloseError = false,
                SubmitButtonText = "Try again",
                ShowSubmitButton = true,
                SubmitButtonCallback = () =>
                {
                    UiManager.Instance.errorHandler.HideError();
                    OnFirebaseSignedIn(_key, _token);
                }
            });
        }

        private async Task LoadPlayerAccount(string uid, string key)
        {
            try
            {
                var user = await MakeRequest<User>($"{RemoteData.MasterServer}/api/auth/account",
                    new Dictionary<string, string>
                    {
                        { "auth", uid },
                        { "regkey", key },
                        { "deviceid", SystemInfo.deviceUniqueIdentifier }
                    });

                if (user != null)
                {
                    LocalPlayerData = user;
                    ConnectToServer(uid);
                }
                else
                {
                    throw new Exception("Failed to load player account");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading player account: {ex.Message}");
                await HandleInitializationError();
            }
        }

        private async void ConnectToServer(string uid)
        {
            _socket = new SocketIOUnity($"{RemoteData.MasterServerSocket}", new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "identity", "player" },
                    { "auth", uid },
                    { "playerid", LocalPlayerData.PlayerId.ToString() },
                    { "firetoken", _token }
                },


                Reconnection = true,
                ReconnectionDelay = 4000,
                ReconnectionAttempts = 10,
                ConnectionTimeout = TimeSpan.FromSeconds(10),
                Transport = TransportProtocol.WebSocket,
                AutoUpgrade = true
            });
            
            
           // _socket.OnAny(SocketHandler);
            SetupSocketEventHandlers();
            

            await _socket.ConnectAsync();
            //await _socket.EmitAsync("updatestatus", "LOBBY");

            // catch (Exception ex)
            // {
            //     Debug.LogError($"Error connecting to server: {ex.Message}");
            //     await HandleInitializationError();
            // }
        }

        private void SetupSocketEventHandlers()
        {
            _socket.OnConnected += OnConnected;
            _socket.OnError += OnError;
            _socket.OnDisconnected += OnDisconnected;
            _socket.OnReconnected += OnReconnected;
            _socket.OnReconnectFailed += OnReconnectFailed;
            _socket.OnAny(SocketHandler);
        }

        private Task<TResponse> MakeRequest<TResponse>(string url, Dictionary<string, string> data,
            bool statusCodeOnly = false)
        {
            var tcs = new TaskCompletionSource<TResponse>();

            StartCoroutine(MakeRequestCoroutine());

            return tcs.Task;

            IEnumerator MakeRequestCoroutine()
            {
                using (var req = UnityWebRequest.Post(url, data))
                {
                    req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
                    req.SetRequestHeader("x-api-key", RemoteData.APIKey);
                    req.timeout = 5;
                    req.redirectLimit = 3;

                    yield return req.SendWebRequest();

                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        try
                        {
                            var response = JsonConvert.DeserializeObject<TResponse>(req.downloadHandler.text);

                            tcs.SetResult(response);
                        }
                        catch (JsonException ex)
                        {
                            Debug.LogError($"Error deserializing response: {ex.Message}");
                            Debug.LogError($"{req.downloadHandler.text}");
                            tcs.SetException(ex);
                        }
                    }
                    else
                    {
                        Debug.LogError($"Request failed: {req.error}");
                        tcs.SetException(new Exception($"Request failed: {req.error}"));
                    }
                }
            }
        }

        #region Socket Event Handlers

        private void OnConnected(object sender, EventArgs e)
        {
            RunOnUnityThread.Instance().Enqueue(() =>
            {
                if (!GameManager.Instance.IsLobbySceneLoaded())
                {
                    GameManager.Instance.LoadLobbyScene();
                }
            });
        }

        private void OnError(object sender, string error) =>
            Debug.LogError($"Socket error: {error}");

        private void OnDisconnected(object sender, string reason) =>
            RunOnUnityThread.Instance().Enqueue(() => OnClientDisconnected?.Invoke());

        private void OnReconnected(object sender, int count) =>
            RunOnUnityThread.Instance().Enqueue(() => OnClientReconnected?.Invoke());

        private void OnReconnectFailed(object sender, EventArgs e) =>
            RunOnUnityThread.Instance().Enqueue(() => OnClientReconnectFailed?.Invoke());

        private void SocketHandler(string eventName, SocketIOResponse response)
        {
           // Debug.Log($"res: {response}");
            switch (eventName)
            {
                case "friendRequest":
                    HandleFriendRequest(response);
                    break;
                case "friendRemoved":
                    HandleFriendRemoval(response);
                    break;
                case "friendAccepted":
                    HandleFriendAccept(response);
                    break;
                case "activityChanged":
                    HandleFriendActivityChange(response);
                    break;
                case "PrivateMessage":
                    HandlePrivateMessage(response);
                    break;
                case "fetchedMessages":
                    HandlePrivateMessagesLoaded(response);
                    break;
                default:
                    Debug.LogError($"Unhandled socket event: {eventName}");
                    break;
            }
        }

        private void HandleFriendRequest(SocketIOResponse response)
        {
            var userInfo = DeserializeResponse<UserInfo>(response);
            RunOnUnityThread.Instance().Enqueue(() => OnFriendRequestReceived?.Invoke(userInfo));
        }

        private void HandleFriendRemoval(SocketIOResponse response)
        {
            var userInfo = DeserializeResponse<UserInfo>(response);
            LocalPlayerData.Friends = LocalPlayerData.Friends.Where(x => x.PlayerId != userInfo.PlayerId).ToList();
            RunOnUnityThread.Instance().Enqueue(() => OnUnfriended?.Invoke(userInfo));
        }

        private void HandleFriendAccept(SocketIOResponse response)
        {
            var userInfo = DeserializeResponse<UserInfo>(response);
            LocalPlayerData.Friends.Add(userInfo);
            RunOnUnityThread.Instance().Enqueue(() => OnOutgoingFriendRequestAccepted?.Invoke(userInfo));
        }

        private void HandleFriendActivityChange(SocketIOResponse response)
        {
            var userInfo = DeserializeResponse<UserInfo>(response);
            var friend = LocalPlayerData.Friends.FirstOrDefault(x => x.PlayerId == userInfo.PlayerId);
            if (friend != null)
            {
                friend.Activity = userInfo.Activity;
                RunOnUnityThread.Instance().Enqueue(() => OnFriendActivityChange?.Invoke(userInfo));
            }
        }

        private void HandlePrivateMessage(SocketIOResponse response)
        {
            try
            {
                // MessageSchema msg = JsonConvert.DeserializeObject<MessageSchema>(SocketDataParser(response));
                var msg = DeserializeResponse<MessageSchema>(response);
                if (msg != null)
                {
                    msg.channel = chatChannel.Friends;
                    // Debug.Log($"getting msg: {msg.content}");
                }

                RunOnUnityThread.Instance().Enqueue(() => { OnPrivateMessageReceived?.Invoke(msg); });

                // NotificationManager.Instance.AddChatMessage(msg);
            }
            catch (Exception e)
            {
                UiManager.HandleException(e);
            }
        }

        private void HandlePrivateMessagesLoaded(SocketIOResponse response)
        {
            // var messages = DeserializeResponse<List<MessageSchema>>(response);
            // LocalPlayerData.PrivateMessages.Clear();
            // LocalPlayerData.PrivateMessages.AddRange(messages);
            // RunOnUnityThread.Instance().Enqueue(() => OnPrivateMessagesLoaded?.Invoke());

            try
            {
                List<MessageSchema> msgs
                    = JsonConvert.DeserializeObject<List<MessageSchema>>(SocketDataParser(response));

                if (msgs != null && msgs.Count > 0)
                {
                    LocalPlayerData.PrivateMessages.Clear();

                    RunOnUnityThread.Instance().Enqueue(() =>
                    {
                        foreach (var m in msgs)
                        {
                            if (!LocalPlayerData.PrivateMessages.Contains(m))
                            {
                                LocalPlayerData.PrivateMessages.Add(m);
                            }
                        }
                    });
                }

                RunOnUnityThread.Instance().Enqueue(() => { OnPrivateMessagesLoaded?.Invoke(); });
            }
            catch (Exception e)
            {
                UiManager.HandleException(e);
            }
        }

        private string SocketDataParser(SocketIOResponse response)
        {
            var json = response.ToString();
            var jsonArray = JArray.Parse(json);

            return jsonArray[0].ToString();
        }

        private T DeserializeResponse<T>(SocketIOResponse response)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(SocketDataParser(response));
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Error deserializing response: {ex.Message}");
                return default;
            }
        }

        private void DisableReconnectPage()
        {
            if (UiManager.Instance.RecconectPage.Active)
            {
                UiManager.Instance.RecconectPage.Disable();
            }

            if (!GameManager.Instance.IsLobbySceneLoaded())
            {
                GameManager.Instance.LoadLobbyScene();
            }
        }

        #endregion

        #region Social API Methods

        public async Task<ApiCodeResponse> RejectFriendRequest(UserInfo other)
        {
            return await MakeRequest<ApiCodeResponse>(
                $"{RemoteData.MasterServer}/api/social/rejectfriendrequest",
                new Dictionary<string, string>
                {
                    { "r_playerid", LocalPlayerData.PlayerId.ToString() },
                    { "r_receiver", other.PlayerId.ToString() }
                });
        }

        public async Task<UserInfo> SearchFriend(string id)
        {
            return await MakeRequest<UserInfo>($"{RemoteData.MasterServer}/api/social/searchfriend",
                new Dictionary<string, string>
                {
                    { "r_playerid", LocalPlayerData.PlayerId.ToString() },
                    { "r_searchterm", id }
                });
        }

        public async Task<ApiCodeResponse> AddFriend(UserInfo user)
        {
            return await MakeRequest<ApiCodeResponse>($"{RemoteData.MasterServer}/api/social/addfriend",
                new Dictionary<string, string>
                {
                    { "r_playerid", LocalPlayerData.PlayerId.ToString() },
                    { "r_receiver", user.PlayerId.ToString() }
                });
        }

        public async Task<ApiCodeResponse> AcceptFriendRequest(UserInfo other)
        {
            return await MakeRequest<ApiCodeResponse>(
                $"{RemoteData.MasterServer}/api/social/acceptfriendrequest",
                new Dictionary<string, string>
                {
                    { "r_playerid", LocalPlayerData.PlayerId.ToString() },
                    { "r_receiver", other.PlayerId.ToString() },
                    { "token", _token }
                });
        }

        public async Task<ApiCodeResponse> RemoveFriend(UserInfo other)
        {
            var result = await MakeRequest<ApiCodeResponse>($"{RemoteData.MasterServer}/api/social/removefriend",
                new Dictionary<string, string>
                {
                    { "r_playerid", LocalPlayerData.PlayerId.ToString() },
                    { "r_receiver", other.PlayerId.ToString() }
                });

            if (result.code == 200)
            {
                LocalPlayerData.Friends = LocalPlayerData.Friends.Where(x => x.PlayerId != other.PlayerId).ToList();
                RunOnUnityThread.Instance().Enqueue(() => OnUnfriended?.Invoke(other));
            }
            else
            {
                UiManager.Instance.SendTopNotification($"Couldn't remove friend: {result}",
                    UiManager.NotificationType.Error, 2f);
            }

            return result;
        }

        public async Task SendChatMessageToFriend(string friendId, string message)
        {
            if (_socket != null)
            {
                await _socket.EmitStringAsJSONAsync("MessageToFriend", new JObject
                {
                    { "senderId", LocalPlayerData.PlayerId },
                    { "friendId", friendId },
                    { "message", message }
                }.ToString());
            }
        }

        public async void GetPrivateMessages(int id)
        {
            if (_socket != null)
            {
                // await _socket.EmitAsync("getPrivateMessages", new JObject { { "user", id } });

                JObject jb = new JObject
                {
                    new JProperty("user", id),
                };
                //_socket.Emit("getPrivateMessages", id);
                await _socket.EmitStringAsJSONAsync("getPrivateMessages", jb.ToString());
            }
        }

        #endregion
    }
}