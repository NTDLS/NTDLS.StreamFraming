using NTDLS.StreamFraming.Payloads;

namespace NTDLS.StreamFraming
{
    public class Types
    {
        public delegate void ProcessFrameNotification(IFramePayloadNotification payload);

        public delegate IFramePayloadQueryReply ProcessFrameQuery(IFramePayloadQuery payload);

        internal static class NtFrameDefaults
        {
            public const int FRAME_DELIMITER = 948724593;
            public const int FRAME_HEADER_SIZE = 10;
        }
    }
}
