using System;
using System.Collections;
using System.Collections.Generic;
using _Scripts.Managers.Singeltons;
using Core.Managers;
using core.ui;
using DG.Tweening;
using QFSW.QC;
using Sirenix.OdinInspector;
using TMPro;
using UI_;
using UnityEngine;
using UnityEngine.UI;

namespace core.Managers {
    /// <summary>
    /// UiManager is a singleton class that manages all UI elements in the game.
    /// It handles notifications, confirmations, vehicle controls, and various UI pages.
    /// </summary>
    public class UiManager : Singleton<UiManager> {
        #region Variables

        public CanvasGroupToggler WorldCanvas;
        public CanvasGroupToggler MainCanvas;

        #region Error Handler

        [BoxGroup("Error Handler")] public ErrorHandler errorHandler;

        #endregion

        #region Pages

        [FoldoutGroup("Pages")] [SerializeField] private CanvasGroupToggler gameControllerPage;
        [FoldoutGroup("Pages")] [SerializeField] private CanvasGroupToggler vehicleControllerPage;
        [FoldoutGroup("Pages")]                  public  CanvasGroupToggler RecconectPage;

        #endregion

        #region Settings

        [FoldoutGroup("Settings", expanded: false)] [SerializeField] private CanvasGroupToggler SettingsPage;
        [FoldoutGroup("Settings/QualityPresets")] [SerializeField]   private cButton            bt_preset_Verylow;
        [FoldoutGroup("Settings/QualityPresets")] [SerializeField]   private cButton            bt_preset_low;
        [FoldoutGroup("Settings/QualityPresets")] [SerializeField]   private cButton            bt_preset_medium;
        [FoldoutGroup("Settings/QualityPresets")] [SerializeField]   private cButton            bt_preset_high;

        // anti aliasing

        [FoldoutGroup("Settings/Anti Aliasing")] [SerializeField] private cButton bt_antiAliasing_off;
        [FoldoutGroup("Settings/Anti Aliasing")] [SerializeField] private cButton bt_antiAliasing_x2;
        [FoldoutGroup("Settings/Anti Aliasing")] [SerializeField] private cButton bt_antiAliasing_x4;
        [FoldoutGroup("Settings/Anti Aliasing")] [SerializeField] private cButton bt_antiAliasing_x8;

        //shadow quality

        [FoldoutGroup("Settings/ShadowQuality")] [SerializeField] private cButton bt_shadowQuality_off;
        [FoldoutGroup("Settings/ShadowQuality")] [SerializeField] private cButton bt_shadowQuality_veryLow;
        [FoldoutGroup("Settings/ShadowQuality")] [SerializeField] private cButton bt_shadowQuality_low;
        [FoldoutGroup("Settings/ShadowQuality")] [SerializeField] private cButton bt_shadowQuality_medium;
        [FoldoutGroup("Settings/ShadowQuality")] [SerializeField] private cButton bt_shadowQuality_high;

        #endregion

        #region Camera

        [FoldoutGroup("Camera")] public UiDrag uiDrag;

        #endregion

        #region Controller

        [FoldoutGroup("Controller")] [FoldoutGroup("Controller/Player")] public VariableJoystick joystick;
        [FoldoutGroup("Controller/Player")]                              public Button           PlayerJump;
        [FoldoutGroup("Controller/Vehicle")]                             public Button           cardoorEnterDriver;
        [FoldoutGroup("Controller/Vehicle")]                             public Button           cardoorEnterPassanger;
        [FoldoutGroup("Controller/Vehicle")]                             public Button           cardoorExit;
        [FoldoutGroup("Controller/Vehicle")]                             public GameObject       driverControlButtons;

        #endregion

        #region Vehicle Control

        [FoldoutGroup("Vehicle Control", expanded: false)] public RCC_UI_Controller gasButton;
        [FoldoutGroup("Vehicle Control", expanded: false)] public RCC_UI_Controller brakeButton;
        [FoldoutGroup("Vehicle Control", expanded: false)] public RCC_UI_Controller leftButton;
        [FoldoutGroup("Vehicle Control", expanded: false)] public RCC_UI_Controller rightButton;

        //gas and speed

        [FoldoutGroup("Vehicle Control/Vehicle info", expanded: false)] [GUIColor("green")] [SerializeField] private Image gasStationIcon;

        [FoldoutGroup("Vehicle Control/Vehicle info", expanded: false)] [GUIColor("green")] [SerializeField] private Image fuelProgressBar;

        [FoldoutGroup("Vehicle Control/Vehicle info", expanded: false)] [GUIColor("green")] [SerializeField] private TMP_Text vehicleSpeedText;

        // timer to update vehicle speed and fuel
        private float FuelAndGasUpdatetimer;

        #endregion

        #region Notification

        public enum NotificationType {
            Info,
            Warning,
            Error,
            Success
        }

        private struct NotificationPayload {
            public NotificationType type;
            public string           message;
        }

        [FoldoutGroup("Top Notification")] [SerializeField] private TMP_Text                   notificationText;
        [FoldoutGroup("Top Notification")] [SerializeField] private GameObject                 notificationBox;
        private                                                     Queue<NotificationPayload> notificationQueue;
        private                                                     bool                       isNotificationActive;
        [FoldoutGroup("Top Notification")] [SerializeField] private Image                      GradientBackground;
        [FoldoutGroup("Top Notification")] [SerializeField] private Image                      IconToReplace;

        [FoldoutGroup("Top Notification/Icons")] [SerializeField] private Sprite iconInfo;
        [FoldoutGroup("Top Notification/Icons")] [SerializeField] private Sprite iconWarning;
        [FoldoutGroup("Top Notification/Icons")] [SerializeField] private Sprite iconError;

        #endregion

        #region Confirmation Box

        [Required] [SerializeField] private CanvasGroupToggler _confirmBox_Canvas;
        [SerializeField]            private TMP_Text           _conforimBox_Question;
        [SerializeField]            private Button             _confirmBox_OK;
        [SerializeField]            private Button             _confirmBox_NO;
        private                             Queue<string>      _confirmBox_que = new();

        #endregion

        #region Ban Page

        [SerializeField] private CanvasGroupToggler banpage;
        [SerializeField] public  CanvasGroupToggler loginpage;
        [SerializeField] private TMP_Text           bandetails;

        #endregion

        #region Events

        public event Action OnCardoorDriverButtonClicked;
        public event Action OnCardoorPassangerButtonClicked;
        public event Action OnCardoorExitButtonClicked;

        public event Action OnJumpPressed;

        #endregion

        #endregion

        #region Unity Methods

        private void Awake() {
            InitializeUI();
            //  SetupEventListeners();
            // ListenForGraphicsSettings();
        }

        #endregion

        #region Public Methods

        private void Update() {
            UpdatePlayerCarStatus();
        }

        private void UpdatePlayerCarStatus() {
            if (GameManager.Instance.character is null) return;
            if (GameManager.Instance.character.playerCar is null) return;

            FuelAndGasUpdatetimer += Time.deltaTime;
            if (FuelAndGasUpdatetimer >= 0.5f) {
                // Update fuel and gas here
                UpdateVehicleStatus(GameManager.Instance.character.playerCar.controllerV4);
                FuelAndGasUpdatetimer = 0f; // Reset the timer
            }
        }

        #region Input

        /// <summary>
        /// Gets input from a specified UI controller button.
        /// </summary>
        /// <param name="button">The RCC_UIController button to get input from.</param>
        /// <returns>The input value from the button, or 0 if the button is null.</returns>
        public float GetInput(RCC_UI_Controller button) {
            return button != null
                       ? button.input
                       : 0f;
        }

        #endregion

        #region Exception Handling

        /// <summary>
        /// Handles exceptions by logging them (TODO: Implement proper exception handling).
        /// </summary>
        /// <param name="e">The exception to handle.</param>
        public static void HandleException(Exception e) {
            Debug.Log($"TODO: Implement proper exception handling: {e}");
        }

        #endregion

        #region Notifications

        /// <summary>
        /// Sends a top notification with a specified message, type, and duration.
        /// </summary>
        /// <param name="notificationMessage">The message to display in the notification.</param>
        /// <param name="type">The type of notification (default is Info).</param>
        /// <param name="duration">The duration of the notification in seconds (default is 3 seconds).</param>
        [Command("no" + "tif")]
        public void SendTopNotification(string notificationMessage, NotificationType type = NotificationType.Info, float duration = 3f) {
            NotificationPayload payload = new NotificationPayload { type = type, message = notificationMessage };

            if (notificationQueue.Contains(payload)) return;

            if (isNotificationActive) {
                notificationQueue.Enqueue(payload);
            }
            else {
                ShowNotification(payload, duration);
            }
        }

        #endregion

        #region Confirmation

        /// <summary>
        /// Displays a confirmation dialog with a question and optional callbacks for OK and NO buttons.
        /// </summary>
        /// <param name="question">The question to display in the confirmation dialog.</param>
        /// <param name="OkClicked">Action to perform when OK button is clicked (optional).</param>
        /// <param name="NoClicked">Action to perform when NO button is clicked (optional).</param>
        public void AskForConfirmation(string question, Action OkClicked = null, Action NoClicked = null) {
            _conforimBox_Question.text = question;
            _confirmBox_Canvas.Enable(true);

            _confirmBox_OK.onClick.RemoveAllListeners();
            _confirmBox_NO.onClick.RemoveAllListeners();

            if (OkClicked != null) {
                _confirmBox_OK.onClick.AddListener(() => OkClicked());
            }

            if (NoClicked != null) {
                _confirmBox_NO.onClick.AddListener(() => NoClicked());
            }
        }

        /// <summary>
        /// Hides the confirmation dialog.
        /// </summary>
        public void HideConfirmationBox() => _confirmBox_Canvas.Disable(true);

        #endregion

        #region Ban Management

        /// <summary>
        /// Sets the ban message and displays the ban page.
        /// </summary>
        /// <param name="text">The ban message to display.</param>
        public void SetBan(string text) {
            bandetails.text = text;
            banpage.Enable();
        }

        #endregion

        #region Vehicle Controls

        /// <summary>
        /// Shows or hides the vehicle enter/exit buttons based on seat availability.
        /// </summary>
        /// <param name="show">Whether to show or hide the buttons.</param>
        /// <param name="DriverSeatFree">Whether the driver seat is free.</param>
        /// <param name="PassangerSeatFree">Whether the passenger seat is free.</param>
        public void ShowVehicleEnterExitButtons(bool show, bool DriverSeatFree, bool PassangerSeatFree) {
            cardoorEnterDriver.gameObject.SetActive(show && !DriverSeatFree);
            cardoorEnterPassanger.gameObject.SetActive(show && PassangerSeatFree);
        }

        /// <summary>
        /// Shows the game controller page and hides the vehicle controller page.
        /// </summary>
        public void ShowControllerPage() {
            gameControllerPage.Enable();
            vehicleControllerPage.Disable();
            driverControlButtons.gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows the vehicle controller page and optionally the driver control buttons.
        /// </summary>
        /// <param name="driver">Whether the player is the driver.</param>
        public void ShowVehicleControllerPage(bool driver) {
            driverControlButtons.gameObject.SetActive(driver);
            gameControllerPage.Disable();
            vehicleControllerPage.Enable();
        }

        public void UpdateVehicleStatus(RCC_CarControllerV4 car) {
            if (car != null) {
                var fuelTankCapacity = car.fuelTankCapacity;
                var fuelTankLevel    = car.fuelTank;
                fuelTankLevel              = Mathf.Clamp(fuelTankLevel, 0f, fuelTankCapacity);
                fuelProgressBar.fillAmount = fuelTankLevel / fuelTankCapacity;
                vehicleSpeedText.text      = $"{car.speed:F0} km/h";


                switch (fuelTankLevel) {
                    case <= 0.1f:
                        gasStationIcon.color = new Color(1f, 0.25f, 0.27f);
                        break;
                    case <= 0.2f:
                        gasStationIcon.color = new Color(1f, 0.25f, 0.27f);
                        break;
                    case <= 0.3f:
                        gasStationIcon.color = Color.yellow;
                        break;
                    case <= 0.4f:
                        gasStationIcon.color = Color.yellow;
                        break;
                    case <= 0.5f:
                        gasStationIcon.color = Color.yellow;
                        break;
                    case <= 0.6f:
                        gasStationIcon.color = Color.yellow;
                        break;
                    case <= 0.7f:
                        gasStationIcon.color = new Color(0.44f, 1f, 0.48f);
                        break;
                    case <= 0.8f:
                        gasStationIcon.color = new Color(0.44f, 1f, 0.48f);
                        break;
                    case <= 0.9f:
                        gasStationIcon.color = new Color(0.44f, 1f, 0.48f);
                        break;
                }
            }
        }

        #endregion

        #region Settings

        public void ListenForGraphicsSettings() {
            //presets
            bt_preset_Verylow.Clicked += () => { GraphicManager.Instance.SetOverallQualityLevel(OverallQualityLevel.VeryLow); };
            bt_preset_low.Clicked     += () => { GraphicManager.Instance.SetOverallQualityLevel(OverallQualityLevel.Low); };
            bt_preset_medium.Clicked  += () => { GraphicManager.Instance.SetOverallQualityLevel(OverallQualityLevel.Medium); };
            bt_preset_high.Clicked    += () => { GraphicManager.Instance.SetOverallQualityLevel(OverallQualityLevel.High); };


            //antiAliasing
            bt_antiAliasing_off.Clicked += () => { GraphicManager.Instance.SetAntiAliasing(AntiAliasingLevel.None); };
            bt_antiAliasing_x2.Clicked  += () => { GraphicManager.Instance.SetAntiAliasing(AntiAliasingLevel.TwoX); };
            bt_antiAliasing_x4.Clicked  += () => { GraphicManager.Instance.SetAntiAliasing(AntiAliasingLevel.FourX); };
            bt_antiAliasing_x8.Clicked  += () => { GraphicManager.Instance.SetAntiAliasing(AntiAliasingLevel.EightX); };


            //shadow
            bt_shadowQuality_off.Clicked     += () => { GraphicManager.Instance.SetShadowQuality(ShadowQualityLevel.NoShadows); };
            bt_shadowQuality_veryLow.Clicked += () => { GraphicManager.Instance.SetShadowQuality(ShadowQualityLevel.VeryLow); };
            bt_shadowQuality_low.Clicked     += () => { GraphicManager.Instance.SetShadowQuality(ShadowQualityLevel.Low); };
            bt_shadowQuality_medium.Clicked  += () => { GraphicManager.Instance.SetShadowQuality(ShadowQualityLevel.Medium); };
            bt_shadowQuality_high.Clicked    += () => { GraphicManager.Instance.SetShadowQuality(ShadowQualityLevel.High); };


            UpdateGraphicsSettingsUI();
        }

        public void UpdateGraphicsSettingsUI() {
            bt_preset_Verylow.SetSelected(false);
            bt_preset_low.SetSelected(false);
            bt_preset_medium.SetSelected(false);
            bt_preset_high.SetSelected(false);

            bt_antiAliasing_off.SetSelected(false);
            bt_antiAliasing_x2.SetSelected(false);
            bt_antiAliasing_x4.SetSelected(false);
            bt_antiAliasing_x8.SetSelected(false);

            bt_shadowQuality_off.SetSelected(false);
            bt_shadowQuality_veryLow.SetSelected(false);
            bt_shadowQuality_low.SetSelected(false);
            bt_shadowQuality_medium.SetSelected(false);
            bt_shadowQuality_high.SetSelected(false);


            switch (GraphicManager.Instance.GetOverallQualityLevel()) {
                case OverallQualityLevel.VeryLow:
                    bt_preset_Verylow.SetSelected(true);
                    break;
                case OverallQualityLevel.Low:
                    bt_preset_low.SetSelected(true);
                    break;
                case OverallQualityLevel.Medium:
                    bt_preset_medium.SetSelected(true);
                    break;
                case OverallQualityLevel.High:
                    bt_preset_high.SetSelected(true);
                    break;
            }


            switch (GraphicManager.Instance.GetAntiAliasing()) {
                case 0:
                    bt_antiAliasing_off.SetSelected(true);
                    break;
                case 2:
                    bt_antiAliasing_x2.SetSelected(true);
                    break;
                case 4:
                    bt_antiAliasing_x4.SetSelected(true);
                    break;
                case 8:
                    bt_antiAliasing_x8.SetSelected(true);
                    break;
            }

            switch (GraphicManager.Instance.GetShadowQuality()) {
                case ShadowQualityLevel.NoShadows:
                    bt_shadowQuality_off.SetSelected(true);
                    break;
                case ShadowQualityLevel.VeryLow:
                    bt_shadowQuality_veryLow.SetSelected(true);
                    break;
                case ShadowQualityLevel.Low:
                    bt_shadowQuality_low.SetSelected(true);
                    break;
                case ShadowQualityLevel.Medium:
                    bt_shadowQuality_medium.SetSelected(true);
                    break;
                case ShadowQualityLevel.High:
                    bt_shadowQuality_high.SetSelected(true);
                    break;
            }
        }

        /// <summary>
        /// Shows the settings page.
        /// </summary>
        public void ShowSettings() {
            AudioManager.Instance.UI_Click();
            SettingsPage.Enable(true);
        }

        /// <summary>
        /// Hides the settings page.
        /// </summary>
        public void HideSettings() {
            
            AudioManager.Instance.UI_Return();
            SettingsPage.Disable();
        }

        private bool showGraphicsSettings;

        #endregion

        #endregion

        #region Private Methods

        #region Initialization

        /// <summary>
        /// Initializes the UI components and sets up initial state.
        /// </summary>
        private void InitializeUI() {
            ResetLayout();
            notificationQueue    = new Queue<NotificationPayload>();
            isNotificationActive = false;
        }

        /// <summary>
        /// Sets up event listeners for various UI elements and client connections.
        /// </summary>
        public void SetupEventListeners() {
            cardoorEnterDriver.onClick.AddListener(() => OnCardoorDriverButtonClicked?.Invoke());
            cardoorEnterPassanger.onClick.AddListener(() => OnCardoorPassangerButtonClicked?.Invoke());
            cardoorExit.onClick.AddListener(() => OnCardoorExitButtonClicked?.Invoke());

            PlayerJump.onClick.AddListener(() => OnJumpPressed?.Invoke());
            if (ClientManager.Instance != null) {
                ClientManager.OnClientDisconnected    += () => RecconectPage.Enable(true);
                ClientManager.OnClientReconnected     += () => RecconectPage.Disable(true);
                ClientManager.OnClientReconnectFailed += () => throw new Exception("Failed to reconnect to server. Go back to preview screen?");
            }
        }

        /// <summary>
        /// Resets the layout of the UI to its initial state.
        /// </summary>
        private void ResetLayout() {
            cardoorEnterDriver.gameObject.SetActive(false);
            cardoorEnterPassanger.gameObject.SetActive(false);
            if (banpage.Active) banpage.Disable();

            bandetails.text = "";
            notificationBox.SetActive(false);
        }

        #endregion

        #region Notification Handling

        /// <summary>
        /// Shows a notification with the specified payload and duration.
        /// </summary>
        /// <param name="payload">The notification payload containing type and message.</param>
        /// <param name="duration">The duration of the notification in seconds.</param>
        private void ShowNotification(NotificationPayload payload, float duration = 3f) {
            SetNotificationStyle(payload.type);
            AnimateNotification(payload.message);
            StartCoroutine(HideNotificationAfterDelay(duration));
        }

        /// <summary>
        /// Sets the visual style of the notification based on its type.
        /// </summary>
        /// <param name="type">The type of notification.</param>
        private void SetNotificationStyle(NotificationType type) {
            switch (type) {
                case NotificationType.Info:
                    GradientBackground.color = new Color(0.16f, 0.16f, 0.16f, 0.5f);
                    IconToReplace.sprite     = iconInfo;
                    IconToReplace.color      = new Color(0.82f, 0.82f, 0.82f);
                    break;
                case NotificationType.Error:
                    GradientBackground.color = new Color(0.75f, 0.2f, 0.24f, 0.5f);
                    IconToReplace.sprite     = iconError;
                    IconToReplace.color      = new Color(0.75f, 0.2f, 0.24f);
                    break;
                case NotificationType.Warning:
                    GradientBackground.color = new Color(0.91f, 0.84f, 0.25f, 0.5f);
                    IconToReplace.sprite     = iconWarning;
                    IconToReplace.color      = new Color(0.91f, 0.84f, 0.25f);
                    break;
                case NotificationType.Success:
                    GradientBackground.color = new Color(0.47f, 0.91f, 0.33f, 0.5f);
                    IconToReplace.sprite     = iconInfo;
                    IconToReplace.color      = new Color(0.47f, 0.91f, 0.33f);
                    break;
            }
        }

        /// <summary>
        /// Animates the notification appearance and text.
        /// </summary>
        /// <param name="message">The message to display in the notification.</param>
        private void AnimateNotification(string message) {
            notificationBox.SetActive(true);
            isNotificationActive = true;

            notificationBox.transform.DOScaleX(1f, 0.2f)
                           .SetEase(Ease.OutBounce)
                           .onComplete += () => {
                IconToReplace.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), 0.2f)
                             .SetEase(Ease.Flash);
                notificationText.DOText(message, 0.2f, false, ScrambleMode.Uppercase);
            };
        }

        /// <summary>
        /// Coroutine to hide the notification after a specified delay and show the next queued notification if any.
        /// </summary>
        /// <param name="delay">The delay in seconds before hiding the notification.</param>
        private IEnumerator HideNotificationAfterDelay(float delay) {
            yield return new WaitForSeconds(delay);

            if (notificationQueue.Count > 0) {
                notificationBox.transform.DOScaleX(0f, 0f);
                NotificationPayload payload = notificationQueue.Dequeue();
                ShowNotification(payload);
            }
            else {
                notificationText.text = "";
                notificationBox.transform.DOScaleX(0f, 0.2f)
                               .SetEase(Ease.OutBounce)
                               .onComplete += () => {
                    notificationBox.SetActive(false);
                    isNotificationActive = false;
                };
            }
        }

        #endregion

        #endregion
    }
}