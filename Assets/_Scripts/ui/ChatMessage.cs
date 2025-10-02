using core.ApiModels;
using TMPro;
using UnityEngine;

namespace core.ui {

    public class ChatMessage : MonoBehaviour {

        public RectTransform xrect;
        public ProfilePicture profilePicture;
        public TMP_Text username;
        public TMP_Text msg;
        


        public void SetMessage ( MessageSchema message , UserInfo u )
        {
            if(message.content.Length > 160) return;
            username.text = message.senderName;
            msg.text      = message.content;
            profilePicture.SetUser(u);
            ResizeDialog ();
        }
    

         public void ResizeDialog ()
        {
            float totalHeight;

            if (msg.text.Length > 28) {
                totalHeight = Mathf.Max (1, Mathf.CeilToInt ((float)msg.text.Length / 25)) * 45f;

            } else {

                totalHeight = 90f;
            }

            // Adjust the size of the RectTransform based on the total height
            xrect.sizeDelta = new Vector2 (xrect.sizeDelta.x, totalHeight);
        }
        

    }

}
