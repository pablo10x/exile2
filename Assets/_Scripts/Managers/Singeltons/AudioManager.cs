
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Scripts.Managers.Singeltons
{
    public class AudioManager : Singleton<AudioManager>
    {
        [Required] [SerializeField] private AudioSource _audioSource;


        [FoldoutGroup("Audio Clips")] [SerializeField]
        private AudioClip ui_button_click;

        [FoldoutGroup("Audio Clips/UI")] [SerializeField] private AudioClip ui_button_return;
        [FoldoutGroup("Audio Clips/UI")] [SerializeField] private AudioClip ui_error;
        
        
        //volumes
        
        


        public void UI_Click()
        {
            _audioSource.PlayOneShot(ui_button_click);
        }

        public void UI_Return()
        {
            _audioSource.PlayOneShot(ui_button_return);
        }

        public void UI_Error()
        {
            _audioSource.PlayOneShot(ui_error);
        }
    }
}