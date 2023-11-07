using NTDLS.StreamFraming.Payloads;
using System;
using System.Threading;

namespace NTDLS.StreamFraming
{
    internal class QueryAwaitingReply
    {
        public Guid FrameId { get; set; }
        public AutoResetEvent WaitEvent { get; set; } = new(false);
        public IFrameQueryReply? ReplyPayload { get; set; }
    }
}
