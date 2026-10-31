using UnityEditor;
using UnityEngine;

/// <summary>
/// Aplica as configurações básicas de Pixel Art às imagens nas pastas do projeto.
/// </summary>
public class PixelArtTextureImporter : AssetPostprocessor
{
    private const string SpritesFolder = "Assets/Art/Sprites/";
    private const string TilesFolder = "Assets/Art/Tiles/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(SpritesFolder) && !assetPath.StartsWith(TilesFolder))
        {
            return;
        }

        if (assetImporter is not TextureImporter textureImporter)
        {
            return;
        }

        textureImporter.textureType = TextureImporterType.Sprite;
        textureImporter.spritePixelsPerUnit = 32;
        textureImporter.filterMode = FilterMode.Point;
        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
        textureImporter.mipmapEnabled = false;
        textureImporter.npotScale = TextureImporterNPOTScale.None;
        textureImporter.wrapMode = TextureWrapMode.Clamp;
    }
}
