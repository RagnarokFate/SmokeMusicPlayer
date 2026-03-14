using SmokeMusicPlayer.Audio;
using SmokeMusicPlayer.Data;
using SmokeMusicPlayer.Fluid;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SmokeMusicPlayer.UI
{
    public class PlayerUI : MonoBehaviour
    {
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private AppController appController; 
        
        public bool showDebugUI = false;
        private string[] micDevices;
        private int selectedMicIndex = 0;
        private int currentTab = 0;
        private readonly string[] tabLabels = { "Audio", "Simulation", "Presets" };

        private Vector2 lastMousePos;
        private bool isDragging;

        private void Start()
        {
            if (audioManager != null)
            {
                micDevices = audioManager.GetMicrophoneDevices();
                if (!string.IsNullOrEmpty(audioManager.selectedMicName))
                {
                    for (int i = 0; i < micDevices.Length; i++)
                    {
                        if (micDevices[i] == audioManager.selectedMicName)
                        {
                            selectedMicIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        private void Update()
        {
            if (this == null || audioManager == null || appController == null) return;
            HandleKeyboardInput();
            HandleMouseInput();
            UpdateDebugStats();
        }

        private void OnGUI()
        {
            if (this == null || appController == null || audioManager == null) return;
            
            VisualizerProfile profile = appController.GetCurrentProfile();
            if (profile == null) return;

            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.12f, 0.95f);
            GUI.Box(new Rect(10, 10, 350, 320), "<b>Smoke Music Player</b> v1.1.0");
            
            GUILayout.BeginArea(new Rect(20, 40, 330, 280));
            currentTab = GUILayout.Toolbar(currentTab, tabLabels, GUILayout.Height(25));
            GUILayout.Space(10);

            switch (currentTab)
            {
                case 0: DrawAudioTab(); break;
                case 1: DrawSimulationTab(profile); break;
                case 2: DrawPresetsTab(); break;
            }
            GUILayout.EndArea();
        }

        private void DrawAudioTab()
        {
            GUILayout.Label("<b>Source Mode</b>");
            if (GUILayout.Button($"Current Mode: {audioManager.initialMode}", GUILayout.Height(30)))
            {
                CycleAudioMode();
            }
            
            GUILayout.Space(5);
            GUILayout.Label($"Live Sensitivity: {audioManager.liveSensitivity:F1}x");
            audioManager.liveSensitivity = GUILayout.HorizontalSlider(audioManager.liveSensitivity, 1f, 100f);

            if (audioManager.initialMode == AudioManager.SourceMode.Microphone)
            {
                GUILayout.Space(10);
                GUILayout.Label("<b>Select Microphone</b>");
                if (micDevices != null && micDevices.Length > 0)
                {
                    for (int i = 0; i < micDevices.Length; i++)
                    {
                        if (GUILayout.Toggle(selectedMicIndex == i, micDevices[i]))
                        {
                            if (selectedMicIndex != i)
                            {
                                selectedMicIndex = i;
                                audioManager.ApplyMode(AudioManager.SourceMode.Microphone, micDevices[selectedMicIndex]);
                            }
                        }
                    }
                }
            }
        }

        private void DrawSimulationTab(VisualizerProfile profile)
        {
            GUILayout.Label($"Simulation Speed: {profile.simulationSpeed:F2}x");
            float newSpeed = GUILayout.HorizontalSlider(profile.simulationSpeed, 0.5f, 2.0f);
            if (Mathf.Abs(newSpeed - profile.simulationSpeed) > 0.01f) SetSpeed(newSpeed);

            GUILayout.Space(5);
            GUILayout.Label($"Stereo Balance: {profile.stereoBalance:F2}");
            profile.stereoBalance = GUILayout.HorizontalSlider(profile.stereoBalance, -1.0f, 1.0f);
            
            GUILayout.Space(5);
            GUILayout.Label($"Smoke Viscosity: {profile.viscosity:F6}");
            profile.viscosity = GUILayout.HorizontalSlider(profile.viscosity, 0.00001f, 0.005f);

            GUILayout.Space(10);
            if (GUILayout.Button("Reset Defaults"))
            {
                profile.simulationSpeed = 1.0f;
                profile.stereoBalance = 0.0f;
                profile.viscosity = 0.0001f;
            }
        }

        private void DrawPresetsTab()
        {
            if (GUILayout.Button("Save Preset", GUILayout.Height(30))) SaveCurrentProfile();
            if (GUILayout.Button("Load Default", GUILayout.Height(30))) LoadProfile("Default");
            GUILayout.Space(10);
            showDebugUI = GUILayout.Toggle(showDebugUI, "Show Debug Stats");
            if (showDebugUI)
            {
                GUILayout.Label($"FPS: {CurrentFPS}");
                GUILayout.Label($"Grid: {CurrentGridSize}x{CurrentGridSize}");
            }
        }

        private void CycleAudioMode()
        {
            AudioManager.SourceMode nextMode = audioManager.initialMode == AudioManager.SourceMode.File 
                ? AudioManager.SourceMode.Microphone 
                : AudioManager.SourceMode.File;

            audioManager.initialMode = nextMode;
            audioManager.ApplyMode(nextMode, (micDevices != null && micDevices.Length > 0) ? micDevices[selectedMicIndex] : "");
        }

        private void SetSpeed(float speed)
        {
            if (appController != null && appController.GetCurrentProfile() != null)
            {
                appController.GetCurrentProfile().simulationSpeed = speed;
                audioManager.SetPlaybackSpeed(speed);
            }
        }

        private void UpdateDebugStats() { CurrentFPS = Mathf.RoundToInt(1.0f / Time.smoothDeltaTime); if (appController != null && appController.GetCurrentProfile() != null) CurrentGridSize = appController.GetCurrentProfile().gridResolution; }
        public int CurrentFPS { get; private set; }
        public int CurrentGridSize { get; private set; }

        private void HandleKeyboardInput() { if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) { if (audioManager.IsPlaying) audioManager.Pause(); else audioManager.Play(); } }

        private void HandleMouseInput()
        {
            if (Mouse.current == null) return;
            if (Mouse.current.leftButton.wasPressedThisFrame) { isDragging = true; lastMousePos = Mouse.current.position.ReadValue(); }
            else if (Mouse.current.leftButton.wasReleasedThisFrame) isDragging = false;
            if (isDragging) { Vector2 pos = Mouse.current.position.ReadValue(); appController.InjectUserForce(new Vector2(pos.x / Screen.width, pos.y / Screen.height), pos - lastMousePos); lastMousePos = pos; }
        }

        public void SaveCurrentProfile() { if (appController != null) PresetManager.SaveProfile(appController.GetCurrentProfile()); }
        public void LoadProfile(string n) { if (appController != null) { var p = PresetManager.LoadProfile(n); if (p != null) { appController.SetCurrentProfile(p); audioManager.SetPlaybackSpeed(p.simulationSpeed); } } }
    }
}