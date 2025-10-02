using System;
using System.Threading.Tasks;
using core.Managers;
using Firebase;

using Firebase.Auth;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using Managers;
using QFSW.QC;
using Sirenix.OdinInspector;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Managers
{
    public class AuthenticationHandler : MonoBehaviour
    {
        [SerializeField, FoldoutGroup("UI References")]
        private GameObject loginBoxContainer;

        [SerializeField, FoldoutGroup("UI References")]
        private Button googleButton;

        [SerializeField, FoldoutGroup("UI References")]
        private Button anonymousButton;

        [SerializeField, FoldoutGroup("UI References")]
        public Button touchToContinueButton;

        [SerializeField, FoldoutGroup("UI References")]
        private Button logoutButton;

        [SerializeField, BoxGroup("Error Handler")]
        public ErrorHandler errorHandler;

        public event Action<string, string> OnFirebaseSignedIn;

        public FirebaseApp App { get; private set; }
        private FirebaseAuth _auth;
        public FirebaseUser User { get; private set; }

        private bool _initialized;

        private void Awake()
        {
            logoutButton.gameObject.SetActive(false);
            loginBoxContainer.SetActive(false);
        }

        private async void Start()
        {
            await Task.Delay(2000); // Replace WaitForSeconds with Task.Delay

            RemoteData.Instance.OnRemoteDataFetchError += HandleRemoteDataFetchError;
            RemoteData.Instance.OnRemoteDataFetched += HandleRemoteDataFetched;
        }

        private void HandleRemoteDataFetchError()
        {
            errorHandler.SetError(new ErrorOptions
            {
                ErrorDescription = "Couldn't connect to server",
                CanCloseError = false,
                ShowSubmitButton = false
            });
        }

        private async void HandleRemoteDataFetched()
        {
            await UnityServices.InitializeAsync();
            InitializeFirebase();

#if UNITY_ANDROID
            InitializeGooglePlayGames();
#endif

            SetupButtonListeners();
        }

        private void InitializeFirebase()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    try
                    {
                        App = FirebaseApp.DefaultInstance;
                        _auth = FirebaseAuth.DefaultInstance;
                        _initialized = true;
                        _auth.StateChanged += HandleAuthStateChanged;
                    }
                    catch (Exception ex)
                    {
                        HandleFirebaseInitializationError(ex);
                    }
                }
                else
                {
                    HandleFirebaseDependencyError();
                }
            });
        }

#if UNITY_ANDROID
        private void InitializeGooglePlayGames()
        {
            try
            {
                PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
                    .RequestServerAuthCode(false)
                    .Build();
                PlayGamesPlatform.InitializeInstance(config);
                PlayGamesPlatform.Activate();
            }
            catch (Exception e)
            {
                HandleGooglePlayGamesInitializationError(e);
            }
        }
#endif

        private void SetupButtonListeners()
        {
#if UNITY_ANDROID
            googleButton.onClick.AddListener(GoogleButtonClicked);
#endif
            anonymousButton.onClick.AddListener(AnonymousButtonClicked);
            touchToContinueButton.onClick.AddListener(TouchToContinueClicked);
            logoutButton.onClick.AddListener(LogoutClicked);
        }

        private void HandleAuthStateChanged(object sender, EventArgs e)
        {
            if (_auth.CurrentUser != null)
            {
                loginBoxContainer.SetActive(false);
                logoutButton.gameObject.SetActive(true);
                User = _auth.CurrentUser;
                touchToContinueButton.gameObject.SetActive(true);
            }
            else
            {
                loginBoxContainer.SetActive(true);
                googleButton.interactable = true;
                anonymousButton.interactable = true;
                touchToContinueButton.gameObject.SetActive(false);
            }
        }

        private void LogoutClicked() => Logout();

        private void AnonymousButtonClicked()
        {
            SignInFirebaseAnonymously();
        }

        private async void TouchToContinueClicked()
        {
            if (User == null)
            {
                UiManager.HandleException(new Exception("Error signing in, please restart the game"));
                return;
            }

            touchToContinueButton.gameObject.SetActive(false);

            string token = await User.TokenAsync(true);
            OnFirebaseSignedIn?.Invoke(User.UserId, token);
        }

#if UNITY_ANDROID
        private void GoogleButtonClicked()
        {
            googleButton.interactable = false;

            if (PlayGamesPlatform.Instance.localUser.authenticated)
            {
                RefreshGooglePlayTokens();
            }
            else
            {
                AuthenticateWithGooglePlay();
            }
        }

        private void RefreshGooglePlayTokens()
        {
            var authCode = PlayGamesPlatform.Instance.GetServerAuthCode();
            Credential credential = PlayGamesAuthProvider.GetCredential(authCode);
            _auth.SignInWithCredentialAsync(credential);
        }

        private void AuthenticateWithGooglePlay()
        {
            PlayGamesPlatform.Instance.Authenticate(success =>
            {
                if (success)
                {
                    var authCode = PlayGamesPlatform.Instance.GetServerAuthCode();
                    Credential credential = PlayGamesAuthProvider.GetCredential(authCode);
                    SignInWithGoogleCredential(credential);
                }
                else
                {
                    HandleGooglePlayAuthenticationFailure();
                }
            });
        }

        private async void SignInWithGoogleCredential(Credential credential)
        {
            try
            {
                var result = await _auth.SignInWithCredentialAsync(credential);
                var token = await result.TokenAsync(false);
                // Handle successful sign-in
            }
            catch (Exception ex)
            {
                HandleGoogleSignInError(ex);
            }
        }
#endif

        // public async void SignInFirebaseAnonymously()
        // {
        //     try
        //     {
        //         
        //         var user = await FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync();
        //         var token = await user.TokenAsync(false);
        //         anonymousButton.interactable = false;
        //     }
        //     catch (Exception)
        //     {
        //         await HandleAnonymousSignInError();
        //     }
        // }

        public async void SignInFirebaseAnonymously() {
            try {
                // SignInAnonymouslyAsync now returns an AuthResult object.
                AuthResult authResult = await FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync();

                // The FirebaseUser is available through the AuthResult.
                FirebaseUser user = authResult.User;

                // Call TokenAsync() without the boolean parameter.
                // The SDK will handle refreshing the token when necessary.
                string token = await user.TokenAsync(false);

                Debug.Log($"Anonymous user signed in successfully: {user.UserId}");
                Debug.Log($"ID Token: {token}");

                // Disable the button after successful sign-in
                if (anonymousButton != null) {
                    anonymousButton.interactable = false;
                }
            }
            catch (Exception ex) {
                Debug.LogError($"Anonymous sign-in failed: {ex.Message}");
                await HandleAnonymousSignInError();
            }
        }
        
        [Command("signout")]
        [Button("signout")]
        public void Logout()
        {
            if (!_initialized)
            {
                Debug.LogError("Firebase not ready");
                return;
            }

            _auth.SignOut();

#if UNITY_ANDROID
            if (PlayGamesPlatform.Instance.IsAuthenticated())
            {
                PlayGamesPlatform.Instance.SignOut();
            }
#endif
        }

        // Error handling methods
        private void HandleFirebaseInitializationError(Exception ex)
        {
            errorHandler.SetError(new ErrorOptions
            {
                ErrorTitle = "Authentication",
                ErrorDescription = $"Couldn't create authentication session: {ex.Message}",
                CanCloseError = false,
            });
        }

        private void HandleFirebaseDependencyError()
        {
            errorHandler.SetError(new ErrorOptions
            {
                ErrorTitle = "Error",
                ErrorDescription = "Can't connect to server",
                CanCloseError = false,
                SubmitButtonText = "Try again",
                ShowSubmitButton = true,
                SubmitButtonCallback = () =>
                {
                    errorHandler.HideError();
                    InitializeFirebase();
                }
            });
        }

#if UNITY_ANDROID
        private void HandleGooglePlayGamesInitializationError(Exception e)
        {
            errorHandler.SetError(new ErrorOptions
            {
                ErrorTitle = "Error",
                ErrorDescription = $"Failed to initialize Google Play Games: {e.Message}",
                CanCloseError = false,
                SubmitButtonText = "Try again",
                ShowSubmitButton = true,
                SubmitButtonCallback = () =>
                {
                    errorHandler.HideError();
                    InitializeGooglePlayGames();
                }
            });
        }

        private void HandleGooglePlayAuthenticationFailure()
        {
            errorHandler.SetError(new ErrorOptions
            {
                ErrorTitle = "Authentication Failed",
                ErrorDescription = "Failed to authenticate with Google Play Games",
                CanCloseError = true,
                SubmitButtonText = "OK",
                ShowSubmitButton = true,
                SubmitButtonCallback = () => googleButton.interactable = true
            });
        }

        private void HandleGoogleSignInError(Exception ex)
        {
            errorHandler.SetError(new ErrorOptions
            {
                ErrorTitle = "Sign-In Error",
                ErrorDescription = $"Error signing in with Google: {ex.Message}",
                CanCloseError = true,
                SubmitButtonText = "OK",
                ShowSubmitButton = true,
                SubmitButtonCallback = () => googleButton.interactable = true
            });
        }
#endif

        private async Task HandleAnonymousSignInError()
        {
            await errorHandler.SetErrorAsync(new ErrorOptions
            {
                ErrorTitle = "Error",
                ErrorDescription = "Error Authenticating",
                CanCloseError = false,
                SubmitButtonText = "Try again",
                ShowSubmitButton = true,
                SubmitButtonCallback = () =>
                {
                    errorHandler.HideError();
                    SignInFirebaseAnonymously();
                }
            });
        }
    }
}