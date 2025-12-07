using System;
using System.Threading.Tasks;
using core.ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ErrorHandler : MonoBehaviour
{
    [Header("References")] [SerializeField]
    private TMP_Text errorTitleText;

    [SerializeField] private TMP_Text errorDescriptionText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button submitButton;
    [SerializeField] private TMP_Text submitButtonText;
    [SerializeField] private CanvasGroupToggler uiPage;
    [SerializeField] private AudioSource errorSound;

    //private bool _isErrorShown;
    private Action _submitButtonCallback;

    private void Awake()
    {
        closeButton.onClick.AddListener(HideError);
        submitButton.onClick.AddListener(InvokeSubmitButtonCallback);
        uiPage.Disable();
    }

    public void SetError(ErrorOptions options)
    {
        uiPage.Enable(true);
        errorSound.Play();

        errorTitleText.text = options.ErrorTitle;
        errorDescriptionText.text = options.ErrorDescription;
        submitButton.gameObject.SetActive(options.ShowSubmitButton);
        submitButtonText.text = options.SubmitButtonText;
        closeButton.gameObject.SetActive(options.CanCloseError);
        _submitButtonCallback = options.SubmitButtonCallback;

       // _isErrorShown = true;
    }

    public async Task SetErrorAsync(ErrorOptions options)
    {
        uiPage.Enable(true);
        errorSound.Play();
        errorTitleText.text = options.ErrorTitle;
        errorDescriptionText.text = options.ErrorDescription;
        submitButton.gameObject.SetActive(options.ShowSubmitButton);
        submitButtonText.text = options.SubmitButtonText;
        closeButton.gameObject.SetActive(options.CanCloseError);
        _submitButtonCallback = options.SubmitButtonCallback;
        if (_submitButtonCallback != null)
        {
            var tcs = new TaskCompletionSource<bool>();
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(() =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    _submitButtonCallback?.Invoke();
                    tcs.SetResult(true);
                }
            });
            await tcs.Task;
        }
       // _isErrorShown = true;
    }

    public void HideError()
    {
        errorTitleText.text = "ERROR";
        errorDescriptionText.text = "...";
       // _isErrorShown = false;
        uiPage.Disable();
    }

    private void InvokeSubmitButtonCallback()
    {
        _submitButtonCallback?.Invoke();
        HideError();
    }
}

public class ErrorOptions
{
    public string ErrorTitle { get; set; }
    public string ErrorDescription { get; set; }
    public bool CanCloseError { get; set; } = true;
    public string SubmitButtonText { get; set; } = "OK";
    public bool ShowSubmitButton { get; set; } = false;
    public Action SubmitButtonCallback { get; set; }
}