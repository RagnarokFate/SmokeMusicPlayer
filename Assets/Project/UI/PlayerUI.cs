using SmokeMusicPlayer.Audio;
using SmokeMusicPlayer.Data;
using SmokeMusicPlayer.Fluid;
using UnityEngine;
using UnityEngine.InputSystem; // Added InputSystem

namespace SmokeMusicPlayer.UI
{
    public class PlayerUI : MonoBehaviour
    {
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private AppController appController; // Needed to pass fluid inputs if we separate concerns further
        
        public bool showDebugUI = false;
        
        // Expose a public method to pass mouse position from a UI Raycaster if needed
        private Vector2 lastMousePos;
        private bool isDragging;

        private void Update()
        {
            HandleKeyboardInput();
            HandleMouseInput();
            UpdateDebugStats();
        }

        private void OnGUI()
        {
            if (appController == null || appController.GetCurrentProfile() == null) return;

            VisualizerProfile profile = appController.GetCurrentProfile();

            // Set background box
            GUI.Box(new Rect(10, 10, 300, 120), "Smoke Music Player Settings");

            // Simulation/Playback Speed Slider
            GUI.Label(new Rect(20, 40, 200, 20), $"Simulation Speed: {profile.simulationSpeed:F2}x");
            float newSpeed = GUI.HorizontalSlider(new Rect(20, 60, 280, 20), profile.simulationSpeed, 0.5f, 2.0f);
            if (Mathf.Abs(newSpeed - profile.simulationSpeed) > 0.01f)
            {
                SetSpeed(newSpeed);
            }

            // Stereo Balance Slider
            GUI.Label(new Rect(20, 80, 200, 20), $"Stereo Balance (Left-Right): {profile.stereoBalance:F2}");
            float newBalance = GUI.HorizontalSlider(new Rect(20, 100, 280, 20), profile.stereoBalance, -1.0f, 1.0f);
            if (Mathf.Abs(newBalance - profile.stereoBalance) > 0.01f)
            {
                profile.stereoBalance = newBalance;
            }
        }

        private void UpdateDebugStats()
        {
            // Simple FPS calculation for MVP
            float fps = 1.0f / Time.smoothDeltaTime;
            
            // In a real UI this would update a Text component
            // We expose it here so a UI Text component can bind to it
            CurrentFPS = Mathf.RoundToInt(fps);
            if (appController != null && appController.GetCurrentProfile() != null)
            {
                CurrentGridSize = appController.GetCurrentProfile().gridResolution;
            }
        }
        
        public int CurrentFPS { get; private set; }
        public int CurrentGridSize { get; private set; }

        private void HandleKeyboardInput()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (audioManager.IsPlaying) audioManager.Pause();
                else audioManager.Play();
            }
            
            // Temporary hotkeys for saving/loading MVP default profile
            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                SaveCurrentProfile();
            }
            if (Keyboard.current.lKey.wasPressedThisFrame)
            {
                LoadProfile("Default");
            }
            if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                showDebugUI = !showDebugUI;
            }

            // Speed Controls: 1 = 0.5x, 2 = 1.0x, 3 = 2.0x
            if (Keyboard.current.digit1Key.wasPressedThisFrame) SetSpeed(0.5f);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) SetSpeed(1.0f);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) SetSpeed(2.0f);
        }

        private void SetSpeed(float speed)
        {
            if (appController != null && appController.GetCurrentProfile() != null)
            {
                appController.GetCurrentProfile().simulationSpeed = speed;
                audioManager.SetPlaybackSpeed(speed);
            }
        }

        private void HandleMouseInput()
        {
            if (Mouse.current == null) return;

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                isDragging = true;
                lastMousePos = Mouse.current.position.ReadValue();
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                isDragging = false;
            }

            if (isDragging)
            {
                Vector2 currentMousePos = Mouse.current.position.ReadValue();
                Vector2 delta = currentMousePos - lastMousePos;

                if (delta.magnitude > 0.1f)
                {
                    // Normalize mouse pos to 0-1 range based on screen size
                    Vector2 uvPos = new Vector2(currentMousePos.x / Screen.width, currentMousePos.y / Screen.height);
                    
                    // Fire an event or directly call solver
                    // For MVP simplicity, we expose a static or direct reference to inject
                    if (appController != null)
                    {
                        // Needs a public method on AppController to inject user force
                        appController.InjectUserForce(uvPos, delta);
                    }
                }
                
                lastMousePos = currentMousePos;
            }
        }

        public void SaveCurrentProfile()
        {
            if (appController != null)
            {
                PresetManager.SaveProfile(appController.GetCurrentProfile());
            }
        }

        public void LoadProfile(string profileName)
        {
            if (appController != null)
            {
                VisualizerProfile loaded = PresetManager.LoadProfile(profileName);
                if (loaded != null)
                {
                    appController.SetCurrentProfile(loaded);
                    audioManager.SetPlaybackSpeed(loaded.simulationSpeed);
                }
            }
        }
    }
}
