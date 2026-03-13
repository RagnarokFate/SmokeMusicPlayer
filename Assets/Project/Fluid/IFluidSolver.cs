using SmokeMusicPlayer.Data;
using UnityEngine;

namespace SmokeMusicPlayer.Fluid
{
    public interface IFluidSolver
    {
        void Initialize(int resolution);
        void UpdateFluid(float dt, AudioSpectrumData audioData, VisualizerProfile profile);
        void InjectDensity(Vector2 position, float radius, float amount, Color color);
        void InjectVelocity(Vector2 position, float radius, Vector2 force);
        RenderTexture GetDensityTexture();
        void Release();
    }
}
