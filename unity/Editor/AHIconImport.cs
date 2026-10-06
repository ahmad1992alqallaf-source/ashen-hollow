// Ashen Hollow: icon atlases are UI pictures: no mipmaps (they blur in the bag), clamp, keep alpha, full size.
using UnityEditor;

public class AHIconImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').Contains("/Resources/AH/Icons/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.mipmapEnabled = false; ti.alphaIsTransparency = true; ti.maxTextureSize = 2048;
        ti.wrapMode = UnityEngine.TextureWrapMode.Clamp; ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.npotScale = TextureImporterNPOTScale.None;
    }
}
