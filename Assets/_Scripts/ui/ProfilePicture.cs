using core.ApiModels;
using Core.Managers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class ProfilePicture : MonoBehaviour {

	[ Required ] [ BoxGroup ("UI") ] [ SerializeField ] private Image avatarFrame;
	[ Required ] [ BoxGroup ("UI") ] [ SerializeField ] private Image profilePicture;


	private UserInfo _userInfo;


	[ SerializeField ] private Material _mat_de_saturated;
	[ SerializeField ] private Material _mat_saturated;


	void Start ()
	{

		//_mat                    = new Material (_mat.shader);
		//profilePicture.material = _mat;
	}

	private void Awake ()
	{
		ClientManager.OnFriendActivityChange += u => {

			if (_userInfo != null) {
				if (u.PlayerId == _userInfo.PlayerId) {
					SetOnline (u.Activity.IsOnline);
				}
			}

		};
	}


	public void SetUser ( UserInfo u )
	{
		if (u is null) return;
		_userInfo = u;

		if (ClientManager.Instance.LocalPlayerData.PlayerId == u.PlayerId) {

			SetOnline (true);
		} else {

			SetOnline (u.Activity.IsOnline);
		}

	}

	private void SetOnline ( bool on )
	{

		if (on) {

			profilePicture.material = _mat_saturated;

			// Saturate the image
		} else {
			profilePicture.material = _mat_de_saturated;
		}
	}


	private void SetProfilePicture ( Image pic, Image frame = null )
	{
		if (pic != null) {
			profilePicture = pic;
			if (frame != null) avatarFrame = frame;
		}
	}

	// Update is called once per frame
	void Update ()
	{

	}

}
