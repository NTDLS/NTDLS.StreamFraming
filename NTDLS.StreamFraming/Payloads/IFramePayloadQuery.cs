namespace NTDLS.StreamFraming.Payloads
{
    /// <summary>
    /// All query frames must in inherit from this interface and be json serializable.
    /// </summary>
    public interface IFramePayloadQuery : IFramePayload
    {
    }
}
