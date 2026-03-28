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

            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.12f, 0.95f);
            GUI.Box(new Rect(10, 10, 400, 420), "<b>Smoke Music Player</b> v1.2.0");
            
            GUILayout.BeginArea(new Rect(20, 40, 380, 380));
            currentTab = GUILayout.Toolbar(currentTab, tabLabels, GUILayout.Height(25));
            GUILayout.Space(10);

            switch (currentTab)
            {
                case 0: DrawAudioTab(); break;
                case 1: DrawVisualsTab(); break;
                case 2: DrawSimulationTab(profile); break;
                case 3: DrawPresetsTab(); break;
            }
            GUILayout.EndArea();
        }

        private void DrawVisualsTab()
        {
            AudioSpectrumData spectrum = audioManager.GetSpectrumData();
            
            GUILayout.BeginHorizontal(GUILayout.Height(150));
            DrawFrequencyMeter(GUILayoutUtility.GetRect(250, 150));
            GUILayout.Space(10);
            DrawDBMeter(GUILayoutUtility.GetRect(50, 150), spectrum);
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            GUILayout.Label("<b>Spectrum Visualizer</b>");
            GUILayout.Label("Logarithmic Scale: 20Hz - 20kHz");
            
            GUILayout.Label($"Overall Gain: {spectrum.overallAmplitude * 100:F1}%");
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
                // Logarithmic binning
                float normalized = (float)i / bands;
                float power = Mathf.Pow(normalized, 2.0f);
                int binIndex = Mathf.Clamp(Mathf.RoundToInt(power * 255), 0, 511);
                
                float amplitude = spectrum[binIndex] * 10f; // Boost for display
                float barHeight = Mathf.Clamp(amplitude * rect.height, 2, rect.height);
                
                Color barColor = Color.Lerp(Color.red, Color.cyan, normalized);
                GUI.color = barColor;
                GUI.DrawTexture(new Rect(rect.x + (i * barWidth), rect.y + rect.height - barHeight, barWidth - 1, barHeight), whiteTex);
            }
            GUI.color = Color.white;
            
            // Labels
            GUI.Label(new Rect(rect.x, rect.y + rect.height + 2, 50, 20), "<size=9>20Hz</size>");
            GUI.Label(new Rect(rect.x + rect.width - 50, rect.y + rect.height + 2, 50, 20), "<size=9>20kHz</size>");
        }

        private void DrawDBMeter(Rect rect, AudioSpectrumData data)
        {
            GUI.Box(rect, "");
            float dbNorm = Mathf.InverseLerp(-60f, 0f, data.currentDB);
            float peakNorm = Mathf.InverseLerp(-60f, 0f, data.peakDB);
            
            float meterHeight = rect.height * dbNorm;
            float peakY = rect.y + rect.height - (rect.height * peakNorm);

            // Background color zones
            GUI.color = new Color(0, 1, 0, 0.2f); // Green zone
            GUI.DrawTexture(new Rect(rect.x, rect.y + (rect.height * 0.2f), rect.width, rect.height * 0.8f), whiteTex);
            GUI.color = new Color(1, 1, 0, 0.2f); // Yellow zone (-12dB to -3dB approx)
            GUI.DrawTexture(new Rect(rect.x, rect.y + (rect.height * 0.05f), rect.width, rect.height * 0.15f), whiteTex);
            GUI.color = new Color(1, 0, 0, 0.2f); // Red zone (-3dB to 0dB)
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, rect.height * 0.05f), whiteTex);

            // Current level
            GUI.color = Color.Lerp(Color.green, Color.red, dbNorm);
            GUI.DrawTexture(new Rect(rect.x + 5, rect.y + rect.height - meterHeight, rect.width - 10, meterHeight), whiteTex);
            
            // Peak line
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(rect.x + 2, peakY, rect.width - 4, 2), whiteTex);
            
            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x + rect.width + 5, rect.y, 40, 20), "<size=9>0dB</size>");
            GUI.Label(new Rect(rect.x + rect.width + 5, rect.y + rect.height - 15, 40, 20), "<size=9>-60dB</size>");
            GUI.Label(new Rect(rect.x, rect.y - 18, 50, 20), $"<b>{data.currentDB:F1}</b>");
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
            GUILayout.Label("<b>Advanced Visuals</b>");
            profile.useFrequencyToHue = GUILayout.Toggle(profile.useFrequencyToHue, " Use Frequency-to-Hue Mapping");
            if (profile.useFrequencyToHue)
            {
                GUILayout.Label($"Color Sensitivity: {profile.colorSensitivity:F1}");
                profile.colorSensitivity = GUILayout.HorizontalSlider(profile.colorSensitivity, 0.1f, 5.0f);
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Reset Defaults"))
            {
                profile.simulationSpeed = 1.0f;
                profile.stereoBalance = 0.0f;
                profile.viscosity = 0.0001f;
                profile.useFrequencyToHue = true;
                profile.colorSensitivity = 1.0f;
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