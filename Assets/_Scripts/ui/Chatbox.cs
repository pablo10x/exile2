using System.Collections.Generic;
using core.ApiModels;
using core.Managers;
using Core.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace core.ui
{
    public class Chatbox : MonoBehaviour
    {
        //hi

        [SerializeField] private TMP_Text _username;
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private ProfilePicture _profilePicture;

        [SerializeField] private Button _sendButton;
        [SerializeField] private CanvasGroupToggler canvasGroupToggler;


        [SerializeField] private ChatMessage _chat_other_prefab;
        [SerializeField] private ChatMessage _chat_player_prefab;
        [SerializeField] private Transform chat_container;


        //loading icon
        [SerializeField] private GameObject loadingIcon;
        [SerializeField] private TMP_Text midText;


        private readonly List<ChatMessage> chatprefabs = new();
        public UserInfo _chatWithUser;

        private void Awake()
        {
            _inputField.characterLimit = 150;
            _inputField.keyboardType = TouchScreenKeyboardType.Social;
            _inputField.onSubmit.AddListener(SendChat);

            _inputField.onValueChanged.AddListener(msg => { _sendButton.interactable = msg.Length > 1; });

            _sendButton.onClick.AddListener(() => SendChat(_inputField.text));

            ClientManager.OnPrivateMessageReceived += OnPrivateMessageReceived;
            ClientManager.OnPrivateMessagesLoaded += OnPrivateMessagesLoaded;

            ClientManager.OnUnfriended += f =>
            {
                // if we are chatting with this guy and he unfriend us !!!

                clearMessages();
                _inputField.interactable = false;
                _sendButton.interactable = false;
                midText.text = $"{f.Name}\nis no longer your friend :(";
                midText.enabled = true;
            };
        }

        /// <summary>
        /// invoked when a conversation loaded between 2 users
        /// </summary>
        private void OnPrivateMessagesLoaded()
        {
            loadingIcon.SetActive(false);

            midText.enabled = false;

            foreach (MessageSchema msg in ClientManager.Instance.LocalPlayerData.PrivateMessages)
            {
                //local player
                if (msg.senderID == ClientManager.Instance.LocalPlayerData.PlayerId)
                {
                    ChatMessage msgx = Instantiate(_chat_player_prefab, chat_container);
                    msgx.SetMessage(msg, ClientManager.Instance.LocalPlayerData.ToUserInfo());
                    chatprefabs.Add(msgx);
                }

                //local player
                if (msg.senderID == _chatWithUser.PlayerId)
                {
                    ChatMessage msgx = Instantiate(_chat_other_prefab, chat_container);
                    msgx.SetMessage(msg, _chatWithUser);
                    chatprefabs.Add(msgx);
                }
            }
        }

        /// <summary>
        /// invoked when received a private message , spawn chat prefab if we get the message from same guy we talking to
        /// </summary>
        /// <param name="message"></param>
        private void OnPrivateMessageReceived(MessageSchema message)
        {
            if (message.senderID == _chatWithUser.PlayerId)
            {
                ChatMessage msg = Instantiate(_chat_other_prefab, chat_container);
                msg.SetMessage(message, _chatWithUser);
                chatprefabs.Add(msg);
            }

            if (message.senderID == ClientManager.Instance.LocalPlayerData.PlayerId)
            {
                ChatMessage msg = Instantiate(_chat_player_prefab, chat_container);
                msg.SetMessage(message, ClientManager.Instance.LocalPlayerData.ToUserInfo());
                chatprefabs.Add(msg);
            }

            if (!ClientManager.Instance.LocalPlayerData.PrivateMessages.Contains(message))
                ClientManager.Instance.LocalPlayerData.PrivateMessages.Add(message);
        }


        public  void openChat(UserInfo user)
        {
            if (LobbyManager.Instance.friendsearchBox.Active)
            {
                LobbyManager.Instance.friendsearchBox.Disable();
            }

            canvasGroupToggler.Enable(true);
            _sendButton.interactable = true;
            _inputField.interactable = true;
            _username.text = user.Name;
            _chatWithUser = user;
            _profilePicture.SetUser(user);

            loadingIcon.SetActive(true);

            midText.text = "Retrieving chat history..";

            //todo load old convo if there is any
             ClientManager.Instance.GetPrivateMessages(user.PlayerId);

            //print chat history ?
        }


        private async void SendChat(string message)
        {
            if (_chatWithUser is null) return;

            if (message.Length <= 1)
            {
                UiManager.Instance.SendTopNotification("message is too short");

                return;
            }

            _inputField.text = "";

            //_sendButton.interactable = false;
            //_inputField.interactable = false;

           await ClientManager.Instance.SendChatMessageToFriend(_chatWithUser.PlayerId.ToString(), message);
        }


        public void clearchat()
        {
            _username.text = "username";
            _inputField.text = "";
            _chatWithUser = null;
            canvasGroupToggler.Disable();

            //clearing the list
            foreach (ChatMessage chatprefab in chatprefabs)
            {
                Destroy(chatprefab.gameObject);
            }

            chatprefabs.Clear();
        }

        private void clearMessages()
        {
            //clearing the list
            foreach (ChatMessage chatprefab in chatprefabs)
            {
                Destroy(chatprefab.gameObject);
            }

            chatprefabs.Clear();
        }
    }
}