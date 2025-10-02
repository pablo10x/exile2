using core.ApiModels;
using Core.Managers;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace core.ui {

    public class FriendRequest_ui : MonoBehaviour {

        [ Required ] public ProfilePicture profilePicture;
        [ Required ] [ SerializeField ] private TMP_Text username;
        [ Required ] [ SerializeField ] private Button acceptBt;
        [ Required ] [ SerializeField ] private Button rejectBt;
        [ Required ] [ SerializeField ] private GameObject loadingIcon;
        [ Required ] [ SerializeField ] private GameObject buttonsBox;
        [ Required ] [ SerializeField ] private Image baseImage;


        private UserInfo _userID;


        private void Awake ()
        {
            loadingIcon.SetActive (false);
            acceptBt.onClick.AddListener (() => OnAcceptClicked ());
            rejectBt.onClick.AddListener (() => OnRejectClicked ());
        }

        private async void OnRejectClicked ()
        {
            if (_userID is null) return;
            acceptBt.interactable = false;
            rejectBt.interactable = false;
            loadingIcon.SetActive (true);
            buttonsBox.SetActive (false);
            var result = await ClientManager.Instance.RejectFriendRequest (_userID);
            loadingIcon.SetActive (false);

            switch (result.code) {
                case 200:
                    Destroy (gameObject);

                    break;
                default:
                    buttonsBox.SetActive (true);
                    acceptBt.interactable = true;
                    rejectBt.interactable = true;

                    break;
            }
        }

        private async void OnAcceptClicked ()
        {
            if (_userID is null) return;
            acceptBt.interactable = false;
            rejectBt.interactable = false;
            loadingIcon.SetActive (true);
            buttonsBox.SetActive (false);
            var result = await ClientManager.Instance.AcceptFriendRequest (_userID);
            loadingIcon.SetActive (false);

            switch (result.code) {
                case 200:

                    //animations
                    //username.color  = new Color (0.36f, 1f, 0.39f);
                    //baseImage.color = new Color (0.5f, 0.46f, 0.44f, 0.5f);

                    Destroy (gameObject);

                    // username.DOText ($"{_userID.Name} Accepted ! ", 1f, false, ScrambleMode.Uppercase).onComplete += () => {
                    //     baseImage.DOFade (0f, .5f).SetEase (Ease.Flash).onComplete += () => {
                    //         Destroy (gameObject);
                    //         
                    //     };
                    // };

                    break;
                default:
                    buttonsBox.SetActive (true);
                    acceptBt.interactable = true;
                    rejectBt.interactable = true;

                    break;
            }

        }
        

        public void SetRequestData ( UserInfo userInfo )
        {
            _userID       = userInfo;
            username.text = userInfo.Name;

            //profilePicture.SetProfilePicture();
        }

    }

}
