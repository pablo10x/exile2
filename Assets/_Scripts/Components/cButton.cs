using System;
using _Scripts.Managers.Singeltons;
using core.Managers;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class cButton : MonoBehaviour
{
    [FoldoutGroup("Colors ")] [SerializeField] private Color   normalColor;
    [FoldoutGroup("Colors ")] [SerializeField] private Color32 selectedColor;


    [Required] [SerializeField] private Button   button;
    [Required] [SerializeField] private TMP_Text buttonText;
    [Required] [SerializeField] private GameObject    selectedImage;


    public event Action Clicked;

    private void Awake()
    {
        button.onClick.AddListener(() =>
        {
            AudioManager.Instance.UI_Click();
            Clicked?.Invoke();
            UiManager.Instance.UpdateGraphicsSettingsUI();
        });
        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            buttonText.color = selectedColor;
            selectedImage.SetActive(true);
        }
        else
        {
            buttonText.color = normalColor;
            selectedImage.SetActive(false);
        }
        
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        button ??= GetComponent<Button>();

        buttonText ??= GetComponentInChildren<TMP_Text>();
        
      
    }
#endif
    
}