namespace NTDLS.StreamFraming.Payloads
{
    /// <summary>
    /// All query reply frames must in inherit from this interface and be json serializable.
    /// </summary>
    public interface IFramePayloadQueryReply : IFramePayload
    {
    }
}
