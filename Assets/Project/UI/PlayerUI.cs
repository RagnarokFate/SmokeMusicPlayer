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
                appController.SetCurrentProfile(loaded);
            }
        }
    }
}
