using core.ApiModels;
using core.Managers;
using Core.Managers;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace core.ui {

	public class ui_FriendTab : MonoBehaviour {


		[ Required ] [ SerializeField ] private ProfilePicture profilePicture;
		[ Required ] [ SerializeField ] private GameObject     loadingIcon;
		[ Required ] [ SerializeField ] private GameObject     container;
		[ Required ] [ SerializeField ] private GameObject     options;
		[ Required ] [ SerializeField ] private Image          vipBadge;


		[ Required ] [ SerializeField ] private TMP_Text username;
		[ Required ] [ SerializeField ] private TMP_Text playerlevel;
		[ Required ] [ SerializeField ] private TMP_Text activity;


		//remove toggle


		public UserInfo User;


		private void Awake ()
		{
			container.SetActive (false);
			loadingIcon.SetActive (true);
		}


		public void UpdateActivity ( Activity ac )
		{
			if (ac.IsOnline) {
				activity.text  = "ONLINE";
				activity.color = new Color (0.48f, 1f, 0.53f);
			} else {
				activity.text  = $"last seen: {ac.LastSeen} ";
				activity.color = new Color (0.49f, 0.48f, 0.46f);
			}
		}


		public void SetFriend ( UserInfo user )
		{

			User = user;

			profilePicture.SetUser (User);

			username.text    = user.Name;
			playerlevel.text = "level " + user.AccountLevel;
			container.SetActive (true);
			loadingIcon.SetActive (false);

			if (user.Activity.IsOnline) {
				activity.text  = "ONLINE";
				activity.color = new Color (0.48f, 1f, 0.53f);
			} else {
				activity.text  = $"last seen: {user.Activity.LastSeen} ";
				activity.color = new Color (0.49f, 0.48f, 0.46f);
			}

			if (user.Vip) vipBadge.enabled = true;

		}

		public void OnDeleteClicked ()
		{
			if (User is null) return;

			UiManager.Instance.AskForConfirmation ($"Delete {User.Name} ?", OkClicked, () => { UiManager.Instance.HideConfirmationBox (); });

		}

		private async void OkClicked ()
		{
			var res = await ClientManager.Instance.RemoveFriend (User);

			if (res.code == 200) {
				LobbyManager.Instance.RemoveFriend (User);
			}

			UiManager.Instance.HideConfirmationBox ();
		}

		public void onChatClicked ()
		{
			if (LobbyManager.Instance.chatbox._chatWithUser != null) {
				if (LobbyManager.Instance.chatbox._chatWithUser.PlayerId == User.PlayerId) return;
			}

			LobbyManager.Instance.chatbox.openChat (User);

		}

	}

}
