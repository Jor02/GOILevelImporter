using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GOILevelImporter.Core
{
    /// <summary>
    /// Reads the Levels folder and turns each file into a Response describing
    /// what it found. Doesn't touch scenes or asset bundles; that part is
    /// LevelLoader's job once a level has actually been picked.
    /// </summary>
    static class LevelFileScanner
    {
        private static readonly byte[] GlfHeader = { 0x47, 0x4F, 0x49, 0x4C, 0x46 };
        private static readonly byte[] BundleSignature = { 0x55, 0x6E, 0x69, 0x74, 0x79, 0x46, 0x53, 0x00 };

        public struct Response
        {
            public ResponseType Message;
            public string LevelName;
            public string Author;
            public string Description;
            public long HeaderSize;
            public string LevelPath;
            public bool Legacy;
            public bool Incompatible;
            public string BuiltWithVersion;
            public Texture2D Thumbnail;
            public LevelMetadata Metadata;

            public Response(ResponseType message)
            {
                Message = message;
                LevelName = "";
                LevelPath = "";
                Author = "";
                Description = "";
                Legacy = false;
                Incompatible = false;
                BuiltWithVersion = null;
                Thumbnail = null;
                HeaderSize = 0;
                Metadata = default;
            }

            public Response(ResponseType message, bool legacy, string levelPath, LevelMetadata metadata, Texture2D thumbnail, long headerSize, string builtWithVersion = null)
            {
                Message = message;

                LevelName = metadata.LevelName;
                Author = metadata.Author;
                Description = metadata.Description;
                Metadata = metadata;

                LevelPath = levelPath;
                Legacy = legacy;
                Thumbnail = thumbnail;
                HeaderSize = headerSize;
                BuiltWithVersion = builtWithVersion;
                Incompatible = !IsCompatibleVersion(builtWithVersion);
            }

            public enum ResponseType
            {
                success,
                metadataNotFound,
                directoryNotFound,
                wrongFileType
            }
        }

        /// <summary>
        /// Scans every level file under levelsPath.
        /// </summary>
        public static Response[] Scan(string levelsPath)
        {
            var responses = new List<Response>();

            if (!Directory.Exists(levelsPath))
            {
                Directory.CreateDirectory(levelsPath);
                responses.Add(new Response(Response.ResponseType.directoryNotFound));
                return responses.ToArray();
            }

            var levelFiles = Directory.GetFiles(levelsPath)
                .Where(name => !name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));

            foreach (string path in levelFiles)
            {
                if (path.EndsWith(".scene"))
                {
                    responses.Add(ReadLegacyLevel(path));
                }
                else if (path.EndsWith(".glf"))
                {
                    responses.Add(ReadGlfLevel(path));
                }
            }

            return responses.ToArray();
        }

        /// <summary>
        /// Splits scan results into levels that loaded fine and levels that didn't.
        /// </summary>
        public static bool TrySplitResults(Response[] responses, out Response[] successfulResponses)
        {
            Menu.LoadingError error = Menu.LoadingError.Instance;
            var successful = new List<Response>();

            foreach (Response response in responses)
            {
                switch (response.Message)
                {
                    case Response.ResponseType.success:
                        successful.Add(response);
                        break;
                    case Response.ResponseType.directoryNotFound:
                        error.AddError("Level directory created, Please restart the game to load maps", true);
                        break;
                    case Response.ResponseType.metadataNotFound:
                    default:
                        error.AddError("An error occured: " + response.Message, true);
                        break;
                }
            }

            successfulResponses = successful.ToArray();
            return true;
        }

        /// <summary>
        /// Legacy levels are a bare .scene file with an optional .txt/.mdata sidecar for level properties.
        /// </summary>
        private static Response ReadLegacyLevel(string path)
        {
            string levelName = Path.GetFileNameWithoutExtension(path);
            string author = string.Empty;
            string description = levelName + " (Legacy Mode)";
            var props = new Dictionary<string, string>();

            string sidecarPath = File.Exists(Path.ChangeExtension(path, "txt"))
                ? Path.ChangeExtension(path, "txt")
                : Path.ChangeExtension(path, "mdata");

            if (File.Exists(sidecarPath))
            {
                foreach (string line in File.ReadAllLines(sidecarPath))
                {
                    // A line without an = is ignored.
                    string[] pair = line.Split('=');
                    if (pair.Length < 2) continue;

                    props[pair[0].Trim()] = pair[1].Trim();
                }
            }

            if (props.TryGetValue("credit", out var credit)) author = credit;
            if (props.TryGetValue("description", out var legacyDescription)) description = legacyDescription;

            // A legacy level basically an asset bundle so the unity version is at offset 0.
            string builtWith = ReadBundleUnityVersion(path, 0);

            var metadata = new LevelMetadata(levelName, author, description, true, false, null, 0, props);
            return new Response(Response.ResponseType.success, metadata.LegacyMap, path, metadata, metadata.GetThumbnail(), 0, builtWith);
        }

        /// <summary>
        /// Reads a GLF file from path.
        /// A .glf file is a 5 byte magic header followed by LZMA-compressed metadata blob, then the asset bundle itself.
        /// </summary>
        public static Response ReadSingleLevel(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return new Response(Response.ResponseType.metadataNotFound);
            }

            if (path.EndsWith(".scene", StringComparison.OrdinalIgnoreCase))
            {
                return ReadLegacyLevel(path);
            }

            if (path.EndsWith(".glf", StringComparison.OrdinalIgnoreCase))
            {
                return ReadGlfLevel(path);
            }

            return new Response(Response.ResponseType.wrongFileType);
        }

        private static Response ReadGlfLevel(string path)
        {
            long headerSize;
            LevelMetadata metadata;

            using (Stream stream = new FileStream(path, FileMode.Open))
            using (var reader = new BinaryReader(stream))
            {
                if (!reader.ReadBytes(5).SequenceEqual(GlfHeader))
                {
                    return new Response(Response.ResponseType.wrongFileType);
                }

                int metaDataLength = reader.ReadInt32();
                byte[] compressedMetaData = reader.ReadBytes(metaDataLength);
                headerSize = stream.Position;

                byte[] decompressedMetaData = SevenZip.Compression.LZMA.SevenZipHelper.Decompress(compressedMetaData);
                metadata = DecodeGlfMetadata(decompressedMetaData);
            }

            string builtWith = ReadBundleUnityVersion(path, headerSize);

            return new Response(Response.ResponseType.success, metadata.LegacyMap, path, metadata, metadata.GetThumbnail(), headerSize, builtWith);
        }

        /// <summary>
        /// Reads the Unity version that built the asset bundle sitting at a given offset.
        /// </summary>
        public static string ReadBundleUnityVersion(string path, long offset)
        {
            try
            {
                using Stream stream = new FileStream(path, FileMode.Open);
                stream.Seek(offset, SeekOrigin.Begin);

                byte[] signature = new byte[BundleSignature.Length];

                if (stream.Read(signature, 0, signature.Length) != signature.Length || !signature.SequenceEqual(BundleSignature))
                {
                    return null;
                }

                // The format version comes first, then the minimum player version
                // and the version of the editor that built the bundle.
                stream.Seek(4, SeekOrigin.Current);
                ReadNullTerminatedString(stream);
                return ReadNullTerminatedString(stream);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool IsCompatibleVersion(string builtWith)
        {
            if (!TryParseVersionLine(builtWith, out int major, out int minor)) return true;
            if (!TryParseVersionLine(Application.unityVersion, out int playerMajor, out int playerMinor)) return true;

            return major == playerMajor && minor == playerMinor;
        }

        private static bool TryParseVersionLine(string version, out int major, out int minor)
        {
            major = 0;
            minor = 0;

            if (string.IsNullOrWhiteSpace(version)) return false;

            string[] parts = version.Split('.');
            if (parts.Length < 2) return false;

            if (!int.TryParse(parts[0], out major)) return false;

            string patch = parts[1];
            int suffix = patch.IndexOfAny(new[] { 'f', 'p', 'b', 'a', 'x' });
            if (suffix >= 0) patch = patch.Substring(0, suffix);

            return int.TryParse(patch, out minor);
        }

        private static string ReadNullTerminatedString(Stream stream)
        {
            var builder = new System.Text.StringBuilder();

            while (true)
            {
                int value = stream.ReadByte();

                // A file that ends early hands back -1, which ends the string too.
                if (value <= 0)
                {
                    return builder.ToString();
                }

                builder.Append((char)value);
            }
        }

        private static LevelMetadata DecodeGlfMetadata(byte[] decompressedMetaData)
        {
            using var memStream = new MemoryStream(decompressedMetaData);
            using var memReader = new BinaryReader(memStream);

            var props = new Dictionary<string, string>();
            int propertyCount = memReader.ReadInt32();
            for (int i = 0; i < propertyCount; i++)
            {
                string key = memReader.ReadString();
                string value = memReader.ReadString();
                props[key] = value;
            }

            string levelName = props.TryGetValue("LevelName", out var n) ? n : "Untitled";
            string author = props.TryGetValue("Author", out var a) ? a : "Unknown";
            string description = props.TryGetValue("Description", out var d) ? d : "";
            bool hasThumbnail = props.TryGetValue("HasThumbnail", out var ht) && bool.TryParse(ht, out var hasThumb) && hasThumb;
            byte thumbnailFormat = props.TryGetValue("ThumbnailFormat", out var tf) && byte.TryParse(tf, out var format) ? format : (byte)0;

            // The thumbnail is the last thing in the stream, so whatever is left
            // after the properties is the image.
            byte[] thumbnail = Array.Empty<byte>();
            if (hasThumbnail && memStream.Position < memStream.Length)
            {
                thumbnail = memReader.ReadBytes((int)(memStream.Length - memStream.Position));
            }

            return new LevelMetadata(levelName, author, description, false, hasThumbnail, thumbnail, thumbnailFormat, props);
        }
    }
}
