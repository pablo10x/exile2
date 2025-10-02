using System.Collections.Generic;
using System.Linq;
using core.ApiModels;
using Core.Managers;
using core.ui;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace core.Managers {
    public class LobbyManager : Singleton<LobbyManager> {
        [Required] [FoldoutGroup("ui")] [SerializeField] private TMP_Text playerName;

        [Required] [FoldoutGroup("ui")] [SerializeField] private TMP_Text cash;

        [Required] [FoldoutGroup("ui")] [SerializeField] private TMP_Text onlineUsersText;

        [Required] [FoldoutGroup("ui")] [SerializeField] private Image vipBadge;

        #region Tabs ( canvas groups )

        [Required] [FoldoutGroup("canvas group")] [SerializeField] private CanvasGroupToggler canvasHome;

        [Required] [FoldoutGroup("canvas group")] [SerializeField] private CanvasGroupToggler canvasFriends;

        #endregion

        #region friends

        [Required] [FoldoutGroup("Fiends")] [SerializeField] private Image requestsImage;

        [Required] [FoldoutGroup("Fiends")] [SerializeField] private TMP_Text requestsCount;

        //containers

        [Required] [FoldoutGroup("Container")] [SerializeField] private Transform containerFriendRequests;

        [Required] [FoldoutGroup("Container")] [SerializeField] private Transform containerFriendtabs;

        //prefabs
        [Required] [FoldoutGroup("FiendRquestes")] [SerializeField] private GameObject friendRequestPrefab;

        [Required] [FoldoutGroup("FiendRquestes")] [SerializeField] private GameObject friendtabPrefab;

        #endregion

        [BoxGroup("friendSearch Box")] [Required] public CanvasGroupToggler friendsearchBox;

        #region chatbox

        public Chatbox chatbox;

        #endregion

        public Color disabled_color     = new Color32(164, 164, 164, 77);
        public Color notification_color = new Color32(243, 129, 59, 100);

        public struct UiFriendTab {
            public ui_FriendTab tab;
            public GameObject   prefab;
        }

        private readonly List<UiFriendTab> FriendsUI       = new();
        private readonly List<UserInfo>    _friendRequests = new();

        private void OnDisable() {
            ClientManager.OnFriendRequestRejected -= RemoveIncommingFriendRequestUITab;
        }

        private void Awake() {
            if (ClientManager.Instance != null) {
                UiManager.Instance.ListenForGraphicsSettings();
                if (ClientManager.Instance.LocalPlayerData != null) {
                    PopulateLobby();

                    //list all incomming friend requests
                    foreach (var request in ClientManager.Instance.LocalPlayerData.IncFriendRequests) {
                        OnFriendRequestRecived(request);
                    }
                }

                //sub to events


                // new friend request reveived
                ClientManager.OnFriendRequestReceived += f => {
                    OnFriendRequestRecived(f);
                    requestsImage.color = notification_color;
                    if (!requestsCount.enabled) requestsCount.enabled = true;
                    requestsCount.text = _friendRequests.Count.ToString();
                };

                // incomming request > accepted
                ClientManager.OnFriendRequestReceived += f => {
                    //remove from player data
                    // IncommingFriendRequest uf = ClientManager.Instance.LocalPlayerData.IncFriendRequests.First (x => x.From.PlayerId == f.PlayerId);
                    //
                    // if (uf != null) {
                    //     ClientManager.Instance.LocalPlayerData.IncFriendRequests.Remove (uf);
                    //     Debug.Log ("removed from ongoing Friend requests");
                    // }

                    AddFriend(f);
                    RemoveIncommingFriendRequestUITab(f);
                };

                // ongoing request > accepted
                ClientManager.OnOutgoingFriendRequestAccepted += f => {
                    //remove from player data
                    // var uf = ClientManager.Instance.LocalPlayerData.OutFriendRequests.First (x => x.To.PlayerId == f.PlayerId);
                    //
                    // if (uf != null) {
                    //     ClientManager.Instance.LocalPlayerData.OutFriendRequests.Remove (uf);
                    //     Debug.Log ("removed from ongoing Friend requests");

                    // }

                    AddFriend(f);
                };


                // we rejected the request
                ClientManager.OnFriendRequestRejected += f => { RemoveIncommingFriendRequestUITab(f); };

                //unfriended by a user
                ClientManager.OnUnfriended += f => { RemoveFriend(f); };

                // activity changed
                ClientManager.OnFriendActivityChange += f => {
                    var fr = FriendsUI.First(x => x.tab.User.PlayerId == f.PlayerId);
                    fr.tab.UpdateActivity(f.Activity);
                    UpdateFriendsData();
                };
            }
        }

        #region Friends events methods

        private void OnFriendRequestRecived(UserInfo user) {
            FriendRequest_ui friendRequestUI = Instantiate(friendRequestPrefab, containerFriendRequests)
                .GetComponent<FriendRequest_ui>();
            friendRequestUI.SetRequestData(user);
            friendRequestUI.profilePicture.SetUser(user);
            if (!_friendRequests.Contains(user)) _friendRequests.Add(user);
        }

        #endregion

        private void PopulateLobby() {
            var user = ClientManager.Instance.LocalPlayerData;

            #region player

            playerName.text = user.Name;
            cash.text       = $"{user.Cash}$";

            if (user.Vip) {
                vipBadge.color = new Color32(127, 223, 255, 255); //(127, 223, 255, 255);
            }
            else {
                vipBadge.color = new Color32(49, 49, 49, 255);
            }

            #endregion

            #region General

            if (user.Friends != null) {
                var onlineFriends = user.Friends?.Count(x => x.Activity?.IsOnline == true) ?? 0;

                if (user.Friends != null) {
                    onlineUsersText.text = $"ONLINE: <color=#64F2A4>{onlineFriends}</color> / {user.Friends.Count}";

                    foreach (var fr in user.Friends) {
                        AddFriend(fr);
                    }
                }
            }

            if (user.IncFriendRequests.Count > 0) {
                requestsImage.color = notification_color;
                requestsCount.text  = user.IncFriendRequests.Count.ToString();
                if (!requestsCount.enabled) requestsCount.enabled = true;
            }

            #endregion
        }

        public void SettingsClicked() {
            if (UiManager.Instance != null) {
                UiManager.Instance.ShowSettings();
            }
        }

        public void AddFriend(UserInfo friend) {
            // Check for null friend
            if (friend == null) {
                Debug.LogError("Cannot add null friend.");

                return;
            }

            // Instantiate the friend tab prefab
            ui_FriendTab tab = Instantiate(friendtabPrefab, containerFriendtabs)
                ?.GetComponent<ui_FriendTab>();

            // Check for null tab
            if (tab == null) {
                Debug.LogError("Failed to instantiate friend tab prefab.");

                return;
            }

            // Set friend information on the tab
            tab.SetFriend(friend);
            tab.gameObject.transform.SetAsLastSibling();

            // Add the friend tab to the FriendsUI collection
            FriendsUI.Add(new UiFriendTab { prefab = tab.gameObject, tab = tab });
            UpdateFriendsData();
        }

        public void RemoveFriend(UserInfo friend) {
            UpdateFriendsData();
            var friendToRemove = FriendsUI.FirstOrDefault(fr => fr.tab.User.PlayerId == friend.PlayerId);

            if (friendToRemove.tab != null && friendToRemove.prefab != null) {
                Destroy(friendToRemove.prefab);
                FriendsUI.Remove(friendToRemove);
            }
        }

        public void RemoveIncommingFriendRequestUITab(UserInfo f) {
            if (_friendRequests.Contains(f)) {
                _friendRequests.Remove(f);
            }

            requestsCount.text = _friendRequests.Count.ToString();

            if (_friendRequests.Count == 0) {
                // hide friendrequest ui if no more requests left
                requestsImage.color   = disabled_color;
                requestsCount.enabled = false;
            }
        }

        private void UpdateFriendsData() {
            var onlineFriends = ClientManager.Instance.LocalPlayerData.Friends.Where(x => x.Activity.IsOnline)
                                             .ToList()
                                             .Count;

            //	onlineUsersText.text = $"ONLINE: <color=#64F2A4>{onlineFriends}</color> / {ClientManager.Instance.LocalPlayerData.Friends.Count}";
            onlineUsersText.DOText($"ONLINE: <color=#64F2A4>{onlineFriends}</color> / {ClientManager.Instance.LocalPlayerData.Friends.Count}", 0.2f);
        }
    }
}