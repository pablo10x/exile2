using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace core.ui {

    [ RequireComponent (typeof(CanvasGroup)) ] public class CanvasGroupToggler : MonoBehaviour  {

        [ Required ] [ SerializeField ] private CanvasGroup canvasGroup;
        private Image _image;

        public bool Active => canvasGroup.blocksRaycasts;


        public event UnityAction OnOpened;
        public event UnityAction OnClosed;

        [ Button ("Enable",ButtonSizes.Large )]     private void en ()  => Enable ();
        [ Button ("disable",ButtonSizes.Large) ] private void dis () => Disable ();
        
        
       public void Enable ( bool anim = false )
        {

            if (!anim) {
                canvasGroup.alpha         = 1;
                canvasGroup.interactable   = true;
                canvasGroup.blocksRaycasts = true;
            } else {

                canvasGroup.DOFade (1, 0.2f).onComplete = () => {
                    //_canvasGroup.alpha          = 1;
                    canvasGroup.interactable   = true;
                    canvasGroup.blocksRaycasts = true;

                };
            }
            OnOpened?.Invoke();

        }

         public void Disable ( bool anim = false )
        {
            if (!anim) {
                canvasGroup.alpha          = 0f;
                canvasGroup.interactable   = false;
                canvasGroup.blocksRaycasts = false;
            } else {
                canvasGroup.DOFade (0, 0.2f).onComplete = () => {
                    canvasGroup.interactable   = false;
                    canvasGroup.blocksRaycasts = false;

                };
            }
            OnClosed?.Invoke();

        }


        public void Toggle() {
            if (Active) Disable();
            else Enable();
        }
        #if UNITY_EDITOR
        public void OnValidate ()
        {
            if (canvasGroup is null) canvasGroup = GetComponent <CanvasGroup> ();
            if (_image is null) _image           = GetComponent <Image> ();
        }
        #endif

    }

}
