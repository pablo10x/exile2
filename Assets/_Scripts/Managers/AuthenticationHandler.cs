using System;
using System.Threading.Tasks;
using Best.HTTP;
using Best.HTTP.Request.Upload;
using Best.WebSockets;
using core.ApiModels;
using core.Managers;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
#if UNITY_ANDROID
using GooglePlayGames;
#endif
using QFSW.QC;
using Sirenix.OdinInspector;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Managers {
    /// <summary>
    /// Manages user authentication lifecycle, including Firebase login, 
    /// Google Play Games integration, and backend server connection via WebSocket.
    /// </summary>
    public class AuthenticationHandler : MonoBehaviour {
        #region Configuration
        [Title("Network Configuration")]
        [SerializeField] private string _authUrl = "http://localhost:8081/api/game/auth";
        [SerializeField] private string _wsUrlFormat = "ws://localhost:8081/api/game/ws?key={0}";
        [SerializeField] private string _apiKey = "game_exile_key140_beta";
        
        private const string ApiKeyHeader = "X-Game-API-KEY";
        #endregion

        #region UI References
        [Title("UI References")]
        [SerializeField, Required] private GameObject loginBoxContainer;
        [SerializeField, Required] private Button googleButton;
        [SerializeField, Required] private Button anonymousButton;
        [SerializeField, Required] public Button touchToContinueButton;
        [SerializeField, Required] private Button logoutButton;
        [SerializeField, BoxGroup("Error Handler"), Required] public ErrorHandler errorHandler;
        #endregion

        #region Events
        public event Action<string, string> OnFirebaseSignedIn;
        #endregion

        #region Properties
        public FirebaseApp App { get; private set; }
        public FirebaseUser User { get; private set; }
        public bool IsAuthenticated => User != null;
        #endregion

        #region Private Fields
        private FirebaseAuth _auth;
        private bool _initialized;
        private WebSocket _webSocket;
        #endregion

        #region Unity Lifecycle
        private void Awake() {
            ValidateReferences();
            UpdateUIState(false);
        }

        private void Start() {
            // Subscribe to remote config fetch event
            if (frRemote.Instance != null) {
                frRemote.Instance.OnRemoteDataFetched -= HandleRemoteDataFetched;
                frRemote.Instance.OnRemoteDataFetched += HandleRemoteDataFetched;
            }
            else {
                Debug.LogError($"[{nameof(AuthenticationHandler)}] frRemote instance is missing!");
            }
        }

        private void OnDestroy() {
            DisconnectWebSocket();
            
            if (frRemote.Instance != null) {
                frRemote.Instance.OnRemoteDataFetched -= HandleRemoteDataFetched;
            }

            if (_auth != null) {
                _auth.StateChanged -= HandleAuthStateChanged;
            }
        }
        #endregion

        #region Initialization
        private void ValidateReferences() {
            if (loginBoxContainer == null) Debug.LogError($"[{nameof(AuthenticationHandler)}] LoginBoxContainer is not assigned!", this);
            if (googleButton == null) Debug.LogError($"[{nameof(AuthenticationHandler)}] GoogleButton is not assigned!", this);
            if (anonymousButton == null) Debug.LogError($"[{nameof(AuthenticationHandler)}] AnonymousButton is not assigned!", this);
            if (touchToContinueButton == null) Debug.LogError($"[{nameof(AuthenticationHandler)}] TouchToContinueButton is not assigned!", this);
            if (logoutButton == null) Debug.LogError($"[{nameof(AuthenticationHandler)}] LogoutButton is not assigned!", this);
            if (errorHandler == null) Debug.LogError($"[{nameof(AuthenticationHandler)}] ErrorHandler is not assigned!", this);
        }

        private async void HandleRemoteDataFetched() {
            try {
                await UnityServices.InitializeAsync();
                InitializeFirebase();
#if UNITY_ANDROID
                InitializeGooglePlayGames();
#endif
                SetupButtonListeners();
            }
            catch (Exception e) {
                Debug.LogError($"[{nameof(AuthenticationHandler)}] Initialization failed: {e.Message}");
                errorHandler?.SetError(new ErrorOptions { ErrorDescription = "Initialization failed. Please restart.", CanCloseError = false });
            }
        }

        private void InitializeFirebase() {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
                if (task.Result == DependencyStatus.Available) {
                    try {
                        App = FirebaseApp.DefaultInstance;
                        _auth = FirebaseAuth.DefaultInstance;
                        _initialized = true;
                        _auth.StateChanged += HandleAuthStateChanged;
                    }
                    catch (Exception ex) {
                        HandleFirebaseInitializationError(ex);
                    }
                }
                else {
                    HandleFirebaseDependencyError(task.Result);
                }
            });
        }

#if UNITY_ANDROID
        private void InitializeGooglePlayGames() {
            try {
                // PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
                //        .RequestServerAuthCode(false)
                //        .Build();
                // PlayGamesPlatform.InitializeInstance(config);
                // PlayGamesPlatform.Activate();
            }
            catch (Exception e) {
                HandleGooglePlayGamesInitializationError(e);
            }
        }
#endif

        private void SetupButtonListeners() {
#if UNITY_ANDROID
            googleButton.onClick.RemoveAllListeners();
            googleButton.onClick.AddListener(GoogleButtonClicked);
#endif
            anonymousButton.onClick.RemoveAllListeners();
            anonymousButton.onClick.AddListener(AnonymousButtonClicked);
            
            touchToContinueButton.onClick.RemoveAllListeners();
            touchToContinueButton.onClick.AddListener(TouchToContinueClicked);
            
            logoutButton.onClick.RemoveAllListeners();
            logoutButton.onClick.AddListener(LogoutClicked);
        }
        #endregion

        #region Authentication Logic
        private void HandleAuthStateChanged(object sender, EventArgs e) {
            UpdateUIState(_auth.CurrentUser != null);
            if (_auth.CurrentUser != null) {
                User = _auth.CurrentUser;
            }
        }

        private void UpdateUIState(bool isAuthenticated) {
            if (isAuthenticated) {
                loginBoxContainer.SetActive(false);
                logoutButton.gameObject.SetActive(true);
                touchToContinueButton.gameObject.SetActive(true);
            }
            else {
                loginBoxContainer.SetActive(true);
                logoutButton.gameObject.SetActive(false);
                touchToContinueButton.gameObject.SetActive(false);
                
                googleButton.interactable = true;
                anonymousButton.interactable = true;
            }
        }

        private void LogoutClicked() => Logout();

        private void AnonymousButtonClicked() {
            anonymousButton.interactable = false;
            SignInFirebaseAnonymously();
        }

        private async void TouchToContinueClicked() {
            if (User == null) {
                UiManager.HandleException(new Exception("Error signing in, please restart the game"));
                return;
            }

            touchToContinueButton.interactable = false; // Prevent double clicks

            try {
                var token = await User.TokenAsync(false);
                AuthenticateUser(token);
                OnFirebaseSignedIn?.Invoke(User.UserId, token);
            }
            catch (Exception ex) {
                Debug.LogError($"[{nameof(AuthenticationHandler)}] Failed to get token: {ex.Message}");
                touchToContinueButton.interactable = true;
            }
        }

        public async void SignInFirebaseAnonymously() {
            try {
                var user = await FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync();
                // Ensure we can get a token to validate the session
                await user.User.TokenAsync(true);
            }
            catch (Exception ex) {
                Debug.LogError($"[{nameof(AuthenticationHandler)}] Anonymous sign in failed: {ex.Message}");
                await HandleAnonymousSignInError();
                anonymousButton.interactable = true;
            }
        }

        [Button("Sign Out")]
        public void Logout() {
            if (!_initialized) {
                Debug.LogError($"[{nameof(AuthenticationHandler)}] Firebase not ready");
                return;
            }

            DisconnectWebSocket();
            _auth.SignOut();

#if UNITY_ANDROID
            if (PlayGamesPlatform.Instance.IsAuthenticated()) {
                // PlayGamesPlatform.Instance.SignOut();
            }
#endif
            UpdateUIState(false);
        }
        #endregion

        #region Google Play Games (Android)
#if UNITY_ANDROID
        private void GoogleButtonClicked() {
            googleButton.interactable = false;

            if (PlayGamesPlatform.Instance.localUser.authenticated) {
                RefreshGooglePlayTokens();
            }
            else {
                AuthenticateWithGooglePlay();
            }
        }

        private void RefreshGooglePlayTokens() {
            // Implementation for refreshing tokens
        }

        private void AuthenticateWithGooglePlay() {
            // Implementation for authenticating with Google Play
        }

        private async void SignInWithGoogleCredential(Credential credential) {
            try {
                var result = await _auth.SignInWithCredentialAsync(credential);
                await result.TokenAsync(false);
            }
            catch (Exception ex) {
                HandleGoogleSignInError(ex);
            }
        }
#endif
        #endregion

        #region Error Handling
        private void HandleFirebaseInitializationError(Exception ex) {
            errorHandler?.SetError(new ErrorOptions { 
                ErrorTitle = "Authentication Error", 
                ErrorDescription = $"Couldn't create authentication session: {ex.Message}", 
                CanCloseError = false 
            });
        }

        private void HandleFirebaseDependencyError(DependencyStatus status) {
            errorHandler?.SetError(new ErrorOptions {
                ErrorTitle = "Dependency Error",
                ErrorDescription = $"Firebase dependencies not available: {status}",
                CanCloseError = false,
                SubmitButtonText = "Try again",
                ShowSubmitButton = true,
                SubmitButtonCallback = () => {
                    errorHandler?.HideError();
                    InitializeFirebase();
                }
            });
        }

        private async Task HandleAnonymousSignInError() {
            await errorHandler?.SetErrorAsync(new ErrorOptions {
                ErrorTitle       = "Login Error",
                ErrorDescription = "Failed to authenticate anonymously.",
                CanCloseError    = false,
                SubmitButtonText = "Try again",
                ShowSubmitButton = true,
                SubmitButtonCallback = () => {
                    errorHandler?.HideError();
                    SignInFirebaseAnonymously();
                }
            })!;
        }

#if UNITY_ANDROID
        private void HandleGooglePlayGamesInitializationError(Exception e) {
            errorHandler?.SetError(new ErrorOptions {
                ErrorTitle = "Initialization Error",
                ErrorDescription = $"Failed to initialize Google Play Games: {e.Message}",
                CanCloseError = false,
                SubmitButtonText = "Retry",
                ShowSubmitButton = true,
                SubmitButtonCallback = () => {
                    errorHandler?.HideError();
                    InitializeGooglePlayGames();
                }
            });
        }

        private void HandleGoogleSignInError(Exception ex) {
            errorHandler?.SetError(new ErrorOptions {
                ErrorTitle = "Sign-In Error",
                ErrorDescription = $"Error signing in with Google: {ex.Message}",
                CanCloseError = true,
                SubmitButtonText = "OK",
                ShowSubmitButton = true,
                SubmitButtonCallback = () => googleButton.interactable = true
            });
        }
#endif
        #endregion

        #region API & WebSocket
        private void AuthenticateUser(string token) {
            var request = HTTPRequest.CreatePost(_authUrl, OnAuthRequestFinished);
            
            request.UploadSettings.UploadStream = new JSonDataStream<APIAuthRequest>(new APIAuthRequest { id_token = token });
            request.SetHeader(ApiKeyHeader, _apiKey);
            
            request.Send();
        }

        private void OnAuthRequestFinished(HTTPRequest request, HTTPResponse response) {
            touchToContinueButton.interactable = true; // Re-enable button

            switch (request.State) {
                case HTTPRequestStates.Finished:
                    if (response.IsSuccess) {
                        try {
                            APIAuthResponse res = JsonUtility.FromJson<APIAuthResponse>(response.DataAsText);
                            Debug.Log($"[{nameof(AuthenticationHandler)}] Auth Success. Account exists: {res.accountexist}");
                            
                            ConnectToWebSocket(res.ws_auth_key);
                        }
                        catch (Exception e) {
                            Debug.LogError($"[{nameof(AuthenticationHandler)}] Failed to parse auth response: {e.Message}");
                        }
                    }
                    else {
                        Debug.LogError($"[{nameof(AuthenticationHandler)}] Auth request failed: {response.StatusCode} {response.Message}");
                        errorHandler?.SetError(new ErrorOptions { ErrorDescription = "Server authentication failed.", CanCloseError = true });
                    }
                    break;
                default:
                    Debug.LogError($"[{nameof(AuthenticationHandler)}] Auth request error state: {request.State}");
                    errorHandler?.SetError(new ErrorOptions { ErrorDescription = "Connection error.", CanCloseError = true });
                    break;
            }
        }

        private void ConnectToWebSocket(string wsAuthKey) {
            DisconnectWebSocket(); // Ensure previous connection is closed

            try {
                string url = string.Format(_wsUrlFormat, wsAuthKey);
                _webSocket = new WebSocket(new Uri(url));

                // Add the custom header to the internal HTTP request for WebSocket handshake
                _webSocket.OnInternalRequestCreated = (ws, internalRequest) => { 
                    internalRequest.SetHeader(ApiKeyHeader, _apiKey); 
                };

                _webSocket.OnOpen += OnWebSocketOpen;
                _webSocket.OnMessage += OnMessageReceived;
                _webSocket.OnClosed += OnWebSocketClosed;
               
                
                _webSocket.Open();
            }
            catch (Exception e) {
                Debug.LogError($"[{nameof(AuthenticationHandler)}] Failed to create WebSocket connection: {e.Message}");
            }
        }

        private void DisconnectWebSocket() {
            if (_webSocket != null) {
                _webSocket.OnOpen -= OnWebSocketOpen;
                _webSocket.OnMessage -= OnMessageReceived;
                _webSocket.OnClosed -= OnWebSocketClosed;
                
                _webSocket.Close();
                _webSocket = null;
            }
        }

        private void OnWebSocketOpen(WebSocket webSocket) {
            Debug.Log($"[{nameof(AuthenticationHandler)}] WebSocket Connected!");
        }

        private void OnMessageReceived(WebSocket webSocket, string message) {
            Debug.Log($"[{nameof(AuthenticationHandler)}] WS Message: {message}");
        }

        private void OnWebSocketClosed(WebSocket webSocket, WebSocketStatusCodes code, string message) {
            Debug.Log($"[{nameof(AuthenticationHandler)}] WebSocket Closed: {code} - {message}");
        }

        private void OnWebSocketError(WebSocket webSocket, Exception ex) {
            Debug.LogError($"[{nameof(AuthenticationHandler)}] WebSocket Error: {ex?.Message}");
        }
        #endregion
    }
}