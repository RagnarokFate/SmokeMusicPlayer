using SmokeMusicPlayer.Audio;
using SmokeMusicPlayer.Data;
using SmokeMusicPlayer.Fluid;
using UnityEngine;

namespace SmokeMusicPlayer
{
        public class AppController : MonoBehaviour
        {
            [SerializeField] private AudioManager audioManager;
            [SerializeField] private FluidRenderer fluidRenderer;
    
            // We will load this from JSON later, creating a default for now
            private VisualizerProfile currentProfile;
            private IFluidSolver solver;
    
            private void Start()
            {
                currentProfile = new VisualizerProfile();
    
                // Check system capabilities and initialize appropriate solver
                if (SystemInfo.supportsComputeShaders)
                {
                    Debug.Log("Compute Shaders supported. Initializing GPUFluidSolver.");
                    solver = GetComponent<GPUFluidSolver>();
                    if (solver == null)
                    {
                        Debug.LogError("GPUFluidSolver component missing! Please attach it to the GameController.");
                        return;
                    }
                }
                else
                {
                    Debug.LogWarning("Compute Shaders NOT supported. Falling back to CPUFluidSolver.");
                    solver = GetComponent<CPUFluidSolver>();
                    if (solver == null)
                    {
                        Debug.LogError("CPUFluidSolver component missing! Please attach it to the GameController.");
                        return;
                    }
                }
    
                solver.Initialize(currentProfile.gridResolution);
                fluidRenderer.Initialize(solver);
            }
        private void Update()
        {
            if (solver == null) return;

            // Step 1: Get Audio Data
            AudioSpectrumData spectrum = audioManager.GetSpectrumData();

            // Step 2: Inject forces based on audio
            if (audioManager.IsPlaying)
            {
                InjectAudioForces(spectrum);
            }

            // Step 3: Step fluid simulation
            if (currentProfile != null)
            {
                solver.UpdateFluid(Time.deltaTime * currentProfile.simulationSpeed, spectrum, currentProfile);
            }
        }

        private void InjectAudioForces(AudioSpectrumData spectrum)
        {
            if (currentProfile == null) return;
            
            int bandsToRender = 128; // The number of "emitters" across the X axis
            
            for (int i = 0; i < bandsToRender; i++)
            {
                float normalizedX = (float)i / (bandsToRender - 1);
                
                // Logarithmic frequency mapping. 
                // Most music energy is in the lower 1/4th of the spectrum array.
                // normalizedX = 0 -> bin 0
                // normalizedX = 1 -> bin 255 (out of 511)
                float power = Mathf.Pow(normalizedX, 2.0f); // stretches lower frequencies across a wider X area
                int binIndex = Mathf.Clamp(Mathf.RoundToInt(power * 255), 0, 511);
                
                float rawAmplitude = spectrum.spectrum[binIndex];
                
                // High frequencies naturally have much lower amplitudes in FFT.
                // We boost them based on their bin index so they visually match bass.
                float frequencyBoost = 1.0f + (binIndex * 0.1f); 
                float chunkAmplitude = rawAmplitude * frequencyBoost;

                if (chunkAmplitude > 0.002f)
                {
                    Vector2 pos = new Vector2(normalizedX, 0.02f); 
                    
                    // Smoothly blend colors based on X position across the entire palette
                    Color color;
                    if (currentProfile.colorPalette.Count <= 1)
                    {
                        color = currentProfile.colorPalette.Count == 1 ? currentProfile.colorPalette[0] : Color.white;
                    }
                    else
                    {
                        float colorPos = normalizedX * (currentProfile.colorPalette.Count - 1);
                        int colorIndex = Mathf.Clamp(Mathf.FloorToInt(colorPos), 0, currentProfile.colorPalette.Count - 2);
                        float colorT = colorPos - colorIndex;
                        color = Color.Lerp(currentProfile.colorPalette[colorIndex], currentProfile.colorPalette[colorIndex + 1], colorT);
                    }
                    
                    // Smoothly blend multipliers based on frequency bands
                    float multiplier;
                    if (normalizedX < 0.33f) 
                    {
                        multiplier = currentProfile.bassMultiplier; 
                    }
                    else if (normalizedX < 0.66f) 
                    {
                        multiplier = currentProfile.trebleMultiplier * 1.5f;
                    }
                    else 
                    {
                        multiplier = currentProfile.trebleMultiplier * 2.0f; // Further boost the highest highs
                    }
                    
                    float baseAmount = chunkAmplitude * multiplier;
                    
                    // Inject density: This is an amount per second now. 
                    // 10.0 means it takes 0.1 seconds to reach full opacity (1.0 alpha).
                    float densityAmount = baseAmount * 5.0f;
                    solver.InjectDensity(pos, 0.08f, densityAmount, color);
                    
                    // Upward thrust
                    float horizontalDrift = Mathf.Sin(Time.time * currentProfile.vorticity * 2f + (normalizedX * Mathf.PI * 8f));
                    // Velocity force: 1500 means 1500 pixels per second, so it shoots up nicely across the 512px grid
                    Vector2 force = new Vector2(horizontalDrift * 0.4f, 1.0f).normalized * baseAmount * 1500f;
                    solver.InjectVelocity(pos, 0.12f, force);
                }
            }
        }

        public void InjectUserForce(Vector2 uvPosition, Vector2 deltaMouse)
        {
            if (solver == null) return;
            
            // Inject a bit of density (white smoke) and velocity at the mouse cursor
            solver.InjectDensity(uvPosition, 0.05f, 0.5f, Color.white);
            
            // Normalize delta to reasonable force limits (delta is pixels per frame, we want pixels per sec)
            Vector2 force = deltaMouse * 50.0f;
            solver.InjectVelocity(uvPosition, 0.05f, force);
        }

        public VisualizerProfile GetCurrentProfile()
        {
            return currentProfile;
        }

        public void SetCurrentProfile(VisualizerProfile profile)
        {
            currentProfile = profile;
        }
    }
}
