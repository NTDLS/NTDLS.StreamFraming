using Newtonsoft.Json;
using NTDLS.StreamFraming.Payloads;
using ProtoBuf;
using System;
using System.Text;

namespace NTDLS.StreamFraming
{
    /// <summary>
    /// Comprises the body of the frame. Contains the payload and all information needed to deserialize it.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class FrameBody
    {
        /// <summary>
        /// The unique ID of the frame body. This is also used to pair query replies with waiting queries.
        /// </summary>
        [ProtoMember(1)]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The full assembly qualified name of the type of the payload.
        /// </summary>
        [ProtoMember(2)]
        public string ObjectType { get; set; } = string.Empty;

        /// <summary>
        /// Sometimes we just need to send a byte array without all the overhead of json, that's when we use BytesPayload.
        /// </summary>
        [ProtoMember(3)]
        public byte[] Bytes { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Instantiates a frame payload with a serialized payload.
        /// </summary>
        /// <param name="framePayload"></param>
        public FrameBody(IFramePayload framePayload)
        {
            var assemblyQualifiedName = framePayload.GetType()?.AssemblyQualifiedName ?? string.Empty;
            var parts = assemblyQualifiedName.Split(','); //We only want the first two parts, not the version and such.
            ObjectType = parts.Length > 1 ? $"{parts[0]},{parts[1].Trim()}" : assemblyQualifiedName;

            Bytes = Encoding.UTF8.GetBytes(Utility.JsonSerialize(framePayload));
        }

        /// <summary>
        /// Instantiates a frame payload using a raw byte array.
        /// </summary>
        /// <param name="bytesPayload"></param>
        public FrameBody(byte[] bytesPayload)
        {
            ObjectType = "byte[]";
            Bytes = bytesPayload;
        }

        /// <summary>
        /// Instantiates a frame payload.
        /// </summary>
        public FrameBody()
        {
        }
    }
}
