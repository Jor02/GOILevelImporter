using System.Collections.Generic;
using System.IO;
using SevenZip.Compression.LZMA;
using UnityEngine;

/// <summary>
/// Writes the .glf container the mod's level loader reads: a GOILF header, the
/// length-prefixed LZMA metadata block, then the raw asset bundle bytes.
/// </summary>
public static class GltWriter
{
    // Same five bytes LevelLoader checks before it treats a file as a .glf.
    private static readonly byte[] Header = { 0x47, 0x4F, 0x49, 0x4C, 0x46 };
    private const int targetThumbnailWidth = 700;
    private const int targetTumbnailHeight = 400;


    /// <summary>
    /// Builds a .glf and returns the path written.
    /// </summary>
    /// <param name="level">Level supplying the metadata fields and thumbnail</param>
    /// <param name="bundlePath">Path to the asset bundle holding the scenes</param>
    /// <param name="outputPath">Destination .glf path</param>
    public static void Write(CustomLevelObject level, string bundlePath, string outputPath)
    {
        byte[] metadata = BuildMetadata(level);

        using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(output))
        {
            writer.Write(Header);

            byte[] compressed = SevenZipHelper.Compress(metadata);
            writer.Write(compressed.Length);
            writer.Write(compressed);

            // The loader hands this offset to AssetBundle.LoadFromFile as the
            // header size, so the bundle has to start right here.
            long headerSize = output.Position;

            using (var bundle = new FileStream(bundlePath, FileMode.Open, FileAccess.Read))
            {
                bundle.CopyTo(output);
            }

            Debug.Log($"Header size for '{Path.GetFileName(outputPath)}': {headerSize} bytes");
        }
    }

    /// <summary>
    /// Serializes the property bag the loader reads back, followed by the
    /// thumbnail bytes. The thumbnail is last because the loader treats
    /// everything past the properties as image data.
    /// </summary>
    private static byte[] BuildMetadata(CustomLevelObject level)
    {
        byte[] thumbnail = EncodeThumbnail(level.Thumbnail);

        var props = new Dictionary<string, string>
        {
            { "LevelName", level.LevelName ?? string.Empty },
            { "Author", level.Author ?? string.Empty },
            { "Description", level.Description ?? string.Empty },
            { "HasThumbnail", (thumbnail.Length > 0).ToString() },
            { "ThumbnailFormat", ((byte)TextureFormat.RGBA32).ToString() }
        };

        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(props.Count);

            foreach (var prop in props)
            {
                writer.Write(prop.Key);
                writer.Write(prop.Value);
            }

            writer.Write(thumbnail);

            writer.Flush();
            return stream.ToArray();
        }
    }

    /// <summary>
    /// The loader rebuilds the thumbnail with ImageConversion.LoadImage, which
    /// only understands PNG or JPG, so the source is re-encoded to PNG here
    /// regardless of how it was imported.
    /// </summary>
    private static byte[] EncodeThumbnail(Texture2D source)
    {
        if (source == null)
        {
            return new byte[0];
        }

        RenderTexture renderTexture = RenderTexture.GetTemporary(targetThumbnailWidth, targetTumbnailHeight, 0, RenderTextureFormat.ARGB32);

        Graphics.Blit(source, renderTexture);

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = renderTexture;

        Texture2D copy = new Texture2D(targetThumbnailWidth, targetTumbnailHeight, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0, 0, targetThumbnailWidth, targetTumbnailHeight), 0, 0);
        copy.Apply();

        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(renderTexture);

        byte[] png = ImageConversion.EncodeToPNG(copy);
        Object.DestroyImmediate(copy);

        return png;
    }
}