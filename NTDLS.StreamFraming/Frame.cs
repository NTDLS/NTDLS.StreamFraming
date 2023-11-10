using ProtoBuf;
using System;

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
        /// Json serialized user defined payload of the type denoted by EnclosedPayloadType.
        /// </summary>
        [ProtoMember(3)]
        public string Payload { get; set; } = string.Empty;
    }
}
