using System;
using SmokeMusicPlayer.Data;
using UnityEngine;

namespace SmokeMusicPlayer.Fluid
{
        public class GPUFluidSolver : MonoBehaviour, IFluidSolver
        {
            [SerializeField] private ComputeShader fluidCompute;
            private int resolution;
    
            private RenderTexture densityWrite, densityRead;
            private RenderTexture velocityWrite, velocityRead;
            private RenderTexture pressureWrite, pressureRead;
            private RenderTexture divergenceWrite, divergenceRead;
    
            private int kernelInjectDensity;
                    private int kernelInjectVelocity;
                    private int kernelDissipate;
                    private int kernelAdvect;
                    private int kernelVorticity;
                    private int kernelDivergence;
                    private int kernelJacobi;
                    private int kernelSubtractGradient;
            
                    public void Initialize(int resolution)
                    {
                        if (fluidCompute == null) throw new NullReferenceException("Compute Shader not assigned.");
                        this.resolution = resolution;
            
                        densityWrite = CreateRenderTexture(RenderTextureFormat.ARGBFloat);
                        densityRead = CreateRenderTexture(RenderTextureFormat.ARGBFloat);
                        velocityWrite = CreateRenderTexture(RenderTextureFormat.RGFloat);
                        velocityRead = CreateRenderTexture(RenderTextureFormat.RGFloat);
                        pressureWrite = CreateRenderTexture(RenderTextureFormat.RFloat);
                        pressureRead = CreateRenderTexture(RenderTextureFormat.RFloat);
                        divergenceWrite = CreateRenderTexture(RenderTextureFormat.RFloat);
                        divergenceRead = CreateRenderTexture(RenderTextureFormat.RFloat);
            
                        ClearRenderTexture(densityWrite);
                        ClearRenderTexture(densityRead);
                        ClearRenderTexture(velocityWrite);
                        ClearRenderTexture(velocityRead);
                        ClearRenderTexture(pressureWrite);
                        ClearRenderTexture(pressureRead);
                        ClearRenderTexture(divergenceWrite);
                        ClearRenderTexture(divergenceRead);
            
                        kernelInjectDensity = fluidCompute.FindKernel("InjectDensity");
                        kernelInjectVelocity = fluidCompute.FindKernel("InjectVelocity");
                        kernelDissipate = fluidCompute.FindKernel("Dissipate");
                        kernelAdvect = fluidCompute.FindKernel("Advect");
                        kernelVorticity = fluidCompute.FindKernel("Vorticity");
                        kernelDivergence = fluidCompute.FindKernel("Divergence");
                        kernelJacobi = fluidCompute.FindKernel("Jacobi");
                        kernelSubtractGradient = fluidCompute.FindKernel("SubtractGradient");
                    }
            
                    private void ClearRenderTexture(RenderTexture rt)
                    {
                        RenderTexture previousActive = RenderTexture.active;
                        RenderTexture.active = rt;
                        GL.Clear(true, true, Color.clear);
                        RenderTexture.active = previousActive;
                    }
            
                    private RenderTexture CreateRenderTexture(RenderTextureFormat format)
                    {
                        RenderTexture rt = new RenderTexture(resolution, resolution, 0, format, RenderTextureReadWrite.Linear);
                        rt.enableRandomWrite = true;
                        rt.filterMode = FilterMode.Bilinear;
                        rt.wrapMode = TextureWrapMode.Clamp;
                        rt.Create();
                        return rt;
                    }
            
                    public void UpdateFluid(float dt, AudioSpectrumData audioData, VisualizerProfile profile)
                    {
                        fluidCompute.SetFloat("dt", dt);
                        fluidCompute.SetInt("width", resolution);
                        fluidCompute.SetInt("height", resolution);
                        fluidCompute.SetFloat("fadeRate", profile.fadeRate);
                        fluidCompute.SetFloat("velocityDissipation", profile.velocityDissipation);
                        fluidCompute.SetFloat("vorticityScale", profile.vorticity * 50f);
            
                        int threadGroups = Mathf.CeilToInt(resolution / 8.0f);
            
                        // 1. Advect
                        fluidCompute.SetTexture(kernelAdvect, "DensityRead", densityRead);
                        fluidCompute.SetTexture(kernelAdvect, "DensityWrite", densityWrite);
                        fluidCompute.SetTexture(kernelAdvect, "VelocityRead", velocityRead);
                        fluidCompute.SetTexture(kernelAdvect, "VelocityWrite", velocityWrite);
                        fluidCompute.Dispatch(kernelAdvect, threadGroups, threadGroups, 1);
                        
                        Swap(ref densityRead, ref densityWrite);
                        Swap(ref velocityRead, ref velocityWrite);
            
                                                // 2. Vorticity Confinement (Adds curl/swirls back in)
            
                                                fluidCompute.SetTexture(kernelVorticity, "VelocityRead", velocityRead);
            
                                                fluidCompute.SetTexture(kernelVorticity, "VelocityWrite", velocityWrite);
            
                                                fluidCompute.Dispatch(kernelVorticity, threadGroups, threadGroups, 1);
            
                                                Swap(ref velocityRead, ref velocityWrite);
            
                        
            
                                                // 3. Compute Divergence
            
                                                fluidCompute.SetTexture(kernelDivergence, "VelocityRead", velocityRead);
            
                                                fluidCompute.SetTexture(kernelDivergence, "DivergenceWrite", divergenceWrite);
            
                                                fluidCompute.SetTexture(kernelDivergence, "PressureWrite", pressureWrite); // Reset pressure
            
                                                fluidCompute.Dispatch(kernelDivergence, threadGroups, threadGroups, 1);
                
                Swap(ref divergenceRead, ref divergenceWrite);
                Swap(ref pressureRead, ref pressureWrite);
    
                // 3. Jacobi (Pressure Solve) - iterate multiple times for stability
                int jacobiIterations = 40; // High iterations for stable fluids
                for (int i = 0; i < jacobiIterations; i++)
                {
                    fluidCompute.SetTexture(kernelJacobi, "PressureRead", pressureRead);
                    fluidCompute.SetTexture(kernelJacobi, "DivergenceRead", divergenceRead);
                    fluidCompute.SetTexture(kernelJacobi, "PressureWrite", pressureWrite);
                    fluidCompute.Dispatch(kernelJacobi, threadGroups, threadGroups, 1);
                    Swap(ref pressureRead, ref pressureWrite);
                }
    
                // 4. Subtract Gradient (Make velocity divergence-free)
                fluidCompute.SetTexture(kernelSubtractGradient, "PressureRead", pressureRead);
                fluidCompute.SetTexture(kernelSubtractGradient, "VelocityRead", velocityRead);
                fluidCompute.SetTexture(kernelSubtractGradient, "VelocityWrite", velocityWrite);
                fluidCompute.Dispatch(kernelSubtractGradient, threadGroups, threadGroups, 1);
                Swap(ref velocityRead, ref velocityWrite);
    
                // 5. Dissipate
                fluidCompute.SetTexture(kernelDissipate, "DensityRead", densityRead);
                fluidCompute.SetTexture(kernelDissipate, "DensityWrite", densityWrite);
                fluidCompute.SetTexture(kernelDissipate, "VelocityRead", velocityRead);
                fluidCompute.SetTexture(kernelDissipate, "VelocityWrite", velocityWrite);
                
                fluidCompute.Dispatch(kernelDissipate, threadGroups, threadGroups, 1);
    
                Swap(ref densityRead, ref densityWrite);
                Swap(ref velocityRead, ref velocityWrite);
            }
                public void InjectDensity(Vector2 position, float radius, float amount, Color color)
                {
                    fluidCompute.SetVector("injectPos", new Vector2(position.x * resolution, position.y * resolution));
                    fluidCompute.SetFloat("injectRadius", radius * resolution);
                    
                    // Pass pure RGB and use the W channel for amount
                    fluidCompute.SetVector("injectColor", new Vector4(color.r, color.g, color.b, amount));
        
                    fluidCompute.SetTexture(kernelInjectDensity, "DensityRead", densityRead);
                    fluidCompute.SetTexture(kernelInjectDensity, "DensityWrite", densityWrite);
        
                    int threadGroups = Mathf.CeilToInt(resolution / 8.0f);
                    fluidCompute.Dispatch(kernelInjectDensity, threadGroups, threadGroups, 1);
        
                    Swap(ref densityRead, ref densityWrite);
                }

        public void InjectVelocity(Vector2 position, float radius, Vector2 force)
        {
            fluidCompute.SetVector("injectPos", new Vector2(position.x * resolution, position.y * resolution));
            fluidCompute.SetFloat("injectRadius", radius * resolution);
            fluidCompute.SetVector("injectForce", force);

            fluidCompute.SetTexture(kernelInjectVelocity, "VelocityRead", velocityRead);
            fluidCompute.SetTexture(kernelInjectVelocity, "VelocityWrite", velocityWrite);

            int threadGroups = Mathf.CeilToInt(resolution / 8.0f);
            fluidCompute.Dispatch(kernelInjectVelocity, threadGroups, threadGroups, 1);

            Swap(ref velocityRead, ref velocityWrite);
        }

        public RenderTexture GetDensityTexture()
        {
            return densityRead;
        }

        public void Release()
        {
            if (densityWrite != null) densityWrite.Release();
            if (densityRead != null) densityRead.Release();
            if (velocityWrite != null) velocityWrite.Release();
            if (velocityRead != null) velocityRead.Release();
            if (pressureWrite != null) pressureWrite.Release();
            if (pressureRead != null) pressureRead.Release();
            if (divergenceWrite != null) divergenceWrite.Release();
            if (divergenceRead != null) divergenceRead.Release();
        }

        private void OnDestroy()
        {
            Release();
        }

        private void Swap(ref RenderTexture a, ref RenderTexture b)
        {
            RenderTexture temp = a;
            a = b;
            b = temp;
        }
    }
}
