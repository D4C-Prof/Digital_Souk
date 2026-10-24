using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Cursor = UnityEngine.Cursor;

namespace DigitalSouk.UI
{
    /// <summary>
    /// Master UI Toolkit controller for Digital Souk.
    /// Manages Start Menu, Pause Menu, In-game HUD, Settings, and Controls overlays.
    /// Supports both Mouse and Keyboard/Gamepad arrow navigation.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class GameMenuManager : MonoBehaviour
    {
        public enum UIState
        {
            StartMenu,
            Playing,
            Paused,
            Settings,
            Controls
        }

        [Header("Configuration")]
        [Tooltip("If true, the game begins at the Start Menu. If false, drops directly into gameplay.")]
        [SerializeField] private bool startAtMainMenu = true;

        [Header("Gameplay Script References (Auto-found if null)")]
        [SerializeField] private movement playerMovement;
        [SerializeField] private camFollow cameraFollow;

        // Current state tracking
        private UIState currentState = UIState.StartMenu;
        private UIState previousStateBeforeModal = UIState.StartMenu;

        // Keyboard & Gamepad Arrow Navigation
        private readonly List<VisualElement> currentNavList = new List<VisualElement>();
        private int currentNavIndex = 0;

        // UI Document references
        private UIDocument uiDocument;
        private VisualElement root;

        // Overlays
        private VisualElement startMenuOverlay;
        private VisualElement pauseMenuOverlay;
        private VisualElement settingsOverlay;
        private VisualElement controlsOverlay;
        private VisualElement hudOverlay;

        // Start Menu Elements
        private Button startPlayBtn;
        private Button startControlsBtn;
        private Button startSettingsBtn;
        private Button startQuitBtn;

        // Pause Menu Elements
        private Button pauseResumeBtn;
        private Button pauseRestartBtn;
        private Button pauseControlsBtn;
        private Button pauseSettingsBtn;
        private Button pauseMainMenuBtn;
        private Button pauseQuitBtn;

        // Settings Elements
        private Slider volumeSlider;
        private Label volumeLabel;
        private Slider sensitivitySlider;
        private Label sensitivityLabel;
        private Toggle invertYToggle;
        private Button settingsBackBtn;

        // Controls Elements
        private Button controlsBackBtn;

        // PlayerPrefs Keys
        private const string PrefVolume = "DigitalSouk_MasterVolume";
        private const string PrefSensitivity = "DigitalSouk_MouseSensitivity";
        private const string PrefInvertY = "DigitalSouk_InvertY";

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();

            // Auto-locate gameplay scripts if not explicitly assigned
            if (playerMovement == null)
            {
                playerMovement = Object.FindFirstObjectByType<movement>();
            }

            if (cameraFollow == null)
            {
                cameraFollow = Object.FindFirstObjectByType<camFollow>();
            }
        }

        private void OnEnable()
        {
            root = uiDocument.rootVisualElement;
            if (root == null)
            {
                Debug.LogWarning("[GameMenuManager] Root VisualElement is null. Please ensure a valid UXML is assigned to UIDocument.");
                return;
            }

            QueryUIElements();
            RegisterCallbacks();
            LoadSavedSettings();

            if (startAtMainMenu)
            {
                SetState(UIState.StartMenu);
            }
            else
            {
                SetState(UIState.Playing);
            }
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
        }

        private void Update()
        {
            HandleKeyboardShortcuts();
            HandleMenuNavigation();
        }

        private void QueryUIElements()
        {
            // Overlays
            startMenuOverlay = root.Q<VisualElement>("start-menu-overlay");
            pauseMenuOverlay = root.Q<VisualElement>("pause-menu-overlay");
            settingsOverlay = root.Q<VisualElement>("settings-overlay");
            controlsOverlay = root.Q<VisualElement>("controls-overlay");
            hudOverlay = root.Q<VisualElement>("hud-overlay");

            // Start Menu Buttons
            startPlayBtn = root.Q<Button>("start-btn-play");
            startControlsBtn = root.Q<Button>("start-btn-controls");
            startSettingsBtn = root.Q<Button>("start-btn-settings");
            startQuitBtn = root.Q<Button>("start-btn-quit");

            // Pause Menu Buttons
            pauseResumeBtn = root.Q<Button>("pause-btn-resume");
            pauseRestartBtn = root.Q<Button>("pause-btn-restart");
            pauseControlsBtn = root.Q<Button>("pause-btn-controls");
            pauseSettingsBtn = root.Q<Button>("pause-btn-settings");
            pauseMainMenuBtn = root.Q<Button>("pause-btn-mainmenu");
            pauseQuitBtn = root.Q<Button>("pause-btn-quit");

            // Settings Controls
            volumeSlider = root.Q<Slider>("audio-volume-slider");
            volumeLabel = root.Q<Label>("audio-volume-label");
            sensitivitySlider = root.Q<Slider>("sensitivity-slider");
            sensitivityLabel = root.Q<Label>("sensitivity-label");
            invertYToggle = root.Q<Toggle>("invert-y-toggle");
            settingsBackBtn = root.Q<Button>("settings-btn-back");

            // Controls Modal
            controlsBackBtn = root.Q<Button>("controls-btn-back");
        }

        private void RegisterCallbacks()
        {
            // Start Menu
            if (startPlayBtn != null) startPlayBtn.clicked += StartGame;
            if (startControlsBtn != null) startControlsBtn.clicked += () => OpenModal(UIState.Controls);
            if (startSettingsBtn != null) startSettingsBtn.clicked += () => OpenModal(UIState.Settings);
            if (startQuitBtn != null) startQuitBtn.clicked += QuitGame;

            // Pause Menu
            if (pauseResumeBtn != null) pauseResumeBtn.clicked += ResumeGame;
            if (pauseRestartBtn != null) pauseRestartBtn.clicked += RestartCurrentScene;
            if (pauseControlsBtn != null) pauseControlsBtn.clicked += () => OpenModal(UIState.Controls);
            if (pauseSettingsBtn != null) pauseSettingsBtn.clicked += () => OpenModal(UIState.Settings);
            if (pauseMainMenuBtn != null) pauseMainMenuBtn.clicked += ReturnToMainMenu;
            if (pauseQuitBtn != null) pauseQuitBtn.clicked += QuitGame;

            // Settings
            if (volumeSlider != null)
            {
                volumeSlider.RegisterValueChangedCallback(evt => OnVolumeChanged(evt.newValue));
            }

            if (sensitivitySlider != null)
            {
                sensitivitySlider.RegisterValueChangedCallback(evt => OnSensitivityChanged(evt.newValue));
            }

            if (invertYToggle != null)
            {
                invertYToggle.RegisterValueChangedCallback(evt => OnInvertYChanged(evt.newValue));
            }

            if (settingsBackBtn != null) settingsBackBtn.clicked += CloseModal;

            // Controls
            if (controlsBackBtn != null) controlsBackBtn.clicked += CloseModal;

            // Hover synchronization with keyboard focus
            RegisterHoverFocus(startPlayBtn);
            RegisterHoverFocus(startControlsBtn);
            RegisterHoverFocus(startSettingsBtn);
            RegisterHoverFocus(startQuitBtn);
            RegisterHoverFocus(pauseResumeBtn);
            RegisterHoverFocus(pauseRestartBtn);
            RegisterHoverFocus(pauseControlsBtn);
            RegisterHoverFocus(pauseSettingsBtn);
            RegisterHoverFocus(pauseMainMenuBtn);
            RegisterHoverFocus(pauseQuitBtn);
            RegisterHoverFocus(volumeSlider);
            RegisterHoverFocus(sensitivitySlider);
            RegisterHoverFocus(invertYToggle);
            RegisterHoverFocus(settingsBackBtn);
            RegisterHoverFocus(controlsBackBtn);
        }

        private void RegisterHoverFocus(VisualElement el)
        {
            if (el == null) return;
            el.RegisterCallback<PointerEnterEvent>(_ =>
            {
                int idx = currentNavList.IndexOf(el);
                if (idx >= 0)
                {
                    currentNavIndex = idx;
                    el.Focus();
                }
            });
        }

        private void UnregisterCallbacks()
        {
            if (startPlayBtn != null) startPlayBtn.clicked -= StartGame;
            if (pauseResumeBtn != null) pauseResumeBtn.clicked -= ResumeGame;
            if (pauseRestartBtn != null) pauseRestartBtn.clicked -= RestartCurrentScene;
            if (pauseMainMenuBtn != null) pauseMainMenuBtn.clicked -= ReturnToMainMenu;
            if (pauseQuitBtn != null) pauseQuitBtn.clicked -= QuitGame;
            if (startQuitBtn != null) startQuitBtn.clicked -= QuitGame;
            if (settingsBackBtn != null) settingsBackBtn.clicked -= CloseModal;
            if (controlsBackBtn != null) controlsBackBtn.clicked -= CloseModal;
        }

        private void HandleKeyboardShortcuts()
        {
            bool pausePressed = false;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                pausePressed = true;
            }

            if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            {
                pausePressed = true;
            }

            if (!pausePressed) return;

            switch (currentState)
            {
                case UIState.Playing:
                    PauseGame();
                    break;

                case UIState.Paused:
                    ResumeGame();
                    break;

                case UIState.Settings:
                case UIState.Controls:
                    CloseModal();
                    break;

                case UIState.StartMenu:
                    // In start menu, escape does nothing or stays in menu
                    break;
            }
        }

        public void SetState(UIState newState)
        {
            currentState = newState;

            // Hide all overlays initially
            SetElementVisible(startMenuOverlay, false);
            SetElementVisible(pauseMenuOverlay, false);
            SetElementVisible(settingsOverlay, false);
            SetElementVisible(controlsOverlay, false);
            SetElementVisible(hudOverlay, false);

            switch (currentState)
            {
                case UIState.StartMenu:
                    SetElementVisible(startMenuOverlay, true);
                    Time.timeScale = 1f; // allow idle animations if any
                    SetGameplayActive(false);
                    SetCursorUnlocked();
                    UpdateNavigationList(UIState.StartMenu);
                    break;

                case UIState.Playing:
                    SetElementVisible(hudOverlay, true);
                    Time.timeScale = 1f;
                    SetGameplayActive(true);
                    SetCursorLocked();
                    currentNavList.Clear();
                    break;

                case UIState.Paused:
                    SetElementVisible(pauseMenuOverlay, true);
                    Time.timeScale = 0f; // Freeze simulation
                    SetGameplayActive(false);
                    SetCursorUnlocked();
                    UpdateNavigationList(UIState.Paused);
                    break;

                case UIState.Settings:
                    SetElementVisible(settingsOverlay, true);
                    SetGameplayActive(false);
                    SetCursorUnlocked();
                    UpdateNavigationList(UIState.Settings);
                    break;

                case UIState.Controls:
                    SetElementVisible(controlsOverlay, true);
                    SetGameplayActive(false);
                    SetCursorUnlocked();
                    UpdateNavigationList(UIState.Controls);
                    break;
            }
        }

        private void SetElementVisible(VisualElement el, bool isVisible)
        {
            if (el == null) return;

            if (isVisible)
            {
                el.RemoveFromClassList("overlay-hidden");
                el.style.display = DisplayStyle.Flex;
                el.style.opacity = 1f;
            }
            else
            {
                el.AddToClassList("overlay-hidden");
                el.style.display = DisplayStyle.None;
                el.style.opacity = 0f;
            }
        }

        // --- ARROW KEY & GAMEPAD NAVIGATION ---

        private void UpdateNavigationList(UIState state)
        {
            currentNavList.Clear();

            switch (state)
            {
                case UIState.StartMenu:
                    if (startPlayBtn != null) currentNavList.Add(startPlayBtn);
                    if (startControlsBtn != null) currentNavList.Add(startControlsBtn);
                    if (startSettingsBtn != null) currentNavList.Add(startSettingsBtn);
                    if (startQuitBtn != null) currentNavList.Add(startQuitBtn);
                    break;

                case UIState.Paused:
                    if (pauseResumeBtn != null) currentNavList.Add(pauseResumeBtn);
                    if (pauseRestartBtn != null) currentNavList.Add(pauseRestartBtn);
                    if (pauseControlsBtn != null) currentNavList.Add(pauseControlsBtn);
                    if (pauseSettingsBtn != null) currentNavList.Add(pauseSettingsBtn);
                    if (pauseMainMenuBtn != null) currentNavList.Add(pauseMainMenuBtn);
                    if (pauseQuitBtn != null) currentNavList.Add(pauseQuitBtn);
                    break;

                case UIState.Settings:
                    if (volumeSlider != null) currentNavList.Add(volumeSlider);
                    if (sensitivitySlider != null) currentNavList.Add(sensitivitySlider);
                    if (invertYToggle != null) currentNavList.Add(invertYToggle);
                    if (settingsBackBtn != null) currentNavList.Add(settingsBackBtn);
                    break;

                case UIState.Controls:
                    if (controlsBackBtn != null) currentNavList.Add(controlsBackBtn);
                    break;
            }

            FocusNavElement(0);
        }

        private void FocusNavElement(int index)
        {
            if (currentNavList.Count == 0) return;
            currentNavIndex = Mathf.Clamp(index, 0, currentNavList.Count - 1);
            VisualElement el = currentNavList[currentNavIndex];
            if (el != null)
            {
                el.schedule.Execute(() => el.Focus());
            }
        }

        private void HandleMenuNavigation()
        {
            if (currentState == UIState.Playing || currentNavList.Count == 0) return;

            bool downPressed = false;
            bool upPressed = false;
            bool leftPressed = false;
            bool rightPressed = false;
            bool submitPressed = false;

            if (Keyboard.current != null)
            {
                downPressed = Keyboard.current.downArrowKey.wasPressedThisFrame ||
                              Keyboard.current.sKey.wasPressedThisFrame ||
                              Keyboard.current.tabKey.wasPressedThisFrame;

                upPressed = Keyboard.current.upArrowKey.wasPressedThisFrame ||
                            Keyboard.current.wKey.wasPressedThisFrame;

                leftPressed = Keyboard.current.leftArrowKey.wasPressedThisFrame ||
                             Keyboard.current.aKey.wasPressedThisFrame;

                rightPressed = Keyboard.current.rightArrowKey.wasPressedThisFrame ||
                              Keyboard.current.dKey.wasPressedThisFrame;

                submitPressed = Keyboard.current.enterKey.wasPressedThisFrame ||
                                Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                                Keyboard.current.spaceKey.wasPressedThisFrame;
            }

            if (Gamepad.current != null)
            {
                if (Gamepad.current.dpad.down.wasPressedThisFrame || Gamepad.current.leftStick.down.wasPressedThisFrame) downPressed = true;
                if (Gamepad.current.dpad.up.wasPressedThisFrame || Gamepad.current.leftStick.up.wasPressedThisFrame) upPressed = true;
                if (Gamepad.current.dpad.left.wasPressedThisFrame || Gamepad.current.leftStick.left.wasPressedThisFrame) leftPressed = true;
                if (Gamepad.current.dpad.right.wasPressedThisFrame || Gamepad.current.leftStick.right.wasPressedThisFrame) rightPressed = true;
                if (Gamepad.current.buttonSouth.wasPressedThisFrame) submitPressed = true;
            }

            // Down Navigation (cycle with wrapping)
            if (downPressed)
            {
                currentNavIndex = (currentNavIndex + 1) % currentNavList.Count;
                FocusNavElement(currentNavIndex);
                return;
            }

            // Up Navigation (cycle with wrapping)
            if (upPressed)
            {
                currentNavIndex = (currentNavIndex - 1 + currentNavList.Count) % currentNavList.Count;
                FocusNavElement(currentNavIndex);
                return;
            }

            // Horizontal / Value Adjustments (Sliders & Toggles)
            if (currentNavIndex >= 0 && currentNavIndex < currentNavList.Count)
            {
                VisualElement currentEl = currentNavList[currentNavIndex];

                if (currentEl is Slider slider)
                {
                    float step = (slider == volumeSlider) ? 5f : 0.2f;
                    if (leftPressed)
                    {
                        slider.value = Mathf.Max(slider.lowValue, slider.value - step);
                    }
                    else if (rightPressed)
                    {
                        slider.value = Mathf.Min(slider.highValue, slider.value + step);
                    }
                }
                else if (currentEl is Toggle toggle)
                {
                    if (leftPressed || rightPressed)
                    {
                        toggle.value = !toggle.value;
                    }
                }
            }

            // Submit / Action Trigger
            if (submitPressed && currentNavIndex >= 0 && currentNavIndex < currentNavList.Count)
            {
                VisualElement currentEl = currentNavList[currentNavIndex];
                if (currentEl is Button btn)
                {
                    ExecuteNavButton(btn);
                }
                else if (currentEl is Toggle toggle)
                {
                    toggle.value = !toggle.value;
                }
            }
        }

        private void ExecuteNavButton(Button btn)
        {
            if (btn == null) return;

            if (btn == startPlayBtn) StartGame();
            else if (btn == startControlsBtn) OpenModal(UIState.Controls);
            else if (btn == startSettingsBtn) OpenModal(UIState.Settings);
            else if (btn == startQuitBtn) QuitGame();
            else if (btn == pauseResumeBtn) ResumeGame();
            else if (btn == pauseRestartBtn) RestartCurrentScene();
            else if (btn == pauseControlsBtn) OpenModal(UIState.Controls);
            else if (btn == pauseSettingsBtn) OpenModal(UIState.Settings);
            else if (btn == pauseMainMenuBtn) ReturnToMainMenu();
            else if (btn == pauseQuitBtn) QuitGame();
            else if (btn == settingsBackBtn || btn == controlsBackBtn) CloseModal();
        }

        private void SetGameplayActive(bool active)
        {
            if (playerMovement != null)
            {
                playerMovement.enabled = active;

                // Stop linear residual velocity on freeze
                if (!active && playerMovement.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.linearVelocity = Vector3.zero;
                }
            }

            if (cameraFollow != null)
            {
                cameraFollow.enabled = active;
            }
        }

        private void SetCursorLocked()
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }

        private void SetCursorUnlocked()
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }

        // --- PUBLIC ACTIONS ---

        public void StartGame()
        {
            SetState(UIState.Playing);
        }

        public void PauseGame()
        {
            SetState(UIState.Paused);
        }

        public void ResumeGame()
        {
            SetState(UIState.Playing);
        }

        public void ReturnToMainMenu()
        {
            SetState(UIState.StartMenu);
        }

        public void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }

        public void QuitGame()
        {
            Debug.Log("[GameMenuManager] Quitting Digital Souk application.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OpenModal(UIState modalState)
        {
            previousStateBeforeModal = currentState;
            SetState(modalState);
        }

        private void CloseModal()
        {
            SetState(previousStateBeforeModal);
        }

        // --- SETTINGS MANAGEMENT ---

        private void OnVolumeChanged(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value / 100f);
            if (volumeLabel != null)
            {
                volumeLabel.text = $"Master Volume: {Mathf.RoundToInt(value)}%";
            }
            PlayerPrefs.SetFloat(PrefVolume, value);
        }

        private void OnSensitivityChanged(float value)
        {
            if (cameraFollow != null)
            {
                cameraFollow.mouseSensitivityX = value;
                cameraFollow.mouseSensitivityY = value * 0.8f;
            }

            if (sensitivityLabel != null)
            {
                sensitivityLabel.text = $"Mouse Sensitivity: {value:F1}x";
            }
            PlayerPrefs.SetFloat(PrefSensitivity, value);
        }

        private void OnInvertYChanged(bool inverted)
        {
            if (cameraFollow != null)
            {
                cameraFollow.invertY = inverted;
            }
            PlayerPrefs.SetInt(PrefInvertY, inverted ? 1 : 0);
        }

        private void LoadSavedSettings()
        {
            // Volume
            float savedVolume = PlayerPrefs.GetFloat(PrefVolume, 100f);
            if (volumeSlider != null) volumeSlider.value = savedVolume;
            AudioListener.volume = savedVolume / 100f;
            if (volumeLabel != null) volumeLabel.text = $"Master Volume: {Mathf.RoundToInt(savedVolume)}%";

            // Sensitivity
            float defaultSens = cameraFollow != null ? cameraFollow.mouseSensitivityX : 2.5f;
            float savedSens = PlayerPrefs.GetFloat(PrefSensitivity, defaultSens);
            if (sensitivitySlider != null) sensitivitySlider.value = savedSens;
            if (cameraFollow != null)
            {
                cameraFollow.mouseSensitivityX = savedSens;
                cameraFollow.mouseSensitivityY = savedSens * 0.8f;
            }
            if (sensitivityLabel != null) sensitivityLabel.text = $"Mouse Sensitivity: {savedSens:F1}x";

            // Invert Y
            bool defaultInvert = cameraFollow != null && cameraFollow.invertY;
            bool savedInvert = PlayerPrefs.GetInt(PrefInvertY, defaultInvert ? 1 : 0) == 1;
            if (invertYToggle != null) invertYToggle.value = savedInvert;
            if (cameraFollow != null) cameraFollow.invertY = savedInvert;
        }
    }
}
