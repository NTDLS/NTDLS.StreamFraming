using Newtonsoft.Json;
using NTDLS.StreamFraming.Payloads;
using ProtoBuf;
using System;
using System.Text;

namespace NTDLS.StreamFraming
{
    /// <summary>
    /// Internal frame which allows for lowelevel communication betweeen server and client.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class Frame
    {
        /// <summary>
        /// The unique ID of the frame. This is also used to pair query replies with waiting queries.
        /// </summary>
        [ProtoMember(1)]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The full assembly qualified name of the type that is encosed in the json serialized payload.
        /// </summary>
        [ProtoMember(2)]
        public string EnclosedPayloadType { get; set; } = string.Empty;

        /// <summary>
        /// Sometimes we just need to send a byte array without all the overhead of json, thats when we use BytesPayload.
        /// </summary>
        [ProtoMember(3)]
        public byte[] BytesPayload { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Instanciates a frame payload with a serialized payload.
        /// </summary>
        /// <param name="framePayload"></param>
        public Frame(IFramePayload framePayload)
        {
            EnclosedPayloadType = framePayload.GetType()?.AssemblyQualifiedName ?? string.Empty;
            BytesPayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(framePayload));
        }

        /// <summary>
        /// Instanciates a frame payload using a raw byte array.
        /// </summary>
        /// <param name="bytesPayload"></param>
        public Frame(byte[] bytesPayload)
        {
            EnclosedPayloadType = "byte[]";
            BytesPayload = bytesPayload;
        }

        /// <summary>
        /// Instanciates a frame payload.
        /// </summary>
        public Frame()
        {
        }
    }
}
