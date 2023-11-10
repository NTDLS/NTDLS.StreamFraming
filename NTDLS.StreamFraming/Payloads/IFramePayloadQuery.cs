namespace NTDLS.StreamFraming.Payloads
{
    /// <summary>
    /// All query frames must in herit from this interface and be json serializable.
    /// </summary>
    public interface IFramePayloadQuery : IFramePayload
    {
    }
}
