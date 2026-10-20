using UnityEditor;
using UnityEngine;

namespace AFirma.EditorTools
{
    /// <summary>
    /// Na primeira importação de qualquer imagem do projeto, aplica o padrão de pixel art (docs/ASSETS.md):
    /// Sprite, Filter Point, sem compressão, PPU 32. Depois disso cada um pode mudar no Inspector (ex.: fatiar sprite sheet).
    /// </summary>
    public class PixelArtImporter : AssetPostprocessor
    {
        const int PixelsPerUnit = 32;

        void OnPreprocessTexture()
        {
            if (!assetImporter.importSettingsMissing) return;
            if (!assetPath.StartsWith("Assets/_Project/") && !assetPath.StartsWith("Assets/ThirdParty/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
        }
    }
}
