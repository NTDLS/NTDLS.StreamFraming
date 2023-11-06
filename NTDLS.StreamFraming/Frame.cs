using ProtoBuf;
using System;

namespace NTDLS.StreamFraming
{
    /// <summary>
    /// Internal frame which allows for lowelevel communication betweeen server and client.
    /// </summary>
    [Serializable]
    [ProtoContract]
    internal class Frame
    {
        [ProtoMember(1)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ProtoMember(2)]
        public string EnclosedPayloadType { get; set; } = string.Empty;

        /// <summary>
        /// Json serialized user defined payload.
        /// </summary>
        [ProtoMember(3)]
        public string Payload { get; set; } = string.Empty;
    }
}
