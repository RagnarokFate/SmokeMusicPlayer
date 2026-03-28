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
        private readonly string[] tabLabels = { "Audio", "Visuals", "Simulation", "Presets" };
        private Texture2D whiteTex;

        private Vector2 lastMousePos;
        private bool isDragging;

        private void Start()
        {
            whiteTex = Texture2D.whiteTexture;
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

            GUI.backgroundColor = new Color(0.12f, 0.12f, 0.15f, 0.98f);
            GUI.Box(new Rect(10, 10, 420, 450), ""); // Clean backdrop
            
            // Header
            GUI.Label(new Rect(25, 20, 200, 30), "<size=16><b>SMOKE</b> PLAYER</size>");
            GUI.Label(new Rect(350, 23, 100, 20), "<size=10>v1.3.0</size>");

            GUILayout.BeginArea(new Rect(20, 55, 400, 380));
            currentTab = GUILayout.Toolbar(currentTab, tabLabels, GUILayout.Height(30));
            GUILayout.Space(15);

            switch (currentTab)
            {
                case 0: DrawAudioTab(); break;
                case 1: DrawVisualsTab(); break;
                case 2: DrawSimulationTab(profile); break;
                case 3: DrawPresetsTab(); break;
            }
            GUILayout.EndArea();
        }

        private void DrawAudioTab()
        {
            VisualizerProfile profile = appController.GetCurrentProfile();

            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>AUDIO SOURCE</b>");
            string modeName = audioManager.initialMode == AudioManager.SourceMode.File ? "Local File" : "Microphone Input";
            if (GUILayout.Button($"MODE: {modeName}", GUILayout.Height(40)))
            {
                CycleAudioMode();
            }
            GUILayout.EndVertical();
            
            GUILayout.Space(10);

            if (audioManager.initialMode == AudioManager.SourceMode.Microphone)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("<b>MICROPHONE SETTINGS</b>");
                GUILayout.Label($"Gain / Sensitivity: {audioManager.liveSensitivity:F1}x");
                audioManager.liveSensitivity = GUILayout.HorizontalSlider(audioManager.liveSensitivity, 1f, 10f);
                
                GUILayout.Space(5);
                if (micDevices != null && micDevices.Length > 0)
                {
                    selectedMicIndex = GUILayout.SelectionGrid(selectedMicIndex, micDevices, 1, "toggle");
                    if (GUI.changed)
                    {
                        audioManager.ApplyMode(AudioManager.SourceMode.Microphone, micDevices[selectedMicIndex]);
                    }
                }
                GUILayout.EndVertical();
            }
            else // File Mode
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("<b>PLAYBACK CONTROLS</b>");
                GUILayout.Label($"Speed / Pitch: {profile.simulationSpeed:F2}x");
                float newSpeed = GUILayout.HorizontalSlider(profile.simulationSpeed, 0.5f, 2.0f);
                if (Mathf.Abs(newSpeed - profile.simulationSpeed) > 0.01f) SetSpeed(newSpeed);

                GUILayout.Space(10);
                GUILayout.Label($"Stereo Width: {profile.stereoBalance:F2}");
                profile.stereoBalance = GUILayout.HorizontalSlider(profile.stereoBalance, -1.0f, 1.0f);
                GUILayout.EndVertical();
                
                GUILayout.Space(10);
                if (GUILayout.Button("Open File (Experimental)", GUILayout.Height(30))) 
                {
                    // Placeholder for future file browser
                    Debug.Log("File browser integration coming soon.");
                }
            }
        }

        private void DrawVisualsTab()
        {
            AudioSpectrumData spectrum = audioManager.GetSpectrumData();
            VisualizerProfile profile = appController.GetCurrentProfile();

            GUILayout.BeginHorizontal(GUILayout.Height(150));
            DrawFrequencyMeter(GUILayoutUtility.GetRect(280, 150));
            GUILayout.Space(10);
            DrawDBMeter(GUILayoutUtility.GetRect(60, 150), spectrum);
            GUILayout.EndHorizontal();

            GUILayout.Space(15);
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>RENDER SETTINGS</b>");
            profile.useFrequencyToHue = GUILayout.Toggle(profile.useFrequencyToHue, " Enable Dynamic Frequency-to-Color");
            
            if (profile.useFrequencyToHue)
            {
                GUILayout.Label($"Color Range Shift: {profile.colorSensitivity:F1}");
                profile.colorSensitivity = GUILayout.HorizontalSlider(profile.colorSensitivity, 0.1f, 2.0f);
            }
            GUILayout.EndVertical();
        }

        private void DrawSimulationTab(VisualizerProfile profile)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>FLUID DYNAMICS</b>");
            
            GUILayout.Label($"Viscosity (Thickness): {profile.viscosity:F5}");
            profile.viscosity = GUILayout.HorizontalSlider(profile.viscosity, 0.00001f, 0.002f);

            GUILayout.Space(5);
            GUILayout.Label($"Vorticity (Swirl Intensity): {profile.vorticity:F1}");
            profile.vorticity = GUILayout.HorizontalSlider(profile.vorticity, 0.0f, 12.0f);
            
            GUILayout.Space(5);
            GUILayout.Label($"Fade Rate: {profile.fadeRate:F4}");
            profile.fadeRate = GUILayout.HorizontalSlider(profile.fadeRate, 0.001f, 0.02f);
            GUILayout.EndVertical();

            GUILayout.Space(15);
            if (GUILayout.Button("Reset to Optimal Simulation", GUILayout.Height(40)))
            {
                profile.viscosity = 0.0001f;
                profile.vorticity = 8.0f;
                profile.fadeRate = 0.002f;
                profile.simulationSpeed = 1.0f;
                profile.stereoBalance = 0.0f;
            }
        }

        private void DrawPresetsTab()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>PROFILE MANAGEMENT</b>");
            if (GUILayout.Button("Save Current Profile", GUILayout.Height(35))) SaveCurrentProfile();
            if (GUILayout.Button("Reload Default Profile", GUILayout.Height(35))) LoadProfile("Default");
            GUILayout.EndVertical();
            
            GUILayout.Space(15);
            showDebugUI = GUILayout.Toggle(showDebugUI, " Show Performance Overlay");
            if (showDebugUI)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label($"Frame Rate: {CurrentFPS} FPS");
                GUILayout.Label($"Solver Grid: {CurrentGridSize} x {CurrentGridSize}");
                GUILayout.EndVertical();
            }
        }

        private void DrawFrequencyMeter(Rect rect)
        {
            GUI.Box(rect, "");
            float[] spectrum = audioManager.GetSmoothSpectrum();
            if (spectrum == null) return;

            int bands = 64;
            float barWidth = rect.width / bands;
            
            for (int i = 0; i < bands; i++)
            {
                float normalized = (float)i / bands;
                float power = Mathf.Pow(normalized, 1.5f);
                int binIndex = Mathf.Clamp(Mathf.RoundToInt(power * 320), 0, 511);
                
                float amplitude = spectrum[binIndex] * 12f;
                float barHeight = Mathf.Clamp(amplitude * rect.height, 2, rect.height);
                
                GUI.color = Color.Lerp(Color.red, Color.cyan, normalized);
                GUI.DrawTexture(new Rect(rect.x + (i * barWidth), rect.y + rect.height - barHeight, barWidth - 1, barHeight), whiteTex);
            }
            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x, rect.y + rect.height + 2, 50, 20), "<size=9>20Hz</size>");
            GUI.Label(new Rect(rect.x + rect.width - 50, rect.y + rect.height + 2, 50, 20), "<size=9>20kHz</size>");
        }

        private void DrawDBMeter(Rect rect, AudioSpectrumData data)
        {
            GUI.Box(rect, "");
            float dbNorm = Mathf.InverseLerp(-70f, 0f, data.currentDB);
            float peakNorm = Mathf.InverseLerp(-70f, 0f, data.peakDB);
            
            float meterHeight = rect.height * dbNorm;
            float peakY = rect.y + rect.height - (rect.height * peakNorm);

            GUI.color = new Color(0, 1, 0, 0.2f);
            GUI.DrawTexture(new Rect(rect.x, rect.y + (rect.height * 0.3f), rect.width, rect.height * 0.7f), whiteTex);
            GUI.color = new Color(1, 1, 0, 0.2f);
            GUI.DrawTexture(new Rect(rect.x, rect.y + (rect.height * 0.05f), rect.width, rect.height * 0.25f), whiteTex);
            GUI.color = new Color(1, 0, 0, 0.2f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, rect.height * 0.05f), whiteTex);

            GUI.color = Color.Lerp(Color.green, Color.red, dbNorm);
            GUI.DrawTexture(new Rect(rect.x + 5, rect.y + rect.height - meterHeight, rect.width - 10, meterHeight), whiteTex);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(rect.x + 2, peakY, rect.width - 4, 2), whiteTex);
            
            GUI.Label(new Rect(rect.x + rect.width + 5, rect.y, 40, 20), "<size=9>0dB</size>");
            GUI.Label(new Rect(rect.x + rect.width + 5, rect.y + rect.height - 15, 40, 20), "<size=9>-70dB</size>");
            GUI.Label(new Rect(rect.x, rect.y - 18, 50, 20), $"<b>{data.currentDB:F1}</b>");
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