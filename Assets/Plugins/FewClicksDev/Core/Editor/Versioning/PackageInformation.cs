namespace FewClicksDev.Core
{
    using FewClicksDev.Core.Versioning;
    using System;
    using System.Net.Http;
    using UnityEditor;
    using UnityEngine;

    public class PackageInformation : ScriptableObject
    {
        [Header("Name and description")]
        [SerializeField] private string toolName = string.Empty;
        [SerializeField] private string toolDescription = string.Empty;
        [SerializeField] private string assetStoreLink = string.Empty;
        [SerializeField] private string menuItemPath = string.Empty;
        [SerializeField] private bool isFree = false;

        public string ToolName => toolName;
        public string ToolDescription => toolDescription;
        public string AssetStoreLink => assetStoreLink;
        public string MenuItemPath => menuItemPath;
        public bool IsFree => isFree;

        public bool IsInstalled { get; private set; }
        public bool IsLinkValid { get; private set; }

        [Header("Resources")]
        [SerializeField] private Texture2D toolIcon = null;
        [SerializeField] private Color toolMainColor = Color.white;

        public Texture2D ToolIcon => toolIcon;
        public Color ToolMainColor => toolMainColor;

        private HttpClient client = new HttpClient();

        public void CacheIsInstalled()
        {
            IsInstalled = false;
            var _changelogs = AssetsUtilities.GetAssetsOfType<Changelog>();

            foreach (var _changelog in _changelogs)
            {
                if (_changelog.PackageInfo == this)
                {
                    IsInstalled = true;
                }
            }

            checkIfLinkIsValid(assetStoreLink);
        }

        public void OpenWindow()
        {
            EditorApplication.ExecuteMenuItem(menuItemPath);
        }

        public void OpenAssetStoreLink()
        {
            Application.OpenURL(assetStoreLink);
        }

        private async void checkIfLinkIsValid(string _url)
        {
            Uri _uri = new Uri(_url);

            if (_uri.IsWellFormedOriginalString() == false)
            {
                IsLinkValid = false;
                return;
            }

            try
            {
                using (HttpRequestMessage _request = new HttpRequestMessage(HttpMethod.Get, _uri))
                {
                    var _task = client.SendAsync(_request);
                    await _task;

                    IsLinkValid = _task.Result.IsSuccessStatusCode;
                }
            }
            catch
            {
                IsLinkValid = false;
            }
        }
    }
}
