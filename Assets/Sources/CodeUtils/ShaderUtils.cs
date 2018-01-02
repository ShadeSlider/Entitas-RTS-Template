using UnityEngine;

namespace CodeUtils
{
    public static class ShaderUtils
    {
        public static void SetRenderModeToTransparent(ref Material pMaterial)
        {
            if (pMaterial == null)
            {
                throw new UnassignedReferenceException("Material not initialized");
            }
            pMaterial.SetFloat("_Mode", 2);
            pMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            pMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            pMaterial.SetInt("_ZWrite", 0);
            pMaterial.DisableKeyword("_ALPHATEST_ON");
            pMaterial.EnableKeyword("_ALPHABLEND_ON");
            pMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            pMaterial.renderQueue = 3000;
        }
        
        public static void SetRenderModeToOpaque(ref Material pMaterial)
        {
            if (pMaterial == null)
            {
                throw new UnassignedReferenceException("Material not initialized");
            }
            pMaterial.SetFloat("_Mode", 0);
            pMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            pMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            pMaterial.SetInt("_ZWrite", 1);
            pMaterial.DisableKeyword("_ALPHATEST_ON");
            pMaterial.DisableKeyword("_ALPHABLEND_ON");
            pMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            pMaterial.renderQueue = -1;
        }
    }
}