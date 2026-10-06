// Ashen Hollow: icon atlases are UI pictures: no mipmaps (they blur in the bag), clamp, keep alpha, full size.
using UnityEditor;

public class AHIconImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        var ap = assetPath.Replace('\\', '/'); if (!ap.Contains("/Resources/AH/Icons/") && !ap.Contains("/Resources/AH/UI/talent_")) return;
        var ti = (TextureImporter)assetImporter;
        ti.mipmapEnabled = false; ti.alphaIsTransparency = true; ti.maxTextureSize = 2048;
        ti.wrapMode = UnityEngine.TextureWrapMode.Clamp; ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.npotScale = TextureImporterNPOTScale.None;
    }
}
