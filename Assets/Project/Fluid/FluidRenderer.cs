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
            if (this == null || solver == null || targetImage == null) return;
            
            RenderTexture densityTex = solver.GetDensityTexture();
            if (densityTex != null)
            {
                targetImage.texture = densityTex;
            }
        }

        private void OnDestroy()
        {
            if (targetImage != null)
            {
                targetImage.texture = null;
            }
        }
    }
}
