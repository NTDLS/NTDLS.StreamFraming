using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.IO;
using System.IO.Compression;

namespace NTDLS.StreamFraming
{
    internal static class Utility
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public static byte[] SerializeToByteArray(object obj)
        {
            if (obj == null) return Array.Empty<byte>();
            using var stream = new MemoryStream();
            Serializer.Serialize(stream, obj);
            return stream.ToArray();
        }

        public static string JsonSerialize<T>(T obj)
            => JsonConvert.SerializeObject(obj, _jsonSettings);

        public static T? JsonDeserializeToObject<T>(string json)
            => JsonConvert.DeserializeObject<T>(json, _jsonSettings);

        public static T DeserializeToObject<T>(byte[] arrBytes)
        {
            using var stream = new MemoryStream();
            stream.Write(arrBytes, 0, arrBytes.Length);
            stream.Seek(0, SeekOrigin.Begin);
            return Serializer.Deserialize<T>(stream);
        }

        public static byte[] Compress(byte[]? bytes)
        {
            if (bytes == null) return Array.Empty<byte>();

            using var msi = new MemoryStream(bytes);
            using var mso = new MemoryStream();
            using (var gs = new DeflateStream(mso, CompressionLevel.SmallestSize))
            {
                msi.CopyTo(gs);
            }
            return mso.ToArray();
        }

        public static byte[] Decompress(byte[] bytes)
        {
            using var msi = new MemoryStream(bytes);
            using var mso = new MemoryStream();
            using (var gs = new DeflateStream(msi, CompressionMode.Decompress))
            {
                gs.CopyTo(mso);
            }
            return mso.ToArray();
        }
    }
}
