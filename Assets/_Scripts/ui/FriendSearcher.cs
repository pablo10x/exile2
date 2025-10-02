using System.Linq;
using core.ApiModels;
using Core.Managers;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendSearcher : MonoBehaviour
{
    [Required][SerializeField] private Button searchButton;
    [Required][SerializeField] private TMP_InputField idInput;

    [Header("Container")]
    [Required][SerializeField] private GameObject container;
    [Required][SerializeField] private ProfilePicture profilePicture;
    [Required][SerializeField] private TMP_Text username;
    [Required][SerializeField] private TMP_Text inNotificationText;
    [Required][SerializeField] private GameObject inNotificationBox;
    [Required][SerializeField] private Button addButton;
    [Required][SerializeField] private GameObject spinner;

    private UserInfo _user;

    private  void Awake()
    {
        idInput.keyboardType = TouchScreenKeyboardType.Search;
        searchButton.onClick.AddListener(OnSearchClicked);
        addButton.onClick.AddListener(AddButtonClicked);

        idInput.onSubmit.AddListener(s =>
        {
            OnSearchClicked();
        });
        SetInteractableState(true);
    }

    private async void AddButtonClicked()
    {
        SetUIStateWhileProcessing(true);

        var result = await ClientManager.Instance.AddFriend(_user);

        switch (result.code)
        {
            case 200:
                ShowNotification("Friend request sent successfully.");
                container.SetActive(false);
                break;
            default:
                ShowNotification("Failed to send friend request.");
                break;
        }

        SetUIStateWhileProcessing(false);
    }

    private async void OnSearchClicked()
    {
        if (string.IsNullOrWhiteSpace(idInput.text))
        {
            ShowNotification("Provide a valid player ID or name.");
            return;
        }

        if (IsSearchingForSelf())
        {
            ShowNotification("Searching for yourself?");
            return;
        }

        SetUIStateWhileProcessing(true);

        var searchedPlayer = await ClientManager.Instance.SearchFriend(idInput.text);

        if (searchedPlayer != null)
            ProcessFoundPlayer(searchedPlayer);
        else
            ShowPlayerNotFoundNotification();

        SetUIStateWhileProcessing(false);
    }

    private bool IsSearchingForSelf()
    {
        return ClientManager.Instance.LocalPlayerData.PlayerId.ToString() == idInput.text ||
               ClientManager.Instance.LocalPlayerData.Name == idInput.text;
    }

    private void ProcessFoundPlayer(UserInfo player)
    {
        container.SetActive(true);
        _user = player;
        username.text = player.Name;
        inNotificationBox.SetActive(false);
        inNotificationText.text = "";

        if (IsPlayerAlreadyAFriend(player))
        {
            addButton.gameObject.SetActive(false);
            ShowNotification($"{player.Name} is already in your friend list.");
        }
    }

    private bool IsPlayerAlreadyAFriend(UserInfo player)
    {
        return ClientManager.Instance.LocalPlayerData.Friends.Any(fr => fr.PlayerId == player.PlayerId);
    }

    private void ShowPlayerNotFoundNotification()
    {
        ShowNotification("Player not found.");

        if (container.activeSelf)
            container.SetActive(false);
    }

    private void ShowNotification(string message)
    {
        inNotificationBox.SetActive(true);
        inNotificationText.DOText(message, 0.1f);
    }

    private void SetUIStateWhileProcessing(bool isProcessing)
    {
        SetInteractableState(!isProcessing);
        spinner.SetActive(isProcessing);
    }

    private void SetInteractableState(bool state)
    {
        idInput.interactable = state;
        searchButton.interactable = state;
        addButton.interactable = state;
    }
}
