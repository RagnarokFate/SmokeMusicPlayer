using UnityEngine;
using UnityEngine.UI;

namespace SmokeMusicPlayer.Fluid
{
    public class FluidRenderer : MonoBehaviour
    {
        [SerializeField] private RawImage targetImage;
        private IFluidSolver solver;

        public void Initialize(IFluidSolver fluidSolver)
        {
            solver = fluidSolver;
        }

        private void Update()
        {
            if (solver != null && targetImage != null)
            {
                RenderTexture densityTex = solver.GetDensityTexture();
                if (densityTex != null)
                {
                    targetImage.texture = densityTex;
                }
            }
        }
    }
}
