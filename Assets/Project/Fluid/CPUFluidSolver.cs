using SmokeMusicPlayer.Data;
using UnityEngine;

namespace SmokeMusicPlayer.Fluid
{
    public class CPUFluidSolver : MonoBehaviour, IFluidSolver
    {
        private int resolution;
        private Color[] density;
        private Texture2D outputTexture;
        private RenderTexture renderTexture;

        public void Initialize(int resolution)
        {
            this.resolution = resolution;
            density = new Color[resolution * resolution];
            
            outputTexture = new Texture2D(resolution, resolution, TextureFormat.RGBAFloat, false);
            renderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat);
        }

        public void UpdateFluid(float dt, AudioSpectrumData audioData, VisualizerProfile profile)
        {
            // Simplified CPU fallback: just fade colors over time
            for (int i = 0; i < density.Length; i++)
            {
                density[i].r = Mathf.Max(0, density[i].r - profile.fadeRate * dt);
                density[i].g = Mathf.Max(0, density[i].g - profile.fadeRate * dt);
                density[i].b = Mathf.Max(0, density[i].b - profile.fadeRate * dt);
            }

            outputTexture.SetPixels(density);
            outputTexture.Apply();
            Graphics.Blit(outputTexture, renderTexture);
        }

        public void InjectDensity(Vector2 position, float radius, float amount, Color color)
        {
            int px = Mathf.FloorToInt(position.x * resolution);
            int py = Mathf.FloorToInt(position.y * resolution);
            int r = Mathf.FloorToInt(radius * resolution);

            for (int y = -r; y <= r; y++)
            {
                for (int x = -r; x <= r; x++)
                {
                    int cx = px + x;
                    int cy = py + y;

                    if (cx >= 0 && cx < resolution && cy >= 0 && cy < resolution)
                    {
                        if (x * x + y * y <= r * r)
                        {
                            int index = cy * resolution + cx;
                            density[index] += color * amount;
                        }
                    }
                }
            }
        }

        public void InjectVelocity(Vector2 position, float radius, Vector2 force)
        {
            // CPU velocity advection not implemented in MVP fallback due to performance
        }

        public RenderTexture GetDensityTexture()
        {
            return renderTexture;
        }

        public void Release()
        {
            if (renderTexture != null) renderTexture.Release();
            if (outputTexture != null) Destroy(outputTexture);
        }

        private void OnDestroy()
        {
            Release();
        }
    }
}
